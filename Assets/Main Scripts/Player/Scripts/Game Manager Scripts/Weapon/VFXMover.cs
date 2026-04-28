using UnityEngine;

public class VfxMover : MonoBehaviour
{
    [SerializeField] private Vector3 directionWS = Vector3.forward;
    [SerializeField] private float speed = 10f;
    [SerializeField] private float lifetime = 2f;
    [SerializeField] private bool alignToDirection = true;
    private float dieAt = -1f;

    public void Launch(Vector3 dirWorld, float moveSpeed, float lifeSeconds, bool align = true)
    {
        directionWS = dirWorld.sqrMagnitude > 0.0001f ? dirWorld.normalized : Vector3.forward;
        speed = Mathf.Max(0f, moveSpeed);
        lifetime = Mathf.Max(0f, lifeSeconds);
        alignToDirection = align;
        if (alignToDirection && directionWS.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(directionWS, Vector3.up);
        dieAt = (lifetime > 0f) ? Time.time + lifetime : -1f;
    }

    private void Update()
    {
        if (speed > 0f) transform.position += directionWS * (speed * Time.deltaTime);
        if (dieAt > 0f && Time.time >= dieAt) Destroy(gameObject);
    }
}