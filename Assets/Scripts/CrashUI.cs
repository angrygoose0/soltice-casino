using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using Solana.Unity.Wallet;
using TMPro;
using System.Collections.Generic;
using Solana.Unity.SDK;

public class CrashUI : MonoBehaviour
{
    [Header("Builder")]
    [SerializeField] private CrashTransactionBuilder builder;

    [Header("Initialize")]
    [SerializeField] private Button initializeAuthorityButton;
    [SerializeField] private Button initializeGameButton;
    [SerializeField] private Button initializePlayerBetButton;
    [SerializeField] private Button startGameButton;

    [Header("Delegation (optional inputs shared)")]
    [SerializeField] private InputField delegateCommitFrequencyMsInput; // optional
    [SerializeField] private InputField delegateValidatorPubkeyInput;   // optional
    [SerializeField] private Button delegateAuthorityButton;
    [SerializeField] private Button delegateGameButton;
    [SerializeField] private Button delegatePlayerBetButton;

    [Header("Game Loop")]
    [SerializeField] private Button tickButton;

    [Header("Randomness")]
    [SerializeField] private Button requestRandomnessButton;
    [SerializeField] private InputField requestRandomnessSeedInput; // byte 0-255

    [Header("Betting")]
    [SerializeField] private Button placeBetButton;
    [SerializeField] private InputField placeBetAmountInput;
    [SerializeField] private Button claimBetButton;

    [Header("Fetch")]
    [SerializeField] private Button getGameDataButton;
    [SerializeField] private Button getPlayerBetButton;

    [Header("Feedback (optional)")]
    [SerializeField] private TMP_Text statusTMPText;

    // Cached data from fetches
    private Crash.Accounts.Game _cachedGame;
    private Crash.Accounts.PlayerBet _cachedPlayerBet;

    private void Awake()
    {
        if (builder == null)
        {
            builder = FindObjectOfType<CrashTransactionBuilder>();
        }
        if (builder == null)
        {
            Debug.LogError("CrashUI: CrashTransactionBuilder reference is missing; disabling UI");
            enabled = false;
            return;
        }
    }

    private void OnEnable()
    {
        if (initializeAuthorityButton != null) initializeAuthorityButton.onClick.AddListener(OnClickInitializeAuthority);
        if (initializeGameButton != null) initializeGameButton.onClick.AddListener(OnClickInitializeGame);
        if (initializePlayerBetButton != null) initializePlayerBetButton.onClick.AddListener(OnClickInitializePlayerBet);
        if (startGameButton != null) startGameButton.onClick.AddListener(OnClickStartGame);

        if (delegateAuthorityButton != null) delegateAuthorityButton.onClick.AddListener(OnClickDelegateAuthority);
        if (delegateGameButton != null) delegateGameButton.onClick.AddListener(OnClickDelegateGame);
        if (delegatePlayerBetButton != null) delegatePlayerBetButton.onClick.AddListener(OnClickDelegatePlayerBet);

        if (tickButton != null) tickButton.onClick.AddListener(OnClickTick);

        if (requestRandomnessButton != null) requestRandomnessButton.onClick.AddListener(OnClickRequestRandomness);

        if (placeBetButton != null) placeBetButton.onClick.AddListener(OnClickPlaceBet);
        if (claimBetButton != null) claimBetButton.onClick.AddListener(OnClickClaimBet);

        if (getGameDataButton != null) getGameDataButton.onClick.AddListener(OnClickGetGameData);
        if (getPlayerBetButton != null) getPlayerBetButton.onClick.AddListener(OnClickGetPlayerBet);

        CacheOriginalButtonTexts();
        RefreshButtons();
    }

    private void OnDisable()
    {
        if (initializeAuthorityButton != null) initializeAuthorityButton.onClick.RemoveListener(OnClickInitializeAuthority);
        if (initializeGameButton != null) initializeGameButton.onClick.RemoveListener(OnClickInitializeGame);
        if (initializePlayerBetButton != null) initializePlayerBetButton.onClick.RemoveListener(OnClickInitializePlayerBet);
        if (startGameButton != null) startGameButton.onClick.RemoveListener(OnClickStartGame);

        if (delegateAuthorityButton != null) delegateAuthorityButton.onClick.RemoveListener(OnClickDelegateAuthority);
        if (delegateGameButton != null) delegateGameButton.onClick.RemoveListener(OnClickDelegateGame);
        if (delegatePlayerBetButton != null) delegatePlayerBetButton.onClick.RemoveListener(OnClickDelegatePlayerBet);

        if (tickButton != null) tickButton.onClick.RemoveListener(OnClickTick);

        if (requestRandomnessButton != null) requestRandomnessButton.onClick.RemoveListener(OnClickRequestRandomness);

        if (placeBetButton != null) placeBetButton.onClick.RemoveListener(OnClickPlaceBet);
        if (claimBetButton != null) claimBetButton.onClick.RemoveListener(OnClickClaimBet);

        if (getGameDataButton != null) getGameDataButton.onClick.RemoveListener(OnClickGetGameData);
        if (getPlayerBetButton != null) getPlayerBetButton.onClick.RemoveListener(OnClickGetPlayerBet);
    }

