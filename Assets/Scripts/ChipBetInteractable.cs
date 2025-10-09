using UnityEngine;
using TMPro;
using System.Globalization;
using System.Collections;

public class ChipBetInteractable : MonoBehaviour, IInteractable
{
    public enum BetActionType
    {
        Add,
        Set,
        PlaceBet,
        StartGame,
        ClaimBet
    }

    [SerializeField] public BetActionType action = BetActionType.Add;
    [SerializeField] public ulong amount;
    private TextMeshProUGUI label;
    private UserUI userUI;

    // Bet amount animation state (mirrors BalloonSimulator multiplier easing)
    [SerializeField] private float betCountDuration = 0.35f;
    private Coroutine betCountCoroutine;
    private ulong lastRenderedBetAmount;
    private ulong animationTargetBetAmount;
    private double currentDisplayedBetValue;

    private void Awake()
    {
        if (label == null)
        {
            label = GetComponentInChildren<TextMeshProUGUI>();
        }

        if (userUI == null)
        {
            userUI = FindObjectOfType<UserUI>();
        }

        lastRenderedBetAmount = userUI != null ? userUI.betAmount : 0ul;
        currentDisplayedBetValue = lastRenderedBetAmount;
    }

    private void Update()
    {
        if (label == null) return;

        if (action == BetActionType.PlaceBet && userUI != null)
        {
            ulong currentBet = userUI.betAmount;

            if (betCountCoroutine == null)
            {
                if (currentBet != lastRenderedBetAmount)
                {
                    animationTargetBetAmount = currentBet;
                    betCountCoroutine = StartCoroutine(AnimateBetAmount(lastRenderedBetAmount, currentBet));
                }
                else
                {
                    label.text = $"Bet: {FormatShortAmount(currentBet)}";
                }
            }
            else
            {
                // If bet changed mid-animation, retarget smoothly from current displayed value
                if (currentBet != animationTargetBetAmount)
                {
                    StopCoroutine(betCountCoroutine);
                    betCountCoroutine = StartCoroutine(AnimateBetAmount((ulong)System.Math.Round(currentDisplayedBetValue), currentBet));
                    animationTargetBetAmount = currentBet;
                }
            }
            return;
        }

        if (action == BetActionType.Add)
        {
            label.text = FormatShortAmount(amount);
            return;
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

    public void OnInteract()
    {
        if (userUI == null) return;

        switch (action)
        {
            case BetActionType.Add:
                userUI.betAmount += amount;
                break;
            case BetActionType.Set:
                userUI.betAmount = amount;
                break;
            case BetActionType.PlaceBet:
                userUI.PlaceBet();
                break;
            case BetActionType.StartGame:
                userUI.StartGame();
                break;
            case BetActionType.ClaimBet:
                userUI.ClaimBet();
                break;
        }
    }

    // Animate bet label value from start to end with ease-out cubic
    private IEnumerator AnimateBetAmount(ulong startValue, ulong endValue)
    {
        float duration = Mathf.Max(0.01f, betCountDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float easedT = 1f - Mathf.Pow(1f - t, 3f);
            double current = Mathf.Lerp((float)startValue, (float)endValue, easedT);
            currentDisplayedBetValue = current;
            label.text = $"Bet: {FormatShortAmount((ulong)current)}";
            elapsed += Time.deltaTime;
            yield return null;
        }

        currentDisplayedBetValue = endValue;
        lastRenderedBetAmount = endValue;
        label.text = $"Bet: {FormatShortAmount(endValue)}";
        betCountCoroutine = null;
    }
}


