using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class EnemyState : MonoBehaviour{
    EnemyScript enemyScript;
    public float distance;
    public bool isStop;
    int random;
    string anim;
    int enemyType;
    bool isAIRunning = false; // Thêm biến để ngăn chặn nhiều coroutine chạy cùng lúc

    private void Start () {
        enemyScript = GetComponent<EnemyScript> ();
        if (enemyScript != null) {
            enemyScript.enemyAttack = GetComponent<EnemyAttack> ();
            enemyType = (int)enemyScript.enemyType;
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
        // Sửa: Dùng && thay vì & để kiểm tra logic đúng
        if(!enemyScript.attack && !enemyScript.hit){
            enemyScript.attack = true;
            switch(enemyType){
                case 0:
                 enemyScript.animator.Play("attack");
                break;
                case 1:
                 enemyScript.animator.Play("attack");
                break;
                case 2:
                 enemyScript.animator.Play(SelectAction(3));
                break;
                case 3:
                 enemyScript.animator.Play("attack");
                break;
                case 4:
                 enemyScript.animator.Play(SelectAction(5));
                break;
                case 5:
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

                if(enemyScript.enemyState.distance > enemyScript.attackDistance){
                    if(!enemyScript.attack && !enemyScript.hit){
                        if (enemyScript.navMeshAgent != null) {
                            enemyScript.navMeshAgent.isStopped = false;
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
                    if(enemyScript.anim.IsName("Base Layer.run") && enemyScript.navMeshAgent != null && enemyScript.target != null) {
                        enemyScript.navMeshAgent.destination = enemyScript.target.position;
                    }

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
