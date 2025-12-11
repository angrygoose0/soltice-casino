using UnityEngine;
using Blackjack.Accounts;
using Solana.Unity.Wallet;
using Solana.Unity.SDK;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using BlackJackGame = Blackjack.Accounts.BlackJack;

[System.Serializable]
public struct ChipValue
{
    public Material material;
    public ulong value;
}

public class BlackjackTableSimulator : MonoBehaviour
{
    [SerializeField] private OnChainAccountManager accountManager;
    [SerializeField] private UserUI userUI;
    [SerializeField] private InteractableObjects interactableObjects;
    [SerializeField] private PlayerProximityCanvas playerProximityCanvas;
    [SerializeField] private SpringManager springManager;
    [SerializeField] private SolanaManager solanaManager;
    [SerializeField] private string handButtonGlowProfile = "default";
    
    [SerializeField] private List<GameObject> seats = new List<GameObject>();
    [SerializeField] private GameObject handPrefab;
    [SerializeField] private GameObject cardPrefab;

    [Header("Card Meshes (Ace through King, Joker)")]
    [SerializeField] private Mesh[] rankMeshes = new Mesh[14];

    [Header("Suit Materials (Spades, Hearts, Diamonds, Clubs)")]
    [SerializeField] private Material[] suitMaterials = new Material[4];

    [Header("Chip Configuration")]
    [SerializeField] private ChipValue[] chipValues;
    [SerializeField] private GameObject chipPrefab;
    [SerializeField] private float chipStackHeight = 0.05f;
    [SerializeField] private float chipFallHeight = 1.5f;
    [SerializeField] private float chipFallDuration = 0.3f;
    [SerializeField] private float chipFallStagger = 0.05f;
    [SerializeField] private float chipMergeDelay = 0.4f;
    
    [Header("Hand Spacing")]
    [SerializeField] private float handSpacing = 0.5f;
    
    [Header("Betting Button Amounts (in lamports)")]
    [SerializeField] private ulong[] bettingAmounts = new ulong[5] { 100_000_000, 500_000_000, 1_000_000_000, 5_000_000_000, 10_000_000_000 };

    private Transform _dealerCardGroup;
    private Transform _actionCanvas;
    private Transform _bufferHandGroup;
    private SeatData[] _seats = new SeatData[0];
    private Dictionary<PublicKey, (BlackJackHand hand, GameObject prefab)> _hands = new();
    private PublicKey[] _seatPlayers;
    private ulong[] _antedGameNo;
    private ulong _gameNo;

    private static readonly PublicKey DEFAULT_PUBKEY = new PublicKey("11111111111111111111111111111111");

    private List<ulong> _anteBuffer = new List<ulong> { 0 };
    private List<GameObject> _bufferHands = new List<GameObject>();
    private int _selectedBufferHandId = 0;
    private HashSet<byte> _declinedInsurance = new HashSet<byte>();
    private byte? _selectedActiveHandId = null;
    private int _lastTargetSeatIndex = -1;
    
    // Spring keys for smooth position/rotation transitions
    private string[] _actionCanvasSprings = new string[4]; // x, y, z, rotY
    private string[] _bufferHandSprings = new string[4];   // x, y, z, rotY
    private Vector3 _actionCanvasTargetLocal = new Vector3(0, 0.5f, -0.5f);
    private Vector3 _bufferHandTargetLocal = Vector3.zero;
    
    // Track chip group amounts for proper drop-then-merge behavior
    private Dictionary<Transform, ulong> _chipGroupAmounts = new();
    private HashSet<Transform> _chipGroupsPendingMerge = new();
    
    // Springs for buffer hand positions
    private Dictionary<GameObject, string> _bufferHandSpringKeys = new();

    public IReadOnlyList<ulong> AnteBuffer => _anteBuffer;
    public byte? SelectedActiveHandId => _selectedActiveHandId;
    public int SelectedBufferHandId => _selectedBufferHandId;
    public ulong SelectedAnte => _anteBuffer[_selectedBufferHandId];
    public ulong TotalAnte => _anteBuffer.Aggregate(0UL, (sum, a) => sum + a);

    private struct SeatData
    {
        public GameObject root;
        public Transform activeHandsGroup;
    }

    private void Awake()
    {
        _dealerCardGroup = transform.Find("dealerCardGroup");
        _actionCanvas = transform.Find("actionCanvas");
        _bufferHandGroup = transform.Find("bufferHandGroup");
        
        _seats = new SeatData[seats.Count];
        for (int i = 0; i < seats.Count; i++)
        {
            if (seats[i] == null) continue;
            _seats[i] = new SeatData
            {
                root = seats[i],
                activeHandsGroup = seats[i].transform.Find("activeHandsGroup")
            };
        }
        
        InitializeActionCanvas();
        InitializePositionSprings();
        
        if (_bufferHandGroup != null)
            _bufferHandGroup.gameObject.SetActive(false);
    }
    
