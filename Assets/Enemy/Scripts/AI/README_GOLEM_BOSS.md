# 🗿 GOLEM BOSS - QUICK START GUIDE

## 🚀 SETUP NHANH TRONG 5 BƯỚC

### **Bước 1: Chuẩn bị Prefab**
```
1. Mở: Assets/golem/02_Golem/Prefabs/GolemPrefab.prefab
2. Drag vào scene
3. Đặt tên: "Golem_Boss"
```

### **Bước 2: Chạy Setup Tool**
```
1. Menu → Tools → Boss Setup → Setup Golem Boss
2. Chọn "Golem_Boss" trong scene
3. Chọn difficulty: Normal (hoặc tùy chọn)
4. Click "⚡ SETUP BOSS"
5. ✅ Done! Boss đã ready
```

### **Bước 3: Configure Animator**
```
1. Select Golem_Boss
2. Tìm Animator component
3. Assign: Runtime Animator Controller = GolemAnimator
4. ✅ Animations ready
```

### **Bước 4: Setup Effects (Optional)**
```
Trong GolemBossHealth component:
- Damage Number Prefab: [Your damage number]
- Hit Effect Prefab: [Hit VFX]
- Death Effect Prefab: [Death VFX]

Trong GolemBossAttacks component:
- Basic Attack Effect: [Punch VFX]
- Ground Slam Effect: [Slam VFX]
- Rage Attack Effect: [Rage VFX]
```

### **Bước 5: Test!**
```
1. Thêm Player vào scene
2. Set Player layer = "Player"
3. Play scene
4. Boss sẽ tự động detect và attack!
```

---

## 📋 COMPONENTS OVERVIEW

### **GolemBossAI** (Behavior Tree)
- ❤️ Health: 1000
- 👁️ Detection Range: 15m
- ⚔️ Attack Ranges: Melee 3m, Ranged 8m
- 🏃 Speeds: Walk 2, Chase 4, Rage 6
- 📊 Phases: 3 phases (66%, 33% HP)

### **GolemBossAnimator** (Animation Control)
- 🎬 Controls all animations
- 🔄 Smooth transitions
- 🎯 Animation events for damage timing
- ✅ Compatible với GolemAnimator.controller

### **GolemBossHealth** (Health System)
- 💚 Max HP: 1000 (configurable)
- 🛡️ Damage reduction: 20%
- 💔 Takes damage from player attacks
- 🎨 Visual feedback (flash, effects)
- 📊 Health bar support

### **GolemBossAttacks** (Attack System)
- ⚔️ Basic Attack: 50 damage
- 🥊 Combo Attack: 35x2 damage
- 🌍 Ground Slam: 80 damage (AOE)
- 💥 Rage Attack: 100 damage (huge AOE)

---

## 🎯 BOSS PHASES

### **PHASE 1: NORMAL** (100-66% HP)
```
🟢 Green Aura
- Slow movement (walk: 2)
- Basic attacks only
- Long cooldowns (2s)
- Easy to dodge
```

### **PHASE 2: AGGRESSIVE** (66-33% HP)
```
🟡 Yellow Aura
- Faster movement (chase: 4)
- Combo attacks unlocked
- Medium cooldowns (1.5s)
- More aggressive AI
```

### **PHASE 3: ENRAGED** (33-0% HP)
```
🔴 Red Aura + Fire
- Very fast (rage: 6)
- Rage attack unlocked
- Short cooldowns (0.8s)
- Relentless pursuit
- Can heal once at 20% HP
```

---

## ⚔️ ATTACK PATTERNS

### **Basic Attack** (All Phases)
```
Damage: 50
Range: 3m (melee)
Cooldown: 2s
Type: Single target punch
Knockback: Medium
```

### **Combo Attack** (Phase 2+)
```
Damage: 35 x 2 hits = 70 total
Range: 3m (melee)
Cooldown: 5s
Type: Right punch → Left punch
Knockback: Strong on 2nd hit
```

### **Ground Slam** (All Phases)
```
Damage: 80
Range: 6m (AOE)
Cooldown: 8s
Type: Jump → Slam
Effects: Shockwave, camera shake
Knockback: Massive
```

### **Rage Attack** (Phase 3 Only)
```
Damage: 100
Range: 10m (360° AOE)
Cooldown: 10s
Type: Roar + shockwave
Effects: Multiple waves, screen shake
Knockback: Extreme
```

---

## 🎬 ANIMATIONS USED

| Animation | GolemAnimator Parameter | Usage |
|-----------|------------------------|-------|
| **Idle** | Default state | Standing idle |
| **Walk** | Walk (Float 0-6) | Movement |
| **Hit** | Hit (Trigger) | Basic attack 1 |
| **Hit2** | Hit2 (Trigger) | Basic attack 2 / Combo |
| **Jump** | Jump (Trigger) | Ground slam start |
| **Land** | Land (Trigger) | Ground slam impact |
| **Rage** | Rage (Trigger) | Rage attack / Roar / Phase transition |
| **Damage** | Damage (Trigger) | Hurt reaction |
| **Die** | Die (Trigger) | Death |
| **SleepStart** | SleepStart (Trigger) | Heal start |
| **SleepEnd** | SleepEnd (Trigger) | Heal end |

