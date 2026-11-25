using UnityEngine;

namespace NutriMind.Gameplay
{
    // Third-person movement for a mobile game: joystick (or WASD in the Editor), Run toggle button.
    // Needs a CharacterController on the same GameObject.
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("VirtualJoystick component for on-screen touch movement. Auto-detected if null.")]
        [SerializeField] VirtualJoystick joystick;
        [Tooltip("Generic Joystick (from Joystick Pack) if used instead. Auto-detected if null.")]
        [SerializeField] Joystick genericJoystick;
        [SerializeField] Transform cameraTransform;   // leave empty to use Camera.main
        [SerializeField] float walkSpeed = 2.5f;
        [SerializeField] float runSpeed = 5f;
        [SerializeField] float turnSharpness = 12f;
        [SerializeField] float gravity = -20f;

        [Header("Jump Settings")]
        [SerializeField] float jumpHeight = 1.6f;
        [SerializeField] bool canJump = true;
        [Tooltip("Normalized start time (0-0.5) to skip Mixamo crouch wind-up so animation matches takeoff.")]
        [SerializeField, Range(0f, 0.5f)] float jumpAnimStartOffset = 0.18f;

        [Header("Mobile Controls")]
        [Tooltip("If true, pushing the mobile joystick all the way to the outer edge (> 90%) will also trigger run.")]
        [SerializeField] bool autoSprintOnTiltMax = false;

        [Header("Optional")]
        [Tooltip("Animator handling player locomotion (Speed, IsMoving, IsRunning, Jump).")]
        [SerializeField] Animator animator;

        public bool InputEnabled = true;
        public bool IsRunning { get; private set; }

        CharacterController controller;
        float verticalSpeed;
        float lastGroundedTime;
        float jumpBufferTimer;
        int speedHash;
        int isMovingHash;
        int isRunningHash;
        int jumpHash;
        int isGroundedHash;
        bool animatorHasSpeed;
        bool animatorHasIsMoving;
        bool animatorHasIsRunning;
        bool animatorHasJump;
        bool animatorHasIsGrounded;

        bool CheckGrounded()
        {
            if (controller.isGrounded) return true;
            // Raycast check down 0.35m from slightly above feet to handle uneven slopes while running
            Vector3 origin = transform.position + Vector3.up * 0.15f;
            return Physics.Raycast(origin, Vector3.down, 0.35f, ~0, QueryTriggerInteraction.Ignore);
        }

        void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        void Start()
        {
            if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            if (animator != null)
            {
                speedHash = Animator.StringToHash("Speed");
                isMovingHash = Animator.StringToHash("IsMoving");
                isRunningHash = Animator.StringToHash("IsRunning");
                jumpHash = Animator.StringToHash("Jump");
                isGroundedHash = Animator.StringToHash("IsGrounded");

                foreach (AnimatorControllerParameter p in animator.parameters)
                {
                    if (p.nameHash == speedHash) animatorHasSpeed = true;
                    if (p.nameHash == isMovingHash) animatorHasIsMoving = true;
                    if (p.nameHash == isRunningHash) animatorHasIsRunning = true;
                    if (p.nameHash == jumpHash) animatorHasJump = true;
                    if (p.nameHash == isGroundedHash) animatorHasIsGrounded = true;
                }
            }

            ResolveJoystick();

            // Auto-detect and wire up RunButton in the HUD if present
            GameObject runBtnObj = GameObject.Find("RunButton");
            if (runBtnObj != null && runBtnObj.GetComponent<RunButton>() == null)
            {
                runBtnObj.AddComponent<RunButton>();
            }

            // Auto-detect and wire up JumpButton in the HUD if present
            GameObject jumpBtnObj = GameObject.Find("JumpButton");
            if (jumpBtnObj != null)
            {
                var misplacedRun = jumpBtnObj.GetComponent<RunButton>();
                if (misplacedRun != null) Destroy(misplacedRun);
                if (jumpBtnObj.GetComponent<JumpButton>() == null)
                {
                    jumpBtnObj.AddComponent<JumpButton>();
                }
            }
        }

        void ResolveJoystick()
        {
            if (joystick == null)
            {
                joystick = FindAnyObjectByType<VirtualJoystick>();
            }
            if (genericJoystick == null)
            {
                genericJoystick = FindAnyObjectByType<Joystick>();
            }
        }

        // Hook the Run button or scripts to these:
        public void ToggleRun()
        {
            SetRunning(!IsRunning);
        }

        public void SetRunning(bool running)
        {
            IsRunning = running;
        }

        void Update()
        {
            Vector2 input = InputEnabled ? ReadInput() : Vector2.zero;

            Vector3 forward = Vector3.forward;
            if (cameraTransform != null)
            {
                forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
            }
            Vector3 right = Vector3.Cross(Vector3.up, forward);

            Vector3 direction = forward * input.y + right * input.x;
            float amount = Mathf.Clamp01(direction.magnitude);
            bool isMoving = amount > 0.05f;

            // Keyboard sprint and jump support for testing
            if (InputEnabled)
            {
                CheckKeyboardSprint();
                CheckKeyboardJump();
            }

            float speed = (IsRunning ? runSpeed : walkSpeed) * amount;

            bool grounded = CheckGrounded();
            if (grounded)
            {
                lastGroundedTime = Time.time;
                if (verticalSpeed < 0f) verticalSpeed = -2f;
            }
            else
            {
                verticalSpeed += gravity * Time.deltaTime;
            }

            // Execute buffered jump if player pressed jump right before touching ground
            if (jumpBufferTimer > 0f)
            {
                jumpBufferTimer -= Time.deltaTime;
                if (grounded)
                {
                    jumpBufferTimer = 0f;
                    ExecuteJump();
                }
            }

            Vector3 velocity = direction.normalized * speed + Vector3.up * verticalSpeed;
            controller.Move(velocity * Time.deltaTime);

            if (isMoving)
            {
                Quaternion look = Quaternion.LookRotation(direction.normalized, Vector3.up);
                float t = 1f - Mathf.Exp(-turnSharpness * Time.deltaTime);
                transform.rotation = Quaternion.Slerp(transform.rotation, look, t);
            }

            // Update Animator parameters
            if (animator != null)
            {
                if (animatorHasSpeed) animator.SetFloat(speedHash, speed);
                if (animatorHasIsMoving) animator.SetBool(isMovingHash, isMoving);
                if (animatorHasIsRunning) animator.SetBool(isRunningHash, isMoving && IsRunning);
                if (animatorHasIsGrounded) animator.SetBool(isGroundedHash, grounded);
            }
        }

        public void Jump()
        {
            if (!InputEnabled || !canJump) return;

            // Allow jump if currently grounded or within coyote time (0.2s of leaving ground).
            // No jump buffer: buffering would cause an auto-jump ~1 second later when the
            // player lands, because the buffer countdown runs in Update() and fires ExecuteJump()
            // the moment isGrounded becomes true — which looks like a second unprompted jump.
            if (Time.time - lastGroundedTime <= 0.2f)
            {
                lastGroundedTime = -10f; // Consume coyote time so double jump is prevented
                jumpBufferTimer = 0f;    // Clear any stale buffer
                ExecuteJump();
            }
            // If in mid-air and not within coyote window, do nothing (no buffer).
        }

        void ExecuteJump()
        {
            verticalSpeed = Mathf.Sqrt(jumpHeight * -2f * gravity);

            if (animator != null)
            {
                if (animatorHasJump)
                {
                    animator.ResetTrigger(jumpHash);
                }
                // CrossFade immediately with a slight start offset to skip the Mixamo crouch wind-up
                // so the visual takeoff matches the physical jump without delay.
                animator.CrossFadeInFixedTime("Jump", 0.04f, 0, jumpAnimStartOffset);
            }
        }

        void CheckKeyboardJump()
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
            {
                Jump();
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Jump();
            }
