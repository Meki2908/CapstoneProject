# 🗿 GOLEM BOSS - ANIMATION & BEHAVIOR DESIGN DOCUMENT

## 📋 TỔNG QUAN

Golem Boss là một boss chiến đấu với 3 giai đoạn (phases) khác nhau, mỗi phase có attack patterns và behaviors riêng biệt. Boss sử dụng Behavior Tree AI system để tạo ra các hành vi thông minh và đa dạng.

---

## 🎬 TRẠNG THÁI HOẠT ĐỘNG (ANIMATION STATES)

### **1. IDLE & MOVEMENT STATES**

#### **Idle (Đứng yên)**
- **Animation:** `Idle`
- **Khi nào:** Không có player trong tầm phát hiện, đang chờ đợi tại patrol point
- **Đặc điểm:** Boss đứng yên, thở chậm rãi, có thể nhìn quanh
- **Transitions:**
  - → Walk (khi patrol)
  - → Rage (khi phát hiện player lần đầu)

#### **Idle Action (Động tác idle đặc biệt)**
- **Animation:** `IdleAction`
- **Khi nào:** Random trong lúc idle để tạo sự sống động
- **Đặc điểm:** Vươn vai, búng tay, hoặc các động tác nhỏ
- **Transitions:**
  - → Idle (sau khi hoàn thành)

#### **Walk (Đi bộ)**
- **Animation:** `Walk`
- **Khi nào:** Patrol, di chuyển chậm trong Phase 1
- **Speed Parameter:** Walk = 1.0 - 3.0
- **Đặc điểm:** Di chuyển chậm rãi, nặng nề
- **Transitions:**
  - → Idle (khi dừng lại)
  - → Run (khi tăng tốc)

#### **Run (Chạy/Đuổi theo)**
- **Animation:** `Walk` (với speed cao hơn)
- **Khi nào:** Chase player trong Phase 2-3
- **Speed Parameter:** Walk = 3.5 - 6.0
- **Đặc điểm:** Di chuyển nhanh, đuổi theo player
- **Transitions:**
  - → Walk (khi chậm lại)
  - → Attack (khi đến tầm tấn công)

---

### **2. COMBAT STATES - BASIC ATTACKS**

#### **Hit (Đánh đơn - Tay phải)**
- **Animation:** `Hit`
- **Damage:** 50 (base)
- **Range:** 3m melee
- **Cooldown:** 2s
- **Phases:** All phases
- **Đặc điểm:**
  - Đấm xuống từ trên cao
  - Tạo shockwave nhỏ khi chạm đất
  - Có knockback
- **Animation Events:**
  - Frame 15: `OnAttackHit()` - Deal damage
  - Frame 20: `OnFootstep()` - Sound effect

#### **Hit2 (Đánh đơn - Tay trái)**
- **Animation:** `Hit2`
- **Damage:** 50 (base)
- **Range:** 3m melee
- **Cooldown:** 2s
- **Phases:** All phases
- **Đặc điểm:**
  - Quét ngang từ trái sang phải
  - Rộng hơn Hit nhưng damage tương đương
  - Medium knockback
- **Animation Events:**
  - Frame 18: `OnAttackHit()` - Deal damage

---

### **3. COMBO ATTACKS (Phase 2+)**

#### **Combo Attack (Hit → Hit2)**
- **Animation Sequence:** `Hit` + `Hit2`
- **Total Damage:** 70 (35 × 2 hits)
- **Range:** 3m melee
- **Cooldown:** 5s
- **Phases:** Phase 2, Phase 3
- **Đặc điểm:**
  - Chuỗi 2 đòn liên tiếp
  - Đòn 2 có tracking nhẹ
  - Strong knockback ở đòn cuối
- **Behavior:**
  ```
  1. Hit (right hand) - 0.5s
  2. Wait 0.1s
  3. Hit2 (left hand) - 0.6s
  4. Recovery 0.3s
  ```

---

### **4. AREA ATTACKS**

