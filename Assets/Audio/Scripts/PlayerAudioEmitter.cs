using UnityEngine;

public class PlayerAudioEmitter : MonoBehaviour
{
    [SerializeField] private AudioSource localSource;
    [SerializeField] private WeaponController weaponController;
    [SerializeField] private WeaponType fallbackWeaponType = WeaponType.Sword;
    [SerializeField, Range(0f, 1f)] private float defaultVolume = 1f;

    private Character character;

    private void Reset()
    {
        localSource = GetComponent<AudioSource>();
        weaponController = GetComponentInChildren<WeaponController>();
    }

    private void Awake()
    {
        character = GetComponent<Character>();
    }

    public void AE_PlayBasicAttackSound(int comboIndex)
    {
        SoundManager.PlayBasicAttack(GetCurrentWeaponType(), comboIndex, localSource, defaultVolume);
    }

    /// <summary>First (or only) sound in a skill motion. Use for single-hit skills or the first hit of a double-hit.</summary>
    public void AE_PlaySkillSound(int abilityInputIndex)
    {
        AbilityInput input = (AbilityInput)abilityInputIndex;
        SoundManager.PlaySkill(GetCurrentWeaponType(), input, 0, localSource, defaultVolume);
    }

    /// <summary>Second sound in the same skill motion (e.g. double slash). Add a second Animation Event and call this at the second action.</summary>
    public void AE_PlaySkillSoundSecondHit(int abilityInputIndex)
    {
        AbilityInput input = (AbilityInput)abilityInputIndex;
        SoundManager.PlaySkill(GetCurrentWeaponType(), input, 1, localSource, defaultVolume);
    }

    public void AE_PlayMageProjectileHitSound() => SoundManager.PlayMageProjectileHit(localSource, defaultVolume);

    public void AE_PlayFootstepSound()
    {
        // Don't play footstep while performing normal attack (movement is locked but animator can still fire events from blended layers)
        if (character != null && character.movementSM != null && character.attacking != null &&
            character.movementSM.currentState == character.attacking)
        {
            return;
        }
        SoundManager.PlayFootstep(localSource, defaultVolume);
    }
    public void AE_PlayDashSound() => SoundManager.PlayDash(localSource, defaultVolume);
    public void AE_PlayJumpSound() => SoundManager.PlayJump(localSource, defaultVolume);
    public void AE_PlayLandSound() => SoundManager.PlayLand(localSource, defaultVolume);
    public void AE_PlayCrouchMoveSound() => SoundManager.PlayCrouchMove(localSource, defaultVolume);
    public void AE_PlayGetHitSound() => SoundManager.PlayGetHit(localSource, defaultVolume);
    public void AE_PlayDieSound() => SoundManager.PlayDie(localSource, defaultVolume);

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