#endif
        }

        void CheckKeyboardSprint()
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed)
                {
                    IsRunning = true;
                }
                else if (keyboard.leftShiftKey.wasReleasedThisFrame || keyboard.rightShiftKey.wasReleasedThisFrame)
                {
                    IsRunning = false;
                }

                if (keyboard.rKey.wasPressedThisFrame)
                {
                    ToggleRun();
                }
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                IsRunning = true;
            }
            else if (Input.GetKeyUp(KeyCode.LeftShift) || Input.GetKeyUp(KeyCode.RightShift))
            {
                IsRunning = false;
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                ToggleRun();
            }
#endif
        }

        Vector2 ReadInput()
        {
            if (joystick == null && genericJoystick == null)
            {
                ResolveJoystick();
            }

            if (joystick != null && joystick.Value.sqrMagnitude > 0.01f)
            {
                Vector2 val = joystick.Value;
                if (autoSprintOnTiltMax && val.magnitude > 0.9f)
                {
                    IsRunning = true;
                }
                return val;
            }

            if (genericJoystick != null && genericJoystick.Direction.sqrMagnitude > 0.01f)
            {
                Vector2 val = genericJoystick.Direction;
                if (autoSprintOnTiltMax && val.magnitude > 0.9f)
                {
                    IsRunning = true;
                }
                return val;
            }

            return ReadKeyboard();
        }

        // Keyboard fallback so you can test in the Editor. Works with either Input setting.
        Vector2 ReadKeyboard()
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard == null) return Vector2.zero;
            float x = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
            float y = (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);
            return new Vector2(x, y);
#elif ENABLE_LEGACY_INPUT_MANAGER
            return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
#else
            return Vector2.zero;
#endif
        }
    }
}
