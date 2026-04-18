using UnityEngine;
using Fusion;

public class BossFang : NetworkBehaviour
{
    // ── Loại Nanh ────────────────────────────────────────────────────────────

    public enum FangType { FireFang, IceFang }

    [Header("Fang Settings")]
    [SerializeField] private FangType fangType = FangType.FireFang;

    [Header("Death VFX")]
    [Tooltip("Prefab VFX hiện ra khi Fang bị tiêu diệt. Để trống nếu không cần.")]
    [SerializeField] private GameObject deathVFXPrefab;
    [Tooltip("Thời gian (giây) trước khi tự huỷ VFX. 0 = không tự huỷ (prefab tự xử lý).")]
    [SerializeField] private float deathVFXDuration = 3f;

    [Header("Death SFX")]
    [Tooltip("SoundsSO asset chứa clip âm thanh của Fang. Gán cùng asset với WolfBossSFX.")]
    [SerializeField] private SoundsSO soundsDB;
    [Tooltip("Delay (giây) trước khi Destroy/Despawn thật — để SFX phát xong.\nNên đặt bằng hoặc lớn hơn độ dài clip.")]
    [SerializeField] private float deathSFXDelay = 1f;

    // ── Networked State ───────────────────────────────────────────────────────

    [Networked] public NetworkBool IsFangAlive { get; set; }

    // ── Runtime Refs ──────────────────────────────────────────────────────────

    // Được set bởi WolfBossAI khi spawn
    [HideInInspector] public WolfBossAI bossRef;

    public FangType Type => fangType;

    // ── TakeDamageTest integration ────────────────────────────────────────────

    private TakeDamageTest _health;

    // Local alive flag — dùng khi Fusion không active (standalone mode)
    private bool _localAlive = false;

    // ── CC Immunity ───────────────────────────────────────────────────────────
    // EnemyScript.isBoss = true → BaseEffectScript bỏ qua ApplyEffect()
    // (hất tung, đẩy lùi, tornado... không tác dụng lên Fang)
    private EnemyScript _enemyScript;

    // ── AudioSource (luôn trên root prefab) ──────────────────────────────────
    // Tìm trong Awake, KHÔNG tạo dynamic — phải có sẵn trên prefab.
    private AudioSource _audioSource;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        // AudioSource phải có sẵn trên root prefab
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
            Debug.LogWarning($"[BossFang] {fangType} — Không tìm thấy AudioSource trên root! " +
                             "Thêm AudioSource component vào prefab Fang.");