    private void InitializePositionSprings()
    {
        if (springManager == null) return;
        
        int id = GetInstanceID();
        string[] axes = { "x", "y", "z", "rotY" };
        
        Vector3 actionPos = _actionCanvas != null ? _actionCanvas.position : Vector3.zero;
        float[] actionVals = { actionPos.x, actionPos.y, actionPos.z, _actionCanvas != null ? _actionCanvas.eulerAngles.y : 0f };
        
        Vector3 bufferPos = _bufferHandGroup != null ? _bufferHandGroup.position : Vector3.zero;
        float[] bufferVals = { bufferPos.x, bufferPos.y, bufferPos.z, _bufferHandGroup != null ? _bufferHandGroup.eulerAngles.y : 0f };
        
        for (int i = 0; i < 4; i++)
        {
            _actionCanvasSprings[i] = $"actionCanvas_{axes[i]}_{id}";
            _bufferHandSprings[i] = $"bufferHand_{axes[i]}_{id}";
            springManager.GetSpring(_actionCanvasSprings[i], damping: 0.5f, frequency: 6f, initialValue: actionVals[i]);
            springManager.GetSpring(_bufferHandSprings[i], damping: 0.5f, frequency: 6f, initialValue: bufferVals[i]);
        }
    }

    private void OnEnable()
    {
        if (accountManager == null) return;
        accountManager.OnBlackjackGameUpdated += OnGameUpdated;
        accountManager.OnBlackjackHandUpdated += OnHandUpdated;
        accountManager.OnBlackjackHandRemoved += RemoveHand;
    }

    private void OnDisable()
    {
        if (accountManager == null) return;
        accountManager.OnBlackjackGameUpdated -= OnGameUpdated;
        accountManager.OnBlackjackHandUpdated -= OnHandUpdated;
        accountManager.OnBlackjackHandRemoved -= RemoveHand;
    }

    private void Update()
    {
        int targetSeatIndex = GetTargetSeatIndex();
        if (targetSeatIndex != _lastTargetSeatIndex)
        {
            _lastTargetSeatIndex = targetSeatIndex;
            SyncActionCanvas();
            SyncBufferChips();
        }
        
        ApplyPositionSprings();
    }
    
    private void ApplyPositionSprings()
    {
        if (springManager == null) return;
        
        if (_actionCanvas != null && _actionCanvasSprings[0] != null)
        {
            _actionCanvas.position = new Vector3(
                springManager.GetValue(_actionCanvasSprings[0]),
                springManager.GetValue(_actionCanvasSprings[1]),
                springManager.GetValue(_actionCanvasSprings[2])
            );
            _actionCanvas.rotation = Quaternion.Euler(0, springManager.GetValue(_actionCanvasSprings[3]), 0);
        }
        
        if (_bufferHandGroup != null && _bufferHandSprings[0] != null)
        {
            _bufferHandGroup.position = new Vector3(
                springManager.GetValue(_bufferHandSprings[0]),
                springManager.GetValue(_bufferHandSprings[1]),
                springManager.GetValue(_bufferHandSprings[2])
            );
            _bufferHandGroup.rotation = Quaternion.Euler(0, springManager.GetValue(_bufferHandSprings[3]), 0);
        }
        
        foreach (var hand in _bufferHands)
        {
            if (_bufferHandSpringKeys.TryGetValue(hand, out string springKey))
                hand.transform.localPosition = new Vector3(springManager.GetValue(springKey), 0, 0);
        }
    }
    
    private void MoveSpringGroup(string[] springs, Vector3 pos, float rotY)
    {
        if (springManager == null || springs[0] == null) return;
        springManager.MoveTo(springs[0], pos.x);
        springManager.MoveTo(springs[1], pos.y);
        springManager.MoveTo(springs[2], pos.z);
        springManager.MoveTo(springs[3], rotY);
    }

    private int GetTargetSeatIndex()
    {
        int activeSeatIndex = GetLocalPlayerActiveSeatIndex();
        return activeSeatIndex >= 0 ? activeSeatIndex : FindClosestEligibleSeatIndex();
    }

