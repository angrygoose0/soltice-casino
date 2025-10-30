using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using TMPro;
using Crash.Accounts;

public class BalloonSimulator : MonoBehaviour
{
    public GameObject balloonPrefab;
    public Vector3 defaultSpawnLocation = Vector3.zero;
    
    [SerializeField] private FeedbackManager feedbackManager;
    [SerializeField] private InteractableObjects interactableObjects;

    [SerializeField] private double currentTick = 0;
    public double crashTick = 40;

    private GameObject spawnedBalloon;
    private ParticleSystem popParticle;
    private ParticleSystem burstParticle;
    private Renderer balloonRenderer;
    private TMP_Text multiplierText;
    private TMP_Text playerCountText;
    private TMP_Text totalBetText;

    private Coroutine smoothScaleCoroutineRef;
    private Coroutine multiplierCountCoroutineRef;
    private Coroutine bobBalloonCoroutineRef;

    // Base local position used for bobbing; bobbing offsets from it
    private Vector3 balloonBaseLocalPos;

    [Header("Balloon Bobbing Settings")]
    [SerializeField] private float amplitudeY = 0.2f;
    [SerializeField] private float frequencyY = 1f;
    [SerializeField] private float amplitudeX = 0.05f;
    [SerializeField] private float frequencyX = 0.7f;
    [SerializeField] private float phaseOffset = 1.3f;

    [Header("Balloon Scaling Settings")]
    [SerializeField] private Vector3 scaleConstant = Vector3.one;
    [SerializeField] private Vector3 initialScale;
    [SerializeField] private float scaleDuration = 0.7f;
	[SerializeField] private float overshootStrength = 0.8f; // controls how far past target it goes

    [Header("Multiplier Text Settings")]
    [SerializeField] private float multiplierCountDuration = 0.35f; // fast count animation duration


    void Start()
    {
        SpawnBalloon(defaultSpawnLocation); 
    }


    private void BalloonBasedOnTick(double tick)
    {
        double fromMultiplier = currentTick == 0 ? 0 : System.Math.Pow(1.11, currentTick);
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
        
        currentTick = tick;
        
        double sizeScale = System.Math.Pow(1.1, tick);
        // Stop previous SmoothScaleCoroutine if running
        if (smoothScaleCoroutineRef != null)
        {
            StopCoroutine(smoothScaleCoroutineRef);
        }
        smoothScaleCoroutineRef = StartCoroutine(SmoothScaleCoroutine(spawnedBalloon, (float)sizeScale));
        feedbackManager?.PlayBalloonInflate();
    }

    private void PopBalloon()
    {   
        // Stop all running coroutines for the balloon
        if (smoothScaleCoroutineRef != null)
        {
            StopCoroutine(smoothScaleCoroutineRef);
            smoothScaleCoroutineRef = null;
        }
        if (multiplierCountCoroutineRef != null)
        {
            StopCoroutine(multiplierCountCoroutineRef);
            multiplierCountCoroutineRef = null;
        }
        if (bobBalloonCoroutineRef != null)
        {
            StopCoroutine(bobBalloonCoroutineRef);
            bobBalloonCoroutineRef = null;
        }
        
        // Hide balloon and text immediately, but let particles play
        balloonRenderer.enabled = false;
        multiplierText.enabled = false;
        
        // Play the particle systems (keep the current scale so particles are visible)

        popParticle.Play();
        burstParticle.Play();
        feedbackManager?.PlayBalloonInflate();

        currentTick = 0;
    }

    private void SpawnBalloon(Vector3 position)
    {
		spawnedBalloon = Instantiate(balloonPrefab);
		spawnedBalloon.transform.SetParent(transform, false);
		spawnedBalloon.transform.localPosition = new Vector3(0f, 0.75f, 0f);
		spawnedBalloon.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
		{
			Vector3 p = transform.lossyScale;
			spawnedBalloon.transform.localScale = new Vector3(
				initialScale.x / p.x,
				initialScale.y / p.y,
				initialScale.z / p.z
			);
		}
        balloonBaseLocalPos = spawnedBalloon.transform.localPosition;
        
        // Find and store particle system references
        Transform popTransform = spawnedBalloon.transform.Find("pop");
        Transform burstTransform = spawnedBalloon.transform.Find("burst");
        
        popParticle = popTransform.GetComponent<ParticleSystem>();
        burstParticle = burstTransform.GetComponent<ParticleSystem>();
            
        // Get the balloon renderer
        balloonRenderer = spawnedBalloon.GetComponent<Renderer>();
        balloonRenderer.enabled = false;

        // Cache multiplier TMP text under canvas/multiplier if present
        Transform canvasTransform = spawnedBalloon.transform.Find("canvas");
        Transform canvasExtraTransform = spawnedBalloon.transform.Find("extraCanvas");

        Transform multiplierTransform = canvasTransform.Find("multiplier");
        multiplierText = multiplierTransform.GetComponent<TMP_Text>();

        playerCountText = canvasExtraTransform.Find("playerCount").GetComponent<TMP_Text>();
        totalBetText = canvasExtraTransform.Find("totalBet").GetComponent<TMP_Text>();

        multiplierText.enabled = false; // ensure visible on new spawn
        playerCountText.enabled = false;
        totalBetText.enabled = false;

        // Register balloon as an interactable with glow effects (disabled, not clickable)
        if (interactableObjects != null)
        {
            interactableObjects.RegisterInteractable(spawnedBalloon, "default", InteractableObjects.ActionType.None, false);
        }
    }

