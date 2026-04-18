using UnityEngine;

/// <summary>
/// Quản lý SFX cho boss Vargr (Wolf Boss).
///
/// ── Cách dùng ──
/// 1. Gắn script này lên Root GameObject của Vargr (cùng với WolfBossAI).
/// 2. Gán SoundsSO asset vào field "soundsDB".
/// 3. Gán AudioSource cho boss (hoặc để trống — script sẽ tự thêm).
/// 4. Tạo Animation Events trong các clip sau:
///    • clip "na"   → PlayNa()
///    • clip "roar" → PlayRoar()
///    • clip "ulti" → PlayUltimate()
///    • clip walk   → PlayFootstep()   (phase tự động phát hiện)
///    • clip run    → PlayFootstep()
/// 5. Gọi PlayStun() / PlayDie() từ WolfBossAI khi cần.
/// </summary>
public class WolfBossSFX : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  INSPECTOR
    // ─────────────────────────────────────────────────────────────────────────

    [Header("=== References ===")]
    [Tooltip("SoundsSO asset chứa tất cả clip SFX của game.")]
    [SerializeField] private SoundsSO soundsDB;

    [Tooltip("AudioSource phát SFX. Để trống — script tự thêm.")]
    [SerializeField] private AudioSource audioSource;

    [Tooltip("WolfBossAI trên Root — dùng để kiểm tra phase hiện tại.")]
    [SerializeField] private WolfBossAI bossAI;

    [Header("=== Volume ===")]
    [Range(0f, 1f)]
    [Tooltip("Volume override toàn bộ SFX Vargr (nhân với volume trong SoundList).")]
    [SerializeField] private float volumeScale = 1f;

    // ─────────────────────────────────────────────────────────────────────────
    //  LIFECYCLE
    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        // Tự tìm AudioSource nếu chưa gán
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // Tự tìm WolfBossAI nếu chưa gán
        if (bossAI == null)
            bossAI = GetComponent<WolfBossAI>();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  PUBLIC API — gọi từ Animation Events hoặc WolfBossAI
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Phát SFX Normal Attack. Đặt Animation Event vào frame móng chạm.</summary>
    public void PlayNa()
    {
        if (soundsDB == null) return;
        PlaySound(soundsDB.vargrNa);
    }

    /// <summary>Phát SFX Roar (Phase 2 entry). Đặt Animation Event vào frame miệng há.</summary>
    public void PlayRoar()
    {
        if (soundsDB == null) return;
        PlaySound(soundsDB.vargrRoar);
    }

    /// <summary>Phát SFX Ultimate / triệu hồi Fang. Đặt Animation Event vào frame bùng phát.</summary>
    public void PlayUltimate()
    {
        if (soundsDB == null) return;
        PlaySound(soundsDB.vargrUltimate);
    }

    /// <summary>
    /// Phát SFX bước chân — tự động chọn Normal (Phase 1) hoặc Phase 2.
    /// Đặt Animation Event vào mỗi frame chân chạm đất trong clip walk và run.
    /// </summary>
    public void PlayFootstep()
    {
        if (soundsDB == null) return;

        bool isPhase2 = bossAI != null && bossAI.BossPhase >= 2;
        PlaySound(isPhase2 ? soundsDB.vargrFootstepPhase2 : soundsDB.vargrFootstepNormal);
    }

    /// <summary>Phát SFX Stun. Gọi từ WolfBossAI.TriggerStun() hoặc Animation Event.</summary>
    public void PlayStun()
    {
        if (soundsDB == null) return;
        PlaySound(soundsDB.vargrStun);
    }

    /// <summary>Phát SFX chết. Gọi từ WolfBossAI khi boss chết hoặc Animation Event.</summary>
    public void PlayDie()
    {
        if (soundsDB == null) return;
        PlaySound(soundsDB.vargrDie);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  INTERNAL HELPER
    // ─────────────────────────────────────────────────────────────────────────

    private void PlaySound(SoundList soundList)
    {
        if (!soundList.IsValid()) return;
        if (audioSource == null) return;

        AudioClip clip = soundList.GetRandomClip();
        if (clip == null) return;

        float volume = soundList.volume * volumeScale;

        if (soundList.changePitch)
        {
            // ── Validate pitchMin / pitchMax ─────────────────────────────────
            // Default float = 0f → nếu chưa set trong Inspector thì
            // Random.Range(0,0) = 0 → pitch = 0 → tiếng bị câm hoàn toàn.
            // Clamp về [0.5, 1.5] để đảm bảo luôn nghe được.
            float pMin = Mathf.Max(soundList.pitchMin, 0.5f);
            float pMax = Mathf.Max(soundList.pitchMax, 0.5f);
            if (pMin > pMax) pMax = pMin; // tránh invalid range

            float pitch = Random.Range(pMin, pMax);

            // ── Dùng temporary AudioSource riêng cho mỗi clip có pitch ───────
            // audioSource.pitch là thuộc tính GLOBAL của AudioSource.
            // Nếu PlayOneShot chồng nhau (bước chân liên tục), đổi pitch
            // sẽ ảnh hưởng TẤT CẢ clips đang phát → kết quả không nhất quán.
            // Dùng temp GO riêng biệt để cô lập từng clip.
            PlayWithPitch(clip, volume, pitch, soundList);
        }
        else
        {
            // Không thay đổi pitch — dùng audioSource chính bình thường
            if (soundList.mixer != null)
                audioSource.outputAudioMixerGroup = soundList.mixer;

            audioSource.PlayOneShot(clip, volume);
        }
    }

    /// <summary>
    /// Spawn một GameObject tạm với AudioSource riêng để phát clip ở pitch chỉ định,
    /// hoàn toàn cô lập với audioSource chính. GO tự huỷ sau khi clip kết thúc.
    /// </summary>
    private void PlayWithPitch(AudioClip clip, float volume, float pitch, SoundList soundList)
    {
        var tempGO = new GameObject($"[SFX_Pitch] {clip.name}");
        tempGO.transform.position = transform.position;

        var src = tempGO.AddComponent<AudioSource>();
        src.clip         = clip;
        src.volume       = volume;
        src.pitch        = pitch;
        src.spatialBlend = audioSource.spatialBlend; // giữ setting 2D/3D
        src.rolloffMode  = audioSource.rolloffMode;
        src.maxDistance  = audioSource.maxDistance;

        if (soundList.mixer != null)
            src.outputAudioMixerGroup = soundList.mixer;

        src.Play();

        // Tự huỷ sau clip.length / pitch giây (pitch > 1 = nhanh hơn = ngắn hơn)
        Destroy(tempGO, clip.length / Mathf.Max(pitch, 0.01f) + 0.1f);
    }
}

