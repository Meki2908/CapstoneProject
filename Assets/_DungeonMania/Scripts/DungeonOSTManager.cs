using System.Collections;
using UnityEngine;

/// <summary>
/// Quản lý nhạc nền dungeon: trước boss = random 1 trong 2 theme; boss wave = phase 1;
/// khi boss (BossMultiSkill) vào Phase 2 = theme phase 2.
/// Gắn vào scene dungeon (cùng GameObject với DungeonWaveManager hoặc riêng), gán AudioSource + clip.
/// </summary>
public class DungeonOSTManager : MonoBehaviour
{
    public static DungeonOSTManager Instance { get; private set; }

    [Header("Audio")]
    [Tooltip("AudioSource 2D cho nhạc nền (loop). Nên tách khỏi SFX.")]
    [SerializeField] private AudioSource musicSource;

    [Header("Pre-boss (wave không có boss)")]
    [Tooltip("Đúng 2 clip — random 1 lần khi bắt đầu dungeon (giữ nguyên qua các wave trước boss).")]
    [SerializeField] private AudioClip[] preBossThemes = new AudioClip[2];

    [Header("Boss")]
    [SerializeField] private AudioClip bossThemePhase1;
    [SerializeField] private AudioClip bossThemePhase2;

    [Header("Tùy chọn")]
    [SerializeField] private float crossfadeSeconds = 0.75f;
    [SerializeField] private bool stopMusicOnDungeonEnd = true;
    [SerializeField] private float fadeOutOnEndSeconds = 1.25f;
    [SerializeField] private bool showDebugLog = true;

    private bool _preBossPicked;
    private AudioClip _chosenPreBossClip;
    private Coroutine _fadeRoutine;
    private float _defaultVolume = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[DungeonOST] Duplicate DungeonOSTManager — destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (musicSource == null)
            musicSource = GetComponent<AudioSource>();

