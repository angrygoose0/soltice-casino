using UnityEngine;
using UnityEngine.InputSystem;

public class MovementController : MonoBehaviour
{
    private Vector3 PlayerMovementInput;
    private Vector2 MouseInput;

    [SerializeField] private LayerMask FloorMask;
    [SerializeField] private Transform FeetTransform;
    [SerializeField] private Transform PlayerCamera;
    [SerializeField] private Rigidbody PlayerBody;
    [Space]
    [SerializeField] private float Speed;
    [SerializeField] private float SprintSpeedMultiplier = 1.5f;
    [SerializeField] private float Jumpforce;
    [Space]
    [SerializeField] private float MouseSensitivity = 2f;
    [SerializeField] private float MinZoomDistance = 5f;
    [SerializeField] private float MaxZoomDistance = 15f;
    [SerializeField] private float ZoomSpeed = 2f;
    [SerializeField] private float RotationSmoothTime = 0.1f;

    private float currentZoomDistance = 10f;
    private float currentRotationX = 0f;
    private float currentRotationY = 0f;
    private Vector3 cameraRotationSmoothVelocity;
    private Vector3 currentRotation;
    private bool isRotating = false;

    

    private void Start()
    {
        // Initialize camera position
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
        HandleCamera();
        MovePlayer();
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
            Vector2 mouseDelta = Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
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
        float scrollInput = 0f;
        if (Mouse.current != null)
        {
            // scale down to feel similar to legacy GetAxis("Mouse ScrollWheel")
            scrollInput = Mouse.current.scroll.ReadValue().y * 0.01f;
        }
        currentZoomDistance = Mathf.Clamp(currentZoomDistance - scrollInput * ZoomSpeed, MinZoomDistance, MaxZoomDistance);
    }

    private void HandleCamera()
    {
        if (PlayerCamera == null) return;

        if (isRotating)
        {
            currentRotationX -= MouseInput.y * MouseSensitivity;
            currentRotationY += MouseInput.x * MouseSensitivity;
            
            // Clamp the vertical rotation to prevent over-rotation
            currentRotationX = Mathf.Clamp(currentRotationX, -60f, 80f);
        }

        // Calculate target rotation and smoothly interpolate
        Vector3 targetRotation = new Vector3(currentRotationX, currentRotationY, 0f);
        currentRotation = Vector3.SmoothDamp(currentRotation, targetRotation, ref cameraRotationSmoothVelocity, RotationSmoothTime);

        UpdateCameraPosition();
    }

    private void UpdateCameraPosition()
    {
        // Calculate camera position based on player position, rotation and zoom
        Vector3 targetPosition = transform.position;
        Quaternion rotation = Quaternion.Euler(currentRotation.x, currentRotation.y, 0);
        Vector3 offset = rotation * new Vector3(0, 0, -currentZoomDistance);
        
        PlayerCamera.position = targetPosition + offset;
        PlayerCamera.LookAt(targetPosition + Vector3.up * 1f); // Look slightly above player's feet
    }

    private void MovePlayer()
    {
        bool isSprinting = Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;
        float currentSpeed = isSprinting ? Speed * SprintSpeedMultiplier : Speed;

        // Convert input to camera-relative movement
        Vector3 cameraForward = PlayerCamera.forward;
        Vector3 cameraRight = PlayerCamera.right;
        
        // Project vectors onto the horizontal plane
        cameraForward.y = 0;
        cameraRight.y = 0;
        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 moveDirection = (cameraForward * PlayerMovementInput.z + cameraRight * PlayerMovementInput.x).normalized;
        Vector3 MoveVector = moveDirection * currentSpeed;

        PlayerBody.linearVelocity = new Vector3(MoveVector.x, PlayerBody.linearVelocity.y, MoveVector.z);

        // Rotate player to face movement direction - REMOVED to prevent twitching
        // if (moveDirection != Vector3.zero)
        // {
        //     transform.rotation = Quaternion.LookRotation(moveDirection);
        // }

        bool isGrounded = Physics.CheckSphere(FeetTransform.position, 0.1f, FloorMask);

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame && isGrounded)
        {
            PlayerBody.AddForce(Vector3.up * Jumpforce, ForceMode.Impulse);
        }

        
    }

}
