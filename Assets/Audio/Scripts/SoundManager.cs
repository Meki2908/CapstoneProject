using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioSource))]
public class SoundManager : MonoBehaviour
{
    [SerializeField] private SoundsSO soundDatabase;
    [SerializeField] private bool dontDestroyOnLoad = true;

    private static SoundManager instance;
    private AudioSource audioSource;

    public static SoundManager Instance => instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            // CHỈ xóa component — gameObject có thể là child của Canvas player
            Destroy(this);
            return;
        }

        instance = this;
        audioSource = GetComponent<AudioSource>();

        if (dontDestroyOnLoad)
        {
            // CRITICAL: Tách khỏi parent hierarchy trước khi DontDestroyOnLoad
            // DontDestroyOnLoad chỉ hoạt động trên root GameObjects
            if (transform.parent != null)
            {
                transform.SetParent(null);
            }
            DontDestroyOnLoad(gameObject);
        }
    }

    public static void PlaySound(SoundType sound, AudioSource source = null, float volume = 1f)
    {
        if (instance == null || instance.soundDatabase == null)
        {
            return;
        }

        SoundList soundList = instance.soundDatabase.GetSound(sound);
        if (!soundList.IsValid())
        {
            return;
        }

        AudioClip randomClip = soundList.GetRandomClip();
        if (randomClip == null)
        {
            return;
        }

        float settingsSfxVol = GameSettings.Instance != null ? GameSettings.Instance.sfxVolume : 1f;
        float finalVolume = Mathf.Clamp01(volume) * Mathf.Clamp01(soundList.volume) * settingsSfxVol;
        AudioSource targetSource = source != null ? source : instance.audioSource;

        float originalPitch = targetSource.pitch;
        if (soundList.changePitch)
        {
            float pMin = soundList.pitchMin;
            float pMax = soundList.pitchMax;
            if (pMax <= pMin)
            {
                pMin = 0.92f;
                pMax = 1.08f;
            }
            targetSource.pitch = Mathf.Clamp(UnityEngine.Random.Range(pMin, pMax), 0.01f, 3f);
        }

        targetSource.outputAudioMixerGroup = soundList.mixer;
        if (source != null)
        {
            targetSource.PlayOneShot(randomClip, finalVolume);
            if (soundList.changePitch)
            {
                targetSource.pitch = originalPitch;
            }
            return;
        }

        targetSource.PlayOneShot(randomClip, finalVolume);
        if (soundList.changePitch)
        {
            targetSource.pitch = originalPitch;
        }
    }

    public static void PlayBasicAttack(WeaponType weaponType, int comboIndex, AudioSource source = null, float volume = 1f)
    {
        SoundType soundType = GetBasicAttackSoundType(weaponType, comboIndex);
        PlaySound(soundType, source, volume);
    }

    public static void PlaySkill(WeaponType weaponType, AbilityInput input, AudioSource source = null, float volume = 1f)
    {
        if (weaponType == WeaponType.Sword || weaponType == WeaponType.Mage) return;
        SoundType soundType = GetSkillSoundType(weaponType, input);
        PlaySound(soundType, source, volume);
    }

    public static void PlayMeleeHit(WeaponType weaponType, AudioSource source = null, float volume = 1f)
    {
        switch (weaponType)
        {
            case WeaponType.Sword:
                PlaySound(SoundType.Sword_Hit, source, volume);
                break;
            case WeaponType.Axe:
                PlaySound(SoundType.Axe_Hit, source, volume);
                break;
        }
    }

    public static void PlayMageProjectileHit(AudioSource source = null, float volume = 1f)
        => PlaySound(SoundType.Mage_Projectile_Hit, source, volume);

    public static void PlayFootstep(AudioSource source = null, float volume = 1f)
    {
        if (instance == null || instance.soundDatabase == null)
        {
            return;
        }

        string currentScene = SceneManager.GetActiveScene().name;
        if (instance.soundDatabase.TryGetFootstepForScene(currentScene, out SoundList sceneFootstep) && sceneFootstep.IsValid())
        {
            PlayCustomSoundList(sceneFootstep, source, volume);
            return;
        }

        PlaySound(SoundType.Footstep_Default, source, volume);
    }

    public static void PlayDash(AudioSource source = null, float volume = 1f) => PlaySound(SoundType.Dash, source, volume);
    public static void PlayJump(AudioSource source = null, float volume = 1f) => PlaySound(SoundType.Jump, source, volume);
    public static void PlayLand(AudioSource source = null, float volume = 1f) => PlaySound(SoundType.Land, source, volume);
    public static void PlayCrouchMove(AudioSource source = null, float volume = 1f) => PlaySound(SoundType.Crouch_Move, source, volume);
    public static void PlayGetHit(AudioSource source = null, float volume = 1f) => PlaySound(SoundType.GetHit, source, volume);
    public static void PlayDie(AudioSource source = null, float volume = 1f) => PlaySound(SoundType.Die, source, volume);
    public static void PlayDrawWeapon(WeaponType weaponType, AudioSource source = null, float volume = 1f)
    {
        PlayDrawWeapon(weaponType, 0, source, volume);
    }
    /// <param name="phase">0 = first sound; phase 1 (Draw_2/Sheath_2) không dùng nữa, bỏ qua.</param>
    public static void PlayDrawWeapon(WeaponType weaponType, int phase, AudioSource source = null, float volume = 1f)
    {
        if (phase != 0) return;
        SoundType soundType = GetDrawWeaponSoundType(weaponType);
        PlaySound(soundType, source, volume);
    }
    public static void PlaySheathWeapon(WeaponType weaponType, AudioSource source = null, float volume = 1f)
    {
        PlaySheathWeapon(weaponType, 0, source, volume);
    }
    /// <param name="phase">0 = first sound; phase 1 không dùng nữa, bỏ qua.</param>
    public static void PlaySheathWeapon(WeaponType weaponType, int phase, AudioSource source = null, float volume = 1f)
    {
        if (phase != 0) return;
        SoundType soundType = GetSheathWeaponSoundType(weaponType);
        PlaySound(soundType, source, volume);
    }

    private static void PlayCustomSoundList(SoundList soundList, AudioSource source = null, float volume = 1f)
    {
        AudioClip randomClip = soundList.GetRandomClip();
        if (randomClip == null)
        {
            return;
        }

        float settingsSfxVol = GameSettings.Instance != null ? GameSettings.Instance.sfxVolume : 1f;
        float finalVolume = Mathf.Clamp01(volume) * Mathf.Clamp01(soundList.volume) * settingsSfxVol;
        AudioSource targetSource = source != null ? source : instance.audioSource;

        float originalPitch = targetSource.pitch;
        if (soundList.changePitch)
        {
            float pMin = soundList.pitchMin;
            float pMax = soundList.pitchMax;
            if (pMax <= pMin)
            {
                pMin = 0.92f;
                pMax = 1.08f;
            }
            targetSource.pitch = Mathf.Clamp(UnityEngine.Random.Range(pMin, pMax), 0.01f, 3f);
        }

        targetSource.outputAudioMixerGroup = soundList.mixer;

        if (source != null)
        {
            targetSource.clip = randomClip;
            targetSource.volume = finalVolume;
            targetSource.Play();
            if (soundList.changePitch)
            {
                targetSource.pitch = originalPitch;
            }
            return;
        }

        targetSource.PlayOneShot(randomClip, finalVolume);
        if (soundList.changePitch)
        {
            targetSource.pitch = originalPitch;
        }
    }

    private static SoundType GetDrawWeaponSoundType(WeaponType weaponType)
    {
        return weaponType switch
        {
            WeaponType.Sword => SoundType.Sword_Draw,
            WeaponType.Axe => SoundType.Axe_Draw,
            WeaponType.Mage => SoundType.Mage_Draw,
            _ => SoundType.Sword_Draw
        };
    }

    private static SoundType GetSheathWeaponSoundType(WeaponType weaponType)
    {
        return weaponType switch
        {
            WeaponType.Sword => SoundType.Sword_Sheath,
            WeaponType.Axe => SoundType.Axe_Sheath,
            WeaponType.Mage => SoundType.Mage_Sheath,
            _ => SoundType.Sword_Sheath
        };
    }

    private static SoundType GetBasicAttackSoundType(WeaponType weaponType, int comboIndex)
    {
        switch (weaponType)
        {
            case WeaponType.Sword:
                return Mathf.Clamp(comboIndex, 1, 3) switch
                {
                    1 => SoundType.Sword_Normal_1,
                    2 => SoundType.Sword_Normal_2,
                    _ => SoundType.Sword_Normal_3
                };
            case WeaponType.Axe:
                return Mathf.Clamp(comboIndex, 1, 4) switch
                {
                    1 => SoundType.Axe_Normal_1,
                    2 => SoundType.Axe_Normal_2,
                    3 => SoundType.Axe_Normal_3,
                    _ => SoundType.Axe_Normal_4
                };
            case WeaponType.Mage:
                return Mathf.Clamp(comboIndex, 1, 3) switch
                {
                    1 => SoundType.Mage_Normal_1,
                    2 => SoundType.Mage_Normal_2,
                    _ => SoundType.Mage_Normal_3
                };
            default:
                return SoundType.Sword_Normal_1;
        }
    }

    private static SoundType GetSkillSoundType(WeaponType weaponType, AbilityInput input)
    {
        if (weaponType != WeaponType.Axe) return SoundType.Axe_Skill_E;
        return input switch
        {
            AbilityInput.E => SoundType.Axe_Skill_E,
            AbilityInput.R => SoundType.Axe_Skill_R,
            AbilityInput.T => SoundType.Axe_Skill_T,
            AbilityInput.Q_Ultimate => SoundType.Axe_Skill_Q,
            _ => SoundType.Axe_Skill_E
        };
    }
}
