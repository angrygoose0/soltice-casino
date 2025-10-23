using UnityEngine;
using UnityEngine.InputSystem;
using Coherence.Toolkit;
using Unity.Cinemachine;
using TMPro;

public class NetworkedPlayer : MonoBehaviour
{
    [Header("Movement Settings")]
    public float Speed = 5f;
    public float SprintSpeedMultiplier = 1.5f;
    public float JumpHeight = 2f;
    public float Gravity = -9.81f;
    public float AnimationSmoothTime = 0.1f;
    public float PlayerRotationSpeed = 10f;

    [Header("Local-Only References (Not Synced)")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private CinemachineCamera cinemachineCamera;
    [SerializeField] private CinemachineInputAxisController inputAxisController;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] public TextMeshProUGUI betAmountText;
    
    // Set by PlayerManager at spawn time (scene reference, can't be in prefab)
    [HideInInspector] public MaterialManager materialManager;

    [Header("Synced Variables")]
    public string playerUsername = "Player";
    public float currentBetAmount = 0f;
    
    // Material properties (synced via Coherence)
    public float playerColorR = 1f;
    public float playerColorG = 1f;
    public float playerColorB = 1f;
    public float playerMetallic = 0f;
    public float playerSmoothness = 0f;
    
    // Note: Animation speed is synced via Animator Speed parameter binding in CoherenceSync

    // Local-only private variables
    private CoherenceSync coherenceSync;
    private CharacterController controller;
    private Animator animator;
    private Transform playerTransform;
    private Vector3 playerMovementInput;
    private Vector3 velocity;
    private float currentAnimationSpeed;
    private float animationSpeedVelocity;
    
    // Track last applied material values to avoid redundant updates
    private Color lastAppliedColor = Color.clear;
    private float lastAppliedMetallic = -1f;
    private float lastAppliedSmoothness = -1f;

    private void Awake()
    {
        coherenceSync = GetComponent<CoherenceSync>();
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        playerTransform = transform;
    }

    private void Start()
    {
        // Only enable camera and input for the local player
        if (coherenceSync.HasStateAuthority)
        {
            SetupLocalPlayer();
            
            // Generate random color for local player
            // The material properties will be synced automatically by Coherence
            if (materialManager != null)
            {
                materialManager.SetRandomPlayerColor(gameObject);
            }
        }
        else
        {
            SetupRemotePlayer();
        }

        // Setup UI (both local and remote)
        if (nameText != null)
        {
            nameText.text = playerUsername;
        }

        // Force initial material application (important for remote players)
        ApplyMaterialProperties();
    }

    private void Update()
    {
        // Only process input and movement if we have authority over this player
        if (coherenceSync.HasStateAuthority)
        {
            HandleInput();
            HandleCameraOrbitInput();
            MovePlayer();
        }
        else
        {
            // Remote players: visuals updated automatically via Coherence sync
        }

        // Apply material properties for all players (local and remote)
        ApplyMaterialProperties();

        // Update UI for all players
        UpdateUI();
    }

