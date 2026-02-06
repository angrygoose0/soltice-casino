using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class CinematicModeToggle : MonoBehaviour
{
    private bool cinematicMode = false;
    private List<Component> cachedComponents = new List<Component>();

    void Start()
    {
        CacheComponents();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            ToggleCinematicMode();
        }
    }

    void CacheComponents()
    {
        GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag("CinematicHide");
        
        foreach (GameObject obj in taggedObjects)
        {
            // Renderer handles: MeshRenderer, SkinnedMeshRenderer, SpriteRenderer, etc.
            cachedComponents.AddRange(obj.GetComponentsInChildren<Renderer>(true));
            
            // Graphic handles: Image, Text, RawImage, etc. (all UI visuals)
            cachedComponents.AddRange(obj.GetComponentsInChildren<Graphic>(true));
            
            // Canvas for UI containers
            cachedComponents.AddRange(obj.GetComponentsInChildren<Canvas>(true));
        }

        GameLogger.Log($"Cached {cachedComponents.Count} components for cinematic mode toggle");
    }

    void ToggleCinematicMode()
    {
        cinematicMode = !cinematicMode;

        foreach (Component component in cachedComponents)
        {
            if (component != null)
            {
                if (component is Behaviour behaviour)
                {
                    behaviour.enabled = !cinematicMode;
                }
                else if (component is Renderer renderer)
                {
                    renderer.enabled = !cinematicMode;
                }
            }
        }

        GameLogger.Log($"Cinematic mode: {(cinematicMode ? "ON" : "OFF")}");
    }

    public void RefreshCache()
    {
        cachedComponents.Clear();
        CacheComponents();
    }
}

