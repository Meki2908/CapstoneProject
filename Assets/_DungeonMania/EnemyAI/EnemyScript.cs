using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.AI;

public class EnemyScript : MonoBehaviour {
    [HideInInspector] public bool hit, attack, delay, wait, alive;
    
    // Static flag để chỉ log warning một lần
    private static bool hasLoggedHeroInfoWarning = false;
    
    public EnemyClass enemy;
    
    // ========================================
    // PUBLIC STATS - CÓ THỂ CHỈNH TRONG INSPECTOR
    // ========================================
    [Header("=== ENEMY STATS (Chỉ số tấn công) ===")]
    [Tooltip("Tên enemy")]
    public string enemyName = "Enemy";
    
    // MÁU và EXP được quản lý bởi TakeDamageTest, KHÔNG quản lý ở đây
    
    [Tooltip("Sát thương mỗi đòn đánh")]
    public int attackDamage = 10;
    [Tooltip("Giáp (phòng thủ)")]
    public int armorValue = 5;
    [Tooltip("Sát thương phép cộng thêm")]
    public int magicValue = 0;
    [Tooltip("Tỉ lệ chí mạng (%)")]
    public int critChance = 10;
    [Tooltip("Độ chính xác (%) - 100 = luôn đánh trúng full damage")]
    public int accuracy = 100;
    [Tooltip("Gold khi tiêu diệt")]
    public int goldReward = 10;
    [Tooltip("Điểm khi tiêu diệt")]
    public int scoreReward = 100;
    [Tooltip("Khoảng cách tấn công")]
    public float attackDistanceOverride = 1.5f;
    [Tooltip("Tốc độ di chuyển")]
    public float moveSpeed = 3.5f;
    
    [Header("=== ENEMY TYPE ===")]
    [Tooltip("Loại enemy (phân loại chính)")]
    public bool isBoss = false;
    
    // ========================================
    // EXISTING CODE BELOW
    // ========================================
    
    // Category enemy (phân loại chính)
    public enum EnemyType{
        skelet,
        archer,
        monster,
        lich,
        boss,
        demon,
        stoneogre,  // Boss riêng — dễ quản lý spawn theo dungeon
        golem,
        minotaur,
        ifrit
    }
    public EnemyType enemyType = EnemyType.skelet;
    
    // Specific enemy type (loại enemy cụ thể trong prefab EnemyNew)
    // Theo yêu cầu:
    // Skeleton@Skin = Skelet, skeleton_archer = Skelet
    // Orc@Skin = Monster, Troll@Skin = Monster, Guul@Skin = Monster
    // Lich@Skin = Lich
    // Stoneogre@Skin = Boss, Golem@Skin = Boss, Minotaur@Skin = Boss, Ifrit@Skin = Boss
    // Demon@Skin = Demon
    public enum SpecificEnemyType
    {
        Skeleton,        // index 0
        SkeletonArcher,  // index 1
        Orc,             // index 2
        Troll,           // index 3
        Guul,            // index 4
        Lich,            // index 5
        Stoneogre,       // index 6
        Golem,           // index 7
        Minotaur,        // index 8
        Ifrit,           // index 9
        Demon            // index 10
    }
    public SpecificEnemyType specificEnemyType = SpecificEnemyType.Skeleton;
    
    public EnemyClass.Boss boss = EnemyClass.Boss.None;
    public ParticleSystem[] bow, skill;
    [HideInInspector]public Bow[] bowScript;
    [HideInInspector]public Bow skillScript;
    
    [Header("Boss Skill VFX (PixPlays)")]
    [Tooltip("Kéo VFX prefab từ PixPlays vào đây (Version_URP)")]
    public GameObject skillVfxPrefab;
    [Tooltip("Bán kính vùng damage của skill (mét)")]
    public float skillVfxRadius = 3f;
    [Tooltip("Skill hướng về trước (Golem quật xéo) hay 360° AoE (Stoneogre dậm)?")]
    public bool skillIsDirectional = false;
    [Tooltip("Góc cone phía trước (độ) — chỉ dùng khi skillIsDirectional = true")]
    [Range(30, 180)]
    public float skillAngle = 90f;
    
    [Header("Boss Skill Settings")]
    [Tooltip("Khoảng cách tối đa để boss dùng skill (xa hơn attackDistance)")]
    public float skillDistance = 8f;
    [Tooltip("Thời gian hồi chiêu skill (giây)")]
    public float skillCooldown = 8f;
    [Tooltip("Hệ số damage skill (2.0 = gấp đôi attack thường)")]
    public float skillDamageMultiplier = 2.0f;
    
