using UnityEngine;
using System.Linq;

[RequireComponent(typeof(CharacterController))]
public class NPCPathFollower_CC : MonoBehaviour
{
    public WaypointPath path;

    [Header("Movement")]
    public float moveSpeed = 2.5f;
    public float turnSpeed = 720f;          // độ/giây
    public float arriveDistance = 0.3f;

    [Header("Path Mode")]
    public bool loop = true;                // hết điểm quay về đầu
    public bool pingPong = false;           // đi qua lại (ưu tiên hơn loop)

    [Header("Animation")]
    public Animator animator;
    public string speedParam = "Speed";     // parameter float trong Animator

    [Header("Optional Gravity")]
    public bool useGravity = true;
    public float gravity = -9.81f;

    private CharacterController cc;
    private int index = 0;
    private int dir = 1;                    // dùng cho pingpong
    private float verticalVelocity;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        if (!animator) animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (path == null || path.points == null || path.points.Count == 0) return;

        Transform target = path.points[index];
        if (target == null) return;

        Vector3 targetPos = target.position;

        // bỏ Y nếu game của bạn là top-down phẳng (tùy bạn)
        // targetPos.y = transform.position.y;

        Vector3 toTarget = targetPos - transform.position;
        toTarget.y = 0f;

        float distance = toTarget.magnitude;

        // đến gần điểm -> chuyển điểm tiếp theo
        if (distance <= arriveDistance)
        {
            AdvanceIndex();
            target = path.points[index];
            if (target == null) return;

            targetPos = target.position;
            toTarget = targetPos - transform.position;
            toTarget.y = 0f;
        }

        Vector3 moveDir = toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : Vector3.zero;

        // xoay mượt theo hướng di chuyển
        if (moveDir != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
        }

        // di chuyển
        Vector3 velocity = moveDir * moveSpeed;

        if (useGravity)
        {
            if (cc.isGrounded && verticalVelocity < 0f) verticalVelocity = -1f;
            verticalVelocity += gravity * Time.deltaTime;
            velocity.y = verticalVelocity;
        }

        cc.Move(velocity * Time.deltaTime);

        // animation speed (0..moveSpeed)
        if (animator)
        {
            float planarSpeed = new Vector3(cc.velocity.x, 0, cc.velocity.z).magnitude;
            // Check if the speed parameter exists before setting it
            if (animator.parameters.Any(p => p.name == speedParam))
            {
                animator.SetFloat(speedParam, planarSpeed);
            }
        }
    }

    private void AdvanceIndex()
    {
        int count = path.points.Count;

        if (pingPong)
        {
            index += dir;
            if (index >= count)
            {
                index = count - 2;
                dir = -1;
            }
            else if (index < 0)
            {
                index = 1;
                dir = 1;
            }
            return;
        }

        index++;
        if (index >= count)
        {
            if (loop) index = 0;
            else index = count - 1; // đứng ở điểm cuối
        }
    }
}
