using UnityEngine;
using UnityEngine.InputSystem;

namespace Yobitsugi.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float walkSpeed = 3.2f;
        [SerializeField] private float sprintSpeed = 5.6f;
        [SerializeField] private float crouchSpeed = 1.6f;
        [SerializeField] private float gravity = -18f;

        [Header("Look")]
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private float mouseSensitivity = 0.12f;
        [SerializeField] private float minPitch = -80f;
        [SerializeField] private float maxPitch = 80f;

        [Header("Crouch")]
        [SerializeField] private float standingHeight = 1.8f;
        [SerializeField] private float crouchingHeight = 1.0f;

        private CharacterController controller;
        private Vector2 moveInput;
        private Vector2 lookInput;
        private float pitch;
        private float verticalVelocity;
        private bool isSprintHeld;
        private bool isCrouching;

        public bool CanMove { get; set; } = true;
        public bool IsMoving => CanMove && moveInput.sqrMagnitude > 0.01f;
        public bool IsCrouching => isCrouching;
        public bool IsSprinting => isSprintHeld && !isCrouching && IsMoving;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        private void OnEnable()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            HandleLook();
            HandleMove();
        }

        private void HandleLook()
        {
            if (!CanMove) return;

            float yaw = lookInput.x * mouseSensitivity;
            pitch = Mathf.Clamp(pitch - lookInput.y * mouseSensitivity, minPitch, maxPitch);

            transform.Rotate(Vector3.up * yaw);
            if (cameraPivot != null)
                cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        private void HandleMove()
        {
            float targetHeight = isCrouching ? crouchingHeight : standingHeight;
            controller.height = Mathf.Lerp(controller.height, targetHeight, Time.deltaTime * 8f);
            controller.center = new Vector3(0f, controller.height / 2f, 0f);

            Vector2 currentMove = CanMove ? moveInput : Vector2.zero;
            Vector3 move = transform.right * currentMove.x + transform.forward * currentMove.y;
            move = Vector3.ClampMagnitude(move, 1f);

            float speed = isCrouching ? crouchSpeed : (isSprintHeld ? sprintSpeed : walkSpeed);

            if (controller.isGrounded && verticalVelocity < 0f)
                verticalVelocity = -1f;
            verticalVelocity += gravity * Time.deltaTime;

            Vector3 velocity = move * speed;
            velocity.y = verticalVelocity;
            controller.Move(velocity * Time.deltaTime);
        }

        // --- Input System callbacks, invoked by PlayerInput's "Send Messages" behaviour ---
        public void OnMove(InputValue value) => moveInput = value.Get<Vector2>();
        public void OnLook(InputValue value) => lookInput = value.Get<Vector2>();
        public void OnSprint(InputValue value) => isSprintHeld = value.isPressed;

        public void OnCrouch(InputValue value)
        {
            if (value.isPressed)
                isCrouching = !isCrouching;
        }
    }
}
