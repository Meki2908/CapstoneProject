using System.Collections;
using UnityEngine;

/// <summary>
/// Quản lý nhạc nền dungeon: pre-boss bật ngay <see cref="OnDungeonFlowStarted"/> — không gắn countdown/wave segment.
/// Chuyển boss: <see cref="BossPresenceEnter"/> sau spawn + Phase 2 từ BossMultiSkill. Không có hook OST theo wave.
/// </summary>
public class DungeonOSTManager : MonoBehaviour
{
    public static DungeonOSTManager Instance { get; private set; }

    /// <summary>Còn ít nhất một boss gameplay đang được đếm (spawn register − death leave).</summary>
    public bool BossIsPresented => _bossPresenceCount > 0;

    /// <summary>Loại enemy được tính là boss cho OST (demon, miniboss, legacy boss).</summary>
    public static bool IsOstBossCategory(EnemyScript.EnemyType t)
    {
        switch (t)
        {
            case EnemyScript.EnemyType.boss:
            case EnemyScript.EnemyType.demon:
            case EnemyScript.EnemyType.stoneogre:
            case EnemyScript.EnemyType.golem:
            case EnemyScript.EnemyType.minotaur:
            case EnemyScript.EnemyType.ifrit:
                return true;
            default:
                return false;
        }
    }

    [Header("Audio")]
    [Tooltip("AudioSource 2D cho nhạc nền. Nên tách khỏi SFX.")]
    [SerializeField] private AudioSource musicSource;

    [Header("Pre-boss (wave không có boss)")]
    [Tooltip("Random từng clip; mỗi clip chạy hết (không loop) rồi mới tới clip khác — không bị wave 1 cắt ngang.")]
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
    private Coroutine _preBossPlaybackRoutine;
    private float _defaultVolume = 1f;

    /// <summary>Đã bật nhạc boss — chặn wave/minion gọi lại đổi pre-boss hoặc phase 1.</summary>
    private bool _bossMusicLocked;

    private int _bossPresenceCount;

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
    /// Gọi khi bắt đầu dungeon — reset và phát pre-boss ngay.
    /// </summary>
    public void OnDungeonFlowStarted()
    {
        StopPreBossPlaybackMonitor();
        _bossMusicLocked = false;
        _preBossPicked = false;
        _chosenPreBossClip = null;
        _bossPresenceCount = 0;

        if (showDebugLog) Debug.Log("[DungeonOST] Flow started — reset pre-boss pick.");

        StartPreBossMusicOnDungeonEnter();
    }

    /// <summary>
    /// Sau spawn (wave hoặc summon): đợi 2 frame rồi đọc <see cref="EnemyScript.enemyType"/>; nếu boss OST thì <see cref="BossPresenceEnter"/>.
    /// </summary>
    public void ScheduleBossPresenceCheckForSpawnedRoot(GameObject enemyRoot)
    {
        if (enemyRoot == null) return;
        StartCoroutine(CoRegisterBossPresenceAfterSpawn(enemyRoot));
    }

    private IEnumerator CoRegisterBossPresenceAfterSpawn(GameObject enemyRoot)
    {
        yield return null;
        yield return null;
        if (enemyRoot == null) yield break;

        EnemyScript es = enemyRoot.GetComponentInChildren<EnemyScript>(true);
        if (es == null || !es.gameObject.activeInHierarchy) yield break;
        if (!IsOstBossCategory(es.enemyType)) yield break;

        BossPresenceEnter();
    }

    /// <summary>
    /// Gọi sau khi spawn EnemyNew và đã xác định <see cref="EnemyScript"/> (boss category).
    /// </summary>
    public void BossPresenceEnter()
    {
        int before = _bossPresenceCount;
        _bossPresenceCount++;
        StopPreBossPlaybackMonitor();
        if (showDebugLog) Debug.Log($"[DungeonOST] Boss presence enter (count {before} → {_bossPresenceCount})");

        if (before == 0 && !_bossMusicLocked)
            OnBossEnteredPhase1();
    }

    /// <summary>
    /// Gọi khi một boss (OST category) chết — <see cref="EnemyDeathBridge"/>.
    /// </summary>
    public void BossPresenceLeave()
    {
        if (_bossPresenceCount <= 0)
            return;
        _bossPresenceCount--;
        if (showDebugLog) Debug.Log($"[DungeonOST] Boss presence leave (count → {_bossPresenceCount})");

        if (_bossPresenceCount <= 0)
            OnBossDefeated();
    }

