using UnityEngine;

public class PlayerAudioEmitter : MonoBehaviour
{
    [SerializeField] private AudioSource localSource;
    [SerializeField] private WeaponController weaponController;
    [SerializeField] private WeaponType fallbackWeaponType = WeaponType.Sword;
    [SerializeField, Range(0f, 1f)] private float defaultVolume = 1f;

    private void Reset()
    {
        localSource = GetComponent<AudioSource>();
        weaponController = GetComponentInChildren<WeaponController>();
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

    public void AE_PlayFootstepSound() => SoundManager.PlayFootstep(localSource, defaultVolume);
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
