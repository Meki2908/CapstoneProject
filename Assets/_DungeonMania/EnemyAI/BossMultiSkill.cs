using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Hệ thống Multi-Skill cho boss chính (Wave 5).
/// Quản lý Phase 2, Shield, Projectile, Summon.
/// Gắn lên EnemyNew prefab (cùng object có EnemyScript).
/// </summary>
public class BossMultiSkill : MonoBehaviour
{
    [Header("=== PHASE 2 (HP ≤ 50%) ===")]
    [Range(0.1f, 0.9f)]
    public float phase2Threshold = 0.5f;
    [Tooltip("VFX Aura khi vào Phase 2 (FireAura/WindAura)")]
    public GameObject auraVfxPrefab;
    public float phase2DmgMultiplier = 1.5f;
    public float phase2SpdMultiplier = 1.3f;
    [Tooltip("Thời gian bất tử khi chuyển Phase 2 (giây)")]
    public float phase2InvulDuration = 3f;
    
    [Header("=== SHIELD SKILL ===")]
    [Tooltip("VFX Shield (FireShield/WindShield)")]
    public GameObject shieldVfxPrefab;
    public float shieldDuration = 5f;
    public float shieldCooldown = 15f;
    
    [Header("=== PROJECTILE SKILL ===")]
    [Tooltip("VFX Projectile (FireBeam/Fireball)")]
    public GameObject projectileVfxPrefab;
    public float projectileCooldown = 10f;
    [Tooltip("Damage cơ bản của projectile (không phụ thuộc sword)")]
    public int projectileBaseDamage = 20;
    [Tooltip("Damage phép của projectile")]
    public int projectileMagicDamage = 5;
    
    [Header("=== SUMMON SKILL ===")]
    public bool canSummon = false;
    [Tooltip("Summon dùng được cả Phase 1?")]
    public bool summonAvailablePhase1 = false;
    public int summonSkeletCount = 5;
    public int summonMonsterCount = 0;
    public float summonCooldown = 20f;
    [Tooltip("Delay lần đầu tiên trước khi summon (giây)")]
    public float summonInitialDelay = 10f;
    [Tooltip("Demon Phase 2: triệu hồi boss phụ (Stoneogre + Golem)")]
    public bool summonSubBosses = false;
    
    [Header("=== REFERENCES ===")]
    [Tooltip("Prefab EnemyNew — kéo từ PREFABS/Enemy")]
    public GameObject enemyNewPrefab;
    
    [Header("=== DEBUG ===")]
    public bool showDebug = true;
    
    // === RUNTIME ===
    [HideInInspector] public bool isPhase2 = false;
    [HideInInspector] public bool isShielded = false;
    
    private EnemyScript enemyScript;
    private TakeDamageTest takeDamageTest;
    private GameObject auraInstance;
    private GameObject shieldInstance;
    private float lastShieldTime = -999f;
    private float lastProjectileTime = -999f;
    private float lastSummonTime; // Sẽ set = Time.time + delay trong Start
    private bool hasSubBossSummoned = false;
    private int originalDamage;
    private float originalSpeed;
    private bool initialized = false;
    
    // Cached
    private Transform playerTarget;
    
    void Start()
    {
        enemyScript = GetComponentInChildren<EnemyScript>();
        if (enemyScript == null)
            enemyScript = GetComponentInParent<EnemyScript>();
        
        takeDamageTest = GetComponent<TakeDamageTest>();
        if (takeDamageTest == null)
            takeDamageTest = GetComponentInChildren<TakeDamageTest>();
        if (takeDamageTest == null)
            takeDamageTest = GetComponentInParent<TakeDamageTest>();
        
        if (enemyScript != null)
        {
            enemyScript.bossMultiSkill = this;
            if (showDebug) Debug.Log($"[BossMultiSkill] Linked to {enemyScript.enemyType}");
        }
        
        if (takeDamageTest != null)
        {
            if (showDebug) Debug.Log($"[BossMultiSkill] TakeDamageTest found! HP: {takeDamageTest.CurrentHealth}/{takeDamageTest.MaxHealth}");
        }
        else
        {
            Debug.LogWarning("[BossMultiSkill] TakeDamageTest NOT found! Phase 2 sẽ không hoạt động.");
        }
        
        // Initial delay — lần triệu hồi đầu tiên sẽ đợi summonInitialDelay giây
        lastSummonTime = Time.time + summonInitialDelay - summonCooldown;
    }

