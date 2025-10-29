using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using Solana.Unity.SDK;

public class InteractableObjects : MonoBehaviour
{
    public enum ActionType
    {
        Add,
        Set,
        PlaceBet,
        StartGame,
        ClaimBet,
        ConnectWallet,
        DisconnectWallet,
        JoinWorld
    }

    [System.Serializable]
    public struct GlowSettings
    {
        public float intensity;
        public float scale;
        public Color color;
    }

    [System.Serializable]
    public class GlowProfile
    {
        public GlowSettings idle;
        public GlowSettings hover;
        public GlowSettings pressed;
        public GlowSettings disabled;
    }

    [System.Serializable]
    public class NamedGlowProfile
    {
        public string name;
        public GlowProfile profile;
    }

    [System.Serializable]
    public class InteractableEntry
    {
        public GameObject gameObject;
        public string glowProfileName;
        public ActionType actionType;
        
        [Header("Action Parameters (if needed)")]
        public ulong amount; // Used for Add/Set actions
        
        [Header("UI Disabled Settings")]
        public float disabledAlpha = 0.5f;
        
        // Cached components (non-serialized)
        [System.NonSerialized] public TextMeshProUGUI label;
        [System.NonSerialized] public GlowProfile cachedGlowProfile;
        [System.NonSerialized] public CanvasGroup cachedCanvasGroup;
        [System.NonSerialized] public bool isEnabled = true;
    }

    [SerializeField] private UserUI userUI;
    [SerializeField] private SimpleWorldJoin simpleWorldJoin;
    [SerializeField] private Camera mainCamera; // Assign in inspector or auto-find
    [SerializeField] private FeedbackManager feedbackManager;

    private GameObject lastHoveredObject; // null when nothing is hovered
    private bool lastHoveredPressed; // whether left click was held on last hovered

    [Header("Glow Profiles")]
    [SerializeField] private List<NamedGlowProfile> glowProfiles = new List<NamedGlowProfile>();

    [Header("Centralized Interactables")]
    [SerializeField] private List<InteractableEntry> interactables = new List<InteractableEntry>();

    private void Start()
    {
        // Cache camera reference if not assigned in inspector
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            // Fallback to find any camera with Cinemachine Brain
            if (mainCamera == null)
            {
                mainCamera = FindObjectOfType<Camera>();
            }
        }

