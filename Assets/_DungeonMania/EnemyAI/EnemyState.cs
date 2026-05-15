using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
public class EnemyState : MonoBehaviour{
    EnemyScript enemyScript;
    public float distance;
    public bool isStop;
    int random;
    string anim;
    bool isAIRunning = false;
    private float attackStartTime = 0f; // Timeout failsafe
    private Vector3 lastSetDestination; // Cache để tránh set destination mỗi tick
    [HideInInspector] public bool isCastingSkill = false; // Boss đang cast skill → khóa rotation
    [HideInInspector] public Vector3 skillCastDirection; // Hướng boss lúc bắt đầu cast (cho VFX)

    const float MinYawDeltaToRotate = 2f;

    bool ShouldRotateTowardPlayer()
    {
        if (enemyScript == null || enemyScript.target == null) return false;
        Vector3 dir = enemyScript.target.position - enemyScript.transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return false;
        float yawErr = Quaternion.Angle(enemyScript.transform.rotation, Quaternion.LookRotation(dir));
        return yawErr > MinYawDeltaToRotate;
    }

    private void Start () {
        enemyScript = GetComponent<EnemyScript> ();
        if (enemyScript != null) {
            enemyScript.enemyAttack = GetComponent<EnemyAttack> ();
            
            if (enemyScript.navMeshAgent != null) {
                enemyScript.navMeshAgent.stoppingDistance = enemyScript.attackDistance;
            }
        }
    }
    void Update () {
        if (enemyScript == null) return;

        if(!GameController.pause){
            if( enemyScript.alive ){
                if (!enemyScript.hit && enemyScript.target != null) {
                    if (!isCastingSkill && ShouldRotateTowardPlayer())
                        enemyScript.RotateToPlayer();
                }
                if(enemyScript.cont && !isAIRunning) {
                    StartCoroutine(AI());
                }
                
                // Cập nhật cooldown skill
                if (enemyScript.skillOnCooldown) {
                    if (Time.time - enemyScript.lastSkillTime >= enemyScript.skillCooldown) {
                        enemyScript.skillOnCooldown = false;
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Kiểm tra boss có skillVfxPrefab → dùng hệ thống skill mới
    /// </summary>
    bool HasBossSkill() {
        return enemyScript != null && enemyScript.skillVfxPrefab != null;
    }
    
    /// <summary>
    /// Kiểm tra skill đã hết cooldown chưa
    /// </summary>
    bool CanUseSkill() {
        return HasBossSkill() && !enemyScript.skillOnCooldown;
    }
    
    /// <summary>
    /// Boss dùng skill — CHARGE SYSTEM: đứng yên, xoay mặt theo player, warning lớn dần, rồi bắn
    /// Gọi từ AI loop (attack=false) HOẶC SelectEnemyType (attack=true)
    /// </summary>
    void UseBossSkill() {
        if (isCastingSkill) return; // Đã đang charge → không start thêm coroutine
        StartCoroutine(BossSkillChargeSequence());
    }
    
    /// <summary>
    /// Coroutine charge skill: boss đứng yên, warning mở rộng dần, rồi bắn skill
    /// Hỗ trợ multi-cast (skillRepeatCount > 1, VD: Demon 3x FireBlast)
    /// </summary>
    IEnumerator BossSkillChargeSequence()
    {
        enemyScript.attack = true;
        isCastingSkill = true;
        attackStartTime = Time.time;
        
        // Khóa vị trí, dừng di chuyển
        enemyScript.navMeshAgent.isStopped = true;
        enemyScript.animator.SetBool("run", false);
        
        // === KHÓA RIGIDBODY (tránh bị hất tung/đẩy bởi player spell) ===
        Rigidbody rb = enemyScript.GetComponent<Rigidbody>();
        if (rb == null) rb = enemyScript.GetComponentInParent<Rigidbody>();
        bool wasKinematic = (rb != null) ? rb.isKinematic : true;
        if (rb != null) { rb.isKinematic = true; rb.linearVelocity = Vector3.zero; }
        
        int totalCasts = Mathf.Max(enemyScript.skillRepeatCount, 1);
        
        for (int castIndex = 0; castIndex < totalCasts; castIndex++)
        {
            // Safety check
            if (enemyScript == null || !enemyScript.alive)
            {
                if (rb != null) rb.isKinematic = wasKinematic;
                isCastingSkill = false;
                yield break;
            }
            
            // === CHARGE PHASE ===
            // Cast đầu: full warning. Cast sau: KHÔNG warning, bắn ngay cho nhanh
            float chargeDur = (castIndex == 0) ? enemyScript.skillWarningDuration : 0f;
            
            // Spawn warning CHỈ cho lần cast ĐẦU TIÊN
            BossSkillWarning warning = null;
            if (castIndex == 0 && enemyScript.skillVfxPrefab != null)
            {
                if (enemyScript.skillIsDirectional)
                {
                    warning = BossSkillWarning.SpawnConeTracking(
                        enemyScript.transform,
                        enemyScript.target,
                        enemyScript.skillVfxRadius,
                        enemyScript.skillAngle,
                        chargeDur
                    );
                }
                else
                {
                    warning = BossSkillWarning.SpawnCircleTracking(
                        enemyScript.transform,
                        enemyScript.skillVfxRadius,
                        chargeDur
                    );
                }
            }
            
            // Charge loop: xoay mặt theo player
            float elapsed = 0f;
            while (elapsed < chargeDur)
            {
                if (enemyScript == null || !enemyScript.alive)
                {
                    if (warning != null) Destroy(warning.gameObject);
                    if (rb != null) rb.isKinematic = wasKinematic;
                    isCastingSkill = false;
                    yield break;
                }
                
                // ENFORCE khóa vị trí + trạng thái MỖI FRAME
                enemyScript.navMeshAgent.isStopped = true;
                enemyScript.navMeshAgent.velocity = Vector3.zero;
                enemyScript.hit = false;
                enemyScript.attack = true;
                if (rb != null) { rb.isKinematic = true; rb.linearVelocity = Vector3.zero; }
                
                // Xoay mặt về player
                if (enemyScript.target != null)
                {
                    Vector3 dir = enemyScript.target.position - enemyScript.transform.position;
                    dir.y = 0;
                    if (dir.sqrMagnitude > 0.001f)
                    {
                        Quaternion targetRot = Quaternion.LookRotation(dir);
                        enemyScript.transform.rotation = Quaternion.Slerp(
                            enemyScript.transform.rotation, targetRot,
                            Time.deltaTime * enemyScript.rotationSpeed * 2f
                        );
                    }
                }
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // === FIRE PHASE ===
            skillCastDirection = enemyScript.transform.forward;
            enemyScript.animator.Play("skill");
            
            Debug.Log($"[EnemyState] Boss SKILL #{castIndex+1}/{totalCasts} FIRED! dir={skillCastDirection}");
            
            // Chờ skill animation xong
            yield return new WaitForSeconds(0.15f);
            
            float animWait = 0f;
            while (animWait < 4f)
            {
                if (enemyScript == null || !enemyScript.alive)
                {
                    if (rb != null) rb.isKinematic = wasKinematic;
                    isCastingSkill = false;
                    yield break;
                }
                
                enemyScript.navMeshAgent.isStopped = true;
                enemyScript.hit = false;
                enemyScript.attack = true;
                if (rb != null) { rb.isKinematic = true; rb.linearVelocity = Vector3.zero; }
                
                var animState = enemyScript.animator.GetCurrentAnimatorStateInfo(0);
                bool isSkillPlaying = animState.IsName("Base Layer.skill") || animState.IsName("skill");
                
                if (isSkillPlaying && animState.normalizedTime >= 0.95f)
                    break;
                if (!isSkillPlaying && animWait > 0.3f)
                    break;
                
                animWait += Time.deltaTime;
                yield return null;
            }
            
            // Nếu còn lần cast tiếp → chờ repeatDelay
            if (castIndex < totalCasts - 1)
            {
                float waitTime = enemyScript.skillRepeatDelay;
                float waited = 0f;
                while (waited < waitTime)
                {
                    if (enemyScript == null || !enemyScript.alive)
                    {
                        if (rb != null) rb.isKinematic = wasKinematic;
                        isCastingSkill = false;
                        yield break;
                    }
                    enemyScript.navMeshAgent.isStopped = true;
                    enemyScript.hit = false;
                    if (rb != null) rb.isKinematic = true;
                    waited += Time.deltaTime;
                    yield return null;
                }
            }
        }
        
        // === COOLDOWN chỉ sau lần cast cuối ===
        enemyScript.lastSkillTime = Time.time;
        enemyScript.skillOnCooldown = true;
        
        // RESET — mở khóa hoàn toàn
        enemyScript.attack = false;
        enemyScript.hit = false;
        isCastingSkill = false;
        attackStartTime = 0f;
        if (rb != null) rb.isKinematic = wasKinematic; // Khôi phục physics
        if (enemyScript.navMeshAgent != null)
            enemyScript.navMeshAgent.isStopped = false;
        
        Debug.Log($"[EnemyState] Boss skill complete ({totalCasts} casts). All flags reset.");
    }
    
    /// <summary>
    /// Enemy dùng đánh thường
    /// </summary>
    void UseNormalAttack() {
        SelectEnemyType();
    }
    
    string SelectAction(int maxValue){
        random = Random.Range(0, 10);
        if(random <= maxValue) anim = "skill";
        else anim = "attack";
        return anim;
    }
    void SelectEnemyType(){
        if (enemyScript == null || enemyScript.navMeshAgent == null || enemyScript.animator == null) return;

        enemyScript.navMeshAgent.isStopped = true;
        enemyScript.animator.SetBool("run", false);
        
        int currentEnemyType = (int)enemyScript.enemyType;
        
        if(!enemyScript.attack && !enemyScript.hit){
            enemyScript.attack = true;
            
            // Nếu boss CÓ skill VFX → chỉ đánh thường ở đây (skill xử lý riêng)
            if (HasBossSkill()) {
                // Boss: LUÔN dùng charge skill nếu hết cooldown
                if (CanUseSkill()) {
                    UseBossSkill(); // Charge sequence
                    return;
                }
                enemyScript.animator.Play("attack");
                attackStartTime = Time.time;
                return;
            }
            
            // Enemy bình thường — logic cũ
            switch(currentEnemyType){
                case 0: // skelet - melee
                 enemyScript.animator.Play("attack");
                break;
                case 1: // archer - ranged
                 enemyScript.animator.Play("attack");
                break;
                case 2: // monster - melee + skill
                 enemyScript.animator.Play(SelectAction(3));
                break;
                case 3: // lich - ranged + skill
                 enemyScript.animator.Play(SelectAction(3));
                break;
                case 4: // boss (generic)
                 enemyScript.animator.Play(SelectAction(5));
                break;
                case 5: // demon
                 enemyScript.animator.Play(SelectAction(5));
                break;
                case 6: // stoneogre
                 enemyScript.animator.Play(SelectAction(3));
                break;
                case 7: // golem
                 enemyScript.animator.Play(SelectAction(4));
                break;
                case 8: // minotaur
                 enemyScript.animator.Play(SelectAction(3));
                break;
                case 9: // ifrit
                 enemyScript.animator.Play(SelectAction(4));
                break;
            }
        }
    }
    IEnumerator AI(){
        isAIRunning = true;
        enemyScript.cont = false;

        if (enemyScript == null || enemyScript.enemyState == null) {
            isAIRunning = false;
            yield break;
        }

        if(HeroInformation.alive){
            if (!enemyScript.delay && !enemyScript.wait){
                enemyScript.RefreshTargetIfNeeded();

                if (enemyScript.target != null) {
                    enemyScript.Distance();
                }

                float dist = enemyScript.enemyState.distance;
                
                // Nếu đang charge skill → không xử lý AI (coroutine đang chạy)
                if (isCastingSkill)
                {
                    // Không làm gì — BossSkillChargeSequence đang xử lý
                }
                // === RANGED KITING: lùi lại khi player tới quá gần ===
                // Chỉ retreat nếu attackDistance > minRangedDistance (có khoảng tấn công hợp lệ)
                // Nếu không, retreat sẽ đẩy enemy ra ngoài attack range → vòng lặp vô hạn
                else if (enemyScript.minRangedDistance > 0 
                    && dist < enemyScript.minRangedDistance
                    && enemyScript.attackDistance > enemyScript.minRangedDistance)
                {
                    // FORCE cancel attack + retreat
                    enemyScript.attack = false;
                    enemyScript.hit = false;
                    enemyScript.navMeshAgent.isStopped = false;
                    enemyScript.navMeshAgent.stoppingDistance = 0.5f;
                    
                    // Thử nhiều hướng retreat (trực tiếp lùi → xéo trái → xéo phải)
                    Vector3 awayDir = (enemyScript.transform.position - enemyScript.target.position).normalized;
                    float retreatDist = enemyScript.minRangedDistance + 2f;
                    Vector3[] tryDirs = new Vector3[] {
                        awayDir,                                                    // Lùi thẳng
                        Quaternion.Euler(0, 45, 0) * awayDir,                       // Xéo phải
                        Quaternion.Euler(0, -45, 0) * awayDir,                      // Xéo trái
                        Quaternion.Euler(0, 90, 0) * awayDir,                       // Ngang phải
                        Quaternion.Euler(0, -90, 0) * awayDir                       // Ngang trái
                    };
                    
                    bool found = false;
                    for (int i = 0; i < tryDirs.Length; i++)
                    {
                        Vector3 tryPos = enemyScript.transform.position + tryDirs[i] * retreatDist;
                        NavMeshHit navHit;
                        if (NavMesh.SamplePosition(tryPos, out navHit, 4f, NavMesh.AllAreas))
                        {
                            // Kiểm tra vị trí mới thực sự xa player hơn
                            float newDist = Vector3.Distance(navHit.position, enemyScript.target.position);
                            if (newDist > dist)
                            {
                                enemyScript.navMeshAgent.SetDestination(navHit.position);
                                lastSetDestination = navHit.position;
                                enemyScript.animator.SetBool("run", true);
                                found = true;
                                Debug.Log($"[RETREAT] {gameObject.name} dir#{i} dist={dist:F1}<{enemyScript.minRangedDistance} → newDist={newDist:F1}");
                                break;
                            }
                        }
                    }
                    
                    if (!found)
                    {
                        Debug.LogWarning($"[RETREAT] {gameObject.name} NO valid retreat pos! dist={dist:F1} type={enemyScript.enemyType} minRange={enemyScript.minRangedDistance}");
                    }
                }
                // === BOSS SKILL (from range) ===
                else if (HasBossSkill() && CanUseSkill() 
                    && dist <= enemyScript.skillDistance 
                    && dist > enemyScript.attackDistance
                    && !enemyScript.attack && !enemyScript.hit) 
                {
                    UseBossSkill();
                }
                // === CHASE ===
                else if(dist > enemyScript.attackDistance){
                    if(!enemyScript.attack && !enemyScript.hit){
                        if (enemyScript.navMeshAgent != null) {
                            #region agent log
                            AgentDebugLogger.Log(
                                "pre-fix-1",
                                "H2",
                                "EnemyState.cs:AI:chaseBranch:beforeSetDestination",
                                "Enemy entering chase branch",
                                "{\"enemy\":\"" + gameObject.name + "\",\"dist\":" + dist.ToString("F3") + ",\"attackDistance\":" + enemyScript.attackDistance.ToString("F3") + ",\"isStopped\":" + (enemyScript.navMeshAgent.isStopped ? "true" : "false") + ",\"isOnNavMesh\":" + (enemyScript.navMeshAgent.isOnNavMesh ? "true" : "false") + ",\"hasPath\":" + (enemyScript.navMeshAgent.hasPath ? "true" : "false") + "}");
                            #endregion

                            if (HasBossSkill() && CanUseSkill()) {
                                enemyScript.navMeshAgent.stoppingDistance = Mathf.Max(enemyScript.skillDistance - 1f, 1f);
                            } else {
                                // stoppingDistance phải NHỎ HƠN attackDistance
                                // để boss thực sự đi vào trong attack range
                                enemyScript.navMeshAgent.stoppingDistance = Mathf.Max(enemyScript.attackDistance - 1f, 1f);
                            }
                            
                            enemyScript.navMeshAgent.isStopped = false;
                            if (enemyScript.target != null) {
                                float destDiff = Vector3.Distance(enemyScript.target.position, lastSetDestination);
                                if (destDiff > 1f) {
                                    Vector3 rawTargetPos = enemyScript.target.position;
                                    Vector3 navTargetPos = rawTargetPos;
                                    bool sampled = false;

                                    // Fallback 1: sample trực tiếp quanh target.
                                    if (NavMesh.SamplePosition(rawTargetPos, out NavMeshHit targetHitDirect, 8f, NavMesh.AllAreas))
                                    {
                                        navTargetPos = targetHitDirect.position;
                                        sampled = true;
                                    }

                                    // Fallback 2: project cùng XZ nhưng dùng Y của enemy để tránh chênh cao root/player.
                                    if (!sampled)
                                    {
                                        Vector3 xzProjected = new Vector3(rawTargetPos.x, enemyScript.transform.position.y, rawTargetPos.z);
                                        if (NavMesh.SamplePosition(xzProjected, out NavMeshHit targetHitProjected, 35f, NavMesh.AllAreas))
                                        {
                                            navTargetPos = targetHitProjected.position;
                                            sampled = true;
                                        }
                                    }

                                    // Fallback 3: đi theo hướng tới player một đoạn rồi sample (đảm bảo luôn có điểm chase hợp lệ).
                                    if (!sampled)
                                    {
                                        Vector3 toward = rawTargetPos - enemyScript.transform.position;
                                        toward.y = 0f;
                                        if (toward.sqrMagnitude > 0.0001f)
                                        {
                                            toward.Normalize();
                                            Vector3 forwardProbe = enemyScript.transform.position + toward * 6f;
                                            if (NavMesh.SamplePosition(forwardProbe, out NavMeshHit targetHitForward, 20f, NavMesh.AllAreas))
                                            {
                                                navTargetPos = targetHitForward.position;
                                                sampled = true;
                                            }
                                        }
                                    }

                                    bool setOk = enemyScript.navMeshAgent.SetDestination(navTargetPos);
                                    lastSetDestination = navTargetPos;

                                    #region agent log
                                    AgentDebugLogger.Log(
                                        "post-fix-1",
                                        "H6",
                                        "EnemyState.cs:AI:chaseBranch:afterSetDestination",
                                        "SetDestination applied",
                                        "{\"enemy\":\"" + gameObject.name + "\",\"rawDestY\":" + rawTargetPos.y.ToString("F3") + ",\"sampled\":" + (sampled ? "true" : "false") + ",\"setOk\":" + (setOk ? "true" : "false") + ",\"destX\":" + navTargetPos.x.ToString("F3") + ",\"destY\":" + navTargetPos.y.ToString("F3") + ",\"destZ\":" + navTargetPos.z.ToString("F3") + ",\"pathStatus\":" + (int)enemyScript.navMeshAgent.pathStatus + ",\"remainingDistance\":" + enemyScript.navMeshAgent.remainingDistance.ToString("F3") + ",\"velocityMag\":" + enemyScript.navMeshAgent.velocity.magnitude.ToString("F3") + "}");
                                    #endregion
                                }
                            }
                        }
                        if (enemyScript.animator != null) {
                            enemyScript.animator.SetBool("run", true);
                        }
                    }
                }
                // === MELEE ATTACK ===
                else{
                    SelectEnemyType();
                }

                if (enemyScript.animator != null) {
                    enemyScript.anim = enemyScript.animator.GetCurrentAnimatorStateInfo ( 0 );
                    if(enemyScript.anim.IsName("Base Layer.hit")) enemyScript.animator.SetBool("hit", false);
                    if(enemyScript.anim.IsName("Base Layer.knock")) enemyScript.animator.SetBool("knock", false);
                    
                    if(enemyScript.anim.IsName("Base Layer.idle")){
                        // Đang charge skill → KHÔNG reset (coroutine quản lý)
                        if (!isCastingSkill) {
                            enemyScript.attack = false; enemyScript.hit = false;
                        }
                    }

                    if(enemyScript.anim.IsName("Base Layer.attack") || enemyScript.anim.IsName("attack")) {
                        if(!enemyScript.anim.loop) {
                            if(enemyScript.anim.normalizedTime >= 1.0f) {
                                enemyScript.attack = false;
                                isCastingSkill = false;
                            }
                        }
                    }
                    
                    if(enemyScript.anim.IsName("Base Layer.skill") || enemyScript.anim.IsName("skill")) {
                        if(!enemyScript.anim.loop) {
                            if(enemyScript.anim.normalizedTime >= 1.0f) {
                                enemyScript.attack = false;
                                isCastingSkill = false; // Skill animation xong
                            }
                        }
                    }
                    
                    // === ATTACK TIMEOUT FAILSAFE ===
                    // Tăng timeout lên 8s (charge 1.2s + skill animation ~2s + buffer)
                    if (enemyScript.attack && !isCastingSkill && attackStartTime > 0 && Time.time - attackStartTime > 5f)
                    {
                        Debug.LogWarning($"[EnemyState] Attack timeout on {gameObject.name}! Force reset.");
                        enemyScript.attack = false;
                        enemyScript.hit = false;
                        attackStartTime = 0f;
                    }
                }
            }
        }else{
                if (enemyScript.navMeshAgent != null) enemyScript.navMeshAgent.isStopped = true;
                if (enemyScript.animator != null) {
                    enemyScript.animator.SetBool("hit", false);
                    enemyScript.animator.SetBool("knock", false);
                    enemyScript.animator.SetBool("run", false);
                }
             }
        // Adaptive tick rate: nhanh hơn khi ở gần player
        float tickRate = (enemyScript.enemyState != null && enemyScript.enemyState.distance <= enemyScript.attackDistance * 1.5f) ? 0.05f : 0.12f;
        yield return new WaitForSeconds(tickRate);
        enemyScript.cont = true;
        isAIRunning = false;
    }
    
    /// <summary>
    /// Spawn warning zone tại vị trí skill sẽ đánh (AoE hoặc cone)
    /// </summary>
    void SpawnSkillWarningZone()
    {
        if (enemyScript == null || enemyScript.skillVfxPrefab == null) return;
        
        float warningDur = enemyScript.skillWarningDuration;
        if (warningDur <= 0) return;
        
        if (enemyScript.skillIsDirectional)
        {
            Vector3 center = enemyScript.transform.position + enemyScript.transform.forward * 1.5f;
            BossSkillWarning.SpawnCone(
                center, 
                enemyScript.transform.forward, 
                enemyScript.skillVfxRadius,
                enemyScript.skillAngle,
                warningDur
            );
        }
        else
        {
            BossSkillWarning.SpawnCircle(
                enemyScript.transform.position,
                enemyScript.skillVfxRadius,
                warningDur
            );
        }
    }
}
