using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class InteractableObjects : MonoBehaviour
{
    public enum ActionType
    {
        Add,
        Set,
        PlaceBet,
        StartGame,
        ClaimBet
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
        
        // Cached components (non-serialized)
        [System.NonSerialized] public TextMeshProUGUI label;
        [System.NonSerialized] public GlowProfile cachedGlowProfile;
    }

    [SerializeField] private MaterialManager materialManager;
    [SerializeField] private UserUI userUI;
    [SerializeField] private Camera mainCamera; // Assign in inspector or auto-find

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

            materialManager.SetGlowMaterial(
                entry.gameObject,
                scale: entry.cachedGlowProfile.idle.scale,
                glowColor: entry.cachedGlowProfile.idle.color,
                glowIntensity: entry.cachedGlowProfile.idle.intensity
            );

            // Cache label component from children
            entry.label = entry.gameObject.GetComponentInChildren<TextMeshProUGUI>();
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
            if (clickStartedThisFrame && hitObjectOverall != null)
            {
                if (TryGetInteractableEntry(hitObjectOverall, out InteractableEntry entry))
                {
                    ExecuteAction(entry);
                }
            }
            if (lastHoveredObject != hitObjectOverall)
            {
                if (hitObjectOverall != null)
                {
                    materialManager.SetGlowMaterial(
                        hitObjectOverall,
                        scale: isPressed ? profile.pressed.scale : profile.hover.scale,
                        glowColor: isPressed ? profile.pressed.color : profile.hover.color,
                        glowIntensity: isPressed ? profile.pressed.intensity : profile.hover.intensity
                    );
                }

                if (lastHoveredObject != null)
                {
                    if (TryGetProfile(lastHoveredObject, out GlowProfile lastProfile))
                    {
                        materialManager.SetGlowMaterial(
                            lastHoveredObject,
                            scale: lastProfile.idle.scale,
                            glowColor: lastProfile.idle.color,
                            glowIntensity: lastProfile.idle.intensity
                        );
                    }
                }

                lastHoveredObject = hitObjectOverall;
                lastHoveredPressed = isPressed;
            }
            else if (lastHoveredPressed != isPressed)
            {
                if (hitObjectOverall != null)
                {
                    materialManager.SetGlowMaterial(
                        hitObjectOverall,
                        scale: isPressed ? profile.pressed.scale : profile.hover.scale,
                        glowColor: isPressed ? profile.pressed.color : profile.hover.color,
                        glowIntensity: isPressed ? profile.pressed.intensity : profile.hover.intensity
                    );
                }
                lastHoveredPressed = isPressed;
            }
        }
        else
        {
            if (lastHoveredObject != null)
            {
                if (TryGetProfile(lastHoveredObject, out GlowProfile lastProfile))
                {
                    materialManager.SetGlowMaterial(
                        lastHoveredObject,
                        scale: lastProfile.idle.scale,
                        glowColor: lastProfile.idle.color,
                        glowIntensity: lastProfile.idle.intensity
                    );
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
            if (entry != null && entry.gameObject == obj && entry.cachedGlowProfile != null)
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

    private void ExecuteAction(InteractableEntry entry)
    {
        if (entry == null || userUI == null) return;

        switch (entry.actionType)
        {
            case ActionType.Add:
                userUI.betAmount += entry.amount;
                break;
            case ActionType.Set:
                userUI.betAmount = entry.amount;
                break;
            case ActionType.PlaceBet:
                userUI.PlaceBet();
                break;
            case ActionType.StartGame:
                userUI.StartGame();
                break;
            case ActionType.ClaimBet:
                userUI.ClaimBet();
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
}