        // Initialize centralized interactables
        for (int i = 0; i < interactables.Count; i++)
        {
            InteractableEntry entry = interactables[i];
            if (entry == null || entry.gameObject == null) continue;

            // Cache glow profile
            entry.cachedGlowProfile = GetGlowProfileByName(entry.glowProfileName);
            if (entry.cachedGlowProfile == null) continue;

            // Cache components
            entry.label = entry.gameObject.GetComponentInChildren<TextMeshProUGUI>();
            entry.cachedCanvasGroup = entry.gameObject.GetComponent<CanvasGroup>();
            
            // Add CanvasGroup if it's a UI element without one
            if (entry.cachedCanvasGroup == null && entry.gameObject.GetComponent<RectTransform>() != null)
            {
                entry.cachedCanvasGroup = entry.gameObject.AddComponent<CanvasGroup>();
            }

            // Set initial enabled state
            entry.isEnabled = true;
            
            // Animate to idle state
            if (feedbackManager != null)
            {
                FeedbackManager.MaterialGlowState idleState = new FeedbackManager.MaterialGlowState
                {
                    scale = entry.cachedGlowProfile.idle.scale,
                    intensity = entry.cachedGlowProfile.idle.intensity,
                    color = entry.cachedGlowProfile.idle.color
                };
                feedbackManager.AnimateGlowMaterial(entry.gameObject, idleState);
            }
        }
    }

    private GlowProfile GetGlowProfileByName(string profileName)
    {
        if (string.IsNullOrEmpty(profileName)) return null;
        
        for (int i = 0; i < glowProfiles.Count; i++)
        {
            NamedGlowProfile namedProfile = glowProfiles[i];
            if (namedProfile != null && namedProfile.name == profileName)
            {
                return namedProfile.profile;
            }
        }
        return null;
    }

    private void Update()
    {
        // Update labels for interactables
        UpdateLabels();

        Vector2 mousePosition = Mouse.current != null ? Mouse.current.position.ReadValue() : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        GameObject hitObjectOverall = null;

        // 1) UI raycast first (UI takes priority)
        if (EventSystem.current != null)
        {
            PointerEventData eventData = new PointerEventData(EventSystem.current);
            eventData.position = mousePosition;
            List<RaycastResult> uiHits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, uiHits);
            foreach (var uiHit in uiHits)
            {
                Transform t = uiHit.gameObject.transform;
                while (t != null)
                {
                    if (TryGetProfile(t.gameObject, out _))
                    {
                        hitObjectOverall = t.gameObject;
                        break;
                    }
                    t = t.parent;
                }
                if (hitObjectOverall != null) break;
            }
        }

        // 2) If no UI hit, do 3D physics raycast
        if (hitObjectOverall == null && mainCamera != null)
        {
            Ray ray = mainCamera.ScreenPointToRay(mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                GameObject hitObject = hit.collider.gameObject;
                Transform t = hitObject.transform;
                while (t != null)
                {
                    if (TryGetProfile(t.gameObject, out _))
                    {
                        hitObjectOverall = t.gameObject;
                        break;
                    }
                    t = t.parent;
                }
            }
        }

        // 3) Apply glow based on hover/press state
        if (hitObjectOverall != null)
        {
            bool isPressed = Mouse.current != null && Mouse.current.leftButton.isPressed;
            if (!TryGetProfile(hitObjectOverall, out GlowProfile profile))
            {
                // Not a profiled interactable; treat as no hover
                hitObjectOverall = null;
            }

            // On click down edge, call interact handler if present
            bool clickStartedThisFrame = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            bool clickReleasedThisFrame = Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;
            
            if (clickStartedThisFrame && hitObjectOverall != null)
            {
                feedbackManager?.PlayUIClickDown(hitObjectOverall.transform);
            }
            
            if (clickReleasedThisFrame && hitObjectOverall != null)
            {
                feedbackManager?.PlayUIClickUp(hitObjectOverall.transform);
                
                if (TryGetInteractableEntry(hitObjectOverall, out InteractableEntry entry))
                {
                    ExecuteActionAsync(entry);
                }
            }
            if (lastHoveredObject != hitObjectOverall)
            {
                if (hitObjectOverall != null)
                {
                    feedbackManager?.PlayUIHoverEnter(hitObjectOverall.transform);
                    
                    if (feedbackManager != null)
                    {
                        GlowSettings targetSettings = isPressed ? profile.pressed : profile.hover;
                        FeedbackManager.MaterialGlowState targetState = new FeedbackManager.MaterialGlowState
                        {
                            scale = targetSettings.scale,
                            intensity = targetSettings.intensity,
                            color = targetSettings.color
                        };
                        feedbackManager.AnimateGlowMaterial(hitObjectOverall, targetState);
                    }
                }

                if (lastHoveredObject != null)
                {
                    feedbackManager?.PlayUIHoverExit(lastHoveredObject.transform);
                    
                    if (TryGetProfile(lastHoveredObject, out GlowProfile lastProfile) && feedbackManager != null)
                    {
                        FeedbackManager.MaterialGlowState idleState = new FeedbackManager.MaterialGlowState
                        {
                            scale = lastProfile.idle.scale,
                            intensity = lastProfile.idle.intensity,
                            color = lastProfile.idle.color
                        };
                        feedbackManager.AnimateGlowMaterial(lastHoveredObject, idleState);
                    }
                }

                lastHoveredObject = hitObjectOverall;
                lastHoveredPressed = isPressed;
            }
            else if (lastHoveredPressed != isPressed)
            {
                if (hitObjectOverall != null && feedbackManager != null)
                {
                    GlowSettings targetSettings = isPressed ? profile.pressed : profile.hover;
                    FeedbackManager.MaterialGlowState targetState = new FeedbackManager.MaterialGlowState
                    {
                        scale = targetSettings.scale,
                        intensity = targetSettings.intensity,
                        color = targetSettings.color
                    };
                    feedbackManager.AnimateGlowMaterial(hitObjectOverall, targetState);
                }
                lastHoveredPressed = isPressed;
            }
        }
        else
        {
            if (lastHoveredObject != null)
            {
                feedbackManager?.PlayUIHoverExit(lastHoveredObject.transform);
                
                if (TryGetProfile(lastHoveredObject, out GlowProfile lastProfile) && feedbackManager != null)
                {
                    FeedbackManager.MaterialGlowState idleState = new FeedbackManager.MaterialGlowState
                    {
                        scale = lastProfile.idle.scale,
                        intensity = lastProfile.idle.intensity,
                        color = lastProfile.idle.color
                    };
                    feedbackManager.AnimateGlowMaterial(lastHoveredObject, idleState);
                }
                lastHoveredObject = null;
                lastHoveredPressed = false;
            }
        }
    }

    private bool TryGetProfile(GameObject obj, out GlowProfile profile)
    {
        profile = null;
        if (obj == null) return false;

        for (int i = 0; i < interactables.Count; i++)
        {
            InteractableEntry entry = interactables[i];
            if (entry != null && entry.gameObject == obj && entry.cachedGlowProfile != null && entry.isEnabled)
            {
                profile = entry.cachedGlowProfile;
                return true;
            }
        }
        return false;
    }

    private bool TryGetInteractableEntry(GameObject obj, out InteractableEntry entry)
    {
        entry = null;
        if (obj == null) return false;

        for (int i = 0; i < interactables.Count; i++)
        {
            InteractableEntry e = interactables[i];
            if (e != null && e.gameObject == obj)
            {
                entry = e;
                return true;
            }
        }
        return false;
    }

    private void ExecuteActionAsync(InteractableEntry entry)
    {
        if (entry == null) return;

        switch (entry.actionType)
        {
            case ActionType.Add:
                if (userUI != null) userUI.betAmount += entry.amount;
                break;
            case ActionType.Set:
                if (userUI != null) userUI.betAmount = entry.amount;
                break;
            case ActionType.PlaceBet:
                if (userUI != null) userUI.PlaceBet();
                break;
            case ActionType.StartGame:
                if (userUI != null) userUI.StartGame();
                break;
            case ActionType.ClaimBet:
                if (userUI != null) userUI.ClaimBet();
                break;
            case ActionType.ConnectWallet:
                if (simpleWorldJoin != null) simpleWorldJoin.StartWalletConnection();
                Web3.Instance?.LoginWithWalletAdapter();
                break;
            case ActionType.DisconnectWallet:
                Web3.Instance?.Logout();
                break;
            case ActionType.JoinWorld:
                if (simpleWorldJoin != null) simpleWorldJoin.OnStartClicked();
                break;
        }
    }

    private void UpdateLabels()
    {
        for (int i = 0; i < interactables.Count; i++)
        {
            InteractableEntry entry = interactables[i];
            if (entry == null || entry.label == null) continue;

            if (entry.actionType == ActionType.PlaceBet && userUI != null)
            {
                ulong currentBet = userUI.betAmount;
                ulong lastAmount = TextAnimationManager.Instance.GetLastRenderedAmount(entry.label);

                if (currentBet != lastAmount)
                {
                    TextAnimationManager.Instance.AnimateFormattedNumber(
                        entry.label,
                        lastAmount,
                        currentBet,
                        prefix: "Bet: "
                    );
                }
                else
                {
                    entry.label.text = $"Bet: {FormatShortAmount(currentBet)}";
                }
            }
            else if (entry.actionType == ActionType.Add)
            {
                entry.label.text = FormatShortAmount(entry.amount);
            }
        }
    }

    private string FormatShortAmount(ulong value)
    {
        const double Thousand = 1_000d;
        const double Million = 1_000_000d;
        const double Billion = 1_000_000_000d;
        const double Trillion = 1_000_000_000_000d;

        if (value >= (ulong)Trillion) return (value / Trillion).ToString("0.#", CultureInfo.InvariantCulture) + "t";
        if (value >= (ulong)Billion) return (value / Billion).ToString("0.#", CultureInfo.InvariantCulture) + "b";
        if (value >= (ulong)Million) return (value / Million).ToString("0.#", CultureInfo.InvariantCulture) + "m";
        if (value >= (ulong)Thousand) return (value / Thousand).ToString("0.#", CultureInfo.InvariantCulture) + "k";
        return value.ToString(CultureInfo.InvariantCulture);
    }

    public void SetInteractableEnabled(GameObject obj, bool enabled)
    {
        for (int i = 0; i < interactables.Count; i++)
        {
            InteractableEntry entry = interactables[i];
            if (entry != null && entry.gameObject == obj)
            {
                entry.isEnabled = enabled;

                if (enabled)
                {
                    // Enable: restore to idle state
                    if (entry.cachedCanvasGroup != null)
                    {
                        entry.cachedCanvasGroup.interactable = true;
                        entry.cachedCanvasGroup.alpha = 1f;
                    }

                    if (entry.cachedGlowProfile != null && feedbackManager != null)
                    {
                        FeedbackManager.MaterialGlowState idleState = new FeedbackManager.MaterialGlowState
                        {
                            scale = entry.cachedGlowProfile.idle.scale,
                            intensity = entry.cachedGlowProfile.idle.intensity,
                            color = entry.cachedGlowProfile.idle.color
                        };
                        feedbackManager.AnimateGlowMaterial(entry.gameObject, idleState);
                    }
                }
                else
                {
                    // Disable: set disabled state
                    if (entry.cachedCanvasGroup != null)
                    {
                        entry.cachedCanvasGroup.interactable = false;
                        entry.cachedCanvasGroup.alpha = entry.disabledAlpha;
                    }

                    if (entry.cachedGlowProfile != null && feedbackManager != null)
                    {
                        FeedbackManager.MaterialGlowState disabledState = new FeedbackManager.MaterialGlowState
                        {
                            scale = entry.cachedGlowProfile.disabled.scale,
                            intensity = entry.cachedGlowProfile.disabled.intensity,
                            color = entry.cachedGlowProfile.disabled.color
                        };
                        feedbackManager.AnimateGlowMaterial(entry.gameObject, disabledState);
                    }
                }
                return;
            }
        }
    }

    public void SetInteractableEnabledByAction(ActionType actionType, bool enabled)
    {
        for (int i = 0; i < interactables.Count; i++)
        {
            InteractableEntry entry = interactables[i];
            if (entry != null && entry.actionType == actionType)
            {
                SetInteractableEnabled(entry.gameObject, enabled);
                return;
            }
        }
    }

    public void SetInteractableHardEnabled(GameObject obj, bool enabled)
    {
        for (int i = 0; i < interactables.Count; i++)
        {
            InteractableEntry entry = interactables[i];
            if (entry != null && entry.gameObject == obj)
            {
                entry.gameObject.SetActive(enabled);
            }
        }
    }

    public void SetInteractableHardEnabledByAction(ActionType actionType, bool enabled)
    {
        for (int i = 0; i < interactables.Count; i++)
        {
            InteractableEntry entry = interactables[i];
            if (entry != null && entry.actionType == actionType)
            {
                entry.gameObject.SetActive(enabled);
            }
        }
    }

}



