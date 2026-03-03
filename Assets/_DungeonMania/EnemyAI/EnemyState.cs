using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class EnemyState : MonoBehaviour{
    EnemyScript enemyScript;
    public float distance;
    public bool isStop;
    int random;
    string anim;
    bool isAIRunning = false; // Thêm biến để ngăn chặn nhiều coroutine chạy cùng lúc

    private void Start () {
        enemyScript = GetComponent<EnemyScript> ();
        if (enemyScript != null) {
            enemyScript.enemyAttack = GetComponent<EnemyAttack> ();
            
            // Set NavMeshAgent stoppingDistance = attackDistance 
            // để ranged enemies dừng lại tại khoảng cách tấn công
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
                // Sửa: Chỉ chạy AI nếu không có AI nào đang chạy
                if(enemyScript.cont && !isAIRunning) {
                    StartCoroutine(AI());
                }
            }
        }
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
        
        // Đọc trực tiếp từ enemyScript thay vì cached value
        // Vì SetSpecificEnemyType() có thể thay đổi enemyType sau Start()
        int currentEnemyType = (int)enemyScript.enemyType;
        
        if(!enemyScript.attack && !enemyScript.hit){
            enemyScript.attack = true;
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
                case 3: // lich - ranged + skill (40% skill, 60% attack)
                 enemyScript.animator.Play(SelectAction(3));
                break;
                case 4: // boss
                 enemyScript.animator.Play(SelectAction(5));
                break;
                case 5: // demon
                 enemyScript.animator.Play(SelectAction(5));
                break;
            }
        }
    }
    IEnumerator AI(){
        // Đánh dấu đang chạy AI
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

                // LUÔN update NavMeshAgent destination khi có target (không chỉ khi anim "run")
                // Đảm bảo enemy luôn đuổi theo vị trí HIỆN TẠI của player
                if (enemyScript.navMeshAgent != null && enemyScript.target != null && !enemyScript.navMeshAgent.isStopped) {
                    enemyScript.navMeshAgent.destination = enemyScript.target.position;
                }

                if(enemyScript.enemyState.distance > enemyScript.attackDistance){
                    if(!enemyScript.attack && !enemyScript.hit){
                        if (enemyScript.navMeshAgent != null) {
                            enemyScript.navMeshAgent.isStopped = false;
                            // Update destination ngay khi bắt đầu chase
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
                    SelectEnemyType();
                }

                if (enemyScript.animator != null) {
                    enemyScript.anim = enemyScript.animator.GetCurrentAnimatorStateInfo ( 0 );
                    if(enemyScript.anim.IsName("Base Layer.hit")) enemyScript.animator.SetBool("hit", false);
                    if(enemyScript.anim.IsName("Base Layer.knock")) enemyScript.animator.SetBool("knock", false);
                    if(enemyScript.anim.IsName("Base Layer.idle")){enemyScript.attack = false; enemyScript.hit = false;}

                    // Reset attack flag sau khi animation attack kết thúc
                    if(enemyScript.anim.IsName("Base Layer.attack") || enemyScript.anim.IsName("attack")) {
                        if(!enemyScript.anim.loop) {
                            // Animation không lặp, kiểm tra xem đã kết thúc chưa
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
        // Đánh dấu AI đã hoàn thành
        isAIRunning = false;
    }
}