    private void OnGameUpdated(BlackJackGame game)
    {
        if (_seats.Length > game.MaxPlayers)
            Debug.LogWarning($"Seat count ({_seats.Length}) exceeds game maxPlayers ({game.MaxPlayers}), ignoring excess seats");

        _seatPlayers = game.Players;
        _antedGameNo = game.AntedGameNo;
        
        if (_gameNo != game.GameNo)
        {
            _declinedInsurance.Clear();
            _selectedActiveHandId = null;
        }
        _gameNo = game.GameNo;

        if (_seatPlayers != null && Web3.Account != null && 
            _hands.Values.Any(h => h.hand.Player.Equals(Web3.Account.PublicKey) && h.hand.GameNo == _gameNo))
        {
            EnsureLowestEligibleHandSelected();
        }

        ReparentAllHands();
        SyncDealerCards(game);
        
        foreach (var kvp in _hands)
            SyncCardsForHand(kvp.Value.prefab, kvp.Value.hand);
        
        var selectedHand = GetSelectedHand();
        if (selectedHand != null && !IsHandEligibleForActions(selectedHand, accountManager.BlackjackGameCache))
            EnsureLowestEligibleHandSelected();

        SyncActionCanvas();
    }

    private void OnHandUpdated(PublicKey handPk, BlackJackHand hand, bool isNew)
    {
        bool isInSubscribedGame = accountManager.SubscribedBlackjackPk != null 
            && hand.Blackjack.Equals(accountManager.SubscribedBlackjackPk)
            && (accountManager.BlackjackGameCache == null || hand.GameNo >= accountManager.BlackjackGameCache.GameNo);
        
        if (!isInSubscribedGame)
        {
            RemoveHand(handPk);
            return;
        }

        bool isLocalPlayer = Web3.Account != null && hand.Player.Equals(Web3.Account.PublicKey);

        Debug.Log($"[BJTable] Hand update - PK: {handPk}, GameNo: {hand.GameNo}, CurrentBet: {hand.CurrentBet}, State: {hand.State}, CardCount: {hand.CardCount}, isNew: {!_hands.ContainsKey(handPk)}");
        
        if (_hands.TryGetValue(handPk, out var entry))
        {
            _hands[handPk] = (hand, entry.prefab);
            SyncCardsForHand(entry.prefab, hand);
            SyncChipsForHand(entry.prefab, hand);
            ReparentHand(entry.prefab, hand);
        }
        else
        {
            var parent = GetParentForHand(hand);
            Debug.Log($"[BJTable] Reparenting hand {handPk} to parent {parent}");
            var prefab = Instantiate(handPrefab, parent);
            prefab.transform.localRotation = Quaternion.identity;

            _hands[handPk] = (hand, prefab);
            SyncCardsForHand(prefab, hand);
            SyncChipsForHand(prefab, hand);
            RepositionHandsInGroup(parent);
            
            if (isLocalPlayer)
            {
                interactableObjects?.RegisterInteractable(prefab, handButtonGlowProfile, InteractableObjects.ActionType.None, true, 0);
                EnsureLowestEligibleHandSelected();
                SyncActiveHandGlow();
            }
        }

        if (isLocalPlayer)
            SyncActionCanvas();
    }

    private void EnsureLowestEligibleHandSelected()
    {
        if (Web3.Account == null) return;
        
        var game = accountManager.BlackjackGameCache;
        byte? lowestEligible = null;
        byte? lowestActive = null;
        
        foreach (var kvp in _hands)
        {
            var h = kvp.Value.hand;
            if (!h.Player.Equals(Web3.Account.PublicKey) || h.GameNo != _gameNo) continue;
            
            if (lowestActive == null || h.HandId < lowestActive.Value)
                lowestActive = h.HandId;
            
            if (IsHandEligibleForActions(h, game) && (lowestEligible == null || h.HandId < lowestEligible.Value))
                lowestEligible = h.HandId;
        }
        
        byte? newSelected = lowestEligible ?? lowestActive;
        if (newSelected != _selectedActiveHandId)
        {
            _selectedActiveHandId = newSelected;
            SyncActiveHandGlow();
        }
    }
    
    private void SyncActiveHandGlow()
    {
        if (Web3.Account == null || interactableObjects == null) return;
        
        foreach (var (hand, prefab) in _hands.Values.Where(v => v.hand.Player.Equals(Web3.Account.PublicKey)))
            interactableObjects.SetInteractableGlowActive(prefab, _selectedActiveHandId == hand.HandId);
    }
    
    private bool IsHandEligibleForActions(BlackJackHand hand, BlackJackGame game)
    {
        if (game == null || game.DealerCardCount != 1 || hand.GameNo != _gameNo) return false;
        
        bool insuranceDeclined = _declinedInsurance.Contains(hand.HandId);
        byte effectiveState = (hand.State == 1 && insuranceDeclined) ? (byte)0 : hand.State;
        
        return (effectiveState == 0 && hand.CardCount >= 2) || 
               (hand.State == 1 && !insuranceDeclined && hand.CardCount == 2);
    }

