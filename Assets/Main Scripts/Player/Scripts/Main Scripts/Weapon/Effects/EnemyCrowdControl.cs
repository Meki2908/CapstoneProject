using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Runtime crowd-control helper for enemy movement displacement.
/// Works with NavMeshAgent-based enemies without requiring Rigidbody forces.
/// </summary>
public class EnemyCrowdControl : MonoBehaviour
{
    private NavMeshAgent navMeshAgent;
    private EnemyScript enemyScript;
    private Rigidbody enemyRigidbody;

    private Coroutine activeControlRoutine;
    private bool agentWasStopped;
    private bool rbWasKinematic;
    private int controlToken;

    private void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        enemyScript = GetComponent<EnemyScript>();
        enemyRigidbody = GetComponent<Rigidbody>();
    }

    public void PlayKnockback(Vector3 sourcePosition, float horizontalDistance, float duration, float peakHeight = 0f)
    {
        Vector3 start = transform.position;
        Vector3 planarDir = transform.position - sourcePosition;
        planarDir.y = 0f;
        if (planarDir.sqrMagnitude < 0.0001f)
        {
            planarDir = transform.forward;
            planarDir.y = 0f;
        }

        Vector3 end = start + planarDir.normalized * Mathf.Max(0f, horizontalDistance);
        end.y = start.y;
        StartControlledMove(start, end, duration, peakHeight);
    }

    public void PlayKnockup(Vector3 sourcePosition, float horizontalDistance, float peakHeight, float riseDuration, float fallDuration)
    {
        Vector3 start = transform.position;
        Vector3 planarDir = transform.position - sourcePosition;
        planarDir.y = 0f;
        if (planarDir.sqrMagnitude < 0.0001f)
        {
            planarDir = transform.forward;
            planarDir.y = 0f;
        }

        Vector3 end = start + planarDir.normalized * Mathf.Max(0f, horizontalDistance);
        end.y = start.y;

        int token = BeginControl();
        activeControlRoutine = StartCoroutine(KnockupRoutine(
            token,
            start,
            end,
            Mathf.Max(0f, peakHeight),
            Mathf.Max(0.01f, riseDuration),
            Mathf.Max(0.01f, fallDuration)
        ));
    }

    public void PlayPull(Vector3 targetPosition, float pullDistance, float duration, float peakHeight = 0f)
    {
        Vector3 start = transform.position;
        Vector3 planarDir = targetPosition - start;
        planarDir.y = 0f;
        if (planarDir.sqrMagnitude < 0.0001f)
            return;

        float distanceToTarget = planarDir.magnitude;
        float moveDistance = Mathf.Clamp(pullDistance, 0f, distanceToTarget);
        Vector3 end = start + planarDir.normalized * moveDistance;
        end.y = start.y;
        StartControlledMove(start, end, duration, peakHeight);
    }

    public void PlayTornado(Vector3 center, float radius, float totalRotationDegrees, float duration, float maxHeight)
    {
        int token = BeginControl();
        activeControlRoutine = StartCoroutine(TornadoRoutine(token, center, Mathf.Max(0.1f, radius), totalRotationDegrees, duration, Mathf.Max(0f, maxHeight)));
    }

    private void StartControlledMove(Vector3 start, Vector3 end, float duration, float peakHeight)
    {
        int token = BeginControl();
        activeControlRoutine = StartCoroutine(MoveRoutine(token, start, end, duration, peakHeight));
    }

    private int BeginControl()
    {
        controlToken++;
        if (activeControlRoutine != null)
        {
            StopCoroutine(activeControlRoutine);
            activeControlRoutine = null;
        }

        if (navMeshAgent != null && navMeshAgent.isOnNavMesh)
        {
            agentWasStopped = navMeshAgent.isStopped;
            navMeshAgent.isStopped = true;
            navMeshAgent.ResetPath();
            navMeshAgent.velocity = Vector3.zero;
        }

        if (enemyRigidbody != null)
        {
            rbWasKinematic = enemyRigidbody.isKinematic;
            enemyRigidbody.isKinematic = true;
            enemyRigidbody.linearVelocity = Vector3.zero;
            enemyRigidbody.angularVelocity = Vector3.zero;
        }

        return controlToken;
    }

    private void EndControl(int token)
    {
        if (token != controlToken)
            return;

        activeControlRoutine = null;

        if (enemyRigidbody != null)
        {
            enemyRigidbody.isKinematic = rbWasKinematic;
        }

        if (navMeshAgent != null && navMeshAgent.isOnNavMesh)
        {
            bool canResume = enemyScript == null || enemyScript.alive;
            navMeshAgent.isStopped = canResume ? agentWasStopped : true;
        }
    }

    private IEnumerator MoveRoutine(int token, Vector3 start, Vector3 end, float duration, float peakHeight)
    {
        float safeDuration = Mathf.Max(0.01f, duration);
        float elapsed = 0f;

        while (elapsed < safeDuration)
        {
            if (token != controlToken) yield break;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);
            float yOffset = 4f * peakHeight * t * (1f - t);

            Vector3 nextPos = Vector3.Lerp(start, end, t);
            nextPos.y = start.y + yOffset;
            transform.position = nextPos;

            yield return null;
        }

        Vector3 finalPos = end;
        finalPos.y = start.y;
        transform.position = finalPos;
        EndControl(token);
    }

    private IEnumerator TornadoRoutine(int token, Vector3 center, float radius, float totalRotationDegrees, float duration, float maxHeight)
    {
        float safeDuration = Mathf.Max(0.01f, duration);
        Vector3 start = transform.position;
        Vector3 offset = start - center;
        offset.y = 0f;
        if (offset.sqrMagnitude < 0.0001f)
            offset = transform.forward * radius;

        float startAngle = Mathf.Atan2(offset.z, offset.x) * Mathf.Rad2Deg;
        float baseY = start.y;
        float elapsed = 0f;

        while (elapsed < safeDuration)
        {
            if (token != controlToken) yield break;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);
            float angle = startAngle + totalRotationDegrees * t;
            float rad = angle * Mathf.Deg2Rad;
            float yOffset = 4f * maxHeight * t * (1f - t);

            Vector3 horizontal = center + new Vector3(Mathf.Cos(rad) * radius, 0f, Mathf.Sin(rad) * radius);
            transform.position = new Vector3(horizontal.x, baseY + yOffset, horizontal.z);

            yield return null;
        }

        EndControl(token);
    }

    private IEnumerator KnockupRoutine(int token, Vector3 start, Vector3 end, float peakHeight, float riseDuration, float fallDuration)
    {
        float totalDuration = riseDuration + fallDuration;
        float elapsed = 0f;

        while (elapsed < totalDuration)
        {
            if (token != controlToken) yield break;

            elapsed += Time.deltaTime;
            float clampedElapsed = Mathf.Min(elapsed, totalDuration);
            float horizontalT = clampedElapsed / totalDuration;

            float yOffset;
            if (clampedElapsed <= riseDuration)
            {
                float riseT = clampedElapsed / riseDuration;
                yOffset = Mathf.Lerp(0f, peakHeight, Mathf.SmoothStep(0f, 1f, riseT));
            }
            else
            {
                float fallT = (clampedElapsed - riseDuration) / fallDuration;
                yOffset = Mathf.Lerp(peakHeight, 0f, Mathf.SmoothStep(0f, 1f, fallT));
            }

            Vector3 nextPos = Vector3.Lerp(start, end, horizontalT);
            nextPos.y = start.y + yOffset;
            transform.position = nextPos;

            yield return null;
        }

        Vector3 finalPos = end;
        finalPos.y = start.y;
        transform.position = finalPos;
        EndControl(token);
    }
}
