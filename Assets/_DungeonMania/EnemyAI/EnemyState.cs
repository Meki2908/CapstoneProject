using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class EnemyState : MonoBehaviour{
    EnemyScript enemyScript;
    public float distance;
    public bool isStop;
    int random;
    string anim;
    bool isAIRunning = false;

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
    /// Boss dùng skill — play animation "skill" + bắt đầu cooldown
    /// </summary>
    void UseBossSkill() {
        if (!enemyScript.attack && !enemyScript.hit) {
            enemyScript.attack = true;
            enemyScript.navMeshAgent.isStopped = true;
            enemyScript.animator.SetBool("run", false);
            enemyScript.animator.Play("skill");
            
            // Bắt đầu cooldown
            enemyScript.lastSkillTime = Time.time;
            enemyScript.skillOnCooldown = true;
            
            Debug.Log($"[EnemyState] Boss used SKILL! Cooldown: {enemyScript.skillCooldown}s");
        }
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
                // Boss có VFX skill → ở melee range chỉ đánh thường
                // Trừ khi skill đã sẵn sàng VÀ random trúng → dùng skill
                if (CanUseSkill()) {
                    // Ở melee range: 30% chance dùng skill, 70% đánh thường
                    if (Random.Range(0, 10) <= 2) {
                        enemyScript.lastSkillTime = Time.time;
                        enemyScript.skillOnCooldown = true;
                        enemyScript.animator.Play("skill");
                        return;
                    }
                }
                enemyScript.animator.Play("attack");
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

                if (enemyScript.target != null) {
                    enemyScript.Distance();
                }

                if (enemyScript.navMeshAgent != null && enemyScript.target != null && !enemyScript.navMeshAgent.isStopped) {
                    enemyScript.navMeshAgent.destination = enemyScript.target.position;
                }

                float dist = enemyScript.enemyState.distance;
                
                // === BOSS SKILL SYSTEM MỚI ===
                // Ưu tiên: Skill từ xa → Chase → Melee attack
                if (HasBossSkill() && CanUseSkill() 
                    && dist <= enemyScript.skillDistance 
                    && dist > enemyScript.attackDistance
                    && !enemyScript.attack && !enemyScript.hit) 
                {
                    // Player trong tầm skill (xa) nhưng ngoài tầm melee → DÙNG SKILL
                    UseBossSkill();
                }
                else if(dist > enemyScript.attackDistance){
                    // Ngoài tầm attack → CHASE
                    // Nếu boss có skill: chase đến skillDistance (dừng xa hơn)
                    // Nếu không: chase đến attackDistance (sát)
                    if(!enemyScript.attack && !enemyScript.hit){
                        if (enemyScript.navMeshAgent != null) {
                            // Nếu boss có skill sẵn sàng → dừng ở skillDistance
                            if (HasBossSkill() && CanUseSkill()) {
                                enemyScript.navMeshAgent.stoppingDistance = enemyScript.skillDistance;
                            } else {
                                enemyScript.navMeshAgent.stoppingDistance = enemyScript.attackDistance;
                            }
                            
                            enemyScript.navMeshAgent.isStopped = false;
                            if (enemyScript.target != null) {
                                enemyScript.navMeshAgent.destination = enemyScript.target.position;
                            }
                        }
                        if (enemyScript.animator != null) {
                            enemyScript.animator.SetBool("run", true);
                        }
                    }
                }
                else{
                    // Trong tầm melee → đánh thường (hoặc skill nếu ready + random trúng)
                    SelectEnemyType();
                }

                if (enemyScript.animator != null) {
                    enemyScript.anim = enemyScript.animator.GetCurrentAnimatorStateInfo ( 0 );
                    if(enemyScript.anim.IsName("Base Layer.hit")) enemyScript.animator.SetBool("hit", false);
                    if(enemyScript.anim.IsName("Base Layer.knock")) enemyScript.animator.SetBool("knock", false);
                    if(enemyScript.anim.IsName("Base Layer.idle")){enemyScript.attack = false; enemyScript.hit = false;}

                    if(enemyScript.anim.IsName("Base Layer.attack") || enemyScript.anim.IsName("attack")) {
                        if(!enemyScript.anim.loop) {
                            if(enemyScript.anim.normalizedTime >= 1.0f) {
                                enemyScript.attack = false;
                            }
                        }
                    }
                    
                    // Reset attack flag cho skill animation
                    if(enemyScript.anim.IsName("Base Layer.skill") || enemyScript.anim.IsName("skill")) {
                        if(!enemyScript.anim.loop) {
                            if(enemyScript.anim.normalizedTime >= 1.0f) {
                                enemyScript.attack = false;
                            }
                        }
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
        yield return new WaitForSeconds(0.1f);
        enemyScript.cont = true;
        isAIRunning = false;
    }
}