    private int FindSeatIndexForPlayer(PublicKey player)
    {
        if (_seatPlayers == null || player == null) return -1;
        return System.Array.FindIndex(_seatPlayers, p => p != null && p.Equals(player));
    }
    
    private int GetLocalPlayerActiveSeatIndex()
    {
        if (Web3.Account == null) return -1;
        int seatIndex = FindSeatIndexForPlayer(Web3.Account.PublicKey);
        if (seatIndex < 0 || _antedGameNo == null || seatIndex >= _antedGameNo.Length || _antedGameNo[seatIndex] < _gameNo)
            return -1;
        return seatIndex;
    }
    
    private int FindClosestEligibleSeatIndex()
    {
        if (playerProximityCanvas?.currentPlayer == null || _seatPlayers == null) return -1;
        
        Vector3 playerPos = playerProximityCanvas.currentPlayer.position;
        int closestIndex = -1;
        float closestDist = float.MaxValue;
        
        for (int i = 0; i < _seats.Length && i < _seatPlayers.Length; i++)
        {
            if (_seats[i].root == null) continue;
            
            bool isEmpty = _seatPlayers[i] == null || _seatPlayers[i].Equals(DEFAULT_PUBKEY);
            bool hasNotAnted = !isEmpty && _antedGameNo != null && i < _antedGameNo.Length && _antedGameNo[i] < _gameNo;
            
            if (!isEmpty && !hasNotAnted) continue;
            
            float dist = Vector3.Distance(playerPos, _seats[i].root.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closestIndex = i;
            }
        }
        
        return closestIndex;
    }

    private Transform GetParentForHand(BlackJackHand hand)
    {
        int seatIndex = FindSeatIndexForPlayer(hand.Player);
        if (seatIndex >= 0 && seatIndex < _seats.Length && _seats[seatIndex].activeHandsGroup != null)
            return _seats[seatIndex].activeHandsGroup;
        return transform;
    }

    private void InitializeActionCanvas()
    {
        if (interactableObjects == null || _actionCanvas == null) return;

        var buttonActions = new (string name, InteractableObjects.ActionType action, string label)[]
        {
            ("hit", InteractableObjects.ActionType.BlackjackHit, "Hit"),
            ("stand", InteractableObjects.ActionType.BlackjackStand, "Stand"),
            ("double", InteractableObjects.ActionType.BlackjackDouble, "Double"),
            ("split", InteractableObjects.ActionType.BlackjackSplit, "Split"),
            ("accept_insurance", InteractableObjects.ActionType.BlackjackAcceptInsurance, "Accept"),
            ("decline_insurance", InteractableObjects.ActionType.BlackjackDeclineInsurance, "Decline"),
            ("settle_hand", InteractableObjects.ActionType.BlackjackSettleHand, "Settle"),
            ("submit_ante", InteractableObjects.ActionType.SubmitAnte, "Bet: 0"),
            ("remove_hand", InteractableObjects.ActionType.RemoveBufferHand, "x"),
            ("add_hand", InteractableObjects.ActionType.AddBufferHand, "+")
        };

        var buttonGroup = _actionCanvas.Find("buttonGroup");
        if (buttonGroup != null)
        {
            foreach (var (name, action, label) in buttonActions)
            {
                var button = buttonGroup.Find(name);
                if (button == null) continue;
                
                button.gameObject.SetActive(false);
                interactableObjects.RegisterInteractable(button.gameObject, "ui", action, false, 0);
                
                var tmp = button.GetComponentInChildren<TMP_Text>();
                if (tmp != null)
                    tmp.text = label;
            }
        }
        
        var bettingButtonGroup = _actionCanvas.Find("bettingButtonGroup");
        if (bettingButtonGroup != null)
        {
            int count = Mathf.Min(bettingButtonGroup.childCount, bettingAmounts.Length);
            for (int i = 0; i < count; i++)
            {
                var bettingButton = bettingButtonGroup.GetChild(i).gameObject;
                interactableObjects.RegisterInteractable(bettingButton, "default", InteractableObjects.ActionType.IncrementAnte, true, bettingAmounts[i]);
                
                var tmp = bettingButton.GetComponentInChildren<TMP_Text>();
                if (tmp != null)
                    tmp.text = FormatShortAmount(bettingAmounts[i]);
            }
        }
        
        _actionCanvas.Find("infoText")?.gameObject.SetActive(false);
    }
    
