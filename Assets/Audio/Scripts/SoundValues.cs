public enum SoundType
{
    // Normal combo - Sword
    Sword_Normal_1,
    Sword_Normal_2,
    Sword_Normal_3,

    // Normal combo - Axe
    Axe_Normal_1,
    Axe_Normal_2,
    Axe_Normal_3,
    Axe_Normal_4,

    // Melee impact (enemy was hit)
    Sword_Hit,
    Axe_Hit,

    // Normal combo - Mage
    Mage_Normal_1,
    Mage_Normal_2,
    Mage_Normal_3,

    // Skills - Axe (Sword/Mage skill SFX không dùng – gọi từ AE riêng hoặc tắt)
    Axe_Skill_E,
    Axe_Skill_R,
    Axe_Skill_T,
    Axe_Skill_Q,

    // Mage projectile impact
    Mage_Projectile_Hit,

    // Movement & status
    Footstep_Default,
    Dash,
    Jump,
    Land,
    Crouch_Move,
    GetHit,
    Die,

    // Weapon draw/sheath (phase 2 không dùng nữa)
    Sword_Draw,
    Sword_Sheath,
    Axe_Draw,
    Axe_Sheath,
    Mage_Draw,
    Mage_Sheath
}
