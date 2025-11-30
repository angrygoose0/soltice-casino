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
    [SerializeField] private PlayerProximityCanvas playerProximityCanvas;
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
                activeHandsGroup = seats[i].transform.Find("ActiveHandsGroup")
            };
        }
        
        InitializeActionCanvas();
        
        if (_actionCanvas != null)
            _actionCanvas.gameObject.SetActive(false);
        if (_bufferHandGroup != null)
            _bufferHandGroup.gameObject.SetActive(false);
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
        
        foreach (var kvp in _hands)
        {
            var h = kvp.Value.hand;
            if (!h.Player.Equals(Web3.Account.PublicKey)) continue;
            
            bool isSelected = _selectedActiveHandId.HasValue && h.HandId == _selectedActiveHandId.Value;
            interactableObjects.SetInteractableGlowActive(kvp.Value.prefab, isSelected);
        }
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

        for (int i = 0; i < _seatPlayers.Length; i++)
        {
            if (_seatPlayers[i] != null && _seatPlayers[i].Equals(player))
                return i;
        }
        return -1;
    }
    
    private int GetLocalPlayerActiveSeatIndex()
    {
        if (Web3.Account == null) return -1;
        
        int seatIndex = FindSeatIndexForPlayer(Web3.Account.PublicKey);
        if (seatIndex < 0) return -1;
        
        if (_antedGameNo == null || seatIndex >= _antedGameNo.Length || _antedGameNo[seatIndex] < _gameNo)
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
    }
    
    private string FormatShortAmount(ulong value)
    {
        if (value >= 1_000_000_000) return (value / 1_000_000_000d).ToString("0.#") + "b";
        if (value >= 1_000_000) return (value / 1_000_000d).ToString("0.#") + "m";
        if (value >= 1_000) return (value / 1_000d).ToString("0.#") + "k";
        return value.ToString();
    }

    private void RemoveHand(PublicKey handPk)
    {
        if (!_hands.TryGetValue(handPk, out var entry)) return;
        
        byte removedHandId = entry.hand.HandId;
        bool wasLocalPlayer = Web3.Account != null && entry.hand.Player.Equals(Web3.Account.PublicKey);
        var parent = entry.prefab.transform.parent;
        
        if (wasLocalPlayer)
            interactableObjects?.UnregisterInteractable(entry.prefab);
        
        Destroy(entry.prefab);
        _hands.Remove(handPk);
        
        if (parent != null)
            RepositionHandsInGroup(parent);
        
        if (wasLocalPlayer && _selectedActiveHandId == removedHandId)
        {
            _selectedActiveHandId = null;
            EnsureLowestEligibleHandSelected();
            SyncActionCanvas();
        }
    }

    private BlackJackHand? GetSelectedHand()
    {
        if (_selectedActiveHandId == null || Web3.Account == null) return null;
        
        foreach (var kvp in _hands)
        {
            if (kvp.Value.hand.HandId == _selectedActiveHandId.Value &&
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

        bool hideCards = accountManager.BlackjackGameCache?.DealerCardCount == 1;
        int cardCount = hideCards ? 2 : hand.CardCount;

        for (int i = 0; i < cardCount; i++)
            SpawnCard(cardsGroup, i, hideCards ? (byte)0 : hand.PlayerCards[i]);

        cardsGroup.localPosition = new Vector3(cardCount * 0.075f, cardsGroup.localPosition.y, cardsGroup.localPosition.z);
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

    private void SyncChipsForHand(GameObject handObject, BlackJackHand hand) =>
        SpawnChipsInGroup(handObject.transform.Find("chipGroup"), hand.CurrentBet);

    private void SpawnChipsInGroup(Transform chipGroup, ulong amount)
    {
        if (chipGroup == null || chipPrefab == null) return;

        ClearChildren(chipGroup);

        int totalChips = 0;
        foreach (var (material, count) in GetChipBreakdown(amount))
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

    private void SyncActionCanvas()
    {
        if (_actionCanvas == null) return;
        
        int targetSeatIndex = GetTargetSeatIndex();
        
        if (targetSeatIndex < 0)
        {
            if (_actionCanvas.gameObject.activeSelf)
            {
                _actionCanvas.gameObject.SetActive(false);
                playerProximityCanvas?.RemoveTarget(_actionCanvas);
            }
            return;
        }
        
        var targetSeat = _seats[targetSeatIndex].root.transform;
        if (_actionCanvas.parent != targetSeat)
        {
            _actionCanvas.SetParent(targetSeat);
            _actionCanvas.localPosition = new Vector3(0, 0.5f, -0.5f);
            _actionCanvas.localRotation = Quaternion.identity;
        }
        
        if (!_actionCanvas.gameObject.activeSelf)
        {
            _actionCanvas.gameObject.SetActive(true);
            playerProximityCanvas?.AddTarget(_actionCanvas);
        }
        
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

        buttonGroup.Find("hit")?.gameObject.SetActive(showHitStand);
        buttonGroup.Find("stand")?.gameObject.SetActive(showHitStand);
        buttonGroup.Find("double")?.gameObject.SetActive(showDouble);
        buttonGroup.Find("split")?.gameObject.SetActive(showSplit);
        buttonGroup.Find("accept_insurance")?.gameObject.SetActive(showInsurance);
        buttonGroup.Find("decline_insurance")?.gameObject.SetActive(showInsurance);
        _actionCanvas.Find("infoText")?.gameObject.SetActive(showInsurance);
        buttonGroup.Find("submit_ante")?.gameObject.SetActive(inBettingMode);
        buttonGroup.Find("remove_hand")?.gameObject.SetActive(inBettingMode);
        buttonGroup.Find("add_hand")?.gameObject.SetActive(inBettingMode);
        _actionCanvas.Find("bettingButtonGroup")?.gameObject.SetActive(inBettingMode);
    }

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
        
        foreach (var chip in chipValues)
        {
            if (chip.value == value)
                return chip.material;
        }
        
        return chipValues.OrderBy(c => c.value).First().material;
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
            if (_bufferHandGroup.gameObject.activeSelf)
            {
                _bufferHandGroup.gameObject.SetActive(false);
                playerProximityCanvas?.RemoveTarget(_bufferHandGroup);
            }
            return;
        }

        // Always reparent to correct seat
        var targetParent = _seats[targetSeatIndex].root.transform;
        if (_bufferHandGroup.parent != targetParent)
        {
            _bufferHandGroup.SetParent(targetParent);
            _bufferHandGroup.localPosition = Vector3.zero;
            _bufferHandGroup.localRotation = Quaternion.identity;
        }
        
        // Hide when user has active hands for current game (not in betting mode)
        bool inBettingMode = GetSelectedHand() == null;
        
        if (inBettingMode)
        {
            if (!_bufferHandGroup.gameObject.activeSelf)
            {
                _bufferHandGroup.gameObject.SetActive(true);
                playerProximityCanvas?.AddTarget(_bufferHandGroup);
            }
        }
        else
        {
            if (_bufferHandGroup.gameObject.activeSelf)
            {
                _bufferHandGroup.gameObject.SetActive(false);
                playerProximityCanvas?.RemoveTarget(_bufferHandGroup);
            }
            return;
        }

        // Ensure correct buffer hand count
        bool countChanged = false;
        while (_bufferHands.Count < _anteBuffer.Count)
        {
            var hand = Instantiate(handPrefab, _bufferHandGroup);
            hand.transform.localRotation = Quaternion.identity;
            _bufferHands.Add(hand);
            interactableObjects?.RegisterInteractable(hand, handButtonGlowProfile, InteractableObjects.ActionType.None, true, 0);
            countChanged = true;
        }
        while (_bufferHands.Count > _anteBuffer.Count)
        {
            var hand = _bufferHands[_bufferHands.Count - 1];
            interactableObjects?.UnregisterInteractable(hand);
            Destroy(hand);
            _bufferHands.RemoveAt(_bufferHands.Count - 1);
            countChanged = true;
        }
        
        if (countChanged)
        {
            int count = _bufferHands.Count;
            float totalWidth = (count - 1) * handSpacing;
            float startX = -totalWidth / 2f;
            for (int i = 0; i < count; i++)
                _bufferHands[i].transform.localPosition = new Vector3(startX + i * handSpacing, 0, 0);
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

    private void ClearBufferHands()
    {
        foreach (var hand in _bufferHands)
        {
            interactableObjects?.UnregisterInteractable(hand);
            Destroy(hand);
        }
        _bufferHands.Clear();
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
        SyncActionCanvas();
    }
}