    private string FormatShortAmount(ulong lamports)
    {
        ulong display = ConvertToDisplayAmount(lamports);
        if (display >= 1_000_000_000) return (display / 1_000_000_000d).ToString("0.#") + "b";
        if (display >= 1_000_000) return (display / 1_000_000d).ToString("0.#") + "m";
        if (display >= 1_000) return (display / 1_000d).ToString("0.#") + "k";
        return display.ToString();
    }

    private void RemoveHand(PublicKey handPk)
    {
        if (!_hands.TryGetValue(handPk, out var entry)) return;
        
        bool wasLocalPlayer = Web3.Account != null && entry.hand.Player.Equals(Web3.Account.PublicKey);
        bool wasSelected = wasLocalPlayer && _selectedActiveHandId == entry.hand.HandId;
        var parent = entry.prefab.transform.parent;
        
        if (wasLocalPlayer) interactableObjects?.UnregisterInteractable(entry.prefab);
        Destroy(entry.prefab);
        _hands.Remove(handPk);
        if (parent != null) RepositionHandsInGroup(parent);
        
        if (wasSelected)
        {
            _selectedActiveHandId = null;
            EnsureLowestEligibleHandSelected();
            SyncActionCanvas();
        }
    }

    private BlackJackHand? GetSelectedHand()
    {
        if (_selectedActiveHandId == null || Web3.Account == null) return null;
        return _hands.Values
            .Where(v => v.hand.HandId == _selectedActiveHandId.Value && v.hand.Player.Equals(Web3.Account.PublicKey))
            .Select(v => (BlackJackHand?)v.hand)
            .FirstOrDefault();
    }

    private void ReparentHand(GameObject prefab, BlackJackHand hand)
    {
        var targetParent = GetParentForHand(hand);
        if (prefab.transform.parent == targetParent) return;
        
        prefab.transform.SetParent(targetParent);
        prefab.transform.localRotation = Quaternion.identity;
        RepositionHandsInGroup(targetParent);
    }

    private void ReparentAllHands()
    {
        foreach (var (hand, prefab) in _hands.Values)
        {
            var targetParent = GetParentForHand(hand);
            if (prefab.transform.parent == targetParent) continue;
            prefab.transform.SetParent(targetParent);
            prefab.transform.localRotation = Quaternion.identity;
        }
        
        foreach (var seat in _seats.Where(s => s.activeHandsGroup != null))
            RepositionHandsInGroup(seat.activeHandsGroup);
    }
    
    private void RepositionHandsInGroup(Transform group)
    {
        // Don't reposition if group is the table root (would move seats)
        if (group == null || group == transform) return;
        
        int childCount = group.childCount;
        if (childCount == 0) return;
        
        float totalWidth = (childCount - 1) * handSpacing;
        float startX = -totalWidth / 2f;
        
        for (int i = 0; i < childCount; i++)
            group.GetChild(i).localPosition = new Vector3(startX + i * handSpacing, 0, 0);
    }

    private void SyncCardsForHand(GameObject handObject, BlackJackHand hand)
    {
        var cardsGroup = handObject.transform.Find("cardsGroup");
        if (cardsGroup == null) return;

        ClearChildren(cardsGroup);

        if (hand.GameNo != _gameNo)
        {
            cardsGroup.localPosition = new Vector3(0, cardsGroup.localPosition.y, cardsGroup.localPosition.z);
            return;
        }

        bool showFaceDown = accountManager.BlackjackGameCache?.DealerCardCount == 1 && hand.CardCount == 0;
        
        if (showFaceDown)
        {
            for (int i = 0; i < 2; i++)
                SpawnCard(cardsGroup, i, 0);
            cardsGroup.localPosition = new Vector3(2 * 0.075f, cardsGroup.localPosition.y, cardsGroup.localPosition.z);
        }
        else if (hand.CardCount > 0)
        {
            for (int i = 0; i < hand.CardCount; i++)
                SpawnCard(cardsGroup, i, hand.PlayerCards[i]);
            cardsGroup.localPosition = new Vector3(hand.CardCount * 0.075f, cardsGroup.localPosition.y, cardsGroup.localPosition.z);
        }
    }

    private void SyncDealerCards(BlackJackGame game)
    {
        if (_dealerCardGroup == null) return;

        ClearChildren(_dealerCardGroup);

        for (int i = 0; i < game.DealerCardCount; i++)
            SpawnCard(_dealerCardGroup, i, game.DealerCards[i]);

        if (game.DealerCardCount == 1)
            SpawnCard(_dealerCardGroup, 1, 0);
    }

    private void SpawnCard(Transform parent, int index, byte cardNumber)
    {
        var card = Instantiate(cardPrefab, parent);
        card.transform.localPosition = new Vector3(-0.1f * index, 0.025f * index, -0.15f * index);
        card.transform.localScale = new Vector3(2f, 2f, 2f);
        ConfigureCard(card, cardNumber);
    }

