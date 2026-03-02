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
    [Header("=== ENEMY STATS (Editable) ===")]
    [Tooltip("Tên enemy")]
    public string enemyName = "Enemy";
    
    [Tooltip("Máu tối đa")]
    public int maxHealth = 100;
    [Tooltip("Sát thương tấn công")]
    public int attackDamage = 10;
    [Tooltip("Giáp")]
    public int armorValue = 5;
    [Tooltip("Magic damage")]
    public int magicValue = 0;
    [Tooltip("Crit chance (%)")]
    public int critChance = 10;
    [Tooltip("Accuracy (%)")]
    public int accuracy = 50;
    [Tooltip("EXP khi tiêu diệt")]
    public int expReward = 100;
    [Tooltip("Gold khi tiêu diệt")]
    public int goldReward = 10;
    [Tooltip("Score khi tiêu diệt")]
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
        demon
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
        WinAudioEvent(i);
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
        navMeshAgent = GetComponent<NavMeshAgent> ();
        if (navMeshAgent != null) {
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
            case SpecificEnemyType.SkeletonArcher:
                enemyType = EnemyType.skelet;
                break;
            case SpecificEnemyType.Orc:
            case SpecificEnemyType.Troll:
            case SpecificEnemyType.Guul:
                enemyType = EnemyType.monster;
                break;
            case SpecificEnemyType.Lich:
                enemyType = EnemyType.lich;
                break;
            case SpecificEnemyType.Stoneogre:
            case SpecificEnemyType.Golem:
            case SpecificEnemyType.Minotaur:
            case SpecificEnemyType.Ifrit:
                enemyType = EnemyType.boss;
                // Cập nhật boss type
                switch (specificEnemyType)
                {
                    case SpecificEnemyType.Stoneogre:
                        boss = EnemyClass.Boss.Ogre;
                        break;
                    case SpecificEnemyType.Golem:
                        boss = EnemyClass.Boss.Golem;
                        break;
                    case SpecificEnemyType.Minotaur:
                        boss = EnemyClass.Boss.Mino;
                        break;
                    case SpecificEnemyType.Ifrit:
                        boss = EnemyClass.Boss.Ifrit;
                        break;
                }
                break;
            case SpecificEnemyType.Demon:
                enemyType = EnemyType.demon;
                break;
        }
        
        Debug.Log($"[EnemyScript] Set specific type: {specificEnemyType}, category: {enemyType}");
    }  
    void OnEnable () {
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
    void OnDisable(){
        EnemyEvent.WaitEvent -= Wait;
        EnemyEvent.AttackEvent -= Chase;
    }
    void Wait(){
        if (navMeshAgent == null || animator == null) return;
        wait = true;
        navMeshAgent.isStopped = true;
        animator.SetBool("run", false);
    }
    void Chase(){
        if (navMeshAgent == null || animator == null) return;
        wait = false;
        navMeshAgent.isStopped = false;
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

        // Kiểm tra HeroInformation.player - nếu null thì dùng target (player object)
        // Chỉ log warning một lần để tránh spam console
        if (HeroInformation.player != null) {
            enemy.UpdateEnemy();
        } else {
            // Chỉ log warning nếu chưa từng log (sử dụng static flag)
            if (!hasLoggedHeroInfoWarning) {
                Debug.Log("[EnemyScript] HeroInformation.player is null - using target as fallback (this is normal when not using DungeonMania player)");
                hasLoggedHeroInfoWarning = true;
            }
            // Sử dụng target (player GameObject) nếu HeroInformation.player null
            if (target != null) {
                // Enemy vẫn hoạt động với target là player
            }
        }

        attackDistance = enemy.distance;
        yield return new WaitForSeconds ( 0.5f );
        delay = false;
    }
    public void RotateToPlayer(){
        if (target == null) return;
        direction = target.position - transform.position;
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

        // Áp dụng health
        enemy.helth.value = maxHealth;
        enemy.mainHelth = maxHealth;
        
        // Áp dụng attack
        enemy.attack.value = attackDamage;
        
        // Áp dụng armor
        enemy.armor.value = armorValue;
        
        // Áp dụng magic
        enemy.magic.value = magicValue;
        
        // Áp dụng crit
        enemy.crit.value = critChance;
        
        // Áp dụng accuracy
        enemy.accuracy.value = accuracy;
        
        // Áp dụng rewards
        enemy.experiance = expReward;
        enemy.gold = goldReward;
        enemy.score = scoreReward;
        
        // Áp dụng attack distance
        if (attackDistanceOverride > 0)
        {
            enemy.distance = attackDistanceOverride;
        }
        
        // Áp dụng boss flag
        enemy.isBoss = isBoss;
        
        // Áp dụng move speed cho NavMeshAgent
        var navAgent = GetComponent<NavMeshAgent>();
        if (navAgent != null)
        {
            navAgent.speed = moveSpeed;
        }
    }

    /// <summary>
    /// Public method để áp dụng giá trị từ Inspector (gọi từ bên ngoài sau khi SetSpecificEnemyType)
    /// </summary>
    public void ApplyInspectorValuesManual()
    {
        ApplyInspectorValues();
        Debug.Log($"[EnemyScript] Applied inspector values manually: attackDamage={attackDamage}, maxHealth={maxHealth}");
    }
}



