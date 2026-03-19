using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// AudioManager tích hợp:
/// 1. DungeonMania audio (PlayOneShot các clip combat/door/item/menu...)
/// 2. Settings volume control (Music/SFX/Background Sound từ GameSettings)
///
/// Quy ước tag AudioSource:
///   - "Music"   → nhạc nền (BGM)  
///   - "Ambient" → âm thanh môi trường (gió, chim, nước)
///   - Không tag → SFX (tự phân loại theo tên hoặc heuristic)
/// </summary>
public class AudioManager : MonoBehaviour {

    // ==================== SINGLETON (cho Settings) ====================
    public static AudioManager Instance { get; private set; }

    // ==================== DUNGEON AUDIO CLIPS ====================
    [HideInInspector]public AudioSource audioSource;
    public AudioClip[] commonSceneAudio;
    public AudioClip[] commonEnemySound;
    public AudioClip[] swordMagicDamage;
    public AudioClip[] simpleDamage;
    public AudioClip[] doorAudio;
    public AudioClip[] itemAudio;
    public AudioClip[] menuAudio;
    public AudioClip[] winAudio;
    public AudioClip[] playerSkills;
    public AudioClip[] playerSlash;
    public AudioClip[] playerHits;
    public AudioClip[] playerCommonAudio;
    public AudioClip[] playerSteps;

    // ==================== SETTINGS VOLUME ====================
    private AudioSource[] _allSceneAudioSources;

    void OnEnable(){
        EnemyScript.WinAudioEvent += WinAudio;
    }
    void OnDisable(){
         EnemyScript.WinAudioEvent -= WinAudio;
    }

	void Start () {
        audioSource = GetComponent<AudioSource>();

        // Singleton — giữ instance đầu tiên
        if (Instance == null)
            Instance = this;

        // Subscribe settings events
        GameSettings.OnSettingsChanged += OnSettingsChanged;
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Apply volumes lần đầu
        RefreshAndApplyVolumes();
	}

    void OnDestroy()
    {
        GameSettings.OnSettingsChanged -= OnSettingsChanged;
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (Instance == this)
            Instance = null;
    }

    // ==================== DUNGEON AUDIO METHODS ====================

    public void CommonSceneAudio(int i){
        audioSource.PlayOneShot(commonSceneAudio[i], GetSFXVolume());
    }
    public void CommonEnemySound(int i){
        audioSource.PlayOneShot(commonEnemySound[i], GetSFXVolume());
    }
    public void EnemyDamage(){
        audioSource.PlayOneShot(simpleDamage[Random.Range(0, simpleDamage.Length)], GetSFXVolume());
    }
    public void DoorAudioOpen(){
        audioSource.PlayOneShot(doorAudio[0], GetSFXVolume());
    }
    public void DoorAudioClose(){
        audioSource.PlayOneShot(doorAudio[1], GetSFXVolume());
    }
    public void WinAudio(int i){
        audioSource.PlayOneShot(winAudio[i], GetSFXVolume());
    }
    public void MenuAudio(int i){
        audioSource.PlayOneShot(menuAudio[i], GetSFXVolume());
    }
    public void ItemAudio(int i){
        audioSource.PlayOneShot(itemAudio[i], GetSFXVolume());
    }
    public void PlayerSkill(int i){
        audioSource.PlayOneShot(playerSkills[i], GetSFXVolume());
    }
    public void PlayerHits(){
        audioSource.PlayOneShot(playerHits[Random.Range(0, playerHits.Length)], GetSFXVolume());
    }
    public void PlayerCommonAudio(int i){
        audioSource.PlayOneShot(playerCommonAudio[i], GetSFXVolume());
    }
    public void PlayerSteps(){
        audioSource.PlayOneShot(playerSteps[Random.Range(0, playerSteps.Length)], GetSFXVolume());
    }
    public void SwordMagicDamage(int i){
        audioSource.PlayOneShot(swordMagicDamage[i], GetSFXVolume());
    }
    public void PlayerSlash(int i){
        audioSource.PlayOneShot(playerSlash[i], GetSFXVolume());
    }

    // ==================== SETTINGS VOLUME CONTROL ====================

    private void OnSettingsChanged()
    {
        ApplyVolumes();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Invoke(nameof(RefreshAndApplyVolumes), 0.1f);
    }

    private void RefreshAndApplyVolumes()
    {
        _allSceneAudioSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
        ApplyVolumes();
    }

    /// <summary>
    /// Apply Music/SFX/Background volumes từ GameSettings vào AudioSources
    /// </summary>
    private void ApplyVolumes()
    {
        if (GameSettings.Instance == null) return;
        if (_allSceneAudioSources == null) return;

        var gs = GameSettings.Instance;

        foreach (var source in _allSceneAudioSources)
        {
            if (source == null) continue;

            var type = ClassifyAudioSource(source);
            switch (type)
            {
                case SettingsAudioType.Music:
                    source.volume = gs.musicVolume;
                    break;
                case SettingsAudioType.Ambient:
                    source.mute = !gs.backgroundSoundEnabled;
                    break;
            }
        }
    }

    private enum SettingsAudioType { Music, Ambient, SFX }

    private SettingsAudioType ClassifyAudioSource(AudioSource source)
    {
        string tag = source.gameObject.tag;
        if (tag == "Music") return SettingsAudioType.Music;
        if (tag == "Ambient") return SettingsAudioType.Ambient;

        string name = source.gameObject.name.ToLower();
        string parentName = source.transform.parent != null ? source.transform.parent.name.ToLower() : "";
        string fullName = name + " " + parentName;

        if (fullName.Contains("music") || fullName.Contains("bgm") || fullName.Contains("soundtrack"))
            return SettingsAudioType.Music;

        if (fullName.Contains("ambient") || fullName.Contains("environment") ||
            fullName.Contains("nature") || fullName.Contains("wind") || fullName.Contains("bird"))
            return SettingsAudioType.Ambient;

        if (source.loop && source.clip != null)
        {
            if (source.clip.length > 30f) return SettingsAudioType.Music;
            if (source.clip.length > 5f) return SettingsAudioType.Ambient;
        }

        return SettingsAudioType.SFX;
    }

    // ==================== PUBLIC API ====================

    /// <summary>
    /// Volume cho SFX — tất cả PlayOneShot trong class này đều dùng
    /// </summary>
    public static float GetSFXVolume()
    {
        if (GameSettings.Instance == null) return 0.8f;
        return GameSettings.Instance.sfxVolume;
    }

    public static float GetMusicVolume()
    {
        if (GameSettings.Instance == null) return 0.7f;
        return GameSettings.Instance.musicVolume;
    }

    /// <summary>
    /// Đảm bảo Instance tồn tại — gọi từ GameSettings.ApplyAudio()
    /// </summary>
    public static AudioManager EnsureInstance()
    {
        // Nếu đã có (từ scene DungeonMania) → dùng luôn
        if (Instance != null) return Instance;

        // Tìm trong scene
        Instance = FindFirstObjectByType<AudioManager>();
        if (Instance != null) return Instance;

        // Tạo mới nếu chưa có (scene không có DungeonMania)
        var go = new GameObject("[AudioManager]");
        Instance = go.AddComponent<AudioManager>();
        return Instance;
    }
}