    public void UpdateBalloon(Game game)
    {
        if (!balloonRenderer.enabled) {

            if (game.State == 0) {
                return;
            }
            balloonRenderer.enabled = true;
            multiplierText.enabled = true;
			{
				Vector3 p = transform.lossyScale;
				spawnedBalloon.transform.localScale = new Vector3(
					initialScale.x / p.x,
					initialScale.y / p.y,
					initialScale.z / p.z
				);
			}
            bobBalloonCoroutineRef = StartCoroutine(BobBalloonCoroutine(spawnedBalloon));
            feedbackManager?.PlayBalloonInflate();

            BalloonBasedOnTick(game.Tick);

            if (game.CurrentPlayers > 0) {
                playerCountText.enabled = true;
                playerCountText.text = game.CurrentPlayers.ToString();
                totalBetText.enabled = true;
                totalBetText.text = game.CurrentTotalBet.ToString();
            } else {
                playerCountText.enabled = false;
                totalBetText.enabled = false;
            }
            //size based on tick
            return;
        } else { //balloon is enabled
            if (game.State == 0) {
                PopBalloon();
                return;
            }
            //size based on tick
            if (bobBalloonCoroutineRef == null) {
                bobBalloonCoroutineRef = StartCoroutine(BobBalloonCoroutine(spawnedBalloon));
            }
            BalloonBasedOnTick(game.Tick);
            //update totalbet using the multiplier..


            return;
        }
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
        
        // Set balloon to pressed state at start of growth
        SetBalloonGlowPressed(balloon);
        
		Vector3 originalScale = balloon.transform.localScale;
		Vector3 worldScaleTarget = scaleConstant * scaleFactor;
		Vector3 parentScale = balloon.transform.parent.lossyScale;
		Vector3 newLocalScale = new Vector3(
			worldScaleTarget.x / parentScale.x,
			worldScaleTarget.y / parentScale.y,
			worldScaleTarget.z / parentScale.z
		);
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
			balloon.transform.localScale = Vector3.LerpUnclamped(originalScale, newLocalScale, overshootT);
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (balloon != null)
        {
			balloon.transform.localScale = newLocalScale;
            // Return balloon to idle state when growth stops
            SetBalloonGlowIdle(balloon);
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

    private void SetBalloonGlowPressed(GameObject balloon)
    {
        if (interactableObjects != null && feedbackManager != null && balloon != null)
        {
            feedbackManager.PlayUIClickDown(balloon.transform);
            
            InteractableObjects.GlowProfile profile = GetBalloonGlowProfile(balloon);
            if (profile != null)
            {
                FeedbackManager.MaterialGlowState pressedState = new FeedbackManager.MaterialGlowState
                {
                    scale = profile.pressed.scale,
                    intensity = profile.pressed.intensity,
                    color = profile.pressed.color
                };
                feedbackManager.AnimateGlowMaterial(balloon, pressedState);
            }
        }
    }

    private void SetBalloonGlowIdle(GameObject balloon)
    {
        if (interactableObjects != null && feedbackManager != null && balloon != null)
        {
            feedbackManager.PlayUIClickUp(balloon.transform);
            
            InteractableObjects.GlowProfile profile = GetBalloonGlowProfile(balloon);
            if (profile != null)
            {
                FeedbackManager.MaterialGlowState idleState = new FeedbackManager.MaterialGlowState
                {
                    scale = profile.idle.scale,
                    intensity = profile.idle.intensity,
                    color = profile.idle.color
                };
                feedbackManager.AnimateGlowMaterial(balloon, idleState);
            }
        }
    }

    private InteractableObjects.GlowProfile GetBalloonGlowProfile(GameObject balloon)
    {
        return interactableObjects?.GetGlowProfile(balloon);
    }
}

