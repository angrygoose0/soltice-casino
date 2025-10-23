using UnityEngine;
using Coherence.Toolkit;
using Coherence.Connection;

public class PlayerManager : MonoBehaviour
{
    [Header("Spawn")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform spawnPoint;

    [Header("References")]
    [SerializeField] private MaterialManager materialManager;
    [SerializeField] private CrashUI crashUI;
    [SerializeField] private UserUI userUI;

    private CoherenceBridge _coherenceBridge;
    private GameObject _playerReference;
    private NetworkedPlayer _localPlayer;

    private void OnEnable()
    {
        _coherenceBridge = FindFirstObjectByType<CoherenceBridge>();
        if (_coherenceBridge != null)
        {
            _coherenceBridge.onConnected.AddListener(OnConnected);
            _coherenceBridge.onDisconnected.AddListener(OnDisconnected);
        }
    }

    private void OnDisable()
    {
        if (_coherenceBridge != null)
        {
            _coherenceBridge.onConnected.RemoveListener(OnConnected);
            _coherenceBridge.onDisconnected.RemoveListener(OnDisconnected);
        }
    }

    private void OnConnected(CoherenceBridge arg0)
    {
        if (_playerReference != null)
            return;

        _playerReference = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
        _localPlayer = _playerReference.GetComponent<NetworkedPlayer>();
        
        if (_localPlayer != null)
        {
            // Set username from SimpleWorldJoin (captured when Start button was clicked)
            string username = SimpleWorldJoin.PlayerUsername;
            _localPlayer.SetUsername(username);
            
            // Set scene references (can't be set in prefab)
            _localPlayer.materialManager = materialManager;
            
            // Set currentPlayer on CrashUI
            if (crashUI != null)
            {
                crashUI.currentPlayer = _localPlayer.transform;
            }
            
            // Set player bet text reference in UserUI
            if (userUI != null)
            {
                userUI.SetPlayerBetText(_localPlayer.betAmountText);
            }
        }
    }

    private void OnDisconnected(CoherenceBridge arg0, ConnectionCloseReason reason)
    {
        if (_playerReference != null)
            Destroy(_playerReference);

        _playerReference = null;
        _localPlayer = null;
    }

    // Public method to access the local player (for other systems like UI)
    public NetworkedPlayer GetLocalPlayer()
    {
        return _localPlayer;
    }
}