    private void StartPreBossMusicOnDungeonEnter()
    {
        if (musicSource == null)
        {
            if (showDebugLog) Debug.LogWarning("[DungeonOST] No AudioSource — bỏ qua nhạc khi vào dungeon.");
            return;
        }

        if (_bossPresenceCount > 0 || _bossMusicLocked)
            return;

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
            PlayClipCrossfade(_chosenPreBossClip, loop: false, force: true);
            if (showDebugLog) Debug.Log($"[DungeonOST] Pre-boss theme (dungeon enter, play to end): {_chosenPreBossClip.name}");
            RestartPreBossPlaybackMonitor();
        }
        else if (showDebugLog)
            Debug.LogWarning("[DungeonOST] preBossThemes có slot null — không phát nhạc.");
    }

    private void StopPreBossPlaybackMonitor()
    {
        if (_preBossPlaybackRoutine != null)
        {
            StopCoroutine(_preBossPlaybackRoutine);
            _preBossPlaybackRoutine = null;
        }
    }

    private void RestartPreBossPlaybackMonitor()
    {
        StopPreBossPlaybackMonitor();
        if (musicSource == null) return;
        if (_bossMusicLocked || _bossPresenceCount > 0) return;
        _preBossPlaybackRoutine = StartCoroutine(CoPreBossPlayUntilEnd());
    }

    private IEnumerator CoPreBossPlayUntilEnd()
    {
        const int maxWaitPlaybackStartFrames = 120;

        while (musicSource != null && !_bossMusicLocked && _bossPresenceCount == 0)
        {
            AudioClip playing = musicSource.clip;
            if (playing == null || musicSource.loop)
            {
                _preBossPlaybackRoutine = null;
                yield break;
            }

            // Không coi !isPlaying là "hết clip" khi AudioSource chưa kịp Play (vài frame đầu / crossfade).
            int waitStart = 0;
            while (musicSource != null &&
                   musicSource.clip == playing &&
                   !musicSource.isPlaying &&
                   waitStart < maxWaitPlaybackStartFrames)
            {
                if (_bossMusicLocked || _bossPresenceCount > 0)
                {
                    _preBossPlaybackRoutine = null;
                    yield break;
                }
                waitStart++;
                yield return null;
            }

            if (musicSource == null || _bossMusicLocked || _bossPresenceCount > 0)
            {
                _preBossPlaybackRoutine = null;
                yield break;
            }

            if (musicSource.clip == playing && !musicSource.isPlaying)
            {
                if (showDebugLog)
                    Debug.LogWarning("[DungeonOST] Pre-boss clip không phát — dừng monitor chain.");
                _preBossPlaybackRoutine = null;
                yield break;
            }

            yield return new WaitUntil(() =>
                musicSource == null ||
                _bossMusicLocked ||
                _bossPresenceCount > 0 ||
                !musicSource.isPlaying);

            if (musicSource == null || _bossMusicLocked || _bossPresenceCount > 0)
            {
                _preBossPlaybackRoutine = null;
                yield break;
            }

            if (preBossThemes == null || preBossThemes.Length == 0)
            {
                _preBossPlaybackRoutine = null;
                yield break;
            }

            PickRandomPreBossPreferDifferent(playing);
            if (_chosenPreBossClip == null)
            {
                _preBossPlaybackRoutine = null;
                yield break;
            }

            PlayClipCrossfade(_chosenPreBossClip, loop: false, force: true);
            _preBossPicked = true;
            if (showDebugLog) Debug.Log($"[DungeonOST] Pre-boss next clip: {_chosenPreBossClip.name}");
        }

        _preBossPlaybackRoutine = null;
    }

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

        StopPreBossPlaybackMonitor();
        PlayClipCrossfade(bossThemePhase2, loop: true, force: true);
        _bossMusicLocked = true;
        _preBossPicked = true;
        if (showDebugLog) Debug.Log("[DungeonOST] Boss theme Phase 2.");
    }

    public void OnBossEnteredPhase1()
    {
        if (musicSource == null) return;
        if (bossThemePhase1 == null)
        {
            if (showDebugLog) Debug.LogWarning("[DungeonOST] bossThemePhase1 chưa gán.");
            return;
        }

        if (_bossMusicLocked)
        {
            if (showDebugLog) Debug.Log("[DungeonOST] Boss music locked — ignoring Phase 1 request (e.g. summon).");
            return;
        }

        StopPreBossPlaybackMonitor();
        _preBossPicked = true;

        if (musicSource.clip == bossThemePhase1 && musicSource.isPlaying)
        {
            _bossMusicLocked = true;
            return;
        }

        PlayClipCrossfade(bossThemePhase1, loop: true, force: true);
        _bossMusicLocked = true;
        if (showDebugLog) Debug.Log("[DungeonOST] Boss theme Phase 1 (boss appear).");
    }

    public void OnBossDefeated()
    {
        if (!_bossMusicLocked) return;
        _bossMusicLocked = false;
        if (showDebugLog) Debug.Log("[DungeonOST] Boss defeated — music lock released.");
    }

    public void OnDungeonMusicEnd()
    {
        _bossMusicLocked = false;
        StopPreBossPlaybackMonitor();
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

    private void PickRandomPreBossPreferDifferent(AudioClip exclude)
    {
        if (preBossThemes == null || preBossThemes.Length == 0)
        {
            _chosenPreBossClip = null;
            return;
        }
        if (preBossThemes.Length == 1)
        {
            _chosenPreBossClip = preBossThemes[0];
            return;
        }
        int guard = 0;
        do
        {
            PickRandomPreBoss();
            guard++;
        } while (_chosenPreBossClip == exclude && guard < 24);
    }

    private void PlayClipCrossfade(AudioClip clip, bool loop, bool force = false)
    {
        if (clip == null) return;
        if (!force && musicSource != null && musicSource.clip == clip && musicSource.isPlaying)
            return;

        if (_fadeRoutine != null)
        {
            StopCoroutine(_fadeRoutine);
            _fadeRoutine = null;
        }

        if (crossfadeSeconds <= 0f || musicSource == null || !musicSource.isPlaying)
        {
            if (musicSource != null)
            {
                musicSource.Stop();
                musicSource.clip = clip;
                musicSource.loop = loop;
                musicSource.volume = _defaultVolume;
                musicSource.Play();
            }
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
