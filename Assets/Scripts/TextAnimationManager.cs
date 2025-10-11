using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;

public class TextAnimationManager : MonoBehaviour
{
    private static TextAnimationManager instance;
    public static TextAnimationManager Instance => instance;

    private Dictionary<TextMeshProUGUI, Coroutine> activeAnimations = new Dictionary<TextMeshProUGUI, Coroutine>();
    private Dictionary<TextMeshProUGUI, ulong> lastRenderedAmounts = new Dictionary<TextMeshProUGUI, ulong>();

    [SerializeField] private float defaultDuration = 0.35f;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    public ulong GetLastRenderedAmount(TextMeshProUGUI label)
    {
        if (label == null) return 0;
        return lastRenderedAmounts.TryGetValue(label, out ulong value) ? value : 0;
    }

    public void SetLastRenderedAmount(TextMeshProUGUI label, ulong amount)
    {
        if (label == null) return;
        lastRenderedAmounts[label] = amount;
    }

    public void AnimateNumber(TextMeshProUGUI label, ulong startValue, ulong endValue, float duration = -1f, string prefix = "", string suffix = "", Action onComplete = null)
    {
        if (label == null) return;

        if (duration < 0) duration = defaultDuration;

        StopAnimation(label);
        lastRenderedAmounts[label] = endValue;

        Coroutine coroutine = StartCoroutine(AnimateNumberCoroutine(label, startValue, endValue, duration, prefix, suffix, false, onComplete));
        activeAnimations[label] = coroutine;
    }

    public void AnimateFormattedNumber(TextMeshProUGUI label, ulong startValue, ulong endValue, float duration = -1f, string prefix = "", string suffix = "", Action onComplete = null)
    {
        if (label == null) return;

        if (duration < 0) duration = defaultDuration;

        StopAnimation(label);
        lastRenderedAmounts[label] = endValue;

        Coroutine coroutine = StartCoroutine(AnimateNumberCoroutine(label, startValue, endValue, duration, prefix, suffix, true, onComplete));
        activeAnimations[label] = coroutine;
    }

    public void SetText(TextMeshProUGUI label, string text)
    {
        if (label == null) return;

        StopAnimation(label);
        label.text = text;
    }

    public void StopAnimation(TextMeshProUGUI label)
    {
        if (label == null) return;

        if (activeAnimations.TryGetValue(label, out Coroutine coroutine))
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }
            activeAnimations.Remove(label);
        }
    }

    public void StopAllAnimations()
    {
        foreach (var kvp in activeAnimations)
        {
            if (kvp.Value != null)
            {
                StopCoroutine(kvp.Value);
            }
        }
        activeAnimations.Clear();
    }

    public void ClearLastRenderedAmount(TextMeshProUGUI label)
    {
        if (label == null) return;
        lastRenderedAmounts.Remove(label);
    }

    private IEnumerator AnimateNumberCoroutine(TextMeshProUGUI label, ulong startValue, ulong endValue, float duration, string prefix, string suffix, bool formatted, Action onComplete)
    {
        float elapsed = 0f;
        duration = Mathf.Max(0.01f, duration);

        while (elapsed < duration)
        {
            if (label == null)
            {
                activeAnimations.Remove(label);
                yield break;
            }

            float t = elapsed / duration;
            float easedT = 1f - Mathf.Pow(1f - t, 3f); // ease-out cubic
            double current = Mathf.Lerp((float)startValue, (float)endValue, easedT);
            ulong currentValue = (ulong)current;
            label.text = prefix + (formatted ? FormatShortAmount(currentValue) : currentValue.ToString(CultureInfo.InvariantCulture)) + suffix;
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (label != null)
        {
            label.text = prefix + (formatted ? FormatShortAmount(endValue) : endValue.ToString(CultureInfo.InvariantCulture)) + suffix;
        }

        activeAnimations.Remove(label);
        onComplete?.Invoke();
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

