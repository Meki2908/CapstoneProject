# 🌳 Behavior Tree Improvements

## Tổng Quan
Đã cải tiến hệ thống AI Behavior Tree với nhiều tính năng mới và tối ưu hóa performance.

---

## ✅ Các Cải Tiến Đã Thực Hiện

### 1. **Sửa Bug và Tối Ưu Performance**

#### **Node.cs - GetData() và ClearData()**
- ❌ **Trước:** Sử dụng recursive calls có thể gây stack overflow và chậm
- ✅ **Sau:** Truy cập trực tiếp `dataContext` của parent nodes, không dùng recursive
- ✅ Thêm method `Reset()` để reset node state về initial state

```csharp
// Trước: Recursive call
value = node.GetData(key); // Có thể gây stack overflow

// Sau: Direct access
if (node.dataContext.TryGetValue(key, out value))
    return value;
```

---

### 2. **Tick Rate Control và Pause/Resume**

#### **BehaviorTree.cs**
- ✅ **Tick Rate Control:** Cho phép set tick rate (0 = every frame, 1 = 1 per second, etc.)
- ✅ **Pause/Resume:** Có thể pause/resume tree execution
- ✅ **Manual Evaluation:** Method `EvaluateTree()` để trigger evaluation thủ công
- ✅ **Reset Tree:** Method `ResetTree()` để reset toàn bộ tree về initial state
- ✅ **Debug Logging:** Tùy chọn enable debug logs

```csharp
// Sử dụng trong Inspector
public float tickRate = 0f;  // 0 = every frame, 1 = 1 per second
public bool isPaused = false;
public bool enableDebugLogs = false;

// Hoặc trong code
behaviorTree.Pause();
behaviorTree.Resume();
behaviorTree.ResetTree();
behaviorTree.EvaluateTree(); // Manual trigger
```

---

### 3. **Decorator Nodes Mới**

#### **Repeat.cs**
Lặp lại child node N lần trước khi trả về Success.

```csharp
new Repeat(3, new TaskAttack()) // Lặp attack 3 lần
```

#### **UntilSuccess.cs**
Lặp lại child cho đến khi Success.

```csharp
new UntilSuccess(new TaskChaseTarget()) // Chase cho đến khi thành công
```

#### **UntilFailure.cs**
Lặp lại child cho đến khi Failure (ngược với UntilSuccess).

```csharp
new UntilFailure(new TaskPatrol()) // Patrol cho đến khi fail
```

#### **Cooldown.cs**
Chờ cooldown trước khi thực thi child. Hỗ trợ shared cooldown giữa nhiều nodes.

```csharp
// Cooldown riêng
new Cooldown(2.0f, new TaskSpecialAttack()) // 2 giây cooldown

// Shared cooldown (nhiều nodes dùng chung)
new Cooldown(5.0f, new TaskUltimate(), "ultimate_cooldown")
```

#### **Condition.cs**
Chỉ thực thi child nếu condition đúng.

```csharp
new Condition(() => health < 50, new TaskHeal()) // Heal nếu health < 50%
```

---

### 4. **Blackboard System**

#### **Blackboard.cs**
Hệ thống shared data cho phép nhiều Behavior Trees chia sẻ data với nhau.

**Cách sử dụng:**

1. **Thêm Blackboard component vào GameObject:**
```csharp
Blackboard bb = gameObject.AddComponent<Blackboard>();
bb.blackboardID = "enemy_shared";
```

2. **Set data:**
```csharp
bb.SetData("target", playerTransform);
bb.SetData("health", 100);
```

3. **Get data từ node:**
```csharp
// Tự động tìm trong tree và blackboard
object target = GetData("target");

// Hoặc set vào blackboard
SetBlackboardData("target", playerTransform);
```

4. **Share giữa nhiều objects:**
```csharp
// Object 1
Blackboard bb1 = GetComponent<Blackboard>();
bb1.blackboardID = "shared";

// Object 2
Blackboard bb2 = Blackboard.GetInstance("shared");
bb2.SetData("target", player);
```

---

### 5. **Composite Nodes Mới**

#### **Parallel.cs**
Thực thi tất cả children đồng thời.

```csharp
// AllSuccess: Tất cả phải Success
new Parallel(Parallel.ParallelMode.AllSuccess, new List<Node>
{
    new TaskMove(),
    new TaskAttack(),
    new TaskDefend()
})

// AnySuccess: Chỉ cần 1 Success
new Parallel(Parallel.ParallelMode.AnySuccess, new List<Node>
{
    new TaskFindTarget(),
    new TaskPatrol()
})
```