    void Update()
    {
        if (enemyScript == null) return;
        if (!initialized) TryInit();
        if (!initialized) return;
        
        // === BOSS ĐÃ CHẾT → DỪNG TẤT CẢ ===
        if (!enemyScript.alive)
        {
            StopAllSkills();
            return;
        }
        
        // Tìm player
        if (playerTarget == null && enemyScript.target != null)
            playerTarget = enemyScript.target;
        
        // === CHECK PHASE 2 ===
        CheckPhase2();
        
        // === AUTO SKILLS ===
        // Summon — dùng được Phase 1 (nếu config) hoặc Phase 2
        if (canSummon && (summonAvailablePhase1 || isPhase2))
        {
            TrySummon();
        }
        
        // Shield + Projectile — chỉ Phase 2
        if (isPhase2)
        {
            TryShield();
            TryProjectile();
        }
    }
    
    /// <summary>
    /// Dừng tất cả skill khi boss chết + đảm bảo enemy thực sự die
    /// </summary>
    void StopAllSkills()
    {
        StopAllCoroutines();
        isShielded = false;
        if (shieldInstance != null) Destroy(shieldInstance);
        if (auraInstance != null) Destroy(auraInstance);
        enabled = false; // Tắt script luôn
        
        // === ĐẢM BẢO BOSS THỰC SỰ CHẾT ===
        if (enemyScript != null)
        {
            enemyScript.alive = false;
            if (enemyScript.enemy != null)
                enemyScript.enemy.helth.value = 0;
            
            // Tìm EnemyDamage và chạy Death trên CHÍNH NÓ (không phải trên BossMultiSkill)
            EnemyDamage ed = enemyScript.GetComponent<EnemyDamage>();
            if (ed != null)
            {
                ed.StartCoroutine(ed.Death());
            }
            else
            {
                // Fallback: tắt gameObject
                enemyScript.gameObject.SetActive(false);
            }
        }
        
        if (showDebug) Debug.Log("[BossMultiSkill] Boss died — ALL skills stopped, Death triggered!");
    }
    
    void TryInit()
    {
        if (enemyScript.enemy == null) return;
        originalDamage = enemyScript.enemy.sword.damageMin;
        originalSpeed = enemyScript.moveSpeed;
        initialized = true;
    }
    
    #region PHASE 2
    
    void CheckPhase2()
    {
        if (isPhase2) return;
        if (takeDamageTest == null) return;
        
        // Dùng TakeDamageTest health (hệ thống HP chính)
        float hpPercent = takeDamageTest.CurrentHealth / takeDamageTest.MaxHealth;
        
        if (hpPercent <= phase2Threshold && hpPercent > 0)
        {
            StartCoroutine(Phase2Sequence());
        }
    }
    
    IEnumerator Phase2Sequence()
    {
        isPhase2 = true;

        if (DungeonOSTManager.Instance != null)
            DungeonOSTManager.Instance.OnBossEnteredPhase2();
        
        if (showDebug) Debug.Log("[BossMultiSkill] === PHASE 2 ACTIVATED ===");
        
        // Bất tử ngắn
        isShielded = true;
        
        // === RESET TIMERS — delay trước khi dùng skill Phase 2 ===
        float phase2InitialDelay = 5f;
        lastProjectileTime = Time.time + phase2InitialDelay - projectileCooldown;
        lastShieldTime = Time.time + phase2InitialDelay - shieldCooldown;
        
        // Spawn Aura VFX
        if (auraVfxPrefab != null)
        {
            auraInstance = Instantiate(auraVfxPrefab, transform.position, Quaternion.identity, transform);
            if (showDebug) Debug.Log("[BossMultiSkill] Aura VFX spawned!");
            
            // === BOSS ROAR + AURA SOUND ===
            var eSrc = GetComponent<AudioSource>();
            SoundManager.PlaySound(SoundType.Boss_Roar, eSrc, 1f);
            SoundManager.PlaySound(GetAuraSound(), eSrc, 0.7f);
        }
        
        yield return new WaitForSeconds(phase2InvulDuration);
        
        // Hết bất tử
        isShielded = false;
        
        // Buff stats
        if (enemyScript.enemy != null)
        {
            enemyScript.enemy.sword = new EnemySword(
                (int)(originalDamage * phase2DmgMultiplier)
            );
            enemyScript.moveSpeed = originalSpeed * phase2SpdMultiplier;
            if (enemyScript.navMeshAgent != null)
                enemyScript.navMeshAgent.speed = enemyScript.moveSpeed;
        }
        
        // Demon: summon sub-bosses 1 lần
        if (summonSubBosses && !hasSubBossSummoned)
        {
            hasSubBossSummoned = true;
            SummonSubBosses();
        }
        
        if (showDebug) Debug.Log($"[BossMultiSkill] Phase 2: dmg x{phase2DmgMultiplier}, speed x{phase2SpdMultiplier}");
    }
    
