using UnityEngine;
using System.Collections.Generic;
using System.Collections;

[System.Serializable]
public class ProximityTarget
{
    public Transform target;
    public float maxDistance = 30f;
    
    [HideInInspector] public Vector3 originalScale;
    [HideInInspector] public bool isInRange;
    [HideInInspector] public Coroutine scaleCoroutine;
}

public class PlayerProximityCanvas : MonoBehaviour
{
    public Transform currentPlayer;
    
    [Header("Proximity Targets")]
    public List<ProximityTarget> targets = new List<ProximityTarget>();
    
    [Header("Scale Animation Settings")]
    [SerializeField] private float scaleDuration = 0.7f;
    [SerializeField] private float overshootStrength = 0.8f;
    
    private void Start()
    {
        foreach (var t in targets)
            InitializeTarget(t);
    }
    
    public void AddTarget(Transform target, float maxDistance = 30f)
    {
        var t = new ProximityTarget { target = target, maxDistance = maxDistance };
        InitializeTarget(t);
        targets.Add(t);
    }
    
    public void RemoveTarget(Transform target)
    {
        targets.RemoveAll(t => t.target == target);
    }
    
    private void InitializeTarget(ProximityTarget t)
    {
        if (t.target == null) return;
        t.originalScale = t.target.localScale;
        t.target.localScale = Vector3.zero;
        t.isInRange = false;
    }
    
    private void Update()
    {
        if (currentPlayer == null) return;
        
        foreach (var t in targets)
        {
            if (t.target == null) continue;
            
            float distance = Vector3.Distance(t.target.position, currentPlayer.position);
            bool shouldBeInRange = distance <= t.maxDistance;
            
            if (shouldBeInRange != t.isInRange)
            {
                t.isInRange = shouldBeInRange;
                
                if (t.scaleCoroutine != null)
                    StopCoroutine(t.scaleCoroutine);
                
                float targetScale = t.isInRange ? 1f : 0f;
                t.scaleCoroutine = StartCoroutine(SmoothScaleCoroutine(t, targetScale));
            }
        }
    }
    
    private IEnumerator SmoothScaleCoroutine(ProximityTarget t, float scaleFactor)
    {
        if (t.target == null) yield break;
        
        Vector3 currentScale = t.target.localScale;
        Vector3 targetScale = t.originalScale * scaleFactor;
        float elapsed = 0f;
        
        while (elapsed < scaleDuration && t.target != null)
        {
            float time = elapsed / scaleDuration;
            float expoEaseOutT = 1f - Mathf.Pow(1f - time, 2.2f);
            float s = overshootStrength;
            float p = expoEaseOutT - 1f;
            float overshootT = p * p * ((s + 1f) * p + s) + 1f;
            t.target.localScale = Vector3.LerpUnclamped(currentScale, targetScale, overshootT);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        if (t.target != null)
            t.target.localScale = targetScale;
    }
}