    private void SetupLocalPlayer()
    {
        // Enable camera for local player
        if (cinemachineCamera != null)
        {
            cinemachineCamera.gameObject.SetActive(true);
            cinemachineCamera.Priority.Enabled = true;
            cinemachineCamera.Priority.Value = 10;
        }

        // Get main camera for camera-relative movement
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    private void SetupRemotePlayer()
    {
        // Disable camera for remote players
        if (cinemachineCamera != null)
        {
            cinemachineCamera.gameObject.SetActive(false);
        }

        // Disable input controller for remote players
        if (inputAxisController != null)
        {
            inputAxisController.enabled = false;
        }
    }

    private void HandleInput()
    {
        var kb = Keyboard.current;
        int h = 0;
        int v = 0;
        if (kb != null)
        {
            h = (kb.dKey.isPressed ? 1 : 0) - (kb.aKey.isPressed ? 1 : 0)
              + (kb.rightArrowKey.isPressed ? 1 : 0) - (kb.leftArrowKey.isPressed ? 1 : 0);
            v = (kb.wKey.isPressed ? 1 : 0) - (kb.sKey.isPressed ? 1 : 0)
              + (kb.upArrowKey.isPressed ? 1 : 0) - (kb.downArrowKey.isPressed ? 1 : 0);
        }
        playerMovementInput = new Vector3(Mathf.Clamp(h, -1, 1), 0f, Mathf.Clamp(v, -1, 1));
    }

    private void HandleCameraOrbitInput()
    {
        if (inputAxisController == null)
            return;

        var mouse = Mouse.current;
        bool isRightClickHeld = mouse != null && mouse.rightButton.isPressed;

        var controllers = inputAxisController.Controllers;
        if (controllers != null)
        {
            foreach (var controller in controllers)
            {
                if (controller.Name == "Look Orbit X" || controller.Name == "Look Orbit Y")
                {
                    controller.Enabled = isRightClickHeld;
                }
            }
        }
    }

    private void MovePlayer()
    {
        bool isGrounded = controller.isGrounded;

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        var kb = Keyboard.current;
        bool isSprinting = kb != null && kb.leftShiftKey.isPressed;
        float currentSpeed = isSprinting ? Speed * SprintSpeedMultiplier : Speed;

        // Convert input to camera-relative movement
        Transform basis = cameraTransform != null ? cameraTransform : playerTransform;
        Vector3 forward = basis.forward;
        Vector3 right = basis.right;
        
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        Vector3 moveDirection = (forward * playerMovementInput.z + right * playerMovementInput.x).normalized;
        Vector3 move = moveDirection * currentSpeed;

        // Smoothly rotate to face movement direction
        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            playerTransform.rotation = Quaternion.Slerp(playerTransform.rotation, targetRotation, PlayerRotationSpeed * Time.deltaTime);
        }

        // Jump
        if (kb != null && kb.spaceKey.wasPressedThisFrame && isGrounded)
        {
            velocity.y = Mathf.Sqrt(JumpHeight * -2f * Gravity);
        }

        // Apply gravity
        velocity.y += Gravity * Time.deltaTime;
        controller.Move((move + velocity) * Time.deltaTime);

        // Update animator - CoherenceSync will automatically sync the Speed parameter
        float targetSpeed = playerMovementInput.magnitude * (isSprinting ? 1f : 0.5f);
        currentAnimationSpeed = Mathf.SmoothDamp(currentAnimationSpeed, targetSpeed, ref animationSpeedVelocity, AnimationSmoothTime);
        
        if (animator != null)
        {
            animator.SetFloat("Speed", currentAnimationSpeed);
            // The Speed parameter is automatically synced by CoherenceSync's Animator binding
        }
    }

    private void UpdateUI()
    {
        if (nameText != null)
        {
            nameText.text = playerUsername;
        }

        if (betAmountText != null)
        {
            if (currentBetAmount > 0)
            {
                UIFader.FadeIn(betAmountText.gameObject);
                betAmountText.text = $"Bet: {currentBetAmount}";
            }
            else
            {
                UIFader.FadeOut(betAmountText.gameObject);
            }
        }
    }

    // Public methods for external systems to call
    public void SetUsername(string username)
    {
        playerUsername = username;
        UpdateUI(); // Force immediate UI update
    }

    public void SetBetAmount(float amount)
    {
        currentBetAmount = amount;
    }

    private void ApplyMaterialProperties()
    {
        // Check if material properties have changed
        Color currentColor = new Color(playerColorR, playerColorG, playerColorB);
        
        if (currentColor == lastAppliedColor && 
            playerMetallic == lastAppliedMetallic && 
            playerSmoothness == lastAppliedSmoothness)
        {
            return; // No changes, skip update
        }

        // Apply to all mesh renderers
        Transform avatarMesh = transform.Find("avatarMesh");
        if (avatarMesh == null) return;

        string[] meshNames = { "bodyMesh", "headMesh", "legMesh" };
        foreach (string meshName in meshNames)
        {
            Transform meshTransform = avatarMesh.Find(meshName);
            if (meshTransform != null)
            {
                Renderer renderer = meshTransform.GetComponent<Renderer>();
                if (renderer != null && renderer.material != null)
                {
                    Material mat = renderer.material;
                    mat.SetColor("_Color", currentColor);
                    mat.SetFloat("_Metallic", playerMetallic);
                    mat.SetFloat("_Smoothness", playerSmoothness);
                }
            }
        }

        // Update tracking variables
        lastAppliedColor = currentColor;
        lastAppliedMetallic = playerMetallic;
        lastAppliedSmoothness = playerSmoothness;
    }
}