    [Header("=== SPAWN VFX ===")]
    [Tooltip("VFX hiệu ứng khi enemy spawn (cột sáng/portal) — gán cùng 1 prefab cho tất cả")]
    public GameObject spawnVfxPrefab;
    [Tooltip("Thời gian VFX tồn tại (giây)")]
    public float spawnVfxDuration = 2f;
    private bool hasAwoken = false; // Tránh spawn VFX lúc Awake
    public static bool suppressSpawnVfx = false; // Flag để tránh double VFX khi summon
    
    // Runtime tracking — không hiển thị trong Inspector
    [HideInInspector] public float lastSkillTime = -999f; // Thời điểm dùng skill lần cuối
    [HideInInspector] public bool skillOnCooldown = false;
    [HideInInspector] public BossMultiSkill bossMultiSkill; // Multi-skill system (Phase 2)
    public AnimatorStateInfo anim;
    Transform parent;
    [HideInInspector]public RandomEnemy randomEnemyScript;
    [HideInInspector]public PlayerManager playerManager;
    [HideInInspector]public EnemyManager enemyManager;
    [HideInInspector]public EnemyState enemyState;
    [HideInInspector]public EnemyAttack enemyAttack;
    [HideInInspector]public EnemyDamage enemyDamage;
    GameObject gameManager;
    [HideInInspector]public AudioManager audioManager;
    [HideInInspector]public HeroInformation heroInformation;
    [HideInInspector]public Animator animator;
    [HideInInspector]public NavMeshAgent navMeshAgent;
    [HideInInspector]public Transform target;
    [HideInInspector]public float attackDistance;
    [HideInInspector]public bool cont;
    public delegate void WinAudio(int i);
    public static event WinAudio WinAudioEvent;
    Vector3 direction;
    public void RunWinAudio(int i){
        WinAudioEvent?.Invoke(i);
    }
    private void Awake () {
        enemy = new EnemyClass((int)enemyType);

        // ƯU TIÊN tìm player thật bằng tag "Player" (có DungeonManiaPlayerBridge)
        // Fallback: tìm bằng tên "player" (child object)
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null) {
            playerObj = GameObject.Find("player");
        }
        
        if (playerObj != null) {
            target = playerObj.transform;
            Debug.Log($"[EnemyScript] Found player target: {playerObj.name} (tag: {playerObj.tag})");
        } else {
            Debug.LogWarning("[EnemyScript] Player not found by tag or name!");
        }

        // Setup bow scripts
        if(bow != null && bow.Length != 0){
            bowScript = new Bow[bow.Length];
            for(int i = 0; i < bow.Length; i++){
                if (bow[i] != null) {
                    // Thử tìm trên chính object trước
                    bowScript[i] = bow[i].GetComponent<Bow>();
                    
                    // Nếu không có, tìm trong children
                    if (bowScript[i] == null)
                    {
                        bowScript[i] = bow[i].GetComponentInChildren<Bow>();
                    }
                    
                    // Nếu vẫn không có, tìm trên parent "Bow" object
                    if (bowScript[i] == null && bow[i].transform.parent != null)
                    {
                        bowScript[i] = bow[i].transform.parent.GetComponent<Bow>();
                    }
                    
                    if (bowScript[i] != null) {
                        Debug.Log($"[EnemyScript] Found Bow script for bow[{i}]: {bowScript[i].gameObject.name}");
                    } else {
                        Debug.LogWarning($"[EnemyScript] Bow script not found for bow[{i}]: {bow[i].name}");
                    }
                }
            }
        }

        // Setup skill script (Bow component trên Skill objects — dùng cho Lich skill attack)
        if (skill != null && skill.Length > 0 && skillScript == null) {
            for (int i = 0; i < skill.Length; i++) {
                if (skill[i] == null) continue;
                
                // Tìm Bow script trên chính skill particle
                skillScript = skill[i].GetComponent<Bow>();
                
                // Nếu không có, tìm trên parent (Skill object chứa Bow script)
                if (skillScript == null && skill[i].transform.parent != null) {
                    skillScript = skill[i].transform.parent.GetComponent<Bow>();
                }
                
                // Tìm trong children
                if (skillScript == null) {
                    skillScript = skill[i].GetComponentInChildren<Bow>();
                }
                
                if (skillScript != null) {
                    Debug.Log($"[EnemyScript] Found Skill script on: {skillScript.gameObject.name}");
                    break; // Chỉ cần 1 skillScript
                }
            }
            
            if (skillScript == null) {
                Debug.LogWarning($"[EnemyScript] Skill script (Bow) not found on any skill particle! Skill attack won't work.");
            }
        }

