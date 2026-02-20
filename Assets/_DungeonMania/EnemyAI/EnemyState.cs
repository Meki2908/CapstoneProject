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
    private void Start () {
        enemyScript = GetComponent<EnemyScript> ();
        enemyScript.enemyAttack = GetComponent<EnemyAttack> ();
        enemyType = (int)enemyScript.enemyType;
    }
    void Update () {
        if(!GameController.pause){
            if( enemyScript.alive ){
                if (!enemyScript.hit) enemyScript.RotateToPlayer();
                if(enemyScript.cont) StartCoroutine(AI());
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
        enemyScript.navMeshAgent.isStopped = true;
        enemyScript.animator.SetBool("run", false);
        if(!enemyScript.attack & !enemyScript.hit){
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
        enemyScript.cont = false;  
        if(HeroInformation.alive){
            if (!enemyScript.delay && !enemyScript.wait){  
                enemyScript.Distance();
                if(distance > enemyScript.attackDistance){  
                    if(enemyScript.attack || enemyScript.hit){

                    }
                    else{
                        enemyScript.navMeshAgent.isStopped = false;
                        enemyScript.animator.SetBool("run", true);
                    }
                }
                else{ 
                        SelectEnemyType();
                    }
                 enemyScript.anim = enemyScript.animator.GetCurrentAnimatorStateInfo ( 0 );    
                 if(enemyScript.anim.IsName("Base Layer.hit")) enemyScript.animator.SetBool("hit", false);
                 if(enemyScript.anim.IsName("Base Layer.knock")) enemyScript.animator.SetBool("knock", false);
                 if(enemyScript.anim.IsName("Base Layer.idle")){enemyScript.attack = false; enemyScript.hit = false;}
                 if(enemyScript.anim.IsName("Base Layer.run")) enemyScript.navMeshAgent.destination = enemyScript.target.position;
            }
        }else{
                enemyScript.navMeshAgent.isStopped = true;
                enemyScript.animator.SetBool("hit", false);
                enemyScript.animator.SetBool("knock", false);
                enemyScript.animator.SetBool("run", false);
             }        
     yield return new WaitForSeconds(0.1f);
     enemyScript.cont = true;
    }
}