#### **Ground Slam (Đập đất)**
- **Animation Sequence:** `Jump` → `Land`
- **Damage:** 80
- **Range:** 6m AOE
- **Cooldown:** 8s
- **Phases:** All phases
- **Đặc điểm:**
  - Nhảy lên và đập mạnh xuống đất
  - Tạo shockwave lan tỏa
  - Massive knockback
  - Stun 0.5s
- **Animation Events:**
  - Jump frame 10: Boss lifts off ground
  - Land frame 25: `OnGroundSlamImpact()` - Deal AOE damage
  - Frame 30: Shockwave expands
- **Visual Effects:**
  - Dust/rocks flying
  - Ground cracks
  - Camera shake (intensity: 0.5, duration: 0.3s)

#### **Rage Attack (Tấn công cuồng nộ)**
- **Animation:** `Rage`
- **Damage:** 100
- **Range:** 10m AOE (360°)
- **Cooldown:** 10s
- **Phases:** Phase 3 only
- **Đặc điểm:**
  - Roar và tạo shockwave 360°
  - Đẩy lùi mọi thứ xung quanh
  - Self-buff: tăng speed 20% trong 5s
- **Animation Events:**
  - Frame 20: `OnRageWaveRelease()` - Release shockwave
  - Multiple shockwaves (3 waves)
- **Visual Effects:**
  - Red aura around boss
  - Multiple expanding shockwaves
  - Screen shake (intensity: 1.0, duration: 0.5s)
  - Particle effects

---

### **5. SPECIAL BEHAVIORS**

#### **Roar (Gầm thét - Combat Start)**
- **Animation:** `Rage`
- **Khi nào:** Phát hiện player lần đầu, hoặc phase transition
- **Đặc điểm:**
  - Gầm lên đe dọa
  - Alert player rằng boss đã phát hiện
  - Không gây damage nhưng có slow effect
- **Phases:** All phases
- **Duration:** 2s

#### **Heal/Recovery (Hồi máu)**
- **Animation Sequence:** `SleepStart` → `Sleep` (hold 3s) → `SleepEnd`
- **Khi nào:** HP < 20%, chỉ 1 lần duy nhất
- **Heal Amount:** 15% max HP
- **Đặc điểm:**
  - Boss ngồi xuống meditation pose
  - Green healing aura
  - Invulnerable trong animation
  - Có thể interrupt nếu thiết kế muốn
- **Duration:** 4s total

#### **Sleep (Ngủ - Unused trong boss fight)**
- **Animation:** `Sleep` loop
- **Khi nào:** Không dùng trong boss fight (có thể dùng cho cutscene)
- **Đặc điểm:** Boss nằm nghỉ

---

### **6. DAMAGE REACTIONS**

#### **Damage (Nhận sát thương nhẹ)**
- **Animation:** `Damage`
- **Khi nào:** Nhận damage (30% chance trigger)
- **Đặc điểm:**
  - Giật người nhẹ
  - Không interrupt attack đang thực hiện
  - Very short (0.3s)
- **Phases:** Phase 1, Phase 2 (không dùng ở Phase 3)

#### **Stagger (Choáng - nếu cần)**
- **Animation:** `Damage` (hold longer)
- **Khi nào:** Nhận critical hit hoặc đủ damage trong thời gian ngắn
- **Đặc điểm:**
  - Boss dừng hành động
  - Mất 1-2s để recovery
  - Window để player tấn công

---

### **7. DEATH & TRANSITIONS**

#### **Die (Chết)**
- **Animation:** `Die`
- **Khi nào:** HP = 0
- **Đặc điểm:**
  - Boss ngã gục
  - Vỡ thành đá
  - Spawn loot/rewards
- **Duration:** 5s
- **Visual Effects:**
  - Stone crumbling
  - Dust explosion
  - Light rays (dramatic effect)

#### **Phase Transition (Chuyển phase)**
- **Animation:** `Rage`
- **Khi nào:**
  - HP ≤ 66% → Phase 2
  - HP ≤ 33% → Phase 3
- **Đặc điểm:**
  - Boss roar
  - Visual transformation
  - Brief invulnerability (1s)
  - Color change (aura)
- **Visual Effects:**
  - Phase 1→2: Yellow aura
  - Phase 2→3: Red aura + fire particles

---