#### **SelectorWithMemory.cs**
Giống Selector nhưng nhớ child đang Running, tiếp tục từ đó thay vì bắt đầu lại.

```csharp
new SelectorWithMemory(new List<Node>
{
    new TaskA(), // Nếu Running, lần sau tiếp tục từ đây
    new TaskB(),
    new TaskC()
})
```

#### **SequenceWithMemory.cs**
Giống Sequence nhưng nhớ child đang Running.

```csharp
new SequenceWithMemory(new List<Node>
{
    new Task1(), // Nếu Running, lần sau tiếp tục từ đây
    new Task2(),
    new Task3()
})
```

---

## 📊 So Sánh Trước và Sau

| Tính Năng | Trước | Sau |
|-----------|-------|-----|
| **Performance** | Recursive calls, chậm | Direct access, nhanh hơn |
| **Tick Control** | ❌ Luôn chạy mỗi frame | ✅ Có thể set tick rate |
| **Pause/Resume** | ❌ Không có | ✅ Có |
| **Decorators** | 1 (Inverter) | 6 (Inverter, Repeat, UntilSuccess, UntilFailure, Cooldown, Condition) |
| **Composite Nodes** | 2 (Selector, Sequence) | 5 (+ Parallel, SelectorWithMemory, SequenceWithMemory) |
| **Shared Data** | ❌ Chỉ local context | ✅ Blackboard system |
| **Reset Mechanism** | ❌ Không có | ✅ Có |
| **Debug Tools** | ❌ Hạn chế | ✅ Nhiều hơn |

---

## 🎯 Ví Dụ Sử Dụng

### Ví Dụ 1: Enemy với Cooldown và Condition

```csharp
protected override Node SetupTree()
{
    return new Selector(new List<Node>
    {
        // Check death
        new TaskCheckIsDead(this),
        
        // Combat với cooldown
        new Sequence(new List<Node>
        {
            new TaskDetectTarget(transform, detectionRange, targetLayer),
            
            new Selector(new List<Node>
            {
                // Special attack với cooldown 5 giây
                new Cooldown(5.0f, new TaskSpecialAttack(this), "special_cooldown"),
                
                // Heal nếu health < 30%
                new Condition(() => health < 30, new TaskHeal(this)),
                
                // Normal attack
                new TaskAttack(this, animator, attackCooldown),
                
                // Chase
                new TaskChaseTarget(this, transform, chaseSpeed, animator)
            })
        }),
        
        // Patrol
        new TaskPatrol(this, transform, patrolPoints, patrolRadius, moveSpeed, animator)
    });
}
```

### Ví Dụ 2: Boss với Parallel và Blackboard

```csharp
protected override Node SetupTree()
{
    // Setup blackboard
    Blackboard bb = gameObject.AddComponent<Blackboard>();
    bb.blackboardID = "boss_shared";
    
    return new Selector(new List<Node>
    {
        new TaskBossCheckDeath(this),
        
        // Parallel: Vừa attack vừa move
        new Parallel(Parallel.ParallelMode.AllSuccess, new List<Node>
        {
            new TaskBossBasicAttack(this),
            new TaskBossMove(this)
        }),
        
        new TaskBossPatrol(this)
    });
}
```

---

## 🚀 Performance Tips

1. **Sử dụng Tick Rate:** Set `tickRate > 0` để giảm số lần evaluate mỗi giây
   ```csharp
   tickRate = 2f; // 2 lần mỗi giây thay vì 60 lần
   ```

2. **Sử dụng Blackboard:** Thay vì tìm kiếm data lên tree, dùng blackboard cho shared data

3. **Sử dụng Memory Nodes:** `SelectorWithMemory` và `SequenceWithMemory` hiệu quả hơn khi có nhiều children

4. **Cooldown Shared:** Dùng shared cooldown key để nhiều nodes share cùng 1 cooldown

---

## 📝 Lưu Ý

- Tất cả các node mới đều tương thích với code cũ
- Blackboard là optional, không bắt buộc
- Reset() method được gọi tự động khi tree reset
- GetData() tự động tìm trong tree và blackboard

---

## 🔧 Migration Guide

Code cũ vẫn hoạt động bình thường. Để sử dụng tính năng mới:

1. **Tick Rate:** Set trong Inspector hoặc code
2. **Decorators:** Import và sử dụng như các node khác
3. **Blackboard:** Thêm component và set blackboardID
4. **Memory Nodes:** Thay `Selector` → `SelectorWithMemory` nếu cần

---

**Tác giả:** AI Assistant  
**Ngày:** 2024  
**Version:** 2.0





