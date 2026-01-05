using UnityEngine;
using System.Collections;

public class TornadoEffect : BaseEffectScript
{
    [Header("Tornado Settings")]
    [SerializeField] private float tornadoDuration = 2f;
    [SerializeField] private float tornadoHeight = 5f; // Height enemy flies up

    protected override void ApplyEffect(TakeDamageTest enemy)
    {
        // Start tornado rotation effect
        StartCoroutine(TornadoRotationCoroutine(enemy));

        if (debugMode) Debug.Log($"[TornadoEffect] Applied for {tornadoDuration}s");
    }

    private IEnumerator TornadoRotationCoroutine(TakeDamageTest enemy)
    {
        // Get particle collider radius
        var particleCollider = GetComponent<Collider>();
        float radius = particleCollider != null ? particleCollider.bounds.size.x / 2f : 2f; // Default radius if no collider

        // Calculate total rotation (180 degrees)
        float totalRotation = 180f;

        // Store initial position and direction
        Vector3 center = transform.position;
        Vector3 initialDirection = (enemy.transform.position - center).normalized;
        float initialAngle = Mathf.Atan2(initialDirection.z, initialDirection.x) * Mathf.Rad2Deg;

        // Store initial height
        float initialHeight = enemy.transform.position.y;

        float elapsed = 0f;
        while (elapsed < tornadoDuration)
        {
            // Calculate current angle (180 degrees total)
            float currentAngle = initialAngle + (elapsed / tornadoDuration) * totalRotation;

            // Calculate horizontal position on circle
            Vector3 horizontalPosition = center + new Vector3(
                Mathf.Cos(currentAngle * Mathf.Deg2Rad) * radius,
                0,
                Mathf.Sin(currentAngle * Mathf.Deg2Rad) * radius
            );

            // Calculate vertical position (fly up then fall down)
            float heightProgress = elapsed / tornadoDuration;
            float verticalOffset;
            if (heightProgress < 0.5f) // First half: fly up
            {
                float upProgress = heightProgress * 2f; // 0 to 1
                verticalOffset = Mathf.Lerp(0, tornadoHeight, upProgress);
            }
            else // Second half: fall down
            {
                float downProgress = (heightProgress - 0.5f) * 2f; // 0 to 1
                verticalOffset = Mathf.Lerp(tornadoHeight, 0, downProgress);
            }

            // Calculate final position
            Vector3 newPosition = horizontalPosition + Vector3.up * (initialHeight + verticalOffset);

            // Move enemy to new position
            enemy.transform.position = newPosition;

            // Rotate enemy to face movement direction
            Vector3 movementDirection = (newPosition - enemy.transform.position).normalized;
            if (movementDirection != Vector3.zero)
            {
                enemy.transform.rotation = Quaternion.LookRotation(movementDirection);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }
    }
}