## 🎯 BEHAVIOR PATTERNS (BOSS AI LOGIC)

### **PHASE 1: NORMAL (100% - 66% HP)**

**Characteristics:**
- Slow, methodical movement
- Basic attacks only
- Long cooldowns
- Easy to dodge

**Attack Pattern:**
```
1. Walk towards player (speed: 2)
2. Enter melee range (3m)
3. Random choice:
   - 60%: Basic Attack (Hit or Hit2)
   - 30%: Ground Slam (if range 3-6m)
   - 10%: Idle Action (taunt)
4. Wait 2s cooldown
5. Repeat
```

**Patrol Behavior (no player):**
- Walk between patrol points
- Wait 3s at each point
- Idle Action occasionally

---

### **PHASE 2: AGGRESSIVE (66% - 33% HP)**

**Phase Transition:**
1. Play Rage animation
2. Yellow aura appears
3. Speed boost +25%

**Characteristics:**
- Faster movement (chase speed: 4)
- Combo attacks unlocked
- Shorter cooldowns
- More aggressive

**Attack Pattern:**
```
1. Chase player (speed: 4)
2. Enter melee range (3m)
3. Weighted choice:
   - 40%: Combo Attack (2-hit)
   - 30%: Basic Attack
   - 20%: Ground Slam
   - 10%: Ground Slam → Basic Attack
4. Wait 1.5s cooldown
5. Repeat
```

**Special Behaviors:**
- Occasionally fake attack (feint)
- Jump back if player too close
- Roar every 20s

---

### **PHASE 3: ENRAGED (33% - 0% HP)**

**Phase Transition:**
1. Play Rage animation × 2 (intense)
2. Red aura + fire particles
3. Permanent speed boost +50%
4. Damage reduction +15%

**Characteristics:**
- Very fast movement (rage speed: 6)
- Constant aggression
- Rage Attack unlocked
- Shortest cooldowns
- Never retreats

**Attack Pattern:**
```
1. Sprint towards player (speed: 6)
2. Attack decision tree:
   IF player in 10m range:
      - 30%: Rage Attack (AOE)
      - 20%: Ground Slam
      - 20%: Combo Attack
      - 30%: Basic Attack spam
   IF player in 3m range:
      - 50%: Combo Attack
      - 30%: Basic Attack
      - 20%: Rage Attack (point blank)
3. Minimal cooldown (0.8s)
4. Repeat aggressively
```

**Special Behaviors:**
- Heal at HP < 20% (once only)
- Rage Attack every 10s
- No idle states
- Roar every attack

---

## 🎨 ANIMATION MAPPING TO GOLEMANIMATOR.CONTROLLER

### **Current Parameters:**
```csharp
Walk        (Float)   - Movement speed (0-6)
IdleAction  (Trigger) - Trigger idle animation
Hit         (Trigger) - Basic attack 1
Hit2        (Trigger) - Basic attack 2
Damage      (Trigger) - Damage reaction
Die         (Trigger) - Death
Rage        (Trigger) - Rage/Roar
Jump        (Trigger) - Jump (for ground slam)
Land        (Trigger) - Land (for ground slam)
SleepStart  (Trigger) - Start sleep/heal
SleepEnd    (Trigger) - End sleep/heal
```

### **Animation State Machine Flow:**

```
[ANY STATE] ──────────────────┐
  ├─ IdleAction → Idle         │
  ├─ Hit → Idle                │
  ├─ Hit2 → Idle               │
  ├─ Damage → Idle             │
  ├─ Die → (END)               │
  ├─ Rage → Idle               │
  ├─ Jump → Fly → Land → Idle │
  └─ SleepStart → Sleep → SleepEnd → Idle

[IDLE] ←→ [WALK] (Walk > 0.01)
       ←→ (Walk < 0.01)
```

---

## 📊 RECOMMENDED ANIMATION ADDITIONS

Để boss sinh động hơn, nên thêm:

### **1. More Attack Variations**
- **Uppercut:** Đấm từ dưới lên
- **Spin Attack:** Quay người 360° (Phase 2+)
- **Rock Throw:** Ném đá từ xa (ranged attack)

