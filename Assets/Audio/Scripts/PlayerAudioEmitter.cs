using UnityEngine;

public class PlayerAudioEmitter : MonoBehaviour
{
    [SerializeField] private AudioSource localSource;
    [SerializeField] private WeaponController weaponController;
    [SerializeField] private PlayerFootstepVFX footstepVfx;
    [SerializeField] private WeaponType fallbackWeaponType = WeaponType.Sword;
    [SerializeField, Range(0f, 1f)] private float defaultVolume = 1f;

    private Character character;

    private void Reset()
    {
        localSource = GetComponent<AudioSource>();
        weaponController = GetComponentInChildren<WeaponController>();
        footstepVfx = GetComponent<PlayerFootstepVFX>();
    }

    private void Awake()
    {
        character = GetComponent<Character>();
    }

    public void AE_PlayBasicAttackSound(int comboIndex)
    {
        SoundManager.PlayBasicAttack(GetCurrentWeaponType(), comboIndex, localSource, defaultVolume);
    }

    public void AE_PlaySkillSound(int abilityInputIndex)
    {
        AbilityInput input = (AbilityInput)abilityInputIndex;
        SoundManager.PlaySkill(GetCurrentWeaponType(), input, localSource, defaultVolume);
    }

    public void AE_PlayMageProjectileHitSound() => SoundManager.PlayMageProjectileHit(localSource, defaultVolume);

    /// <summary>Chỉ phát khi vũ khí đang sheath (Base Layer). Gọi từ clip trên Base Layer.</summary>
    public void AE_PlayFootstepSound()
    {
        if (character != null && character.isWeaponDrawn) return;
        SoundManager.PlayFootstep(localSource, defaultVolume);
        footstepVfx?.EmitFromAnimationEvent();
    }

    /// <summary>Chỉ phát khi vũ khí đang draw (weapon layer). Gọi từ clip Locomotion/Combat Blend Tree trên Sword/Axe/Mage Layer.</summary>
    public void AE_PlayFootstepSoundFromWeaponLayer()
    {
        if (character == null || !character.isWeaponDrawn) return;
        SoundManager.PlayFootstep(localSource, defaultVolume);
        footstepVfx?.EmitFromAnimationEvent();
    }
    
    /// <summary>Optional: use explicit foot animation events for more accurate spawn.</summary>
    public void AE_PlayFootstepVFXLeft() => footstepVfx?.EmitLeftFromAnimationEvent();
    /// <summary>Optional: use explicit foot animation events for more accurate spawn.</summary>
    public void AE_PlayFootstepVFXRight() => footstepVfx?.EmitRightFromAnimationEvent();
    public void AE_PlayDashSound() => SoundManager.PlayDash(localSource, defaultVolume);
    public void AE_PlayJumpSound() => SoundManager.PlayJump(localSource, defaultVolume);
    public void AE_PlayLandSound() => SoundManager.PlayLand(localSource, defaultVolume);
    public void AE_PlayCrouchMoveSound() => SoundManager.PlayCrouchMove(localSource, defaultVolume);
    public void AE_PlayGetHitSound() => SoundManager.PlayGetHit(localSource, defaultVolume);
    public void AE_PlayDieSound() => SoundManager.PlayDie(localSource, defaultVolume);
    /// <summary>First (or only) sound in draw weapon motion.</summary>
    public void AE_PlayDrawWeaponSound() => SoundManager.PlayDrawWeapon(GetCurrentWeaponType(), 0, localSource, defaultVolume);
    /// <summary>Second sound in the same draw weapon motion. Add a second Animation Event and call this at the second action.</summary>
    public void AE_PlayDrawWeaponSoundSecond() => SoundManager.PlayDrawWeapon(GetCurrentWeaponType(), 1, localSource, defaultVolume);
    /// <summary>First (or only) sound in sheath weapon motion.</summary>
    public void AE_PlaySheathWeaponSound() => SoundManager.PlaySheathWeapon(GetCurrentWeaponType(), 0, localSource, defaultVolume);
    /// <summary>Second sound in the same sheath weapon motion. Add a second Animation Event and call this at the second action.</summary>
    public void AE_PlaySheathWeaponSoundSecond() => SoundManager.PlaySheathWeapon(GetCurrentWeaponType(), 1, localSource, defaultVolume);

    private WeaponType GetCurrentWeaponType()
    {
        if (weaponController == null)
        {
            return fallbackWeaponType;
        }

        WeaponSO currentWeapon = weaponController.GetCurrentWeapon();
        return currentWeapon != null ? currentWeapon.weaponType : fallbackWeaponType;
    }
}
