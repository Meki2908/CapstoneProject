using UnityEngine;

/// <summary>
/// End-behaviour for VFX: expand horizontally (X/Z) and fade out over time.
/// Designed for particle prefabs used in timeline or one-shot spawn effects.
/// </summary>
[DisallowMultipleComponent]
public class VfxHorizontalExpandFadeOut : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField, Min(0f)] private float delayBeforeTransition = 0.6f;
    [SerializeField, Min(0.05f)] private float scaleDuration = 1.1f;
    [SerializeField] private bool useUnscaledTime = false;

    [Header("Horizontal Expansion")]
    [SerializeField] private Transform scaleTarget;
    [SerializeField, Min(0.01f)] private float horizontalScaleMultiplier = 1.9f;
    [SerializeField] private AnimationCurve horizontalCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField, Min(0.01f)] private float verticalScaleMultiplier = 1f;

    [Header("Fade Out")]
    [SerializeField, Min(0.05f)] private float fadeDuration = 0.45f;
    [SerializeField, Min(0.1f)] private float fadeSpeedMultiplier = 1.2f;
    [SerializeField, Range(0f, 1f)] private float fadeStartOffsetNormalized = 0.2f;
    [SerializeField] private AnimationCurve alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
    [SerializeField] private bool stopEmissionOnStart = true;
    [SerializeField] private ParticleSystem[] particleSystems;
    [SerializeField] private Renderer[] targetRenderers;

    [Header("Finish")]
    [SerializeField] private bool destroyWhenFinished = true;
    [SerializeField, Min(0f)] private float destroyDelay = 0.05f;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int TintColorId = Shader.PropertyToID("_TintColor");

    private Vector3 _initialScale;
    private float _time;
    private bool _finished;
    private bool _transitionStarted;
    private MaterialPropertyBlock _block;

    private void Awake()
    {
        if (scaleTarget == null)
            scaleTarget = transform;

        _initialScale = scaleTarget.localScale;
        _block = new MaterialPropertyBlock();

        if (particleSystems == null || particleSystems.Length == 0)
            particleSystems = GetComponentsInChildren<ParticleSystem>(true);

        if (targetRenderers == null || targetRenderers.Length == 0)
            targetRenderers = GetComponentsInChildren<ParticleSystemRenderer>(true);
    }

    private void OnEnable()
    {
        _time = 0f;
        _finished = false;
        _transitionStarted = false;
        _initialScale = scaleTarget != null ? scaleTarget.localScale : transform.localScale;
        ApplyAlpha(1f);
    }

    private void Update()
    {
        if (_finished || scaleDuration <= 0f || fadeDuration <= 0f)
            return;

        _time += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        if (!_transitionStarted)
        {
            if (_time < delayBeforeTransition)
                return;

            _transitionStarted = true;
            _time = 0f;

            if (stopEmissionOnStart && particleSystems != null)
            {
                for (int i = 0; i < particleSystems.Length; i++)
                {
                    if (particleSystems[i] == null)
                        continue;
                    particleSystems[i].Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }
            }
        }

        float scaleT = Mathf.Clamp01(_time / scaleDuration);
        float fadeElapsed = Mathf.Max(0f, _time - (scaleDuration * fadeStartOffsetNormalized));
        float fadeT = Mathf.Clamp01((fadeElapsed / fadeDuration) * fadeSpeedMultiplier);

        ApplyScale(scaleT);
        ApplyAlpha(alphaCurve.Evaluate(fadeT));

        if (Mathf.Max(scaleT, fadeT) >= 1f)
            Finish();
    }

    private void ApplyScale(float t)
    {
        if (scaleTarget == null)
            return;

        float k = Mathf.LerpUnclamped(1f, horizontalScaleMultiplier, horizontalCurve.Evaluate(t));
        scaleTarget.localScale = new Vector3(
            _initialScale.x * k,
            _initialScale.y * verticalScaleMultiplier,
            _initialScale.z * k
        );
    }

    private void ApplyAlpha(float alpha)
    {
        if (targetRenderers == null)
            return;

        float a = Mathf.Clamp01(alpha);

        for (int i = 0; i < targetRenderers.Length; i++)
        {
            Renderer rendererRef = targetRenderers[i];
            if (rendererRef == null)
                continue;

            rendererRef.GetPropertyBlock(_block);
            Material shared = rendererRef.sharedMaterial;
            if (shared == null)
                continue;

            bool hasAnyColor = false;
            if (shared.HasProperty(BaseColorId))
            {
                Color c = shared.GetColor(BaseColorId);
                c.a = a;
                _block.SetColor(BaseColorId, c);
                hasAnyColor = true;
            }
            if (shared.HasProperty(ColorId))
            {
                Color c = shared.GetColor(ColorId);
                c.a = a;
                _block.SetColor(ColorId, c);
                hasAnyColor = true;
            }
            if (shared.HasProperty(TintColorId))
            {
                Color c = shared.GetColor(TintColorId);
                c.a = a;
                _block.SetColor(TintColorId, c);
                hasAnyColor = true;
            }

            if (hasAnyColor)
                rendererRef.SetPropertyBlock(_block);
        }
    }

    private void Finish()
    {
        _finished = true;
        ApplyAlpha(0f);

        if (!destroyWhenFinished)
            return;

        Destroy(gameObject, destroyDelay);
    }
}
