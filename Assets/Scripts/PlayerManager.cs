using UnityEngine;
using UnityEngine.InputSystem;
using Coherence.Toolkit;
using Coherence.Connection;

public class PlayerManager : MonoBehaviour
{
    private Vector3 PlayerMovementInput;

    [Header("Spawn")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform spawnPoint;

    [Header("References")]
    private CharacterController controller;
    private Animator animator;
	[SerializeField] private Transform cameraTransform;
    
    [Header("Movement Settings")]
    [SerializeField] private float Speed = 5f;
    [SerializeField] private float SprintSpeedMultiplier = 1.5f;
    [SerializeField] private float JumpHeight = 2f;
    [SerializeField] private float Gravity = -9.81f;
    [SerializeField] private float AnimationSmoothTime = 0.1f;
    [SerializeField] private float PlayerRotationSpeed = 10f;

    private Vector3 velocity;
    private float currentAnimationSpeed;
    private float animationSpeedVelocity;

    private CoherenceBridge _coherenceBridge;
    private GameObject _playerReference;
    private Transform _playerTransform;

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

    private void Update()
    {
        if (controller == null)
            return;

        HandleInput();
        MovePlayer();
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
        PlayerMovementInput = new Vector3(Mathf.Clamp(h, -1, 1), 0f, Mathf.Clamp(v, -1, 1));
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

		// Convert input to camera-relative movement (W/S forward/back, A/D strafe)
		Transform basis = cameraTransform != null ? cameraTransform : _playerTransform;
		Vector3 forward = basis.forward;
		Vector3 right = basis.right;
        
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        Vector3 moveDirection = (forward * PlayerMovementInput.z + right * PlayerMovementInput.x).normalized;
        Vector3 move = moveDirection * currentSpeed;

		// Smoothly rotate to face the camera-relative movement direction (includes A/D strafing)
		if (moveDirection != Vector3.zero)
		{
			Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
			_playerTransform.rotation = Quaternion.Slerp(_playerTransform.rotation, targetRotation, PlayerRotationSpeed * Time.deltaTime);
		}

        // Jump
        if (kb != null && kb.spaceKey.wasPressedThisFrame && isGrounded)
        {
            velocity.y = Mathf.Sqrt(JumpHeight * -2f * Gravity);
        }

        // Apply gravity
        velocity.y += Gravity * Time.deltaTime;
        controller.Move((move + velocity) * Time.deltaTime);

        // Update animator
        float targetSpeed = PlayerMovementInput.magnitude * (isSprinting ? 1f : 0.5f);
        currentAnimationSpeed = Mathf.SmoothDamp(currentAnimationSpeed, targetSpeed, ref animationSpeedVelocity, AnimationSmoothTime);
        animator.SetFloat("Speed", currentAnimationSpeed);
    }

    private void OnConnected(CoherenceBridge arg0)
    {
        if (_playerReference != null)
            return;

        _playerReference = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
        _playerTransform = _playerReference.transform;
        controller = _playerReference.GetComponent<CharacterController>();
        animator = _playerReference.GetComponentInChildren<Animator>();
		if (cameraTransform == null && Camera.main != null)
		{
			cameraTransform = Camera.main.transform;
		}
    }

    private void OnDisconnected(CoherenceBridge arg0, ConnectionCloseReason reason)
    {
        if (_playerReference != null)
            Destroy(_playerReference);

        _playerReference = null;
        _playerTransform = null;
        controller = null;
        animator = null;
        velocity = Vector3.zero;
        currentAnimationSpeed = 0f;
    }
}