### **2. Movement Variations**
- **Strafe:** Di chuyển ngang (không quay lưng)
- **Dash:** Lao nhanh về phía player
- **Jump Back:** Nhảy lùi để tạo khoảng cách

### **3. Environmental Interactions**
- **Wall Punch:** Đấm vào tường tạo đá rơi
- **Ground Pound Walk:** Mỗi bước tạo rung động nhỏ
- **Rock Pillar Summon:** Triệu hồi cột đá từ đất

### **4. Phase-Specific Animations**
- **Phase 2 Activation:** Transform animation
- **Phase 3 Activation:** Intense transform với fire effect
- **Enraged Loop:** Idle animation với steam/smoke

---

## 🎮 IMPLEMENTATION CHECKLIST

### ✅ **Đã Hoàn Thành:**
1. ✅ GolemBossAI.cs - Behavior Tree AI
2. ✅ GolemBossAnimator.cs - Animation controller
3. ✅ GolemBossHealth.cs - Health & damage system
4. ✅ GolemBossAttacks.cs - Attack patterns
5. ✅ GolemBossSetupTool.cs - Unity Editor tool

### 🔧 **Cần Làm Thêm:**
1. ⚠️ Add Animation Events vào GolemAnimator.controller:
   - OnAttackHit (frames: 15 for Hit, 18 for Hit2)
   - OnGroundSlamImpact (frame: 25 for Land)
   - OnRageWaveRelease (frame: 20 for Rage)
   - OnFootstep (frames: walking animation)

2. ⚠️ Create Visual Effects:
   - Hit effect particles
   - Ground slam shockwave
   - Rage aura (red/yellow)
   - Heal glow (green)
   - Death explosion

3. ⚠️ Setup Damage Numbers:
   - Assign DamageNumberPrefab in GolemBossHealth
   - Configure colors (white: normal, yellow: critical)

4. ⚠️ Add Audio:
   - Roar sound
   - Attack whoosh
   - Impact sounds
   - Footsteps (heavy)
   - Death sound

5. ⚠️ Create Health Bar UI:
   - World-space canvas
   - Phase color indicators
   - Boss name display

---

## 🎯 USAGE GUIDE

### **Setup Boss trong Unity:**

1. **Mở Setup Tool:**
   ```
   Menu → Tools → Boss Setup → Setup Golem Boss
   ```

2. **Chọn GolemPrefab:**
   - Drag GolemPrefab vào scene
   - Select nó trong Hierarchy
   - Assign vào Boss Setup Tool

3. **Chọn Difficulty:**
   - Easy: 500 HP, 0.7x damage
   - Normal: 1000 HP, 1.0x damage
   - Hard: 1500 HP, 1.3x damage
   - Nightmare: 2500 HP, 1.5x damage

4. **Click "Setup Boss":**
   - Tool sẽ tự động add components
   - Configure stats
   - Setup colliders
   - Assign animator

5. **Manual Adjustments:**
   - Assign visual effects prefabs
   - Configure patrol points
   - Set player layer mask
   - Add health bar UI

---

## 🐛 DEBUG & TESTING

### **Debug Logs:**
- Enable `showDebugLogs` trong các components
- Check Console cho attack patterns
- Monitor phase transitions

### **Debug Gizmos:**
- Enable `showGizmos` để thấy:
  - Detection range (yellow sphere)
  - Attack ranges (red/magenta spheres)
  - Patrol radius (blue sphere)

### **Testing Checklist:**
- [ ] Boss patrol correctly when no player
- [ ] Boss detects player at correct range
- [ ] Boss plays roar on first detection
- [ ] All attacks deal damage correctly
- [ ] Phase transitions at correct HP thresholds
- [ ] Boss dies properly at 0 HP
- [ ] Animations sync with attacks

---

## 📝 NOTES

- Boss scale trong prefab: **5x5x5** (rất lớn!)
- Character Controller cần adjust cho size này
- Animation speed có thể điều chỉnh qua `animationSpeedMultiplier`
- Behavior Tree update mỗi frame, careful với performance

**Tác giả:** GitHub Copilot  
**Ngày tạo:** November 10, 2025  
**Version:** 1.0
