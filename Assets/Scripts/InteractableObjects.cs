using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InteractableObjects : MonoBehaviour
{
    [SerializeField] private List<GameObject> interactables = new List<GameObject>();
    [SerializeField] private MaterialManager materialManager;

    [Header("Idle Glow")]
    [SerializeField] private float idleGlowIntensity = 0.5f;
    [SerializeField] private float idleGlowScale = 1.0f;
    [SerializeField] private Color idleGlowColor = Color.white;

    [Header("Hover Glow")]
    [SerializeField] private float hoverGlowIntensity = 1.5f;
    [SerializeField] private float hoverGlowScale = 1.2f;
    [SerializeField] private Color hoverGlowColor = Color.cyan;

    private GameObject lastHoveredObject; // null when nothing is hovered

    private void Start()
    {
        foreach (GameObject interactable in interactables)
        {
            if (interactable == null) continue;

            Renderer renderer = interactable.GetComponent<Renderer>();
            if (renderer == null) continue;

            materialManager.SetGlowMaterial(
                interactable,
                scale: idleGlowScale,
                glowColor: idleGlowColor,
                glowIntensity: idleGlowIntensity
            );
        }
    }

    public void AddInteractables(List<GameObject> newInteractables)
    {
        if (newInteractables == null || newInteractables.Count == 0) return;

        foreach (GameObject interactable in newInteractables)
        {
            if (interactable == null) continue;

            if (!interactables.Contains(interactable))
            {
                interactables.Add(interactable);
            }

            Renderer renderer = interactable.GetComponent<Renderer>();
            if (renderer == null) continue;

            materialManager.SetGlowMaterial(
                interactable,
                scale: idleGlowScale,
                glowColor: idleGlowColor,
                glowIntensity: idleGlowIntensity
            );
        }
    }

    private void Update()
    {
        Camera cam = Camera.main;
        if (cam != null)
        {
            Vector2 mousePosition = Mouse.current != null ? Mouse.current.position.ReadValue() : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            Ray ray = cam.ScreenPointToRay(mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                GameObject hitObject = hit.collider.gameObject;
                Debug.Log(hitObject);

                if (interactables.Contains(hitObject))
                {
                    if (lastHoveredObject != hitObject)
                    {
                        materialManager.SetGlowMaterial(
                            hitObject,
                            scale: hoverGlowScale,
                            glowColor: hoverGlowColor,
                            glowIntensity: hoverGlowIntensity
                        );

                        if (lastHoveredObject != null)
                        {
                            materialManager.SetGlowMaterial(
                                lastHoveredObject,
                                scale: idleGlowScale,
                                glowColor: idleGlowColor,
                                glowIntensity: idleGlowIntensity
                            );
                        }

                        lastHoveredObject = hitObject;
                    }
                }
                else
                {
                    if (lastHoveredObject != null)
                    {
                        materialManager.SetGlowMaterial(
                            lastHoveredObject,
                            scale: idleGlowScale,
                            glowColor: idleGlowColor,
                            glowIntensity: idleGlowIntensity
                        );
                        lastHoveredObject = null;
                    }

                }
            } else {
                if (lastHoveredObject != null)
                {
                    materialManager.SetGlowMaterial(
                        lastHoveredObject,
                        scale: idleGlowScale,
                        glowColor: idleGlowColor,
                        glowIntensity: idleGlowIntensity
                    );
                    lastHoveredObject = null;
                }
            }
        }
    }
}