        // Setup boss magic type
        switch(boss){
            case EnemyClass.Boss.None:
            break;
            case EnemyClass.Boss.Ogre:
                enemy.enemyMagic = 0;
            break;
            case EnemyClass.Boss.Golem:
                enemy.enemyMagic = (int)EnemyClass.EnemyMagic.ice;
            break;
            case EnemyClass.Boss.Mino:
                enemy.enemyMagic = (int)EnemyClass.EnemyMagic.light;
            break;
            case EnemyClass.Boss.Ifrit:
                enemy.enemyMagic = (int)EnemyClass.EnemyMagic.fire;
            break;
        }

        // Setup NavMeshAgent
        navMeshAgent = GetComponent<NavMeshAgent>();
        if (navMeshAgent != null && navMeshAgent.isOnNavMesh)
        {
            navMeshAgent.isStopped = true;
        }

        animator = GetComponent<Animator> ();

        // Setup parent references
        parent = transform.parent;
        if (parent != null) {
            enemyManager = parent.GetComponent<EnemyManager>();
            randomEnemyScript = parent.GetComponent<RandomEnemy>();
        }

        // Setup player manager với kiểm tra null
        SetupPlayerManagerReference();

        // Setup component references
        enemyState = GetComponent<EnemyState> ();
        enemyAttack = GetComponent<EnemyAttack>();
        enemyDamage = GetComponent<EnemyDamage>();

        // Setup game manager với kiểm tra null
        gameManager = GameObject.Find("GameManager");
        if (gameManager != null) {
            audioManager = gameManager.GetComponent<AudioManager>();
            heroInformation = gameManager.GetComponent<HeroInformation>();
        }

