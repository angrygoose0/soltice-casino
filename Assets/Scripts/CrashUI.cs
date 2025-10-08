using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using Solana.Unity.Wallet;
using TMPro;
using System.Collections.Generic;
using Solana.Unity.SDK;
using System.Collections;
 

public class CrashUI : MonoBehaviour
{
    private GameObject canvas;
    public Transform currentPlayer;

    [SerializeField] private List<GameObject> chipButtons;
    [SerializeField] private GameObject betButton;
    [SerializeField] private InteractableObjects interactableObjects;

    [Header("Distance Scale Settings")]
    public float minDistance = 10f;
    public float maxDistance = 30f; // Distance at which UI is completely shrunk
    
    [Header("Scale Animation Settings")]
    [SerializeField] private float scaleDuration = 0.7f;
    [SerializeField] private float overshootStrength = 0.8f; // controls how far past target it goes
    
    
    private Vector3 originalCanvasScale;
    private Coroutine scaleCoroutine;
    private bool isInRange = false;

 
    
    private void Start()
    {
        canvas = transform.Find("canvas").gameObject;
        originalCanvasScale = canvas.transform.localScale;
        
        // Start with canvas scaled to zero (assuming player starts out of range)
        canvas.transform.localScale = Vector3.zero;
        
        // Register chip 3D children as interactables
        RegisterChipChildren();
    }
    
    private void RegisterChipChildren()
    {
        if (interactableObjects == null) return;

        List<GameObject> collected = new List<GameObject>();
        foreach (GameObject chipButton in chipButtons)
        {
            if (chipButton == null) continue;

            // collect all child gameobjects (excluding the chipButton itself) that have a Renderer
            foreach (Transform child in chipButton.transform.GetComponentsInChildren<Transform>(true))
            {
                if (child == chipButton.transform) continue;
                GameObject go = child.gameObject;
                if (go.GetComponent<Renderer>() != null)
                {
                    collected.Add(go);
                }
            }
        }

        if (collected.Count > 0)
        {
            interactableObjects.AddInteractables(collected);
        }
    }
    
    private void Update()
    {
        if (currentPlayer == null || canvas == null) return;
        
        // Calculate distance between canvas and currentPlayer
        float distance = Vector3.Distance(canvas.transform.position, currentPlayer.position);
        
        // Check if player is in range
        bool shouldBeInRange = distance <= maxDistance;
        
        // If range state changed, animate the transition
        if (shouldBeInRange != isInRange)
        {
            isInRange = shouldBeInRange;
            
            // Stop previous coroutine if running
            if (scaleCoroutine != null)
            {
                StopCoroutine(scaleCoroutine);
            }
            
            // Start new scale animation
            float targetScale = isInRange ? 1f : 0f;
            scaleCoroutine = StartCoroutine(SmoothScaleCoroutine(targetScale));
        }
    }
    
    // Smoothly scales the canvas with overshoot effect (same as BalloonSimulator)
    private IEnumerator SmoothScaleCoroutine(float scaleFactor)
    {
        if (canvas == null) yield break;
        
        Vector3 currentScale = canvas.transform.localScale;
        Vector3 targetScale = originalCanvasScale * scaleFactor;
        float elapsed = 0f;
        
        while (elapsed < scaleDuration && canvas != null)
        {
            float t = elapsed / scaleDuration;
            // Ease-out exponential time mapping for quick start, gentle finish
            float expoEaseOutT = 1f - Mathf.Pow(1f - t, 2.2f);
            // Apply a gentle overshoot (back) so it goes a bit past and corrects
            float s = overshootStrength;
            float p = expoEaseOutT - 1f;
            float overshootT = p * p * ((s + 1f) * p + s) + 1f; // easeOutBack
            canvas.transform.localScale = Vector3.LerpUnclamped(currentScale, targetScale, overshootT);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        if (canvas != null)
            canvas.transform.localScale = targetScale;
    }
}
