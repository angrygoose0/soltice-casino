using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class UIFader : MonoBehaviour
{
    [SerializeField] private float fadeDuration = 0.3f;
    
    private static Dictionary<GameObject, Coroutine> activeCoroutines = new Dictionary<GameObject, Coroutine>();
    
    /// <summary>
    /// Fades in a UI element using its CanvasGroup. Adds CanvasGroup if it doesn't exist.
    /// </summary>
    public static void FadeIn(GameObject target, float duration = 0.3f, MonoBehaviour coroutineRunner = null)
    {
        if (target == null) return;
        
        CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = target.AddComponent<CanvasGroup>();
        }
        
        if (coroutineRunner == null)
        {
            coroutineRunner = FindOrCreateFaderInstance();
        }
        
        StopExistingFade(target);
        
        target.SetActive(true);
        Coroutine coroutine = coroutineRunner.StartCoroutine(FadeCoroutine(canvasGroup, 1f, duration, true));
        activeCoroutines[target] = coroutine;
    }
    
    /// <summary>
    /// Fades out a UI element using its CanvasGroup. Deactivates after fade completes.
    /// </summary>
    public static void FadeOut(GameObject target, float duration = 0.3f, MonoBehaviour coroutineRunner = null)
    {
        if (target == null) return;
        
        CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = target.AddComponent<CanvasGroup>();
        }
        
        if (coroutineRunner == null)
        {
            coroutineRunner = FindOrCreateFaderInstance();
        }
        
        StopExistingFade(target);
        
        Coroutine coroutine = coroutineRunner.StartCoroutine(FadeCoroutine(canvasGroup, 0f, duration, false));
        activeCoroutines[target] = coroutine;
    }
    
    /// <summary>
    /// Shows a UI element immediately without fade
    /// </summary>
    public static void ShowImmediate(GameObject target)
    {
        if (target == null) return;
        
        StopExistingFade(target);
        
        CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = target.AddComponent<CanvasGroup>();
        }
        
        target.SetActive(true);
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }
    
    /// <summary>
    /// Hides a UI element immediately without fade
    /// </summary>
    public static void HideImmediate(GameObject target)
    {
        if (target == null) return;
        
        StopExistingFade(target);
        
        CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        
        target.SetActive(false);
    }
    
    private static void StopExistingFade(GameObject target)
    {
        if (activeCoroutines.ContainsKey(target))
        {
            MonoBehaviour runner = FindOrCreateFaderInstance();
            if (runner != null && activeCoroutines[target] != null)
            {
                runner.StopCoroutine(activeCoroutines[target]);
            }
            activeCoroutines.Remove(target);
        }
    }
    
    private static IEnumerator FadeCoroutine(CanvasGroup canvasGroup, float targetAlpha, float duration, bool fadeIn)
    {
        if (canvasGroup == null) yield break;
        
        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;
        
        // Enable interaction during fade in
        if (fadeIn)
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
        else
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        
        while (elapsed < duration)
        {
            if (canvasGroup == null) yield break;
            
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, smoothT);
            
            yield return null;
        }
        
        if (canvasGroup != null)
        {
            canvasGroup.alpha = targetAlpha;
            
            // Deactivate after fade out completes
            if (!fadeIn && canvasGroup.gameObject != null)
            {
                canvasGroup.gameObject.SetActive(false);
            }
        }
    }
    
    private static UIFader FindOrCreateFaderInstance()
    {
        UIFader instance = FindFirstObjectByType<UIFader>();
        if (instance == null)
        {
            GameObject faderObject = new GameObject("UIFader");
            instance = faderObject.AddComponent<UIFader>();
            DontDestroyOnLoad(faderObject);
        }
        return instance;
    }
}

