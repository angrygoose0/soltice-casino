using UnityEngine;
using Blackjack.Accounts;
using Solana.Unity.Wallet;
using Solana.Unity.SDK;
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
    [SerializeField] private float chipStackHeight = 0.025f;
    
    [Header("Hand Spacing")]
    [SerializeField] private float handSpacing = 0.5f;
    
    [Header("Betting Button Amounts")]
    [SerializeField] private ulong[] bettingAmounts = new ulong[5] { 100, 500, 1000, 5000, 10000 };

    private Transform _dealerCardGroup;
    private SeatData[] _seats = new SeatData[0];
    private Dictionary<PublicKey, (BlackJackHand hand, GameObject prefab)> _hands = new();
    private PublicKey[] _seatPlayers;
    private ulong[] _antedGameNo;
    private ulong _gameNo;

    private static readonly PublicKey DEFAULT_PUBKEY = new PublicKey("11111111111111111111111111111111");

    // Ante buffer for multiple hands
    private List<ulong> _anteBuffer = new List<ulong> { 0 };
    private List<GameObject> _bufferHands = new List<GameObject>();
    private int _selectedBufferHandId = 0;
    
    // Track hands that have declined insurance (by handId)
    private HashSet<byte> _declinedInsurance = new HashSet<byte>();

    // Selected active hand for gameplay actions
    private byte? _selectedActiveHandId = null;

    public IReadOnlyList<ulong> AnteBuffer => _anteBuffer;
    public byte? SelectedActiveHandId => _selectedActiveHandId;
    public int SelectedBufferHandId => _selectedBufferHandId;
    public ulong SelectedAnte => _anteBuffer[_selectedBufferHandId];
    public ulong TotalAnte => _anteBuffer.Aggregate(0UL, (sum, a) => sum + a);

    private void Awake()
    {
        _dealerCardGroup = transform.Find("dealerCardGroup");
        InitializeSeats();
        InitializeActionCanvas();
    }

    private void InitializeSeats()
    {
        _seats = new SeatData[seats.Count];
        
        for (int i = 0; i < seats.Count; i++)
        {
            var root = seats[i];
            if (root == null) continue;
            
            var beforeBetting = root.transform.Find("BeforeBettingGroup");
            var activeHands = root.transform.Find("ActiveHandsGroup");
            var bufferHands = beforeBetting?.Find("bufferHandGroup");
            var actionCanvas = root.transform.Find("actionCanvas");
            
            _seats[i] = new SeatData
            {
                root = root,
                beforeBettingGroup = beforeBetting?.gameObject,
                activeHandsGroup = activeHands,
                bufferHandGroup = bufferHands,
                actionCanvas = actionCanvas
            };
        }
    }

    private struct SeatData
    {
        public GameObject root;
        public GameObject beforeBettingGroup;
        public Transform activeHandsGroup;
        public Transform bufferHandGroup;
        public Transform actionCanvas;
    }

    private void OnEnable()
    {
        if (accountManager != null)
        {
            accountManager.OnBlackjackGameUpdated += OnGameUpdated;
            accountManager.OnBlackjackHandUpdated += OnHandUpdated;
            accountManager.OnBlackjackHandRemoved += OnHandRemoved;
        }
    }

    private void OnDisable()
    {
        if (accountManager != null)
        {
            accountManager.OnBlackjackGameUpdated -= OnGameUpdated;
            accountManager.OnBlackjackHandUpdated -= OnHandUpdated;
            accountManager.OnBlackjackHandRemoved -= OnHandRemoved;
        }
    }

    private void OnGameUpdated(BlackJackGame game)
    {
        if (_seats.Length > game.MaxPlayers)
            Debug.LogWarning($"Seat count ({_seats.Length}) exceeds game maxPlayers ({game.MaxPlayers}), ignoring excess seats");

        _seatPlayers = game.Players;
        _antedGameNo = game.AntedGameNo;
        
        // Clear state when game number changes
        if (_gameNo != game.GameNo)
        {
            _declinedInsurance.Clear();
            _selectedActiveHandId = null;
        }
        
        _gameNo = game.GameNo;

        UpdateSeatPlayerStates();
        ReparentAllHands();
        SyncDealerCards(game);
        SyncAllHandCards();
    }

    private void SyncAllHandCards()
    {
        foreach (var kvp in _hands)
            SyncCardsForHand(kvp.Value.prefab, kvp.Value.hand);
        
        SyncAllActionCanvases();
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
            var prefab = Instantiate(handPrefab, parent);
            prefab.transform.localRotation = Quaternion.identity;

            _hands[handPk] = (hand, prefab);
            SyncCardsForHand(prefab, hand);
            SyncChipsForHand(prefab, hand);
            RepositionHandsInGroup(parent);
            
            // Register local player's hands as selectable
            if (isLocalPlayer)
            {
                interactableObjects?.RegisterInteractable(
                    prefab, 
                    handButtonGlowProfile, 
                    InteractableObjects.ActionType.BlackjackSelectActiveHand, 
                    true, 
                    hand.HandId
                );
                
                EnsureLowestEligibleHandSelected();
            }
        }

        if (isLocalPlayer)
            SyncLocalPlayerActionCanvas();
    }

    private void OnHandRemoved(PublicKey handPk) => RemoveHand(handPk);
    
    private void SyncLocalPlayerActionCanvas()
    {
        int seatIndex = Web3.Account != null ? FindSeatIndexForPlayer(Web3.Account.PublicKey) : -1;
        if (seatIndex >= 0)
            SyncActionCanvas(seatIndex);
    }

    private void UpdateSeatPlayerStates()
    {
        if (_seatPlayers == null) return;

        bool localPlayerHasActiveHands = false;

        for (int i = 0; i < _seats.Length && i < _seatPlayers.Length; i++)
        {
            bool isDefaultPubkey = _seatPlayers[i] == null || _seatPlayers[i].Equals(DEFAULT_PUBKEY);
            bool hasActiveHandsForCurrentGame = false;
            
            if (!isDefaultPubkey)
            {
                bool isLocalPlayer = Web3.Account != null && _seatPlayers[i].Equals(Web3.Account.PublicKey);
                
                if (isLocalPlayer)
                {
                    hasActiveHandsForCurrentGame = _hands.Values.Any(h => h.hand.Player.Equals(Web3.Account.PublicKey) && h.hand.GameNo == _gameNo);
                    localPlayerHasActiveHands = hasActiveHandsForCurrentGame;
                }
                else
                    hasActiveHandsForCurrentGame = _antedGameNo != null && i < _antedGameNo.Length && _antedGameNo[i] >= _gameNo;
            }
            
            bool showBeforeBetting = isDefaultPubkey || !hasActiveHandsForCurrentGame;
            
            if (_seats[i].beforeBettingGroup != null) 
                _seats[i].beforeBettingGroup.SetActive(showBeforeBetting);
        }
        
        if (localPlayerHasActiveHands)
            EnsureLowestEligibleHandSelected();
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
        
        // Prefer eligible hand, fallback to any active hand
        _selectedActiveHandId = lowestEligible ?? lowestActive;
    }
    
    private bool IsHandEligibleForActions(BlackJackHand hand, BlackJackGame game)
    {
        if (game == null || game.DealerCardCount != 1 || hand.GameNo != _gameNo) return false;
        
        bool insuranceDeclined = _declinedInsurance.Contains(hand.HandId);
        byte effectiveState = (hand.State == 1 && insuranceDeclined) ? (byte)0 : hand.State;
        
        bool canHitStand = effectiveState == 0 && hand.CardCount >= 2;
        bool canInsurance = hand.State == 1 && !insuranceDeclined && hand.CardCount == 2;
        
        return canHitStand || canInsurance;
    }

    private int FindSeatIndexForPlayer(PublicKey player)
    {
        if (_seatPlayers == null || player == null) return -1;

        for (int i = 0; i < _seatPlayers.Length; i++)
        {
            if (_seatPlayers[i] != null && _seatPlayers[i].Equals(player))
                return i;
        }
        return -1;
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
        if (interactableObjects == null) return;

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
            ("remove_hand", InteractableObjects.ActionType.RemoveBufferHand, "x")
        };

        foreach (var seat in _seats)
        {
            if (seat.actionCanvas == null) continue;
            
            var buttonGroup = seat.actionCanvas.Find("buttonGroup");
            if (buttonGroup != null)
            {
                foreach (var (name, action, label) in buttonActions)
                {
                    var button = buttonGroup.Find(name);
                    if (button == null) continue;
                    
                    button.gameObject.SetActive(false);
                    interactableObjects.RegisterInteractable(button.gameObject, handButtonGlowProfile, action, false, 0);
                    
                    var tmp = button.GetComponentInChildren<TMP_Text>();
                    if (tmp != null)
                        tmp.text = label;
                }
            }
            
            // Register betting buttons from bettingButtonGroup
            var bettingButtonGroup = seat.actionCanvas.Find("bettingButtonGroup");
            if (bettingButtonGroup != null)
            {
                int count = Mathf.Min(bettingButtonGroup.childCount, bettingAmounts.Length);
                for (int i = 0; i < count; i++)
                {
                    var bettingButton = bettingButtonGroup.GetChild(i).gameObject;
                    interactableObjects.RegisterInteractable(bettingButton, handButtonGlowProfile, InteractableObjects.ActionType.IncrementAnte, true, bettingAmounts[i]);
                    
                    var tmp = bettingButton.GetComponentInChildren<TMP_Text>();
                    if (tmp != null)
                        tmp.text = FormatShortAmount(bettingAmounts[i]);
                }
            }
        }
    }
    
    private string FormatShortAmount(ulong value)
    {
        const double Thousand = 1_000d;
        const double Million = 1_000_000d;
        const double Billion = 1_000_000_000d;

        if (value >= (ulong)Billion) return (value / Billion).ToString("0.#") + "b";
        if (value >= (ulong)Million) return (value / Million).ToString("0.#") + "m";
        if (value >= (ulong)Thousand) return (value / Thousand).ToString("0.#") + "k";
        return value.ToString();
    }

    private void RemoveHand(PublicKey handPk)
    {
        if (!_hands.TryGetValue(handPk, out var entry)) return;
        
        byte removedHandId = entry.hand.HandId;
        bool wasLocalPlayer = Web3.Account != null && entry.hand.Player.Equals(Web3.Account.PublicKey);
        var parent = entry.prefab.transform.parent;
        
        // Unregister hand from interactables if it was local player's
        if (wasLocalPlayer)
            interactableObjects?.UnregisterInteractable(entry.prefab);
        
        Destroy(entry.prefab);
        _hands.Remove(handPk);
        
        // Reposition remaining hands
        if (parent != null)
            RepositionHandsInGroup(parent);
        
        // If removed hand was selected, select the lowest active hand
        if (wasLocalPlayer && _selectedActiveHandId == removedHandId)
        {
            _selectedActiveHandId = null;
            EnsureLowestEligibleHandSelected();
            SyncLocalPlayerActionCanvas();
        }
    }

    public void SelectActiveHand(byte handId)
    {
        // Verify this hand belongs to the local player and exists
        foreach (var kvp in _hands)
        {
            if (kvp.Value.hand.HandId == handId && 
                Web3.Account != null && 
                kvp.Value.hand.Player.Equals(Web3.Account.PublicKey))
            {
                _selectedActiveHandId = handId;
                SyncLocalPlayerActionCanvas();
                return;
            }
        }
    }

    private BlackJackHand? GetSelectedHand()
    {
        if (_selectedActiveHandId == null) return null;
        
        foreach (var kvp in _hands)
        {
            if (kvp.Value.hand.HandId == _selectedActiveHandId.Value &&
                Web3.Account != null &&
                kvp.Value.hand.Player.Equals(Web3.Account.PublicKey))
            {
                return kvp.Value.hand;
            }
        }
        return null;
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
        foreach (var kvp in _hands)
        {
            var targetParent = GetParentForHand(kvp.Value.hand);
            if (kvp.Value.prefab.transform.parent != targetParent)
            {
                kvp.Value.prefab.transform.SetParent(targetParent);
                kvp.Value.prefab.transform.localRotation = Quaternion.identity;
            }
        }
        
        // Reposition hands in each active hands group
        foreach (var seat in _seats)
        {
            if (seat.activeHandsGroup != null)
                RepositionHandsInGroup(seat.activeHandsGroup);
        }
    }
    
    private void RepositionHandsInGroup(Transform group)
    {
        int childCount = group.childCount;
        if (childCount == 0) return;
        
        float totalWidth = (childCount - 1) * handSpacing;
        float startX = -totalWidth / 2f;
        
        for (int i = 0; i < childCount; i++)
        {
            var child = group.GetChild(i);
            child.localPosition = new Vector3(startX + i * handSpacing, 0, 0);
        }
    }
    
    private void RepositionBufferHands()
    {
        int count = _bufferHands.Count;
        if (count == 0) return;
        
        float totalWidth = (count - 1) * handSpacing;
        float startX = -totalWidth / 2f;
        
        for (int i = 0; i < count; i++)
            _bufferHands[i].transform.localPosition = new Vector3(startX + i * handSpacing, 0, 0);
    }

    private Vector3 GetCardOffset(int index) => new Vector3(-0.1f * index, 0.025f * index, -0.15f * index);

    private void SyncCardsForHand(GameObject handObject, BlackJackHand hand)
    {
        var cardsGroup = handObject.transform.Find("cardsGroup");
        if (cardsGroup == null) return;

        for (int i = cardsGroup.childCount - 1; i >= 0; i--)
            Destroy(cardsGroup.GetChild(i).gameObject);

        if (hand.GameNo != _gameNo)
        {
            cardsGroup.localPosition = new Vector3(0, cardsGroup.localPosition.y, cardsGroup.localPosition.z);
            return;
        }

        bool hideCards = accountManager.BlackjackGameCache?.DealerCardCount == 1;
        int cardCount;

        if (hideCards)
        {
            cardCount = 2;
            for (int i = 0; i < cardCount; i++)
            {
                var card = Instantiate(cardPrefab, cardsGroup);
                card.transform.localPosition = GetCardOffset(i);
                card.transform.localScale = new Vector3(2f, 2f, 2f);
                ConfigureCard(card, 0);
            }
        }
        else
        {
            cardCount = hand.CardCount;
            for (int i = 0; i < cardCount; i++)
            {
                var card = Instantiate(cardPrefab, cardsGroup);
                card.transform.localPosition = GetCardOffset(i);
                card.transform.localScale = new Vector3(2f, 2f, 2f);
                ConfigureCard(card, hand.PlayerCards[i]);
            }
        }

        cardsGroup.localPosition = new Vector3(cardCount * 0.075f, cardsGroup.localPosition.y, cardsGroup.localPosition.z);
    }

    private void SyncDealerCards(BlackJackGame game)
    {
        if (_dealerCardGroup == null) return;

        for (int i = _dealerCardGroup.childCount - 1; i >= 0; i--)
            Destroy(_dealerCardGroup.GetChild(i).gameObject);

        for (int i = 0; i < game.DealerCardCount; i++)
        {
            var card = Instantiate(cardPrefab, _dealerCardGroup);
            card.transform.localPosition = GetCardOffset(i);
            card.transform.localScale = new Vector3(2f, 2f, 2f);
            ConfigureCard(card, game.DealerCards[i]);
        }

        if (game.DealerCardCount == 1)
        {
            var hiddenCard = Instantiate(cardPrefab, _dealerCardGroup);
            hiddenCard.transform.localPosition = GetCardOffset(1);
            hiddenCard.transform.localScale = new Vector3(2f, 2f, 2f);
            ConfigureCard(hiddenCard, 0);
        }
    }

    private void SyncChipsForHand(GameObject handObject, BlackJackHand hand)
    {
        var chipGroup = handObject.transform.Find("chipGroup");
        if (chipGroup == null || chipPrefab == null) return;

        for (int i = chipGroup.childCount - 1; i >= 0; i--)
            Destroy(chipGroup.GetChild(i).gameObject);

        var breakdown = GetChipBreakdown(hand.CurrentBet);
        int totalChips = 0;

        foreach (var (material, count) in breakdown)
        {
            for (int i = 0; i < count; i++)
            {
                var chip = Instantiate(chipPrefab, chipGroup);
                chip.transform.localPosition = new Vector3(0, totalChips * chipStackHeight, 0);
                chip.transform.localRotation = Quaternion.identity;
                chip.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);

                var renderer = chip.GetComponentInChildren<MeshRenderer>();
                if (renderer != null && material != null)
                    renderer.material = material;

                totalChips++;
            }
        }
    }

    private void SyncAllActionCanvases()
    {
        // If selected hand is not eligible, try to select a better one
        var hand = GetSelectedHand();
        if (hand.HasValue && !IsHandEligibleForActions(hand.Value, accountManager.BlackjackGameCache))
        {
            var prevSelected = _selectedActiveHandId;
            EnsureLowestEligibleHandSelected();
        }

        for (int i = 0; i < _seats.Length; i++)
            SyncActionCanvas(i);
    }

    private void SyncActionCanvas(int seatIndex)
    {
        if (seatIndex < 0 || seatIndex >= _seats.Length) return;
        
        var actionCanvas = _seats[seatIndex].actionCanvas;
        var buttonGroup = actionCanvas?.Find("buttonGroup");
        if (buttonGroup == null) return;

        bool isDefaultPubkey = _seatPlayers == null || seatIndex >= _seatPlayers.Length 
            || _seatPlayers[seatIndex] == null || _seatPlayers[seatIndex].Equals(DEFAULT_PUBKEY);
        bool isLocalPlayer = !isDefaultPubkey && Web3.Account != null 
            && _seatPlayers[seatIndex].Equals(Web3.Account.PublicKey);

        // Determine if seat is in betting mode
        bool inBettingMode;
        if (isDefaultPubkey)
            inBettingMode = true;
        else if (isLocalPlayer)
            inBettingMode = !GetSelectedHand().HasValue;
        else
            inBettingMode = _antedGameNo == null || seatIndex >= _antedGameNo.Length || _antedGameNo[seatIndex] < _gameNo;

        // Gameplay buttons only for local player with active hand
        bool showHitStand = false, showDouble = false, showSplit = false, showInsurance = false;
        var game = accountManager.BlackjackGameCache;
        var hand = GetSelectedHand();
        
        if (isLocalPlayer && hand.HasValue && game?.DealerCardCount == 1 && hand.Value.GameNo == _gameNo)
        {
            var h = hand.Value;
            bool insuranceDeclined = _declinedInsurance.Contains(h.HandId);
            byte effectiveState = (h.State == 1 && insuranceDeclined) ? (byte)0 : h.State;

            showHitStand = effectiveState == 0 && h.CardCount >= 2;
            showDouble = effectiveState == 0 && h.CardCount == 2;
            showSplit = showDouble && h.PlayerCards[0] > 0 && h.PlayerCards[1] > 0
                && (h.PlayerCards[0] - 1) % 13 == (h.PlayerCards[1] - 1) % 13;
            showInsurance = h.State == 1 && !insuranceDeclined && h.CardCount == 2;
        }

        SetChildActive(buttonGroup, "hit", showHitStand);
        SetChildActive(buttonGroup, "stand", showHitStand);
        SetChildActive(buttonGroup, "double", showDouble);
        SetChildActive(buttonGroup, "split", showSplit);
        SetChildActive(buttonGroup, "accept_insurance", showInsurance);
        SetChildActive(buttonGroup, "decline_insurance", showInsurance);
        SetChildActive(actionCanvas, "infoText", showInsurance);
        SetChildActive(buttonGroup, "submit_ante", inBettingMode);
        SetChildActive(buttonGroup, "remove_hand", inBettingMode);
        SetChildActive(actionCanvas, "bettingButtonGroup", inBettingMode);
    }

    private void SetChildActive(Transform parent, string name, bool active) => 
        parent.Find(name)?.gameObject.SetActive(active);

    private void ConfigureCard(GameObject card, byte cardNumber)
    {
        var cardMesh = card.transform.GetChild(0);
        if (cardMesh == null) return;

        var meshFilter = cardMesh.GetComponent<MeshFilter>();
        var meshRenderer = cardMesh.GetComponent<MeshRenderer>();
        if (meshFilter == null || meshRenderer == null) return;

        if (cardNumber == 0)
        {
            if (rankMeshes[13] != null)
                meshFilter.mesh = rankMeshes[13];
            cardMesh.localRotation = Quaternion.Euler(0, 180, 0);
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

    /// <summary>
    /// Breaks down a total value into chip denominations (greedy, largest first).
    /// Returns list of (material, count) pairs.
    /// </summary>
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
        
        // Exact match first
        foreach (var chip in chipValues)
        {
            if (chip.value == value)
                return chip.material;
        }
        
        // Otherwise return smallest chip material
        return chipValues.OrderBy(c => c.value).First().material;
    }

    public void ConfigureChip(GameObject chip, ulong value)
    {
        var renderer = chip.GetComponentInChildren<MeshRenderer>();
        if (renderer != null)
            renderer.material = GetChipMaterial(value);
    }

    // Ante buffer methods
    public void IncrementSelectedAnte(ulong amount)
    {
        _anteBuffer[_selectedBufferHandId] += amount;
        SyncBufferChips();
    }

    private void SyncBufferChips()
    {
        if (chipPrefab == null) return;

        int seatIndex = Web3.Account != null ? FindSeatIndexForPlayer(Web3.Account.PublicKey) : -1;
        if (seatIndex < 0 || seatIndex >= _seats.Length) return;

        Transform bufferHandGroup = _seats[seatIndex].bufferHandGroup;
        if (bufferHandGroup == null) return;

        // Ensure we have the right number of buffer hands
        bool countChanged = false;
        while (_bufferHands.Count < _anteBuffer.Count)
        {
            var hand = Instantiate(handPrefab, bufferHandGroup);
            hand.transform.localRotation = Quaternion.identity;
            _bufferHands.Add(hand);
            countChanged = true;
        }
        while (_bufferHands.Count > _anteBuffer.Count)
        {
            Destroy(_bufferHands[_bufferHands.Count - 1]);
            _bufferHands.RemoveAt(_bufferHands.Count - 1);
            countChanged = true;
        }
        
        if (countChanged)
            RepositionBufferHands();

        // Sync chips for each buffer hand
        for (int h = 0; h < _bufferHands.Count; h++)
        {
            var chipGroup = _bufferHands[h].transform.Find("chipGroup");
            if (chipGroup == null) continue;

            // Clear existing chips
            for (int i = chipGroup.childCount - 1; i >= 0; i--)
                Destroy(chipGroup.GetChild(i).gameObject);

            // Spawn chips for this hand's ante
            var breakdown = GetChipBreakdown(_anteBuffer[h]);
            int totalChips = 0;

            foreach (var (material, count) in breakdown)
            {
                for (int i = 0; i < count; i++)
                {
                    var chip = Instantiate(chipPrefab, chipGroup);
                    chip.transform.localPosition = new Vector3(0, totalChips * chipStackHeight, 0);
                    chip.transform.localRotation = Quaternion.identity;
                    chip.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);

                    var renderer = chip.GetComponentInChildren<MeshRenderer>();
                    if (renderer != null && material != null)
                        renderer.material = material;

                    totalChips++;
                }
            }
        }
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

    private void ClearBufferHands()
    {
        foreach (var hand in _bufferHands)
            Destroy(hand);
        _bufferHands.Clear();
    }

    public void SelectBufferHand(int index)
    {
        if (index >= 0 && index < _anteBuffer.Count)
            _selectedBufferHandId = index;
    }

    public void SubmitAnte()
    {
        var betAmounts = _anteBuffer.Where(a => a > 0).ToArray();
        if (betAmounts.Length == 0) return;
        
        userUI.Ante(betAmounts);
        RemoveAllBufferHands();
    }

    public void DeclineInsurance(byte handId)
    {
        _declinedInsurance.Add(handId);
        SyncLocalPlayerActionCanvas();
    }
}