        if (musicSource != null)
        {
            _defaultVolume = musicSource.volume;
            musicSource.loop = true;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// Gọi khi bắt đầu dungeon (StartDungeon) — reset random pre-boss và phát nhạc pre-boss ngay (vào dungeon là có nhạc, không chờ wave / intro).
    /// </summary>
    public void OnDungeonFlowStarted()
    {
        _preBossPicked = false;
        _chosenPreBossClip = null;
        if (showDebugLog) Debug.Log("[DungeonOST] Flow started — reset pre-boss pick.");
        StartPreBossMusicOnDungeonEnter();
    }

    /// <summary>
    /// Random 1 theme pre-boss và phát ngay khi vào dungeon.
    /// </summary>
    private void StartPreBossMusicOnDungeonEnter()
    {
        if (musicSource == null)
        {
            if (showDebugLog) Debug.LogWarning("[DungeonOST] No AudioSource — bỏ qua nhạc khi vào dungeon.");
            return;
        }

        if (preBossThemes == null || preBossThemes.Length == 0)
        {
            _preBossPicked = true;
            if (showDebugLog) Debug.LogWarning("[DungeonOST] Chưa gán preBossThemes — không phát nhạc khi vào dungeon.");
            return;
        }

        PickRandomPreBoss();
        _preBossPicked = true;
        if (_chosenPreBossClip != null)
        {
            PlayClipCrossfade(_chosenPreBossClip, true);
            if (showDebugLog) Debug.Log($"[DungeonOST] Pre-boss theme (dungeon enter): {_chosenPreBossClip.name}");
        }
        else if (showDebugLog)
            Debug.LogWarning("[DungeonOST] preBossThemes có slot null — không phát nhạc.");
    }

    /// <summary>
    /// Gọi mỗi khi bắt đầu một wave (sau intro timeline), trước thông báo wave.
    /// Pre-boss thường đã phát từ <see cref="OnDungeonFlowStarted"/>; ở đây chủ yếu chuyển sang boss phase 1 hoặc fallback nếu chưa phát được.
    /// </summary>
    /// <param name="waveNumber">Wave hiện tại (1-based).</param>
    /// <param name="waveHasBossEnemy">Wave này có spawn boss/demon/miniboss không.</param>
    public void OnWaveSegmentChanged(int waveNumber, bool waveHasBossEnemy)
    {
        if (musicSource == null)
        {
            if (showDebugLog) Debug.LogWarning("[DungeonOST] No AudioSource — bỏ qua.");
            return;
        }

        if (waveHasBossEnemy)
        {
            OnBossEnteredPhase1();
            return;
        }

        if (!_preBossPicked)
        {
            PickRandomPreBoss();
            _preBossPicked = true;
            if (_chosenPreBossClip != null)
            {
                PlayClipCrossfade(_chosenPreBossClip, true);
                if (showDebugLog) Debug.Log($"[DungeonOST] Pre-boss theme: {_chosenPreBossClip.name}");
            }
            else if (showDebugLog)
                Debug.LogWarning("[DungeonOST] Chưa gán preBossThemes — không phát nhạc pre-boss.");
        }
        // Các wave sau (vẫn pre-boss): giữ nguyên nhạc — không restart
    }

    /// <summary>
    /// Gọi từ BossMultiSkill khi Demon/boss vào Phase 2 (HP threshold).
    /// </summary>
    public void OnBossEnteredPhase2()
    {
        if (musicSource == null) return;
        if (bossThemePhase2 == null)
        {
            if (showDebugLog) Debug.LogWarning("[DungeonOST] bossThemePhase2 chưa gán.");
            return;
        }

        if (musicSource.clip == bossThemePhase2 && musicSource.isPlaying)
            return;

        PlayClipCrossfade(bossThemePhase2, true);
        if (showDebugLog) Debug.Log("[DungeonOST] Boss theme Phase 2.");
    }

    /// <summary>
    /// Gọi khi boss xuất hiện (thường là wave cuối) để bật boss theme phase 1.
    /// Dùng thêm hook từ script boss để không phụ thuộc logic "wave has boss" hoặc timeline intro.
    /// </summary>
    public void OnBossEnteredPhase1()
    {
        if (musicSource == null) return;
        if (bossThemePhase1 == null)
        {
            if (showDebugLog) Debug.LogWarning("[DungeonOST] bossThemePhase1 chưa gán.");
            return;
        }

        // Đảm bảo wave handler về sau không cố chọn pre-boss thay vì boss.
        _preBossPicked = true;

        if (musicSource.clip == bossThemePhase1 && musicSource.isPlaying)
            return;

        PlayClipCrossfade(bossThemePhase1, true);
        if (showDebugLog) Debug.Log("[DungeonOST] Boss theme Phase 1 (boss appear).");
    }

    /// <summary>
    /// Khi thắng/thua dungeon — fade out hoặc dừng.
    /// </summary>
    public void OnDungeonMusicEnd()
    {
        if (!stopMusicOnDungeonEnd || musicSource == null) return;
        if (_fadeRoutine != null)
        {
            StopCoroutine(_fadeRoutine);
            _fadeRoutine = null;
        }
        _fadeRoutine = StartCoroutine(CoFadeOutAndStop());
    }

    private void PickRandomPreBoss()
    {
        if (preBossThemes == null || preBossThemes.Length == 0)
        {
            _chosenPreBossClip = null;
            return;
        }
        int i = Random.Range(0, preBossThemes.Length);
        _chosenPreBossClip = preBossThemes[i];
    }

    private void PlayClipCrossfade(AudioClip clip, bool loop)
    {
        if (clip == null) return;
        if (_fadeRoutine != null)
        {
            StopCoroutine(_fadeRoutine);
            _fadeRoutine = null;
        }

        if (crossfadeSeconds <= 0f || !musicSource.isPlaying)
        {
            musicSource.Stop();
            musicSource.clip = clip;
            musicSource.loop = loop;
            musicSource.volume = _defaultVolume;
            musicSource.Play();
            return;
        }

        _fadeRoutine = StartCoroutine(CoCrossfade(clip, loop));
    }

    private IEnumerator CoCrossfade(AudioClip clip, bool loop)
    {
        float half = crossfadeSeconds * 0.5f;
        float t = 0f;
        float startVol = musicSource.volume;

        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(startVol, 0f, t / half);
            yield return null;
        }

        musicSource.Stop();
        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.volume = 0f;
        musicSource.Play();

        t = 0f;
        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(0f, _defaultVolume, t / half);
            yield return null;
        }

        musicSource.volume = _defaultVolume;
        _fadeRoutine = null;
    }

    private IEnumerator CoFadeOutAndStop()
    {
        if (musicSource == null) yield break;
        float t = 0f;
        float start = musicSource.volume;
        while (t < fadeOutOnEndSeconds)
        {
            t += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(start, 0f, t / fadeOutOnEndSeconds);
            yield return null;
        }
        musicSource.Stop();
        musicSource.clip = null;
        musicSource.volume = _defaultVolume;
        _fadeRoutine = null;
        if (showDebugLog) Debug.Log("[DungeonOST] Music stopped (dungeon end).");
    }
}
