using UnityEngine;
using Unity.Cinemachine;

[RequireComponent(typeof(CinemachineImpulseSource))]
public class CameraShake : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private CinemachineImpulseSource impulseSource;
    [SerializeField] private ParticleSystem[] particleSystems;

    [Header("Impulse Tuning")]
    [SerializeField] private float pulsesPerSecond = 16f;
    [SerializeField] private float impulseAmplitude = 0.9f;
    [SerializeField] private Vector3 impulseDirection = new Vector3(0.4f, 1f, 0.2f);
    [SerializeField] private bool useUnscaledTime = false;
    [SerializeField] private bool shakeWhenNoParticleFound = false;
    [SerializeField] private bool playOnEnable = false;

    private float nextPulseTime;

    private void Awake()
    {
        if (impulseSource == null) impulseSource = GetComponent<CinemachineImpulseSource>();
        if (particleSystems == null || particleSystems.Length == 0)
            particleSystems = GetComponentsInChildren<ParticleSystem>(true);
    }

    private void OnEnable()
    {
        nextPulseTime = CurrentTime();
        if (playOnEnable)
            EmitPulse();
    }

    private void Update()
    {
        if (!ShouldShake())
        {
            nextPulseTime = CurrentTime();
            return;
        }

        float now = CurrentTime();
        float interval = 1f / Mathf.Max(1f, pulsesPerSecond);
        if (now >= nextPulseTime)
        {
            EmitPulse();
            nextPulseTime = now + interval;
        }
    }

    private bool ShouldShake()
    {
        if (particleSystems == null || particleSystems.Length == 0)
            return shakeWhenNoParticleFound;

        for (int i = 0; i < particleSystems.Length; i++)
        {
            if (particleSystems[i] != null && particleSystems[i].IsAlive(true))
                return true;
        }

        return false;
    }

    private void EmitPulse()
    {
        if (impulseSource == null) return;
        Vector3 direction = impulseDirection.sqrMagnitude > 0.0001f ? impulseDirection.normalized : Vector3.up;
        impulseSource.GenerateImpulse(direction * Mathf.Max(0f, impulseAmplitude));
    }

    private float CurrentTime()
    {
        return useUnscaledTime ? Time.unscaledTime : Time.time;
    }

    private void Reset()
    {
        impulseSource = GetComponent<CinemachineImpulseSource>();
        particleSystems = GetComponentsInChildren<ParticleSystem>(true);
    }
}