    #endregion
    
    #region SHIELD
    
    void TryShield()
    {
        if (shieldVfxPrefab == null) return;
        if (isShielded) return;
        if (Time.time - lastShieldTime < shieldCooldown) return;
        
        StartCoroutine(ShieldSequence());
    }
    
    IEnumerator ShieldSequence()
    {
        lastShieldTime = Time.time;
        isShielded = true;
        
        // Spawn Shield VFX
        shieldInstance = Instantiate(shieldVfxPrefab, transform.position, Quaternion.identity, transform);
        
        // === SHIELD SOUND ===
        SoundManager.PlaySound(GetShieldSound(), GetComponent<AudioSource>(), 0.8f);
        
        if (showDebug) Debug.Log($"[BossMultiSkill] Shield ON! Duration: {shieldDuration}s");
        
        yield return new WaitForSeconds(shieldDuration);
        
        // Hủy shield
        isShielded = false;
        if (shieldInstance != null) Destroy(shieldInstance);
        
        if (showDebug) Debug.Log("[BossMultiSkill] Shield OFF!");
    }
    
    #endregion
    
    #region PROJECTILE
    
    void TryProjectile()
    {
        if (projectileVfxPrefab == null) return;
        if (playerTarget == null) return;
        if (Time.time - lastProjectileTime < projectileCooldown) return;
        if (enemyScript.attack) return; // Đang đánh thường → đợi
        
        lastProjectileTime = Time.time;
        
        // === PROJECTILE SOUND ===
        SoundManager.PlaySound(GetProjectileSound(), GetComponent<AudioSource>(), 0.8f);
        
        SpawnProjectile();
    }
    
    void SpawnProjectile()
    {
        // Vị trí boss model thực — spawn ngay trên đầu boss
        Transform bossModel = enemyScript != null ? enemyScript.transform : transform;
        Vector3 sourcePos = bossModel.position + Vector3.up * 0.3f;
        Vector3 targetPos = playerTarget.position + Vector3.up * 1.0f; // Nhắm giữa người
        
        int dmg = projectileBaseDamage;
        int magic = projectileMagicDamage;
        
        // Instantiate full Fireball prefab
        GameObject proj = Instantiate(projectileVfxPrefab, Vector3.zero, Quaternion.identity);
        
        // Gọi ProjectileVfx.Play() với VfxData (source → target)
        var projectileVfx = proj.GetComponent<PixPlays.ElementalVFX.ProjectileVfx>();
        if (projectileVfx != null)
        {
            var vfxData = new PixPlays.ElementalVFX.VfxData(sourcePos, targetPos, 4f, 3f);
            projectileVfx.Play(vfxData);
            
            // Tìm child "Fireball" (projectile particle) → gắn collision damage
            Transform fireballChild = proj.transform.Find("Fireball");
            if (fireballChild == null)
            {
                // Tìm child có chứa "Projectile" hoặc "fireball" (case insensitive)
                foreach (Transform child in proj.transform)
                {
                    string name = child.name.ToLower();
                    if (name.Contains("fireball") || name.Contains("projectile"))
                    {
                        if (!name.Contains("cast") && !name.Contains("hit"))
                        {
                            fireballChild = child;
                            break;
                        }
                    }
                }
            }
            
            if (fireballChild != null)
            {
                // Gắn BossProjectile lên child projectile → collision damage
                BossProjectile bp = fireballChild.gameObject.AddComponent<BossProjectile>();
                bp.SetupCollisionOnly(dmg, magic);
                if (showDebug) Debug.Log($"[BossMultiSkill] Collision damage attached to: {fireballChild.name}");
            }
            else
            {
                if (showDebug) Debug.LogWarning("[BossMultiSkill] Không tìm thấy Fireball child!");
            }
            
            if (showDebug) Debug.Log($"[BossMultiSkill] Fireball! {sourcePos} → {targetPos}, dmg={dmg}");
        }
        else
        {
            // Fallback: nếu không có ProjectileVfx → dùng BossProjectile self-move
            proj.transform.position = sourcePos;
            BossProjectile bp = proj.GetComponent<BossProjectile>();
            if (bp == null) bp = proj.AddComponent<BossProjectile>();
            bp.Setup(playerTarget, dmg, magic, 12f);
            if (showDebug) Debug.Log("[BossMultiSkill] Fallback: BossProjectile self-move");
        }
    }
    
