using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Unity.Cinemachine;

public sealed class TimelineMainCameraBinder : MonoBehaviour
{
    [Header("Refs (optional)")]
    [SerializeField] private PlayableDirector director;
    [SerializeField] private Character character;

    [Header("Behavior")]
    [Tooltip("If enabled, binds when this component is enabled (safety-net).")]
    [SerializeField] private bool bindOnEnable = false;

    [Tooltip("Only bind for local player (Fusion HasInputAuthority).")]
    [SerializeField] private bool localPlayerOnly = true;

    private static bool _warnedNoMainCamera;
    private static bool _warnedNoBrain;

    private void Awake()
    {
        if (director == null) director = GetComponent<PlayableDirector>();
        if (character == null) character = GetComponentInParent<Character>();
    }

    private void OnEnable()
    {
        if (bindOnEnable)
            BindToMainCameraBrainIfLocal();
    }

    public bool BindToMainCameraBrainIfLocal()
    {
        if (localPlayerOnly && character != null && !character.HasInputAuthority)
            return false;

        if (director == null)
            return false;

        return BindToMainCameraBrain(director, warnOnce: localPlayerOnly);
    }

    public static bool BindToMainCameraBrain(PlayableDirector playableDirector, bool warnOnce)
    {
        if (playableDirector == null)
            return false;

        if (playableDirector.playableAsset is not TimelineAsset timeline)
            return false;

        var cam = Camera.main;
        if (cam == null)
        {
            cam = FindFirstObjectByType<Camera>();
            if (warnOnce && !_warnedNoMainCamera)
            {
                _warnedNoMainCamera = true;
                Debug.LogWarning("[TimelineBinder] No Camera.main found. Falling back to first Camera in scene.");
            }
        }

        if (cam == null)
            return false;

        var brain = cam.GetComponent<CinemachineBrain>();
        if (brain == null)
        {
            if (warnOnce && !_warnedNoBrain)
            {
                _warnedNoBrain = true;
                Debug.LogWarning("[TimelineBinder] Main camera has no CinemachineBrain. Cinemachine timeline tracks may not work.");
            }
        }

        bool anyBound = false;

        foreach (var output in timeline.outputs)
        {
            var targetType = output.outputTargetType;
            if (targetType == null)
                continue;

            // Bind Cinemachine tracks to the scene's brain.
            if (brain != null && typeof(CinemachineBrain).IsAssignableFrom(targetType))
            {
                playableDirector.SetGenericBinding(output.sourceObject, brain);
                anyBound = true;
                continue;
            }

            // Some timelines might output to Camera directly.
            if (typeof(Camera).IsAssignableFrom(targetType))
            {
                playableDirector.SetGenericBinding(output.sourceObject, cam);
                anyBound = true;
                continue;
            }
        }

        return anyBound;
    }
}

