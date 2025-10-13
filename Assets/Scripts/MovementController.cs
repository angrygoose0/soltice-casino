using UnityEngine;
using UnityEngine.InputSystem;

public class MovementController : MonoBehaviour
{
    private Vector3 PlayerMovementInput;
    private Vector2 MouseInput;

    [SerializeField] private Transform PlayerCamera;
    [SerializeField] private CharacterController controller;
    [SerializeField] private Animator animator;
    [Space]
    [SerializeField] private float Speed = 5f;
    [SerializeField] private float SprintSpeedMultiplier = 1.5f;
    [SerializeField] private float JumpHeight = 2f;
    [SerializeField] private float Gravity = -9.81f;
    [SerializeField] private float AnimationSmoothTime = 0.1f;
    [Space]
    [SerializeField] private float MouseSensitivity = 2f;
    [SerializeField] private float MinZoomDistance = 5f;
    [SerializeField] private float MaxZoomDistance = 15f;
    [SerializeField] private float ZoomSpeed = 2f;
    [SerializeField] private float RotationSmoothTime = 0.1f;
    [SerializeField] private float PlayerRotationSpeed = 10f;

    private float currentZoomDistance = 10f;
    private float currentRotationX = 0f;
    private float currentRotationY = 0f;
    private Vector3 cameraRotationSmoothVelocity;
    private Vector3 currentRotation;
    private bool isRotating = false;
    private Vector3 velocity;
    private float currentAnimationSpeed;
    private float animationSpeedVelocity;

    private void Start()
    {
        if (controller == null)
            controller = GetComponent<CharacterController>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (PlayerCamera != null)
        {
            currentRotationY = PlayerCamera.eulerAngles.y;
            UpdateCameraPosition();
        }
        Cursor.lockState = CursorLockMode.None;
    }

    private void Update()
    {
        HandleInput();
        MovePlayer();
    }

    private void LateUpdate()
    {
        HandleCamera();
    }

    private void HandleInput()
    {
        float horizontal = 0f;
        float vertical = 0f;

        if (Keyboard.current != null)
        {
            // WASD
            if (Keyboard.current.aKey.isPressed) horizontal -= 1f;
            if (Keyboard.current.dKey.isPressed) horizontal += 1f;
            if (Keyboard.current.wKey.isPressed) vertical += 1f;
            if (Keyboard.current.sKey.isPressed) vertical -= 1f;

            // Arrow keys
            if (Keyboard.current.leftArrowKey.isPressed) horizontal -= 1f;
            if (Keyboard.current.rightArrowKey.isPressed) horizontal += 1f;
            if (Keyboard.current.upArrowKey.isPressed) vertical += 1f;
            if (Keyboard.current.downArrowKey.isPressed) vertical -= 1f;
        }

        PlayerMovementInput = new Vector3(horizontal, 0f, vertical);
        
        // Handle mouse input only when right mouse button is held
        if (Mouse.current != null && Mouse.current.rightButton.isPressed)
        {
            isRotating = true;
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            MouseInput = new Vector2(mouseDelta.x, mouseDelta.y);
            Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            isRotating = false;
            MouseInput = Vector2.zero;
            Cursor.lockState = CursorLockMode.None;
        }

        // Handle zoom with scroll wheel
        if (Mouse.current != null)
        {
            float scrollInput = Mouse.current.scroll.ReadValue().y * 0.01f;
            currentZoomDistance = Mathf.Clamp(currentZoomDistance - scrollInput * ZoomSpeed, MinZoomDistance, MaxZoomDistance);
        }
    }

    private void HandleCamera()
    {
        if (PlayerCamera == null) return;

        if (isRotating)
        {
            currentRotationX -= MouseInput.y * MouseSensitivity;
            currentRotationY += MouseInput.x * MouseSensitivity;
            currentRotationX = Mathf.Clamp(currentRotationX, -60f, 80f);
        }

        Vector3 targetRotation = new Vector3(currentRotationX, currentRotationY, 0f);
        currentRotation = Vector3.SmoothDamp(currentRotation, targetRotation, ref cameraRotationSmoothVelocity, RotationSmoothTime);

        UpdateCameraPosition();
    }

    private void UpdateCameraPosition()
    {
        Vector3 targetPosition = transform.position;
        Quaternion rotation = Quaternion.Euler(currentRotation.x, currentRotation.y, 0);
        Vector3 offset = rotation * new Vector3(0, 0, -currentZoomDistance);
        
        PlayerCamera.position = targetPosition + offset;
        PlayerCamera.LookAt(targetPosition + Vector3.up * 1f);
    }

    private void MovePlayer()
    {
        bool isGrounded = controller.isGrounded;

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        bool isSprinting = Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;
        float currentSpeed = isSprinting ? Speed * SprintSpeedMultiplier : Speed;

        // Convert input to camera-relative movement
        Vector3 cameraForward = PlayerCamera.forward;
        Vector3 cameraRight = PlayerCamera.right;
        
        cameraForward.y = 0;
        cameraRight.y = 0;
        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 moveDirection = (cameraForward * PlayerMovementInput.z + cameraRight * PlayerMovementInput.x).normalized;
        Vector3 move = moveDirection * currentSpeed;

        controller.Move(move * Time.deltaTime);

        // Smoothly rotate player to face movement direction
        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, PlayerRotationSpeed * Time.deltaTime);
        }

        // Jump
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame && isGrounded)
        {
            velocity.y = Mathf.Sqrt(JumpHeight * -2f * Gravity);
        }

        // Apply gravity
        velocity.y += Gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // Update animator
        if (animator != null)
        {
            // Calculate movement speed (0 = idle, 0.5 = walk, 1 = sprint)
            float targetSpeed = PlayerMovementInput.magnitude * (isSprinting ? 1f : 0.5f);
            currentAnimationSpeed = Mathf.SmoothDamp(currentAnimationSpeed, targetSpeed, ref animationSpeedVelocity, AnimationSmoothTime);
            
            animator.SetFloat("Speed", currentAnimationSpeed);
            animator.SetBool("IsGrounded", isGrounded);
        }
    }
}
