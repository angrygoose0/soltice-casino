using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class InteractableObjects : MonoBehaviour
{
    [System.Serializable]
    private struct GlowSettings
    {
        public float intensity;
        public float scale;
        public Color color;
    }

    [System.Serializable]
    private class GlowProfile
    {
        public GlowSettings idle;
        public GlowSettings hover;
        public GlowSettings pressed;
    }

    [System.Serializable]
    private class TagGlowProfile
    {
        public string tag;
        public GlowProfile profile;
    }

    [SerializeField] private MaterialManager materialManager;


    private GameObject lastHoveredObject; // null when nothing is hovered
    private bool lastHoveredPressed; // whether left click was held on last hovered

    [Header("Per-Type Settings (by Tag)")]
    [SerializeField] private List<TagGlowProfile> tagProfiles = new List<TagGlowProfile>();

    private void Start()
    {
        // Initialize all objects with matching tags to idle state
        for (int i = 0; i < tagProfiles.Count; i++)
        {
            TagGlowProfile tp = tagProfiles[i];
            if (tp == null || tp.profile == null || string.IsNullOrEmpty(tp.tag)) continue;

            GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag(tp.tag);
            for (int j = 0; j < taggedObjects.Length; j++)
            {
                GameObject go = taggedObjects[j];
                if (go == null) continue;
                materialManager.SetGlowMaterial(
                    go,
                    scale: tp.profile.idle.scale,
                    glowColor: tp.profile.idle.color,
                    glowIntensity: tp.profile.idle.intensity
                );
            }
        }
    }

    public void AddInteractables(List<GameObject> newInteractables)
    {
        if (newInteractables == null || newInteractables.Count == 0) return;
        // No longer maintaining a list; just apply idle to any with a matching tag profile
        foreach (GameObject interactable in newInteractables)
        {
            if (interactable == null) continue;
            if (TryGetProfile(interactable, out GlowProfile profile))
            {
                materialManager.SetGlowMaterial(
                    interactable,
                    scale: profile.idle.scale,
                    glowColor: profile.idle.color,
                    glowIntensity: profile.idle.intensity
                );
            }
        }
    }

    private void Update()
    {
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
        if (hitObjectOverall == null)
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                Ray ray = cam.ScreenPointToRay(mousePosition);
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
        string tag = obj.tag;
        if (string.IsNullOrEmpty(tag)) return false;
        for (int i = 0; i < tagProfiles.Count; i++)
        {
            TagGlowProfile tp = tagProfiles[i];
            if (tp != null && tp.profile != null && tp.tag == tag)
            {
                profile = tp.profile;
                return true;
            }
        }
        return false;
    }

    
}