    private void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }

    private ulong ConvertToDisplayAmount(ulong rawAmount) =>
        solanaManager != null ? (ulong)(rawAmount / System.Math.Pow(10, solanaManager.TokenDecimals)) : rawAmount;

    private void SyncChipsForHand(GameObject handObject, BlackJackHand hand) =>
        SpawnChipsInGroup(handObject.transform.Find("chipGroup"), hand.CurrentBet);

    private void SpawnChipsInGroup(Transform chipGroup, ulong lamports)
    {
        if (chipGroup == null || chipPrefab == null) return;

        // Convert lamports to display units for chip breakdown (chipValues are in display units)
        ulong amount = ConvertToDisplayAmount(lamports);

        _chipGroupAmounts.TryGetValue(chipGroup, out ulong previousAmount);
        ulong addedAmount = amount > previousAmount ? amount - previousAmount : 0;
        _chipGroupAmounts[chipGroup] = amount;

        // If amount decreased or stayed same, just update to final state immediately
        if (addedAmount == 0)
        {
            ApplyChipBreakdown(chipGroup, amount);
            return;
        }

        // Amount increased - drop chips for the added amount first, then merge
        int currentCount = chipGroup.childCount;
        var addedBreakdown = GetChipBreakdown(addedAmount);
        
        int newChipIndex = 0;
        foreach (var (material, count) in addedBreakdown)
        {
            for (int i = 0; i < count; i++)
            {
                var chip = Instantiate(chipPrefab, chipGroup);
                int stackPos = currentCount + newChipIndex;
                float targetY = stackPos * chipStackHeight;
                float startY = targetY + chipFallHeight;

                chip.transform.localPosition = new Vector3(0, startY, 0);
                chip.transform.localRotation = Quaternion.identity;
                chip.transform.localScale = new Vector3(3f, 3f, 3f);

                var renderer = chip.GetComponentInChildren<MeshRenderer>();
                if (renderer != null && material != null)
                    renderer.material = material;

                StartCoroutine(AnimateChipFall(chip.transform, startY, targetY, newChipIndex * chipFallStagger));
                newChipIndex++;
            }
        }

        // Schedule merge to optimal breakdown after chips land
        if (!_chipGroupsPendingMerge.Contains(chipGroup))
        {
            _chipGroupsPendingMerge.Add(chipGroup);
            StartCoroutine(MergeChipsAfterDelay(chipGroup, amount));
        }
    }

    private void ApplyChipBreakdown(Transform chipGroup, ulong amount)
    {
        var breakdown = GetChipBreakdown(amount);
        
        // Build flat list of materials
        var materials = breakdown.SelectMany(b => Enumerable.Repeat(b.material, b.count)).ToList();
        int targetCount = materials.Count;

        // Adjust chip count
        while (chipGroup.childCount > targetCount)
            DestroyImmediate(chipGroup.GetChild(chipGroup.childCount - 1).gameObject);
        while (chipGroup.childCount < targetCount)
        {
            var chip = Instantiate(chipPrefab, chipGroup);
            chip.transform.localRotation = Quaternion.identity;
            chip.transform.localScale = new Vector3(3f, 3f, 3f);
        }

        // Update materials and positions
        for (int i = 0; i < chipGroup.childCount; i++)
        {
            var child = chipGroup.GetChild(i);
            child.localPosition = new Vector3(0, i * chipStackHeight, 0);
            var renderer = child.GetComponentInChildren<MeshRenderer>();
            if (renderer != null && materials[i] != null)
                renderer.material = materials[i];
        }
    }

    private IEnumerator MergeChipsAfterDelay(Transform chipGroup, ulong amount)
    {
        // Wait for chips to land plus merge delay
        float totalFallTime = chipFallDuration + (chipGroup.childCount * chipFallStagger);
        yield return new WaitForSeconds(totalFallTime + chipMergeDelay);

        _chipGroupsPendingMerge.Remove(chipGroup);
        
        if (chipGroup == null) yield break;
        
        // Get the current amount (may have changed during delay)
        _chipGroupAmounts.TryGetValue(chipGroup, out ulong currentAmount);
        ApplyChipBreakdown(chipGroup, currentAmount);
    }

    private IEnumerator AnimateChipFall(Transform chip, float startY, float targetY, float delay)
    {
        if (delay > 0)
            yield return new WaitForSeconds(delay);

        float elapsed = 0f;
        while (elapsed < chipFallDuration)
        {
            if (chip == null) yield break;
            
            elapsed += Time.deltaTime;
            float t = elapsed / chipFallDuration;
            // Ease out bounce-ish curve for satisfying landing
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            float y = Mathf.Lerp(startY, targetY, eased);
            chip.localPosition = new Vector3(chip.localPosition.x, y, chip.localPosition.z);
            yield return null;
        }

        if (chip != null)
            chip.localPosition = new Vector3(chip.localPosition.x, targetY, chip.localPosition.z);
    }

    private void SyncActionCanvas()
    {
        if (_actionCanvas == null) return;
        
        int targetSeatIndex = GetTargetSeatIndex();
        
        if (targetSeatIndex < 0) return;
        
        // Position relative to target seat without reparenting
        var targetSeat = _seats[targetSeatIndex].root.transform;
        Vector3 targetWorldPos = targetSeat.TransformPoint(_actionCanvasTargetLocal);
        MoveSpringGroup(_actionCanvasSprings, targetWorldPos, targetSeat.eulerAngles.y);
        
        var buttonGroup = _actionCanvas.Find("buttonGroup");
        if (buttonGroup == null) return;

        bool inBettingMode = GetSelectedHand() == null;
        bool showHitStand = false, showDouble = false, showSplit = false, showInsurance = false;
        
        var game = accountManager.BlackjackGameCache;
        var hand = GetSelectedHand();
        
        if (hand != null && game?.DealerCardCount == 1 && hand.GameNo == _gameNo)
        {
            bool insuranceDeclined = _declinedInsurance.Contains(hand.HandId);
            byte effectiveState = (hand.State == 1 && insuranceDeclined) ? (byte)0 : hand.State;

            showHitStand = effectiveState == 0 && hand.CardCount >= 2;
            showDouble = effectiveState == 0 && hand.CardCount == 2;
            showSplit = showDouble && hand.PlayerCards[0] > 0 && hand.PlayerCards[1] > 0
                && (hand.PlayerCards[0] - 1) % 13 == (hand.PlayerCards[1] - 1) % 13;
            showInsurance = hand.State == 1 && !insuranceDeclined && hand.CardCount == 2;
        }

        SetButtonActive(buttonGroup, "hit", showHitStand);
        SetButtonActive(buttonGroup, "stand", showHitStand);
        SetButtonActive(buttonGroup, "double", showDouble);
        SetButtonActive(buttonGroup, "split", showSplit);
        SetButtonActive(buttonGroup, "accept_insurance", showInsurance);
        SetButtonActive(buttonGroup, "decline_insurance", showInsurance);
        SetButtonActive(_actionCanvas, "infoText", showInsurance);
        SetButtonActive(buttonGroup, "submit_ante", inBettingMode);
        SetButtonActive(buttonGroup, "remove_hand", inBettingMode);
        SetButtonActive(buttonGroup, "add_hand", inBettingMode);
        SetButtonActive(_actionCanvas, "bettingButtonGroup", inBettingMode);
    }
    
    private void SetButtonActive(Transform parent, string name, bool active) =>
        parent.Find(name)?.gameObject.SetActive(active);

    private void ConfigureCard(GameObject card, byte cardNumber)
    {
        var meshFilter = card.GetComponent<MeshFilter>();
        var meshRenderer = card.GetComponent<MeshRenderer>();
        if (meshFilter == null || meshRenderer == null) return;

        if (cardNumber == 0)
        {
            if (rankMeshes[13] != null)
                meshFilter.mesh = rankMeshes[13];
            card.transform.localRotation = Quaternion.Euler(180, 0, 0);
            return;
        }

        if (cardNumber < 1 || cardNumber > 52) return;

        int rank = (cardNumber - 1) % 13;
        int suit = (cardNumber - 1) / 13;

        if (rankMeshes[rank] != null)
            meshFilter.mesh = rankMeshes[rank];

        if (suitMaterials[suit] != null)
            meshRenderer.material = suitMaterials[suit];
    }

    public List<(Material material, int count)> GetChipBreakdown(ulong totalValue)
    {
        var result = new List<(Material, int)>();
        if (chipValues == null || chipValues.Length == 0 || totalValue == 0)
            return result;

        var sorted = chipValues.OrderByDescending(c => c.value).ToArray();
        ulong remaining = totalValue;

        foreach (var chip in sorted)
        {
            if (chip.value == 0) continue;
            int count = (int)(remaining / chip.value);
            if (count > 0)
            {
                result.Add((chip.material, count));
                remaining -= (ulong)count * chip.value;
            }
        }

        return result;
    }

    public Material GetChipMaterial(ulong value)
    {
        if (chipValues == null || chipValues.Length == 0) return null;
        return chipValues.FirstOrDefault(c => c.value == value).material 
            ?? chipValues.OrderBy(c => c.value).First().material;
    }

    public void IncrementSelectedAnte(ulong amount)
    {
        _anteBuffer[_selectedBufferHandId] += amount;
        SyncBufferChips();
    }

    private void SyncBufferChips()
    {
        if (chipPrefab == null || _bufferHandGroup == null) return;

        int targetSeatIndex = GetTargetSeatIndex();
        
        if (targetSeatIndex < 0 || targetSeatIndex >= _seats.Length)
        {
            _bufferHandGroup.gameObject.SetActive(false);
            return;
        }

        // Position relative to target seat without reparenting
        var targetSeat = _seats[targetSeatIndex].root.transform;
        MoveSpringGroup(_bufferHandSprings, targetSeat.TransformPoint(_bufferHandTargetLocal), targetSeat.eulerAngles.y);
        
        // Hide when user has active hands for current game (not in betting mode)
        bool inBettingMode = GetSelectedHand() == null;
        _bufferHandGroup.gameObject.SetActive(inBettingMode);
        if (!inBettingMode) return;

        // Ensure correct buffer hand count
        float edgeX = _bufferHands.Count > 0 ? handSpacing : 0;
        while (_bufferHands.Count < _anteBuffer.Count)
        {
            var hand = Instantiate(handPrefab, _bufferHandGroup);
            hand.transform.localRotation = Quaternion.identity;
            _bufferHands.Add(hand);
            interactableObjects?.RegisterInteractable(hand, handButtonGlowProfile, InteractableObjects.ActionType.None, true, 0);
            
            if (springManager != null)
            {
                string springKey = $"bufferHand_{hand.GetInstanceID()}_x";
                _bufferHandSpringKeys[hand] = springKey;
                springManager.GetSpring(springKey, damping: 0.5f, frequency: 5f, initialValue: edgeX);
            }
        }
        while (_bufferHands.Count > _anteBuffer.Count)
            RemoveLastBufferHand();
        
        // Update target positions using springs for smooth repositioning
        float totalWidth = (_bufferHands.Count - 1) * handSpacing;
        float startX = -totalWidth / 2f;
        for (int i = 0; i < _bufferHands.Count; i++)
        {
            float targetX = startX + i * handSpacing;
            if (_bufferHandSpringKeys.TryGetValue(_bufferHands[i], out string springKey))
                springManager?.MoveTo(springKey, targetX);
            else
                _bufferHands[i].transform.localPosition = new Vector3(targetX, 0, 0);
        }

        for (int h = 0; h < _bufferHands.Count; h++)
            SpawnChipsInGroup(_bufferHands[h].transform.Find("chipGroup"), _anteBuffer[h]);
    }

    public void AddBufferHand()
    {
        _anteBuffer.Add(_anteBuffer[_selectedBufferHandId]);
        _selectedBufferHandId = _anteBuffer.Count - 1;
        SyncBufferChips();
    }

    public void RemoveBufferHand()
    {
        if (_anteBuffer.Count > 1)
        {
            _anteBuffer.RemoveAt(_selectedBufferHandId);
            if (_selectedBufferHandId >= _anteBuffer.Count)
                _selectedBufferHandId = _anteBuffer.Count - 1;
            SyncBufferChips();
        }
        else
        {
            _anteBuffer[0] = 0;
            ClearBufferHands();
        }
    }

    public void RemoveAllBufferHands()
    {
        _anteBuffer.Clear();
        _anteBuffer.Add(0);
        _selectedBufferHandId = 0;
        ClearBufferHands();
    }

    private void RemoveLastBufferHand()
    {
        var hand = _bufferHands[_bufferHands.Count - 1];
        interactableObjects?.UnregisterInteractable(hand);
        if (_bufferHandSpringKeys.TryGetValue(hand, out string springKey))
        {
            springManager?.RemoveSpring(springKey);
            _bufferHandSpringKeys.Remove(hand);
        }
        Destroy(hand);
        _bufferHands.RemoveAt(_bufferHands.Count - 1);
    }

    private void ClearBufferHands()
    {
        while (_bufferHands.Count > 0)
            RemoveLastBufferHand();
    }

    public void SubmitAnte()
    {
        var betAmounts = _anteBuffer.Where(a => a > 0).ToArray();
        if (betAmounts.Length == 0) return;
        
        int targetSeatIndex = GetTargetSeatIndex();
        if (targetSeatIndex < 0) return;
        
        userUI.Ante((byte)targetSeatIndex, betAmounts);
        RemoveAllBufferHands();
    }

    public void DeclineInsurance(byte handId)
    {
        _declinedInsurance.Add(handId);
        SyncActionCanvas();
    }
}
