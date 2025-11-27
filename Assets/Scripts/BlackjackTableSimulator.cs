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
    private int _selectedHandIndex = 0;
    
    // Track hands that have declined insurance (by handId)
    private HashSet<byte> _declinedInsurance = new HashSet<byte>();

    public IReadOnlyList<ulong> AnteBuffer => _anteBuffer;
    public int SelectedHandIndex => _selectedHandIndex;
    public ulong SelectedAnte => _anteBuffer[_selectedHandIndex];

    private void Awake()
    {
        _dealerCardGroup = transform.Find("dealerCardGroup");
        InitializeSeats();
    }

    private void InitializeSeats()
    {
        _seats = new SeatData[seats.Count];
        
        for (int i = 0; i < seats.Count; i++)
        {
            var root = seats[i];
            if (root == null) continue;
            
            var beforeBetting = root.transform.Find("BeforeBettingGroup");
            var afterBetting = root.transform.Find("AfterBettingGroup");
            var activeHands = root.transform.Find("ActiveHandsGroup");
            var buffer = beforeBetting?.Find("bufferGroup");
            
            _seats[i] = new SeatData
            {
                root = root,
                beforeBettingGroup = beforeBetting?.gameObject,
                afterBettingGroup = afterBetting?.gameObject,
                activeHandsGroup = activeHands,
                bufferGroup = buffer
            };
        }
    }

    private struct SeatData
    {
        public GameObject root;
        public GameObject beforeBettingGroup;
        public GameObject afterBettingGroup;
        public Transform activeHandsGroup;
        public Transform bufferGroup;
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
        
        // Clear declined insurance when game number changes
        if (_gameNo != game.GameNo)
            _declinedInsurance.Clear();
        
        _gameNo = game.GameNo;

        UpdateSeatPlayerStates();
        ReparentAllHands();
        SyncDealerCards(game);
        SyncAllHandCards();
    }

    private void SyncAllHandCards()
    {
        foreach (var kvp in _hands)
        {
            SyncCardsForHand(kvp.Value.prefab, kvp.Value.hand);
            SyncButtonsForHand(kvp.Value.prefab, kvp.Value.hand);
        }
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

        if (_hands.TryGetValue(handPk, out var entry))
        {
            _hands[handPk] = (hand, entry.prefab);
            SyncCardsForHand(entry.prefab, hand);
            SyncChipsForHand(entry.prefab, hand);
            SyncButtonsForHand(entry.prefab, hand);
            ReparentHand(entry.prefab, hand);
        }
        else
        {
            var parent = GetParentForHand(hand);
            var prefab = Instantiate(handPrefab, parent);
            prefab.transform.localPosition = Vector3.zero;
            prefab.transform.localRotation = Quaternion.identity;

            InitializeHandButtons(prefab, hand.HandId);

            _hands[handPk] = (hand, prefab);
            SyncCardsForHand(prefab, hand);
            SyncChipsForHand(prefab, hand);
            SyncButtonsForHand(prefab, hand);
        }
    }

    private void OnHandRemoved(PublicKey handPk) => RemoveHand(handPk);

    private void UpdateSeatPlayerStates()
    {
        if (_seatPlayers == null) return;

        for (int i = 0; i < _seats.Length && i < _seatPlayers.Length; i++)
        {
            bool isDefaultPubkey = _seatPlayers[i] == null || _seatPlayers[i].Equals(DEFAULT_PUBKEY);
            bool hasActiveHandsForCurrentGame = false;
            
            if (!isDefaultPubkey)
            {
                bool isLocalPlayer = Web3.Account != null && _seatPlayers[i].Equals(Web3.Account.PublicKey);
                
                if (isLocalPlayer)
                    hasActiveHandsForCurrentGame = _hands.Values.Any(h => h.hand.Player.Equals(Web3.Account.PublicKey) && h.hand.GameNo == _gameNo);
                else
                    hasActiveHandsForCurrentGame = _antedGameNo != null && i < _antedGameNo.Length && _antedGameNo[i] >= _gameNo;
            }
            
            bool showBeforeBetting = isDefaultPubkey || !hasActiveHandsForCurrentGame;
            
            if (_seats[i].beforeBettingGroup != null) 
                _seats[i].beforeBettingGroup.SetActive(showBeforeBetting);
            if (_seats[i].afterBettingGroup != null) 
                _seats[i].afterBettingGroup.SetActive(!showBeforeBetting);
        }
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

    private void InitializeHandButtons(GameObject handObject, byte handId)
    {
        var handCanvas = handObject.transform.Find("handCanvas");
        if (handCanvas == null) return;

        var infoText = handCanvas.Find("infoText");
        if (infoText != null)
            infoText.gameObject.SetActive(false);

        var buttonGroup = handCanvas.Find("buttonGroup");
        if (buttonGroup == null) return;

        var buttonActions = new (string name, InteractableObjects.ActionType action)[]
        {
            ("hit", InteractableObjects.ActionType.BlackjackHit),
            ("stand", InteractableObjects.ActionType.BlackjackStand),
            ("double", InteractableObjects.ActionType.BlackjackDouble),
            ("split", InteractableObjects.ActionType.BlackjackSplit),
            ("accept_insurance", InteractableObjects.ActionType.BlackjackAcceptInsurance),
            ("decline_insurance", InteractableObjects.ActionType.BlackjackDeclineInsurance),
            ("settle_hand", InteractableObjects.ActionType.BlackjackSettleHand)
        };

        foreach (var (name, action) in buttonActions)
        {
            var button = buttonGroup.Find(name);
            if (button == null) continue;
            
            button.gameObject.SetActive(false);
            interactableObjects?.RegisterInteractable(button.gameObject, handButtonGlowProfile, action, false, handId);
        }
    }

    private void RemoveHand(PublicKey handPk)
    {
        if (!_hands.TryGetValue(handPk, out var entry)) return;
        
        UnregisterHandButtons(entry.prefab);
        Destroy(entry.prefab);
        _hands.Remove(handPk);
    }

    private void UnregisterHandButtons(GameObject handObject)
    {
        if (interactableObjects == null) return;
        
        var handCanvas = handObject.transform.Find("handCanvas");
        if (handCanvas == null) return;

        var buttonGroup = handCanvas.Find("buttonGroup");
        if (buttonGroup == null) return;

        string[] buttonNames = { "hit", "stand", "double", "split", "accept_insurance", "decline_insurance", "settle_hand" };
        foreach (var name in buttonNames)
        {
            var button = buttonGroup.Find(name);
            if (button != null)
                interactableObjects.UnregisterInteractable(button.gameObject);
        }
    }

    private void ReparentHand(GameObject prefab, BlackJackHand hand)
    {
        var targetParent = GetParentForHand(hand);
        if (prefab.transform.parent == targetParent) return;
        
        prefab.transform.SetParent(targetParent);
        prefab.transform.localPosition = Vector3.zero;
        prefab.transform.localRotation = Quaternion.identity;
    }

    private void ReparentAllHands()
    {
        foreach (var kvp in _hands)
            ReparentHand(kvp.Value.prefab, kvp.Value.hand);
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
            ConfigureCard(card, game.DealerCards[i]);
        }

        if (game.DealerCardCount == 1)
        {
            var hiddenCard = Instantiate(cardPrefab, _dealerCardGroup);
            hiddenCard.transform.localPosition = GetCardOffset(1);
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

                var renderer = chip.GetComponentInChildren<MeshRenderer>();
                if (renderer != null && material != null)
                    renderer.material = material;

                totalChips++;
            }
        }
    }

    private void SyncButtonsForHand(GameObject handObject, BlackJackHand hand)
    {
        var handCanvas = handObject.transform.Find("handCanvas");
        if (handCanvas == null) return;

        var buttonGroup = handCanvas.Find("buttonGroup");
        if (buttonGroup == null) return;

        var game = accountManager.BlackjackGameCache;
        bool gameCondition = game != null && game.DealerCardCount == 1 && hand.GameNo == _gameNo;
        
        // Check if insurance was declined - treat state 1 as state 0 for button purposes
        bool insuranceDeclined = _declinedInsurance.Contains(hand.HandId);
        byte effectiveState = (hand.State == 1 && insuranceDeclined) ? (byte)0 : hand.State;
        
        bool baseCondition = effectiveState == 0 && gameCondition;

        bool showHitStand = baseCondition && hand.CardCount >= 2;
        bool showDouble = baseCondition && hand.CardCount == 2;
        bool showSplit = showDouble 
            && hand.PlayerCards[0] > 0 && hand.PlayerCards[1] > 0
            && (hand.PlayerCards[0] - 1) % 13 == (hand.PlayerCards[1] - 1) % 13;
        
        // Only show insurance options if state == 1 and hasn't declined yet
        bool showInsuranceOptions = hand.State == 1 && !insuranceDeclined && gameCondition && hand.CardCount == 2;

        var hit = buttonGroup.Find("hit");
        var stand = buttonGroup.Find("stand");
        var doubleBtn = buttonGroup.Find("double");
        var split = buttonGroup.Find("split");
        var insurance = buttonGroup.Find("accept_insurance");
        var declineInsurance = buttonGroup.Find("decline_insurance");

        if (hit != null) hit.gameObject.SetActive(showHitStand);
        if (stand != null) stand.gameObject.SetActive(showHitStand);
        if (doubleBtn != null) doubleBtn.gameObject.SetActive(showDouble);
        if (split != null) split.gameObject.SetActive(showSplit);
        if (insurance != null) insurance.gameObject.SetActive(showInsuranceOptions);
        if (declineInsurance != null) declineInsurance.gameObject.SetActive(showInsuranceOptions);

        var infoText = handCanvas.Find("infoText");
        if (infoText != null)
        {
            infoText.gameObject.SetActive(showInsuranceOptions);
            if (showInsuranceOptions)
                infoText.GetComponent<TMP_Text>().text = "insurance?";
        }
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
        _anteBuffer[_selectedHandIndex] += amount;
        SyncBufferChips();
    }

    private void SyncBufferChips()
    {
        if (chipPrefab == null) return;

        int seatIndex = Web3.Account != null ? FindSeatIndexForPlayer(Web3.Account.PublicKey) : -1;
        if (seatIndex < 0 || seatIndex >= _seats.Length) return;

        Transform bufferGroup = _seats[seatIndex].bufferGroup;
        if (bufferGroup == null) return;

        // Ensure we have the right number of buffer hands
        while (_bufferHands.Count < _anteBuffer.Count)
        {
            var hand = Instantiate(handPrefab, bufferGroup);
            hand.transform.localPosition = Vector3.zero;
            hand.transform.localRotation = Quaternion.identity;
            _bufferHands.Add(hand);
        }
        while (_bufferHands.Count > _anteBuffer.Count)
        {
            Destroy(_bufferHands[_bufferHands.Count - 1]);
            _bufferHands.RemoveAt(_bufferHands.Count - 1);
        }

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

                    var renderer = chip.GetComponentInChildren<MeshRenderer>();
                    if (renderer != null && material != null)
                        renderer.material = material;

                    totalChips++;
                }
            }
        }
    }

    public void AddHand()
    {
        _anteBuffer.Add(_anteBuffer[_selectedHandIndex]);
        _selectedHandIndex = _anteBuffer.Count - 1;
        SyncBufferChips();
    }

    public void RemoveHand()
    {
        if (_anteBuffer.Count > 1)
        {
            _anteBuffer.RemoveAt(_selectedHandIndex);
            if (_selectedHandIndex >= _anteBuffer.Count)
                _selectedHandIndex = _anteBuffer.Count - 1;
            SyncBufferChips();
        }
        else
        {
            _anteBuffer[0] = 0;
            ClearBufferHands();
        }
    }

    public void RemoveAllHands()
    {
        _anteBuffer.Clear();
        _anteBuffer.Add(0);
        _selectedHandIndex = 0;
        ClearBufferHands();
    }

    private void ClearBufferHands()
    {
        foreach (var hand in _bufferHands)
            Destroy(hand);
        _bufferHands.Clear();
    }

    public void SelectHand(int index)
    {
        if (index >= 0 && index < _anteBuffer.Count)
            _selectedHandIndex = index;
    }

    public void SubmitAnte()
    {
        var betAmounts = _anteBuffer.Where(a => a > 0).ToArray();
        if (betAmounts.Length == 0) return;
        
        userUI.Ante(betAmounts);
        RemoveAllHands();
    }

    public void DeclineInsurance(byte handId)
    {
        _declinedInsurance.Add(handId);
        
        // Re-sync buttons for any hands with this ID
        foreach (var kvp in _hands)
        {
            if (kvp.Value.hand.HandId == handId)
                SyncButtonsForHand(kvp.Value.prefab, kvp.Value.hand);
        }
    }
}