---

## 🛠️ TROUBLESHOOTING

### **Boss không di chuyển?**
```
✓ Check CharacterController enabled
✓ Check GolemBossAI.characterController assigned
✓ Verify movement speeds > 0
```

### **Boss không attack?**
```
✓ Check player layer = "Player"
✓ Check playerLayer mask in GolemBossAI
✓ Verify player trong detection range (15m)
```

### **Animations không chạy?**
```
✓ Animator component có controller?
✓ GolemAnimator.controller assigned?
✓ Check animator parameters exist
```

### **Boss không nhận damage?**
```
✓ GolemBossHealth component present?
✓ Check invulnerability timer
✓ Verify TakeDamage() được gọi
```

### **Health bar không hiện?**
```
✓ Health bar UI created?
✓ healthBarFill assigned in GolemBossHealth?
✓ Canvas enabled?
```

---

## 💡 TIPS & TRICKS

### **Điều chỉnh độ khó:**
```csharp
// Trong GolemBossHealth:
maxHealth = 1500f;        // Tăng HP
damageReduction = 0.3f;   // Tăng armor

// Trong GolemBossAI:
chaseSpeed = 5f;          // Tăng tốc
phase2Threshold = 0.5f;   // Phase 2 sớm hơn (50% HP)

// Trong GolemBossAttacks:
basicAttackDamage = 75f;  // Tăng damage
groundSlamRadius = 8f;    // Tăng AOE
```

### **Làm boss sinh động hơn:**
```csharp
// Trong GolemBossAI:
roarOnCombatStart = true;      // Roar khi combat
canHealOnce = true;            // Cho phép heal
shouldPatrol = true;           // Tuần tra khi idle

// Trong GolemBossAnimator:
randomizeBasicAttacks = true;  // Random Hit/Hit2
animationSpeedMultiplier = 1.2f; // Tăng tốc animation
```

### **Debug boss behavior:**
```csharp
// Enable logs:
GolemBossAI.showDebugLogs = true;
GolemBossAnimator.showDebugLogs = true;
GolemBossHealth.showDebugLogs = true;
GolemBossAttacks.showDebugLogs = true;

// Show gizmos:
GolemBossAI.showGizmos = true;
GolemBossAttacks.showDebugGizmos = true;
```

---

## 📊 PERFORMANCE TIPS

### **Optimization:**
```
✓ Disable debug logs trong production
✓ Giảm số lượng particle effects
✓ LOD cho model nếu cần
✓ Object pooling cho effects
```

### **Behavior Tree:**
```
✓ BT update mỗi frame - normal
✓ Không cần optimize thêm cho 1 boss
✓ Nếu có nhiều enemies, consider:
  - Update rate throttling
  - Distance-based updates
```

---

## 🎮 TESTING SCENARIOS

### **Test 1: Basic Combat**
```
1. Spawn boss + player
2. Player approach
3. ✓ Boss roars
4. ✓ Boss attacks
5. ✓ Damage dealt correctly
```

### **Test 2: Phase Transitions**
```
1. Deal damage to 70% HP
2. ✓ Nothing happens (normal)
3. Deal damage to 65% HP
4. ✓ Phase 2 transition (yellow aura)
5. ✓ Speed increases
6. Deal damage to 30% HP
7. ✓ Phase 3 transition (red aura)
8. ✓ Rage attacks unlocked
```

### **Test 3: Special Abilities**
```
1. Reduce HP to 19%
2. ✓ Boss heals (+15% HP)
3. Wait for rage attack
4. ✓ Massive AOE damage
5. ✓ Shockwave effects
```

### **Test 4: Death**
```
1. Reduce HP to 0
2. ✓ Death animation plays
3. ✓ Death effect spawns
4. ✓ Boss destroyed after 5s
```

---

## 📞 SUPPORT

**Có vấn đề?**
- Check GOLEM_BOSS_DESIGN.md cho chi tiết
- Enable debug logs để troubleshoot
- Xem Console log messages

**Script locations:**
```
AI Core:       Assets/Scripts/AI/GolemBossAI.cs
Animator:      Assets/Scripts/AI/GolemBossAnimator.cs
Health:        Assets/Scripts/AI/GolemBossHealth.cs
Attacks:       Assets/Scripts/AI/GolemBossAttacks.cs
Setup Tool:    Assets/Editor/GolemBossSetupTool.cs
Documentation: Assets/Scripts/AI/GOLEM_BOSS_DESIGN.md
```

---

**Version:** 1.0  
**Created:** November 10, 2025  
**Ready to fight! 🗿**
