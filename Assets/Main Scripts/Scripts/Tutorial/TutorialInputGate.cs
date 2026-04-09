using System;

/// <summary>
/// Chỉ hoạt động khi <see cref="TutorialTextDisplay"/> đang chạy tutorial (chưa hoàn thành).
/// Khóa movement / input theo từng bước; bước combat kill & wave mở toàn bộ gameplay.
/// </summary>
[Flags]
public enum TutorialInputMask : int
{
    None = 0,
    Move = 1 << 0,
    Jump = 1 << 1,
    Dash = 1 << 2,
    Sprint = 1 << 3,
    Crouch = 1 << 4,
    ToggleWeapon = 1 << 5,
    Attack = 1 << 6,
    SkillE = 1 << 7,
    SkillR = 1 << 8,
    SkillT = 1 << 9,
    Ultimate = 1 << 10,
    Inventory = 1 << 11,
}

public static class TutorialInputGate
{
    static bool _active;
    static int _step;
    static bool _waiting;
    static bool _completed;

    /// <summary>Mọi hành động gameplay (dùng cho bước đánh quái / wave).</summary>
    public const TutorialInputMask CombatFree =
        TutorialInputMask.Move
        | TutorialInputMask.Jump
        | TutorialInputMask.Dash
        | TutorialInputMask.Sprint
        | TutorialInputMask.Crouch
        | TutorialInputMask.ToggleWeapon
        | TutorialInputMask.Attack
        | TutorialInputMask.SkillE
        | TutorialInputMask.SkillR
        | TutorialInputMask.SkillT
        | TutorialInputMask.Ultimate
        | TutorialInputMask.Inventory;

    public static bool IsActive => _active && !_completed;

    public static void SetState(bool active, int step, bool waiting, bool completed)
    {
        _active = active;
        _step = step;
        _waiting = waiting;
        _completed = completed;
    }

    public static void Clear()
    {
        _active = false;
        _step = 0;
        _waiting = false;
        _completed = false;
    }

    public static TutorialInputMask EffectiveMask
    {
        get
        {
            if (!IsActive)
                return CombatFree;

            if (_waiting)
                return TutorialInputMask.None;

            return MaskForStep(_step);
        }
    }

    public static bool Allows(TutorialInputMask part)
    {
        if (!IsActive)
            return true;
        return (EffectiveMask & part) != 0;
    }

    public static bool AllowsSkill(AbilityInput input)
    {
        if (!IsActive)
            return true;
        return input switch
        {
            AbilityInput.E => Allows(TutorialInputMask.SkillE),
            AbilityInput.R => Allows(TutorialInputMask.SkillR),
            AbilityInput.T => Allows(TutorialInputMask.SkillT),
            AbilityInput.Q_Ultimate => Allows(TutorialInputMask.Ultimate),
            _ => false
        };
    }

    /// <summary>
    /// Trong bước đang dạy 1 skill: mọi lần animation gọi TriggerCooldown đều bị xóa ngay (chỉ tutorial).
    /// </summary>
    public static bool ShouldSuppressCooldownForSkillTutorial(AbilityInput input)
    {
        if (!IsActive || _waiting)
            return false;
        return _step switch
        {
            4 => input == AbilityInput.E,
            5 => input == AbilityInput.R,
            7 => input == AbilityInput.T,
            9 => input == AbilityInput.Q_Ultimate,
            _ => false
        };
    }

    static TutorialInputMask MaskForStep(int step)
    {
        return step switch
        {
            0 => TutorialInputMask.Jump,
            1 => TutorialInputMask.ToggleWeapon,
            2 => TutorialInputMask.Dash,
            3 => TutorialInputMask.Attack,
            4 => TutorialInputMask.SkillE,
            5 => TutorialInputMask.SkillR,
            6 => CombatFree,
            7 => TutorialInputMask.SkillT,
            8 => CombatFree,
            9 => TutorialInputMask.Ultimate,
            10 => CombatFree,
            11 => TutorialInputMask.ToggleWeapon,
            12 => TutorialInputMask.Inventory,
            13 => TutorialInputMask.Inventory,
            14 => TutorialInputMask.ToggleWeapon,
            15 => CombatFree,
            16 => TutorialInputMask.None,
            _ => CombatFree
        };
    }
}
