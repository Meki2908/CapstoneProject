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

    // Skills - Sword
    Sword_Skill_E,
    Sword_Skill_R,
    Sword_Skill_T,
    Sword_Skill_Q,

    // Skills - Axe
    Axe_Skill_E,
    Axe_Skill_R,
    Axe_Skill_T,
    Axe_Skill_Q,

    // Skills - Mage
    Mage_Skill_E,
    Mage_Skill_R,
    Mage_Skill_T,
    Mage_Skill_Q,

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

    // Weapon draw/sheath per type (phase 0 = first sound, phase 1 = second sound in same motion)
    Sword_Draw,
    Sword_Sheath,
    Axe_Draw,
    Axe_Sheath,
    Mage_Draw,
    Mage_Sheath,
    Sword_Draw_2,
    Sword_Sheath_2,
    Axe_Draw_2,
    Axe_Sheath_2,
    Mage_Draw_2,
    Mage_Sheath_2
}
