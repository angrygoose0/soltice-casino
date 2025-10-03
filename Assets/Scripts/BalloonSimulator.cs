using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using TMPro;

public class BalloonSimulator : MonoBehaviour
{
    public GameObject balloonPrefab;
    public Vector3 defaultSpawnLocation = Vector3.zero;

    [SerializeField] private double currentTick = 0;
    public double crashTick = 40;

    private GameObject spawnedBalloon;
    private ParticleSystem popParticle;
    private ParticleSystem burstParticle;
    private Renderer balloonRenderer;
    private TMP_Text multiplierText;

    private Coroutine smoothScaleCoroutineRef;
    private Coroutine moveUpCoroutineRef;
    private Coroutine multiplierCountCoroutineRef;

    // Base local position used for bobbing; MoveUp updates this, bobbing offsets from it
    private Vector3 balloonBaseLocalPos;

    [Header("Balloon Bobbing Settings")]
    [SerializeField] private float amplitudeY = 0.2f;
    [SerializeField] private float frequencyY = 1f;
    [SerializeField] private float amplitudeX = 0.05f;
    [SerializeField] private float frequencyX = 0.7f;
    [SerializeField] private float phaseOffset = 1.3f;

    [Header("Balloon Scaling Settings")]
    [SerializeField] private Vector3 scaleConstant = Vector3.one;
    [SerializeField] private float scaleDuration = 0.7f;
	[SerializeField] private float overshootStrength = 0.8f; // controls how far past target it goes

    [Header("Multiplier Text Settings")]
    [SerializeField] private float multiplierCountDuration = 0.35f; // fast count animation duration


    void Start()
    {
        spawnedBalloon = SpawnBalloon(defaultSpawnLocation);
        currentTick = 0;
        BalloonBasedOnTick(currentTick);
    }

    void Update()
    {
        // When 1 key is pressed, call BalloonBasedOnTick with currentTick + 1
        if (Keyboard.current != null && Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            BalloonBasedOnTick(currentTick + 1);
        }
    }

    void BalloonBasedOnTick(double tick)
    {
        // Update multiplier text appearance and animate value to feel like fast counting
        if (multiplierText != null)
        {
            double fromMultiplier = System.Math.Pow(1.11, currentTick);
            double toMultiplier = System.Math.Pow(1.11, tick);
            // Start/refresh fast-count animation when tick increases; otherwise just set directly
            if (multiplierCountCoroutineRef != null)
            {
                StopCoroutine(multiplierCountCoroutineRef);
                multiplierCountCoroutineRef = null;
            }
            if (currentTick != tick)
            {
                multiplierCountCoroutineRef = StartCoroutine(AnimateMultiplierText((float)fromMultiplier, (float)toMultiplier));
            }
            else
            {
                multiplierText.text = string.Format("{0:0.00}x", toMultiplier);
            }
            // Color gradient independent of crashTick: white -> yellow -> red -> dark red as tick increases
            // Clamp by 43 so 43+ is very dark red
            float t01 = Mathf.Clamp01((float)tick / 43f);
            Color colorWhite = Color.white;
            Color colorYellow = Color.yellow;
            Color colorRed = Color.red;
            Color colorDarkRed = new Color(0.2f, 0f, 0f, 1f); // very dark red
            // Use two pivots to make the color change noticeable earlier
            float pivot1 = 0.2f; // white -> yellow completes early
            float pivot2 = 0.6f; // yellow -> red, then red -> dark red
            Color targetColor;
            if (t01 < pivot1)
            {
                targetColor = Color.Lerp(colorWhite, colorYellow, t01 / pivot1);
            }
            else if (t01 < pivot2)
            {
                targetColor = Color.Lerp(colorYellow, colorRed, (t01 - pivot1) / (pivot2 - pivot1));
            }
            else
            {
                targetColor = Color.Lerp(colorRed, colorDarkRed, (t01 - pivot2) / (1f - pivot2));
            }
            multiplierText.color = targetColor;
        }

        if (currentTick == tick) return;
        currentTick = tick;
        
        // Check if balloon should pop
        if (tick >= crashTick)
        {
            PopBalloon();
            return;
        }
        
        double sizeScale = System.Math.Pow(1.1, tick);
        // Stop previous SmoothScaleCoroutine if running
        if (smoothScaleCoroutineRef != null)
        {
            StopCoroutine(smoothScaleCoroutineRef);
        }
        smoothScaleCoroutineRef = StartCoroutine(SmoothScaleCoroutine(spawnedBalloon, (float)sizeScale));
        // Stop previous MoveUpCoroutine if running
        if (moveUpCoroutineRef != null)
        {
            StopCoroutine(moveUpCoroutineRef);
        }
        //moveUpCoroutineRef = StartCoroutine(MoveUpCoroutine(spawnedBalloon, (float)upScale));
    }

    void PopBalloon()
    {
        // Disable the balloon renderer
        if (balloonRenderer != null)
            balloonRenderer.enabled = false;
        // Hide multiplier text
        if (multiplierText != null)
            multiplierText.enabled = false;
        
        // Play the particle systems
        if (popParticle != null)
            popParticle.Play();
        if (burstParticle != null)
            burstParticle.Play();
        
        // Stop any ongoing coroutines
        if (smoothScaleCoroutineRef != null)
        {
            StopCoroutine(smoothScaleCoroutineRef);
            smoothScaleCoroutineRef = null;
        }
        if (moveUpCoroutineRef != null)
        {
            StopCoroutine(moveUpCoroutineRef);
            moveUpCoroutineRef = null;
        }
        if (multiplierCountCoroutineRef != null)
        {
            StopCoroutine(multiplierCountCoroutineRef);
            multiplierCountCoroutineRef = null;
        }
    }

