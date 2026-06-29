using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference lookAction;
    [SerializeField] private InputActionReference sprintAction;
    [SerializeField] private InputActionReference freeLookAction;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5.5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float rotationSpeed = 12f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float groundedGravity = -2f;

    [Header("Camera")]
    [SerializeField] private Camera playerCam;
    [SerializeField] private float mouseSensivity = 2f;
    [SerializeField] private float lookSmoothTime = 0.04f;
    [SerializeField] private float cameraDistance = 4f;
    [SerializeField] private float cameraHeight = 1.5f;
    [SerializeField] private float minPitch = -35f;
    [SerializeField] private float maxPitch =  65f;
    [SerializeField] private float cameraSmoothSpeed = 20f;

    [Header("Camera Collision")]
    [SerializeField] private LayerMask cameraCollisionMask;
    [SerializeField] private float cameraCollisionRadius = .25f;

    private CharacterController controller;
    
    private float yaw;
    private float pitch;
    private float verticalVelocity;

    private Vector2 currentLookDelta;
    private Vector2 lookDeltaVelocity;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if(playerCam == null) playerCam = Camera.main;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnEnable()
    {
        moveAction.action.Enable();
        lookAction.action.Enable();
        sprintAction.action.Enable();
        freeLookAction.action.Enable();
    }

    private void OnDisable()
    {
        moveAction.action.Disable();
        lookAction.action.Disable();
        sprintAction.action.Disable();
        freeLookAction.action.Disable();
    }

    private void Start()
    {
        yaw = transform.eulerAngles.x;
    }

    private void Update()
    {
        UpdateCameraPosition();
    }

    private void LateUpdate()
    {
        HandleCameraInput();
        HandleMovement();   
    }

    private void HandleCameraInput()
    {
        Vector2 rawLookInput = lookAction.action.ReadValue<Vector2>();
        Vector2 targetLookDelta = 0.02f * mouseSensivity * rawLookInput;

        currentLookDelta = Vector2.SmoothDamp
        (
            currentLookDelta,
            targetLookDelta,
            ref lookDeltaVelocity,
            lookSmoothTime
        );

        yaw += currentLookDelta.x;
        pitch -= currentLookDelta.y;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    private void HandleMovement()
    {
        Vector2 input = moveAction.action.ReadValue<Vector2>();
        input = Vector2.ClampMagnitude(input, 1f);

        bool freeLookMovement = freeLookAction.action.IsPressed();
        
        Vector3 moveDirection;

        if(freeLookMovement)
            moveDirection = transform.forward * input.y + transform.right * input.x;
        else
        {
            Vector3 camForward = playerCam.transform.forward;
            Vector3 camRight = playerCam.transform.right;

            camForward.y = 0f;
            camRight.y = 0f;

            camForward.Normalize();
            camRight.Normalize();
            
            moveDirection = camForward * input.y + camRight * input.x;
        }

        if(moveDirection.sqrMagnitude > 1f) moveDirection.Normalize();

        bool isSprinting = sprintAction.action.IsPressed();
        float currentSpeed = isSprinting ? sprintSpeed : moveSpeed;

        if(controller.isGrounded)
            if(verticalVelocity < 0f) 
                verticalVelocity = groundedGravity;
        else 
            verticalVelocity += gravity * Time.deltaTime;
        
        Vector3 velocity = moveDirection * currentSpeed;
        velocity.y = verticalVelocity;

        controller.Move(velocity * Time.deltaTime);

        if(!freeLookMovement && moveDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void UpdateCameraPosition()
    {
        if(playerCam == null) return;

        Quaternion camRotation = Quaternion.Euler(pitch, yaw, 0f);

        Vector3 targetPosition = transform.position + Vector3.up * cameraHeight;
        Vector3 desiredCameraPosition = targetPosition - camRotation * Vector3.forward * cameraDistance;

        Vector3 directionToCamera = desiredCameraPosition - targetPosition;
        float desiredDistance = cameraDistance;

        if(Physics.SphereCast
        (
            targetPosition, 
            cameraCollisionRadius, 
            directionToCamera.normalized, 
            out RaycastHit hit, 
            cameraDistance, 
            cameraCollisionMask
        ))
        {
            desiredDistance = Mathf.Max(hit.distance, .5f);   
        }

        Vector3 finalCamPosition = targetPosition - camRotation * Vector3.forward * desiredDistance;
        playerCam.transform.SetPositionAndRotation(Vector3.Lerp(playerCam.transform.position, finalCamPosition, cameraSmoothSpeed * Time.deltaTime), camRotation);
    }
}