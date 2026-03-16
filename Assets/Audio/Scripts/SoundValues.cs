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
    Mage_Sheath,

    // Blacksmith socketing
    Blacksmith_Forge,

    // === ENEMY COMMON SOUNDS ===
    Enemy_Attack,           // Monster melee attack swing
    Enemy_GetHit,           // Enemy bị player đánh
    Enemy_Die,              // Enemy chết
    Enemy_Hurt,             // Enemy bị damage (grunt)
    Enemy_Spawn,            // Enemy spawn VFX + summon VFX
    Enemy_Archer_Arrow,     // Archer bắn tên

    // === BOSS COMMON ===
    Boss_Roar,              // Boss war cry khi xuất hiện / phase change
    Boss_Attack,            // Boss melee attack chung

    // === STONEOGRE ===
    Boss_Stoneogre_EarthSlam,   // Nhảy lên dậm xuống, đá nhọn chui ra

    // === GOLEM ===
    Boss_Golem_WaterBlast,      // Giơ 2 tay quật xuống, sóng nước về phía trước

    // === MINOTAUR ===
    Boss_Minotaur_EarthBlast,   // Nhảy xoay + đập rìu xuống, đá nhọn nổi lên

    // === LICH (Wind) ===
    Boss_Lich_WindAoe,          // Cắm trượng xuống đất, vòng gió bắn ra
    Boss_Lich_WindAura,         // Luồng gió xoay quanh người bay lên (phase 2)
    Boss_Lich_WindShield,       // Khiên gió 5 giây
    Boss_Lich_WindBullet,       // Cầu gió bắn về phía player

    // === IFRIT (Fire) ===
    Boss_Ifrit_FireAoe,         // Đập xuống đất, vòng lửa bắn ra
    Boss_Ifrit_FireAura,        // Luồng lửa xoay quanh bay lên (phase 2) — Demon dùng chung
    Boss_Ifrit_FireShield,      // Khiên lửa 5 giây — Demon dùng chung
    Boss_Ifrit_Fireball,        // Cầu lửa bắn player — Demon dùng chung

    // === DEMON ===
    Boss_Demon_FireBlast        // Xoay chém kiếm, sóng lửa về phía trước
}