    public GameObject SpawnBalloon(Vector3 position)
    {
        GameObject balloon = Instantiate(balloonPrefab, position, Quaternion.Euler(-90, 0, 0));
        balloon.transform.localScale = scaleConstant;
        
        // Find and store particle system references
        Transform popTransform = balloon.transform.Find("pop");
        Transform burstTransform = balloon.transform.Find("burst");
        
        if (popTransform != null)
            popParticle = popTransform.GetComponent<ParticleSystem>();
        if (burstTransform != null)
            burstParticle = burstTransform.GetComponent<ParticleSystem>();
            
        // Get the balloon renderer
        balloonRenderer = balloon.GetComponent<Renderer>();

        // Cache multiplier TMP text under canvas/multiplier if present
        Transform canvasTransform = balloon.transform.Find("canvas");
        if (canvasTransform != null)
        {
            Transform multiplierTransform = canvasTransform.Find("multiplier");
            if (multiplierTransform != null)
                multiplierText = multiplierTransform.GetComponent<TMP_Text>();
            if (multiplierText != null)
                multiplierText.enabled = true; // ensure visible on new spawn
        }
        
        // Initialize base position for bobbing
        spawnedBalloon = balloon;
        balloonBaseLocalPos = balloon.transform.localPosition;
        StartCoroutine(BobBalloonCoroutine(balloon));
        return balloon;
    }

    // Coroutine to bob a balloon up and down in place
    private IEnumerator BobBalloonCoroutine(GameObject balloon)
    {
        while (balloon != null)
        {
            // Bob as an offset from the current base position so other movement can update the base cleanly
            Vector3 basePos = balloon == spawnedBalloon ? balloonBaseLocalPos : balloon.transform.localPosition;
            float offsetY = Mathf.Sin(Time.time * frequencyY) * amplitudeY;
            float offsetX = Mathf.Sin(Time.time * frequencyX + phaseOffset) * amplitudeX;
            balloon.transform.localPosition = new Vector3(basePos.x + offsetX, basePos.y + offsetY, basePos.z);
            yield return null;
        }
    }

	// Smoothly scales the balloon to a new scale (uniform, can be up or down)
    private IEnumerator SmoothScaleCoroutine(GameObject balloon, float scaleFactor)
    {
        if (balloon == null) yield break;
        Vector3 originalScale = balloon.transform.localScale;
        Vector3 newScale = scaleConstant * scaleFactor;
        float elapsed = 0f;
        while (elapsed < scaleDuration && balloon != null)
        {
			float t = elapsed / scaleDuration;
			// Ease-out exponential time mapping for quick start, gentle finish
			float expoEaseOutT = 1f - Mathf.Pow(1f - t, 2.2f);
			// Apply a gentle overshoot (back) so it goes a bit past and corrects
			float s = overshootStrength;
			float p = expoEaseOutT - 1f;
			float overshootT = p * p * ((s + 1f) * p + s) + 1f; // easeOutBack
			balloon.transform.localScale = Vector3.LerpUnclamped(originalScale, newScale, overshootT);
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (balloon != null)
            balloon.transform.localScale = newScale;
    }

    // Coroutine to smoothly move the balloon up by deltaY units
    private IEnumerator MoveUpCoroutine(GameObject balloon, float scaleFactor)
    {
        if (balloon == null) yield break;
        if (scaleFactor == 0) yield break;
        // Move the base position so bobbing remains additive and doesn't fight with this motion
        Vector3 startBasePos = balloon == spawnedBalloon ? balloonBaseLocalPos : balloon.transform.localPosition;
        Vector3 endBasePos = startBasePos + new Vector3(0f, scaleFactor, 0f);
        float duration = scaleDuration * scaleFactor;
        float elapsed = 0f;
        while (elapsed < duration && balloon != null)
        {
            float t = elapsed / duration;
            float expT = Mathf.Pow(t, 2.2f); // Exponential curve for smoothness
            Vector3 newBasePos = Vector3.Lerp(startBasePos, endBasePos, expT);
            if (balloon == spawnedBalloon)
            {
                balloonBaseLocalPos = newBasePos;
            }
            else
            {
                // Fallback: if moving a non-tracked balloon, set its position directly
                balloon.transform.localPosition = newBasePos;
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (balloon != null)
        {
            if (balloon == spawnedBalloon)
            {
                balloonBaseLocalPos = endBasePos;
            }
            else
            {
                balloon.transform.localPosition = endBasePos;
            }
        }
    }

    // Animate multiplier text from a start value to an end value quickly
    private IEnumerator AnimateMultiplierText(float startValue, float endValue)
    {
        if (multiplierText == null)
            yield break;
        float duration = Mathf.Max(0.01f, multiplierCountDuration);
        float elapsed = 0f;
        while (elapsed < duration && multiplierText != null)
        {
            float t = elapsed / duration;
            // Ease-out cubic for snappy start and gentle finish
            float easedT = 1f - Mathf.Pow(1f - t, 3f);
            float current = Mathf.Lerp(startValue, endValue, easedT);
            multiplierText.text = string.Format("{0:0.00}x", current);
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (multiplierText != null)
            multiplierText.text = string.Format("{0:0.00}x", endValue);
    }
}