        // CC Immunity: set isBoss=true để BaseEffectScript bỏ qua hất tung / đẩy lùi
        _enemyScript = GetComponent<EnemyScript>();
        if (_enemyScript == null) _enemyScript = GetComponentInChildren<EnemyScript>();
        if (_enemyScript != null)
        {
            _enemyScript.isBoss = true;
            Debug.Log($"[BossFang] {fangType} — isBoss=true set (CC immune).");
        }
        else
        {
            Debug.LogWarning($"[BossFang] {fangType} — EnemyScript không tìm thấy! " +
                             "Fang sẽ KHÔNG immune CC. Thêm EnemyScript lên prefab Fang.");
        }
    }

    /// <summary>
    /// Start(): xử lý standalone mode — khi không có Fusion session,
    /// Spawned() không bao giờ được gọi, nên ta subscribe health events ở đây.
    /// </summary>
    private void Start()
    {
        bool hasFusion = Runner != null && Object != null && Object.IsValid;
        if (!hasFusion)
        {
            _localAlive = true;
            SubscribeHealth();
            Debug.Log($"[BossFang] {fangType} — Standalone mode, subscribed health events via Start().");
        }
    }

    public override void Spawned()
    {
        IsFangAlive = true;
        _localAlive  = true;
        SubscribeHealth();
        Debug.Log($"[BossFang] {fangType} Spawned (Fusion).");
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        UnsubscribeHealth();
    }

    private void OnDestroy()
    {
        UnsubscribeHealth();
    }

    // ── Health subscription helpers ───────────────────────────────────────────

    private void SubscribeHealth()
    {
        _health = GetComponent<TakeDamageTest>();
        if (_health == null) _health = GetComponentInChildren<TakeDamageTest>();

        if (_health != null)
        {
            _health.OnEnemyDied -= OnFangKilled;
            _health.OnEnemyDied += OnFangKilled;
        }
        else
        {
            Debug.LogWarning($"[BossFang] {fangType}: TakeDamageTest không tìm thấy! " +
                             "Fang sẽ không báo death về Boss.");
        }
    }

    private void UnsubscribeHealth()
    {
        if (_health != null)
            _health.OnEnemyDied -= OnFangKilled;
    }

    /// <summary>
    /// Object Pooling: fang bị SetActive(false) thay vì Destroy/Despawn.
    /// </summary>
    private void OnDisable()
    {
        bool alreadyDead = (Runner != null && Object != null && Object.IsValid)
            ? !IsFangAlive
            : !_localAlive;

        if (alreadyDead) return;

        bool hasFusion = Runner != null && Object != null && Object.IsValid;
        if (hasFusion && !Object.HasStateAuthority) return;

        OnFangKilled();
    }

    // ── Logic ─────────────────────────────────────────────────────────────────

    private void OnFangKilled()
    {
        bool hasFusion = Runner != null && Object != null && Object.IsValid;
        if (hasFusion)
        {
            if (!IsFangAlive) return;
            IsFangAlive = false;
        }
        else
        {
            if (!_localAlive) return;
        }

        _localAlive = false;
        Debug.Log($"[BossFang] {fangType} destroyed!");

        // Spawn VFX + phát SFX ngay lập tức
        SpawnDeathVFX();
        PlayDeathSFX();

        // Thông báo cho Boss
        if (bossRef != null)
            bossRef.OnFangDestroyed(this);

        // Unsubscribe để tránh double-call
        UnsubscribeHealth();

        // Tắt visual & collider ngay — Fang "biến mất" với player
        // Nhưng giữ GO sống thêm deathSFXDelay giây để AudioSource phát xong
        DisableVisuals();

        StartCoroutine(DelayedDestroy(hasFusion));
    }

    /// <summary>
    /// Tắt Renderer, Collider, ParticleSystem ngay khi die.
    /// AudioSource vẫn giữ nguyên để SFX phát đủ thời gian.
    /// </summary>
    private void DisableVisuals()
    {
        foreach (var r in GetComponentsInChildren<Renderer>())
            r.enabled = false;

        foreach (var c in GetComponentsInChildren<Collider>())
            c.enabled = false;

        foreach (var ps in GetComponentsInChildren<ParticleSystem>())
            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    /// <summary>
    /// Chờ deathSFXDelay giây rồi mới Despawn (Fusion) hoặc Destroy (standalone).
    /// </summary>
    private System.Collections.IEnumerator DelayedDestroy(bool hasFusion)
    {
        yield return new WaitForSeconds(deathSFXDelay);

        if (hasFusion)
        {
            if (Runner != null && Object != null && Object.IsValid && Object.HasStateAuthority)
                Runner.Despawn(Object);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ── Audio / VFX ───────────────────────────────────────────────────────────

    private void SpawnDeathVFX()
    {
        if (deathVFXPrefab == null) return;

        GameObject vfx = Instantiate(deathVFXPrefab, transform.position, Quaternion.identity);
        if (deathVFXDuration > 0f)
            Destroy(vfx, deathVFXDuration);
    }

    private void PlayDeathSFX()
    {
        if (_audioSource == null || soundsDB == null) return;
        if (!soundsDB.vargrFangDie.IsValid()) return;

        SoundList sfx = soundsDB.vargrFangDie;
        AudioClip clip = sfx.GetRandomClip();
        if (clip == null) return;

        _audioSource.pitch = sfx.changePitch
            ? Random.Range(sfx.pitchMin, sfx.pitchMax)
            : 1f;

        if (sfx.mixer != null)
            _audioSource.outputAudioMixerGroup = sfx.mixer;

        _audioSource.PlayOneShot(clip, sfx.volume);
    }

    /// <summary>
    /// Gọi công khai nếu cần force-kill Fang từ bên ngoài (vd: despawn scene).
    /// </summary>
    public void ForceKill()
    {
        OnFangKilled();
    }

    // ── Gizmos ────────────────────────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = fangType == FangType.FireFang ? Color.red : Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}