    #endregion
    
    #region SUMMON
    
    void TrySummon()
    {
        if (Time.time - lastSummonTime < summonCooldown) return;
        if (enemyNewPrefab == null) return;
        
        lastSummonTime = Time.time;
        
        // === BOSS ROAR khi summon ===
        SoundManager.PlaySound(SoundType.Boss_Roar, GetComponent<AudioSource>(), 0.9f);
        
        StartCoroutine(SummonSequence());
    }
    
    IEnumerator SummonSequence()
    {
        if (showDebug) Debug.Log($"[BossMultiSkill] Summoning: {summonSkeletCount} Skelet + {summonMonsterCount} Monster!");
        
        // Dừng boss di chuyển trong lúc summon
        if (enemyScript != null)
        {
            enemyScript.attack = true; // Ngăn AI chạy
            if (enemyScript.navMeshAgent != null && enemyScript.navMeshAgent.isOnNavMesh)
            {
                enemyScript.navMeshAgent.isStopped = true;
                enemyScript.navMeshAgent.ResetPath();
            }
        }
        
        // Spawn Skeletons (random giữa Skeleton=0 và Archer=1)
        for (int i = 0; i < summonSkeletCount; i++)
        {
            SpawnSummonedEnemy(Random.Range(0, 2)); // 0=Skeleton, 1=Archer
            yield return new WaitForSeconds(0.3f);
        }
        
        // Spawn Monsters (random giữa Orc=2, Troll=3, Guul=4)
        for (int i = 0; i < summonMonsterCount; i++)
        {
            SpawnSummonedEnemy(Random.Range(2, 5)); // 2=Orc, 3=Troll, 4=Guul
            yield return new WaitForSeconds(0.3f);
        }
        
        // Đợi animation summon xong
        yield return new WaitForSeconds(0.5f);
        
        // Reset boss state → tiếp tục AI bình thường
        if (enemyScript != null)
        {
            enemyScript.attack = false;
            if (enemyScript.navMeshAgent != null && enemyScript.navMeshAgent.isOnNavMesh)
            {
                enemyScript.navMeshAgent.isStopped = false;
            }
        }
    }
    
    void SpawnSummonedEnemy(int enemyTypeIndex)
    {
        if (enemyNewPrefab == null) return;
        
        // Random vị trí gần boss (dùng vị trí boss model thực)
        Transform bossModel = enemyScript != null ? enemyScript.transform : transform;
        Vector3 offset = Random.insideUnitSphere * 4f;
        offset.y = 0;
        Vector3 spawnPos = bossModel.position + offset;
        
        // Đảm bảo spawn trên NavMesh
        NavMeshHit hit;
        if (NavMesh.SamplePosition(spawnPos, out hit, 5f, NavMesh.AllAreas))
        {
            spawnPos = hit.position;
        }
        
        // Suppress VFX trong lần OnEnable đầu tiên (tránh double VFX)
        EnemyScript.suppressSpawnVfx = true;
        GameObject enemy = Instantiate(enemyNewPrefab, spawnPos, Quaternion.identity);
        
        // Tắt TẤT CẢ enemy con trước
        foreach (Transform child in enemy.transform)
        {
            child.gameObject.SetActive(false);
        }
        EnemyScript.suppressSpawnVfx = false; // Bật lại VFX cho lần enable đúng
        
        // Enable CHỈ enemy type cụ thể → OnEnable sẽ spawn VFX đúng vị trí
        RandomEnemy randomEnemy = enemy.GetComponent<RandomEnemy>();
        if (randomEnemy != null)
        {
            randomEnemy.EnableSpecificType(enemyTypeIndex);
        }
        
        // Set player target cho enemy vừa spawn
        EnemyScript es = enemy.GetComponentInChildren<EnemyScript>(false);
        if (es != null && playerTarget != null)
        {
            es.target = playerTarget;
            if (showDebug) Debug.Log($"[BossMultiSkill] Summoned index={enemyTypeIndex} at {spawnPos}");
        }

        if (DungeonOSTManager.Instance != null)
            DungeonOSTManager.Instance.ScheduleBossPresenceCheckForSpawnedRoot(enemy);
    }
    