    public void SetBuilder(CrashTransactionBuilder newBuilder)
    {
        builder = newBuilder;
        RefreshButtons();
    }

    public void RefreshButtons()
    {
        SetInteractable(initializeAuthorityButton, true);
        SetInteractable(initializeGameButton, true);
        SetInteractable(initializePlayerBetButton, true);
        SetInteractable(startGameButton, true);
        SetInteractable(delegateAuthorityButton, true);
        SetInteractable(delegateGameButton, true);
        SetInteractable(delegatePlayerBetButton, true);
        SetInteractable(tickButton, true);
        SetInteractable(requestRandomnessButton, true);
        SetInteractable(placeBetButton, true);
        SetInteractable(claimBetButton, true);
        SetInteractable(getGameDataButton, true);
        SetInteractable(getPlayerBetButton, true);
    }

    private static void SetInteractable(Button button, bool interactable)
    {
        if (button != null) button.interactable = interactable;
    }

    private async void OnClickInitializeAuthority()
    {
        await RunAsync(initializeAuthorityButton, async () => await builder.InitializeAuthority());
    }

    private async void OnClickInitializeGame()
    {
        await RunAsync(initializeGameButton, async () => await builder.InitializeGame());
    }

    private async void OnClickInitializePlayerBet()
    {
        await RunAsync(initializePlayerBetButton, async () => await builder.InitializePlayerBet());
    }

    private async void OnClickStartGame()
    {
        await RunAsync(startGameButton, async () => await builder.StartGame());
    }

    private async void OnClickDelegateAuthority()
    {
        uint commitMs = ParseUint(delegateCommitFrequencyMsInput, 1000u);
        PublicKey validator = ParsePublicKey(delegateValidatorPubkeyInput);
        await RunAsync(delegateAuthorityButton, async () => await builder.DelegateAuthority(commitMs, validator));
    }

    private async void OnClickDelegateGame()
    {
        uint commitMs = ParseUint(delegateCommitFrequencyMsInput, 1000u);
        PublicKey validator = ParsePublicKey(delegateValidatorPubkeyInput);
        await RunAsync(delegateGameButton, async () => await builder.DelegateGame(commitMs, validator));
    }

    private async void OnClickDelegatePlayerBet()
    {
        uint commitMs = ParseUint(delegateCommitFrequencyMsInput, 1000u);
        PublicKey validator = ParsePublicKey(delegateValidatorPubkeyInput);
        await RunAsync(delegatePlayerBetButton, async () => await builder.DelegatePlayerBet(commitMs, validator));
    }

    private async void OnClickTick()
    {
        await RunAsync(tickButton, async () => await builder.Tick());
    }

    private async void OnClickRequestRandomness()
    {
        byte seed = ParseByte(requestRandomnessSeedInput, 0);
        await RunAsync(requestRandomnessButton, async () => await builder.RequestRandomness(seed));
    }

    private async void OnClickPlaceBet()
    {
        ulong amount = ParseUlong(placeBetAmountInput, 0ul);
        await RunAsync(placeBetButton, async () => await builder.PlaceBet(amount));
    }

    private async void OnClickClaimBet()
    {
        await RunAsync(claimBetButton, async () => await builder.ClaimBet());
    }

    private async void OnClickGetGameData()
    {
        var game = CrashTransactionBuilder.DeriveGameAccount();
        await RunFetchAsync(getGameDataButton, async () => await builder.GetGameData(game), result =>
        {
            _cachedGame = result;
            return result != null ? "Fetched Game" : "Game not found";
        });
    }

    private async void OnClickGetPlayerBet()
    {
        if (Web3.Account == null)
        {
            SetStatus("Wallet not connected");
            return;
        }
        var userPk = Web3.Account.PublicKey;
        await RunFetchAsync(getPlayerBetButton, async () => await builder.GetPlayerBet(userPk), result =>
        {
            _cachedPlayerBet = result;
            return result != null ? "Fetched PlayerBet" : "PlayerBet not found";
        });
    }

    private readonly Dictionary<Button, string> _originalButtonText = new Dictionary<Button, string>();