        // KHÔNG set active false trong Awake - để RandomEnemy quản lý
        hasAwoken = true;
    }


    /// <summary>
    /// Setup PlayerManager/Bridge reference từ target
    /// </summary>
    private void SetupPlayerManagerReference()
    {
        if (target == null) return;
        
        playerManager = target.GetComponent<PlayerManager>();
        
        // If PlayerManager not found, try to find DungeonManiaPlayerBridge
        if (playerManager == null) {
            DungeonManiaPlayerBridge bridge = target.GetComponent<DungeonManiaPlayerBridge>();
            if (bridge != null) {
                Debug.Log("[EnemyScript] Found DungeonManiaPlayerBridge on player target");
            } else {
                // Nếu target là child "player", tìm bridge trên parent
                if (target.parent != null) {
                    bridge = target.parent.GetComponent<DungeonManiaPlayerBridge>();
                    if (bridge != null) {
                        // Dùng parent làm target thay vì child
                        target = target.parent;
                        Debug.Log("[EnemyScript] Found DungeonManiaPlayerBridge on parent, switched target to parent player");
                    }
                }
            }
        }
    }

    /// <summary>
    /// Được gọi bởi DungeonWaveManager để set player target trực tiếp
    /// </summary>
    public void SetPlayerTarget(Transform playerTransform)
    {
        if (playerTransform == null) return;
        target = playerTransform;
        SetupPlayerManagerReference();
        Debug.Log($"[EnemyScript] Player target set directly: {target.name}");
    }
    
    // Cập nhật specificEnemyType dựa trên index trong mảng enemy của RandomEnemy
    public void SetSpecificEnemyType(int index)
    {
        specificEnemyType = (SpecificEnemyType)Mathf.Clamp(index, 0, System.Enum.GetValues(typeof(SpecificEnemyType)).Length - 1);
        
        // Cập nhật enemyType (category) dựa trên specific type
        switch (specificEnemyType)
        {
            case SpecificEnemyType.Skeleton:
                enemyType = EnemyType.skelet;
                attackDistanceOverride = 1.5f; // Melee
                break;
            case SpecificEnemyType.SkeletonArcher:
                enemyType = EnemyType.archer; // Archer = ranged, KHÔNG phải skelet
                attackDistanceOverride = 8f;   // RANGED — tấn công từ xa
                break;
            case SpecificEnemyType.Orc:
            case SpecificEnemyType.Troll:
            case SpecificEnemyType.Guul:
                enemyType = EnemyType.monster;
                attackDistanceOverride = 2f;   // Melee
                break;
            case SpecificEnemyType.Lich:
                enemyType = EnemyType.lich;
                attackDistanceOverride = 8f;   // RANGED — tấn công từ xa
                break;
            case SpecificEnemyType.Stoneogre:
                enemyType = EnemyType.stoneogre;
                attackDistanceOverride = 2.5f;
                boss = EnemyClass.Boss.Ogre;
                break;
            case SpecificEnemyType.Golem:
                enemyType = EnemyType.golem;
                attackDistanceOverride = 2.5f;
                boss = EnemyClass.Boss.Golem;
                break;
            case SpecificEnemyType.Minotaur:
                enemyType = EnemyType.minotaur;
                attackDistanceOverride = 2.5f;
                boss = EnemyClass.Boss.Mino;
                break;
            case SpecificEnemyType.Ifrit:
                enemyType = EnemyType.ifrit;
                attackDistanceOverride = 2.5f;
                boss = EnemyClass.Boss.Ifrit;
                break;
            case SpecificEnemyType.Demon:
                enemyType = EnemyType.demon;
                attackDistanceOverride = 3f;
                break;
        }
        
        // ÁP DỤNG NGAY attackDistance và stoppingDistance
        // Vì OnEnable() đã chạy trước SetSpecificEnemyType()
        if (enemy != null) {
            enemy.distance = attackDistanceOverride;
        }
        attackDistance = attackDistanceOverride;
        if (navMeshAgent != null) {
            navMeshAgent.stoppingDistance = attackDistanceOverride;
        }
        
        Debug.Log($"[EnemyScript] Set specific type: {specificEnemyType}, category: {enemyType}, attackDistance: {attackDistanceOverride}");
    }  
    void OnEnable () {
        // === SPAWN VFX (delay 1 frame để position chính xác) ===
        if (hasAwoken && spawnVfxPrefab != null && !suppressSpawnVfx)
        {
            StartCoroutine(SpawnVfxDelayed());
        }
        
        // Re-find player nếu target bị null
        if (target == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj == null) {
                playerObj = GameObject.Find("player");
            }
            if (playerObj != null) {
                target = playerObj.transform;
            }
            SetupPlayerManagerReference();
        }
        
        // ÁP DỤNG GIÁ TRỊ TỪ INSPECTOR (chạy mỗi lần enemy được kích hoạt)
        ApplyInspectorValues();

        EnemyEvent.WaitEvent += Wait;
        EnemyEvent.AttackEvent += Chase;
        wait =false;
        delay = true;
        hit = false;
        attack = false;
        alive = true;
        cont = true;
        StartCoroutine(Delay());
    }
    
    /// <summary>
    /// Spawn VFX delay 1 frame để đảm bảo position chính xác 
    /// và giữ nguyên scale đã chỉnh trên prefab
    /// </summary>
    IEnumerator SpawnVfxDelayed()
    {
        yield return null; // Chờ 1 frame → position đã ổn định
        
        if (spawnVfxPrefab == null || !gameObject.activeInHierarchy) yield break;
        
        // === ENEMY SPAWN SOUND ===
        SoundManager.PlaySound(SoundType.Enemy_Spawn, GetComponent<AudioSource>(), 0.6f);
        
        Vector3 groundPos = transform.position;
        
        // Instantiate giữ nguyên scale từ prefab
        GameObject vfx = Instantiate(spawnVfxPrefab);
        vfx.transform.position = groundPos;
        
        // Nếu có PixPlays BaseVfx (BeamVfx, etc.) → gọi Play() với VfxData
        var baseVfx = vfx.GetComponent<PixPlays.ElementalVFX.BaseVfx>();
        if (baseVfx != null)
        {
            // Cột sáng: từ chân enemy bắn lên trời 8m
            Vector3 skyPos = groundPos + Vector3.up * 8f;
            var vfxData = new PixPlays.ElementalVFX.VfxData(groundPos, skyPos, spawnVfxDuration, 1f);
            baseVfx.Play(vfxData);
        }
        else
        {
            // Prefab đơn giản → tự hủy sau duration
            Destroy(vfx, spawnVfxDuration);
        }
    }
    void OnDisable(){
        EnemyEvent.WaitEvent -= Wait;
        EnemyEvent.AttackEvent -= Chase;
    }
    void Wait(){
        if (navMeshAgent == null || animator == null) return;
        wait = true;
        if (navMeshAgent.isOnNavMesh) navMeshAgent.isStopped = true;
        animator.SetBool("run", false);
    }
    void Chase(){
        if (navMeshAgent == null || animator == null) return;
        wait = false;
        if (navMeshAgent.isOnNavMesh) navMeshAgent.isStopped = false;
        animator.SetBool("run", true);
    }
    
    /// <summary>
    /// Được gọi TRỰC TIẾP bởi DungeonWaveManager để bắt đầu chase
    /// Thay vì qua EnemyEvent.AttackEvent (broadcast tới TẤT CẢ enemy)
    /// </summary>
    public void StartChase()
    {
        Debug.Log($"[EnemyScript] StartChase called directly on {gameObject.name}");
        Chase();
    }
    IEnumerator Delay(){
        // Kiểm tra null trước khi tiếp tục
        if (enemy == null) {
            Debug.LogWarning("[EnemyScript] Enemy is null in Delay!");
            delay = false;
            yield break;
        }

        animator.SetBool("hit", false);
        Distance();
        RotateToPlayer();

        // Stats được quản lý hoàn toàn bởi ApplyInspectorValues() trong OnEnable
        // KHÔNG gọi UpdateEnemy() — HeroInformation không được sử dụng
        // Damage = đúng giá trị attackDamage trong Inspector

        attackDistance = enemy.distance;
        yield return new WaitForSeconds ( 0.5f );
        delay = false;
    }
    public void RotateToPlayer(){
        if (target == null) return;
        direction = target.position - transform.position;
        direction.y = 0f; // Chỉ xoay ngang — tránh enemy nghiêng/ngã khi player nhảy
        if (direction.sqrMagnitude > 0.001f) {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 1f);
        }
    }
    public void Distance(){
        if (target == null || enemyState == null) return;
        enemyState.distance = Vector3.Distance(target.position, transform.position);
    }

    /// <summary>
    /// Áp dụng các giá trị từ Inspector vào EnemyClass (private)
    /// </summary>
    private void ApplyInspectorValues()
    {
        if (enemy == null) return;

        // Áp dụng attack
        enemy.attack.value = attackDamage;
        
        // Áp dụng armor
        enemy.armor.value = armorValue;
        
        // Áp dụng magic
        enemy.magic.value = magicValue;
        enemy.magicValue = magicValue;
        
        // Áp dụng crit
        enemy.crit.value = critChance;
        
        // Áp dụng accuracy
        enemy.accuracy.value = accuracy;
        
        // Áp dụng rewards (gold, score — EXP do TakeDamageTest quản lý)
        enemy.gold = goldReward;
        enemy.score = scoreReward;
        
        // Áp dụng attack distance
        if (attackDistanceOverride > 0)
        {
            enemy.distance = attackDistanceOverride;
        }
        
        // Áp dụng boss flag
        enemy.isBoss = isBoss;
        
        // === ELEMENTAL cho 3 boss chính ===
        // Ifrit = fire(1), Lich = dead(4), Demon = sẽ random trong EnemyAttack.D()
        // Set enemyMagic + đảm bảo magicValue > 0 để elemental damage hoạt động
        switch (specificEnemyType)
        {
            case SpecificEnemyType.Ifrit:
                enemy.enemyMagic = 1; // fire
                if (enemy.magicValue <= 0) enemy.magicValue = magicValue > 0 ? magicValue : enemy.attack.value / 3;
                break;
            case SpecificEnemyType.Lich:
                enemy.enemyMagic = 4; // dead
                if (enemy.magicValue <= 0) enemy.magicValue = magicValue > 0 ? magicValue : enemy.attack.value / 3;
                break;
            case SpecificEnemyType.Demon:
                enemy.enemyMagic = 0; // Demon dùng random trong EnemyAttack.D()
                if (enemy.magicValue <= 0) enemy.magicValue = magicValue > 0 ? magicValue : enemy.attack.value / 3;
                break;
        }
        
        // Áp dụng move speed và stoppingDistance cho NavMeshAgent
        var navAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (navAgent != null)
        {
            navAgent.speed = moveSpeed;
            // stoppingDistance = attackDistance để ranged enemies dừng đúng tầm bắn
            navAgent.stoppingDistance = enemy.distance;
        }
    }

    /// <summary>
    /// Public method để áp dụng giá trị từ Inspector (gọi từ bên ngoài sau khi SetSpecificEnemyType)
    /// </summary>
    public void ApplyInspectorValuesManual()
    {
        ApplyInspectorValues();
        Debug.Log($"[EnemyScript] Applied inspector values manually: attackDamage={attackDamage}, attackDistance={attackDistanceOverride}");
    }
    
    /// <summary>
    /// Vẽ vùng damage + tầm skill trong Scene view
    /// Xanh lá/Cam = vùng damage VFX | Xanh dương = tầm dùng skill (AI)
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // === Vẽ Skill Distance (tầm AI dùng skill) — XANH DƯƠNG ĐẬM ===
        if (skillVfxPrefab != null && skillDistance > 0)
        {
            Gizmos.color = new Color(0f, 0.4f, 1f, 1f); // Xanh dương đậm, full opacity
            Gizmos.DrawWireSphere(transform.position, skillDistance);
            // Vẽ thêm vòng nhỏ hơn bên trong để dễ nhìn
            Gizmos.color = new Color(0f, 0.4f, 1f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, skillDistance - 0.1f);
            // Vẽ gạch đánh dấu quanh vòng
            Gizmos.color = new Color(0f, 0.4f, 1f, 1f);
            int rings = 12;
            for (int i = 0; i < rings; i++)
            {
                float angle = (360f / rings) * i;
                Vector3 dir = Quaternion.Euler(0, angle, 0) * Vector3.forward;
                Gizmos.DrawLine(
                    transform.position + dir * (skillDistance - 0.5f),
                    transform.position + dir * skillDistance
                );
            }
        }
        
        // === Vẽ Attack Distance — ĐỎ NHẠ ===
        if (attackDistanceOverride > 0)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, attackDistanceOverride);
        }
        
        // === Vẽ Skill VFX Radius (vùng damage) ===
        if (skillVfxPrefab != null && skillVfxRadius > 0)
        {
            if (skillIsDirectional)
            {
                // Tâm cone = phía trước boss 1.5m
                Vector3 center = transform.position + transform.forward * 1.5f;
                center.y = transform.position.y + 0.1f;
                
                float halfAngle = skillAngle / 2f;
                int segments = 20;
                
                // Vẽ viền cung tròn (cam đậm)
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f);
                Vector3 prevPoint = center + Quaternion.Euler(0, -halfAngle, 0) * transform.forward * skillVfxRadius;
                for (int i = 1; i <= segments; i++)
                {
                    float angle = -halfAngle + (skillAngle * i / segments);
                    Vector3 dir = Quaternion.Euler(0, angle, 0) * transform.forward;
                    Vector3 point = center + dir * skillVfxRadius;
                    Gizmos.DrawLine(prevPoint, point);
                    prevPoint = point;
                }
                
                // 2 cạnh biên
                Vector3 leftEnd = center + Quaternion.Euler(0, -halfAngle, 0) * transform.forward * skillVfxRadius;
                Vector3 rightEnd = center + Quaternion.Euler(0, halfAngle, 0) * transform.forward * skillVfxRadius;
                Gizmos.DrawLine(center, leftEnd);
                Gizmos.DrawLine(center, rightEnd);
                
                // Tô đầy (cam mờ)
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.15f);
                for (int i = 0; i < segments; i++)
                {
                    float a1 = -halfAngle + (skillAngle * i / segments);
                    float a2 = -halfAngle + (skillAngle * (i + 1) / segments);
                    Vector3 p1 = center + Quaternion.Euler(0, a1, 0) * transform.forward * skillVfxRadius;
                    Vector3 p2 = center + Quaternion.Euler(0, a2, 0) * transform.forward * skillVfxRadius;
                    Gizmos.DrawLine(center, p1);
                    Gizmos.DrawLine(center, p2);
                    Gizmos.DrawLine(p1, p2);
                }
                
                // Trục chính
                Gizmos.color = Color.yellow;
                Gizmos.DrawRay(center, transform.forward * skillVfxRadius);
                
                // Chấm tâm
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(center, 0.15f);
            }
            else
            {
                // AoE: vòng tròn xanh lá
                Gizmos.color = new Color(0f, 1f, 0f, 0.4f);
                Gizmos.DrawWireSphere(transform.position, skillVfxRadius);
                Gizmos.color = new Color(0f, 1f, 0f, 0.1f);
                Gizmos.DrawSphere(transform.position, skillVfxRadius);
            }
        }
    }
}