    /// <summary>
    /// Demon Phase 2: Triệu hồi boss phụ Stoneogre + Golem
    /// </summary>
    void SummonSubBosses()
    {
        if (showDebug) Debug.Log("[BossMultiSkill] === SUMMONING SUB-BOSSES: Stoneogre + Golem ===");
        
        // Spawn Stoneogre (index 6)
        SpawnSummonedEnemy(6); // stoneogre
        
        // Spawn Golem (index 7)
        SpawnSummonedEnemy(7); // golem
    }
    
    #endregion
    
    #region PUBLIC API
    
    /// <summary>
    /// Gọi từ EnemyDamage — check nếu boss đang shield → block damage
    /// </summary>
    public bool ShouldBlockDamage()
    {
        return isShielded;
    }
    
    #endregion
    
    #region SOUND HELPERS
    
    /// <summary>
    /// Trả về SoundType cho Aura skill dựa trên loại boss
    /// </summary>
    SoundType GetAuraSound()
    {
        if (enemyScript == null) return SoundType.Boss_Ifrit_FireAura;
        switch (enemyScript.specificEnemyType)
        {
            case EnemyScript.SpecificEnemyType.Lich:
                return SoundType.Boss_Lich_WindAura;
            case EnemyScript.SpecificEnemyType.Ifrit:
            case EnemyScript.SpecificEnemyType.Demon: // Demon dùng chung fire
                return SoundType.Boss_Ifrit_FireAura;
            default:
                return SoundType.Boss_Ifrit_FireAura;
        }
    }
    
    /// <summary>
    /// Trả về SoundType cho Shield skill dựa trên loại boss
    /// </summary>
    SoundType GetShieldSound()
    {
        if (enemyScript == null) return SoundType.Boss_Ifrit_FireShield;
        switch (enemyScript.specificEnemyType)
        {
            case EnemyScript.SpecificEnemyType.Lich:
                return SoundType.Boss_Lich_WindShield;
            case EnemyScript.SpecificEnemyType.Ifrit:
            case EnemyScript.SpecificEnemyType.Demon:
                return SoundType.Boss_Ifrit_FireShield;
            default:
                return SoundType.Boss_Ifrit_FireShield;
        }
    }
    
    /// <summary>
    /// Trả về SoundType cho Projectile skill dựa trên loại boss
    /// </summary>
    SoundType GetProjectileSound()
    {
        if (enemyScript == null) return SoundType.Boss_Ifrit_Fireball;
        switch (enemyScript.specificEnemyType)
        {
            case EnemyScript.SpecificEnemyType.Lich:
                return SoundType.Boss_Lich_WindBullet;
            case EnemyScript.SpecificEnemyType.Ifrit:
            case EnemyScript.SpecificEnemyType.Demon:
                return SoundType.Boss_Ifrit_Fireball;
            default:
                return SoundType.Boss_Ifrit_Fireball;
        }
    }
    
    /// <summary>
    /// Trả về SoundType cho Boss AoE/Power Damage skill dựa trên loại boss
    /// Gọi từ bên ngoài (EnemyAttack.PowerDamage)
    /// </summary>
    public SoundType GetBossAoeSound()
    {
        if (enemyScript == null) return SoundType.Boss_Attack;
        switch (enemyScript.specificEnemyType)
        {
            case EnemyScript.SpecificEnemyType.Stoneogre:
                return SoundType.Boss_Stoneogre_EarthSlam;
            case EnemyScript.SpecificEnemyType.Golem:
                return SoundType.Boss_Golem_WaterBlast;
            case EnemyScript.SpecificEnemyType.Minotaur:
                return SoundType.Boss_Minotaur_EarthBlast;
            case EnemyScript.SpecificEnemyType.Lich:
                return SoundType.Boss_Lich_WindAoe;
            case EnemyScript.SpecificEnemyType.Ifrit:
                return SoundType.Boss_Ifrit_FireAoe;
            case EnemyScript.SpecificEnemyType.Demon:
                return SoundType.Boss_Demon_FireBlast;
            default:
                return SoundType.Boss_Attack;
        }
    }
    
    #endregion
    
    /// <summary>
    /// Cleanup khi boss chết
    /// </summary>
    void OnDestroy()
    {
        if (auraInstance != null) Destroy(auraInstance);
        if (shieldInstance != null) Destroy(shieldInstance);
    }
}