    private void CacheOriginalButtonTexts()
    {
        CacheOriginalText(initializeAuthorityButton);
        CacheOriginalText(initializeGameButton);
        CacheOriginalText(initializePlayerBetButton);
        CacheOriginalText(startGameButton);
        CacheOriginalText(delegateAuthorityButton);
        CacheOriginalText(delegateGameButton);
        CacheOriginalText(delegatePlayerBetButton);
        CacheOriginalText(tickButton);
        CacheOriginalText(requestRandomnessButton);
        CacheOriginalText(placeBetButton);
        CacheOriginalText(claimBetButton);
        CacheOriginalText(getGameDataButton);
        CacheOriginalText(getPlayerBetButton);
    }

    private void CacheOriginalText(Button button)
    {
        if (button == null) return;
        if (_originalButtonText.ContainsKey(button)) return;
        var textComp = FindChildTMPText(button);
        if (textComp != null)
        {
            _originalButtonText[button] = textComp.text;
        }
    }

    private TMP_Text FindChildTMPText(Button button)
    {
        if (button == null) return null;
        return button.GetComponentInChildren<TMP_Text>();
    }

    private void SetButtonText(Button button, string newText)
    {
        if (button == null) return;
        var textComp = FindChildTMPText(button);
        if (textComp == null) return;
        textComp.text = newText;
    }

    private async Task RunAsync(Button contextButton, System.Func<Task<string>> func)
    {
        string originalText = null;
        if (contextButton != null)
        {
            CacheOriginalText(contextButton);
            _originalButtonText.TryGetValue(contextButton, out originalText);
            SetButtonText(contextButton, "Processing...");
        }
        SetAllButtonsInteractable(false);
        try
        {
            string signature = await func();
            var message = string.IsNullOrEmpty(signature) ? "Transaction failed" : $"Tx: {signature}";
            SetStatus(message);
        }
        finally
        {
            if (contextButton != null)
            {
                if (!string.IsNullOrEmpty(originalText))
                {
                    SetButtonText(contextButton, originalText);
                }
            }
            SetAllButtonsInteractable(true);
        }
    }

    private async Task RunFetchAsync<T>(Button contextButton, System.Func<Task<T>> fetch, System.Func<T, string> makeMessage)
    {
        string originalText = null;
        if (contextButton != null)
        {
            CacheOriginalText(contextButton);
            _originalButtonText.TryGetValue(contextButton, out originalText);
            SetButtonText(contextButton, "Loading...");
        }
        SetAllButtonsInteractable(false);
        try
        {
            T result = await fetch();
            string message = makeMessage != null ? makeMessage(result) : (result != null ? "Fetch succeeded" : "Fetch failed");
            SetStatus(message);
        }
        finally
        {
            if (contextButton != null)
            {
                if (!string.IsNullOrEmpty(originalText))
                {
                    SetButtonText(contextButton, originalText);
                }
            }
            SetAllButtonsInteractable(true);
        }
    }

    private void SetAllButtonsInteractable(bool interactable)
    {
        SetInteractable(initializeAuthorityButton, interactable);
        SetInteractable(initializeGameButton, interactable);
        SetInteractable(initializePlayerBetButton, interactable);
        SetInteractable(startGameButton, interactable);
        SetInteractable(delegateAuthorityButton, interactable);
        SetInteractable(delegateGameButton, interactable);
        SetInteractable(delegatePlayerBetButton, interactable);
        SetInteractable(tickButton, interactable);
        SetInteractable(requestRandomnessButton, interactable);
        SetInteractable(placeBetButton, interactable);
        SetInteractable(claimBetButton, interactable);
        SetInteractable(getGameDataButton, interactable);
        SetInteractable(getPlayerBetButton, interactable);
    }

    private void SetStatus(string message)
    {
        if (statusTMPText != null) statusTMPText.text = message;
        else Debug.Log($"CrashUI: {message}");
    }

    private static ulong ParseUlong(InputField input, ulong defaultValue)
    {
        if (input == null || string.IsNullOrWhiteSpace(input.text)) return defaultValue;
        if (ulong.TryParse(input.text, out var result)) return result;
        return defaultValue;
    }

    private static uint ParseUint(InputField input, uint defaultValue)
    {
        if (input == null || string.IsNullOrWhiteSpace(input.text)) return defaultValue;
        if (uint.TryParse(input.text, out var result)) return result;
        return defaultValue;
    }

    private static byte ParseByte(InputField input, byte defaultValue)
    {
        if (input == null || string.IsNullOrWhiteSpace(input.text)) return defaultValue;
        if (byte.TryParse(input.text, out var result)) return result;
        return defaultValue;
    }

    private static PublicKey ParsePublicKey(InputField input)
    {
        if (input == null) return null;
        var value = input.text;
        if (string.IsNullOrWhiteSpace(value)) return null;
        try
        {
            return new PublicKey(value.Trim());
        }
        catch
        {
            return null;
        }
    }
}


