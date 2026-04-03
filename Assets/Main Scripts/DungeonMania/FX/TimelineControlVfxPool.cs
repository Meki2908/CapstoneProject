using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Use as Timeline Control track binding target instead of a heavy nested VFX prefab.
/// Prewarms pooled instances (via <see cref="BossTimelineVfxPrewarmer"/> on the boss root)
/// so the Control clip only enables pre-built objects.
/// </summary>
[DisallowMultipleComponent]
public class TimelineControlVfxPool : MonoBehaviour
{
    [SerializeField] GameObject vfxPrefab;
    [SerializeField] [Min(1)] int poolSize = 2;

    readonly Queue<GameObject> _available = new Queue<GameObject>();
    GameObject _active;

    bool _prewarmed;

    /// <summary>Called from an active ancestor (e.g. boss root) before the Timeline enables this object.</summary>
    public void Prewarm()
    {
        if (_prewarmed || vfxPrefab == null)
            return;
        _prewarmed = true;
        for (var i = 0; i < poolSize; i++)
        {
            var inst = Instantiate(vfxPrefab, transform, false);
            inst.SetActive(false);
            StopAndClearParticles(inst);
            _available.Enqueue(inst);
        }
    }

    void OnEnable()
    {
        if (!_prewarmed)
            Prewarm();

        if (vfxPrefab == null)
            return;

        ReleaseActive();

        if (_available.Count > 0)
            _active = _available.Dequeue();
        else
            _active = Instantiate(vfxPrefab, transform, false);

        _active.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        _active.transform.localScale = Vector3.one;
        _active.SetActive(true);

        foreach (var ps in _active.GetComponentsInChildren<ParticleSystem>(true))
            ps.Play(true);
    }

    void OnDisable()
    {
        ReleaseActive();
    }

    void ReleaseActive()
    {
        if (_active == null)
            return;
        StopAndClearParticles(_active);
        _active.SetActive(false);
        _active.transform.SetParent(transform, false);
        _available.Enqueue(_active);
        _active = null;
    }

    static void StopAndClearParticles(GameObject root)
    {
        foreach (var ps in root.GetComponentsInChildren<ParticleSystem>(true))
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Clear(true);
        }
    }
}
