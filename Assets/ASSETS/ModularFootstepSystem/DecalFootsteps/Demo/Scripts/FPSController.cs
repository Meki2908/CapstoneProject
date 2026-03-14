namespace ModularFootstepSystem.Demo
{
    using UnityEngine;

    /// <summary>
    /// First person player controller.
    /// </summary>
    /// <remarks>
    /// Implements movement, acceleration, rotation and jump by the player. 
    /// A simple implementation to demonstrate how the "Modular Footstep System" works.
    /// </remarks>
    [RequireComponent(typeof(CharacterController))]
    public class FPSController : MonoBehaviour
    {
        protected const string MOVE_AXIS_VERTICAL = "Vertical";
        protected const string MOVE_AXIS_HORIZONTAL = "Horizontal";

        protected const string MOUSE_AXIS_VERTICAL = "Mouse Y";
        protected const string MOUSE_AXIS_HORIZONTAL = "Mouse X";

        protected const string JUMP_BUTTON_NAME = "Jump";

        protected const string WALKING_ANIMATION_PARAMETER_NAME = "isWalking";
        protected const string RUNNING_ANIMATION_PARAMETER_NAME = "isRunning";

        [SerializeField]
        protected Camera playerCamera = default;

        [SerializeField]
        protected Animator animator = default;

        [SerializeField]
        protected float walkSpeed = 2f;
        [SerializeField]
        protected float runningSpeed = 3.5f;
        [SerializeField]
        protected float jumpPower = 4f;
        [SerializeField]
        protected float gravity = 10f;

        [SerializeField]
        protected float lookSpeed = 2f;
        [SerializeField]
        protected float lookXLimit = 45f;

        protected Vector3 moveDirection = Vector3.zero;

        protected CharacterController characterController = default;

        protected Vector3 transformForward = Vector3.zero;
        protected Vector3 transformRight = Vector3.zero;

        protected float curSpeedX = 0f;
        protected float curSpeedY = 0f;
        protected float movementDirectionY = 0f;
        protected float rotationX = 0;

        protected bool isRunning = false;

        protected virtual void Start() => characterController = GetComponent<CharacterController>();

        protected virtual void Update()
        {
            transformForward = transform.TransformDirection(Vector3.forward);
            transformRight = transform.TransformDirection(Vector3.right);

            isRunning = Input.GetKey(KeyCode.LeftShift);

            curSpeedX = (isRunning ? runningSpeed : walkSpeed) * Input.GetAxis(MOVE_AXIS_VERTICAL);
            curSpeedY = (isRunning ? runningSpeed : walkSpeed) * Input.GetAxis(MOVE_AXIS_HORIZONTAL);
            movementDirectionY = moveDirection.y;
            moveDirection = (transformForward * curSpeedX) + (transformRight * curSpeedY);

            moveDirection.y = Input.GetButton(JUMP_BUTTON_NAME) && characterController.isGrounded ? jumpPower : movementDirectionY;

            if (!characterController.isGrounded)
            {
                moveDirection.y -= gravity * Time.deltaTime;
            }

            characterController.Move(moveDirection * Time.deltaTime);

            if (!isRunning)
            {
                animator.SetBool(WALKING_ANIMATION_PARAMETER_NAME, !Mathf.Approximately(curSpeedX, 0) || !Mathf.Approximately(curSpeedY, 0));
            }

            animator.SetBool(RUNNING_ANIMATION_PARAMETER_NAME, isRunning);

            rotationX += -Input.GetAxis(MOUSE_AXIS_VERTICAL) * lookSpeed;
            rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
            playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
            transform.rotation *= Quaternion.Euler(0, Input.GetAxis(MOUSE_AXIS_HORIZONTAL) * lookSpeed, 0);
        }
    }
}