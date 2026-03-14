using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns pooled footstep VFX from animation events.
/// Optimized for frequent footsteps: no per-step instantiate/destroy.
/// </summary>
public class PlayerFootstepVFX : MonoBehaviour
{
    [Header("Foot Points")]
    [SerializeField] private Transform leftFootPoint;
    [SerializeField] private Transform rightFootPoint;

    [Header("VFX")]
    [SerializeField] private ParticleSystem sandDustPrefab;
    [SerializeField, Min(1)] private int poolSize = 8;

    [Header("Surface Filter")]
    [Tooltip("Raycast only these layers for ground detection.")]
    [SerializeField] private LayerMask groundMask = ~0;
    [Tooltip("If not zero, hit layer must match this mask to be considered sand.")]
    [SerializeField] private LayerMask sandLayerMask;
    [Tooltip("Optional fallback when sandLayerMask is not set.")]
    [SerializeField] private string sandTag = "Sand";
    [SerializeField, Min(0.05f)] private float rayDistance = 0.6f;
    [SerializeField] private Vector3 rayOriginOffset = new Vector3(0f, 0.12f, 0f);

    private readonly List<ParticleSystem> pool = new List<ParticleSystem>();
    private int nextPoolIndex;
    private bool useLeftFootNext = true;

    private void Reset()
    {
        // Auto-find common names once; user can still assign manually.
        if (leftFootPoint == null)
            leftFootPoint = transform.Find("Player/HumanMale_Character_Free/Armature/Root_M/Hip_L/Knee_L/Ankle_L/Footstep left vfx");
        if (rightFootPoint == null)
            rightFootPoint = transform.Find("Player/HumanMale_Character_Free/Armature/Root_M/Hip_R/Knee_R/Ankle_R/Footstep right vfx");
    }

    private void Awake()
    {
        BuildPool();
    }

    /// <summary>
    /// Call from existing footstep animation event.
    /// Alternates left/right so you don't need to edit all clips now.
    /// </summary>
    public void EmitFromAnimationEvent()
    {
        Transform foot = useLeftFootNext ? leftFootPoint : rightFootPoint;
        useLeftFootNext = !useLeftFootNext;
        EmitAtFoot(foot);
    }

    /// <summary>Optional: call by explicit left-foot animation event.</summary>
    public void EmitLeftFromAnimationEvent() => EmitAtFoot(leftFootPoint);

    /// <summary>Optional: call by explicit right-foot animation event.</summary>
    public void EmitRightFromAnimationEvent() => EmitAtFoot(rightFootPoint);

    private void BuildPool()
    {
        pool.Clear();

        if (sandDustPrefab == null)
            return;

        for (int i = 0; i < poolSize; i++)
        {
            ParticleSystem instance = Instantiate(sandDustPrefab, transform);
            instance.gameObject.SetActive(false);
            instance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            pool.Add(instance);
        }

        nextPoolIndex = 0;
    }

    private void EmitAtFoot(Transform footPoint)
    {
        if (footPoint == null || sandDustPrefab == null || pool.Count == 0)
            return;

        if (!TryGetSandHit(footPoint.position + rayOriginOffset, out RaycastHit hit))
            return;

        ParticleSystem ps = pool[nextPoolIndex];
        nextPoolIndex = (nextPoolIndex + 1) % pool.Count;

        // If slot is still in use, recycle it deterministically.
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ps.transform.position = hit.point;
        ps.transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
        ps.gameObject.SetActive(true);
        ps.Play(true);
    }

    private bool TryGetSandHit(Vector3 origin, out RaycastHit hit)
    {
        if (!Physics.Raycast(origin, Vector3.down, out hit, rayDistance, groundMask, QueryTriggerInteraction.Ignore))
            return false;

        bool hasSandLayerMask = sandLayerMask.value != 0;
        if (hasSandLayerMask)
            return ((1 << hit.collider.gameObject.layer) & sandLayerMask.value) != 0;

        return !string.IsNullOrWhiteSpace(sandTag) && hit.collider.CompareTag(sandTag);
    }
}

