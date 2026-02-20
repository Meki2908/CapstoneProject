using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.AI;
public class EnemyScript : MonoBehaviour {
    [HideInInspector] public bool hit, attack, delay, wait, alive;
    public EnemyClass enemy;
    public enum EnemyType{
        skelet,
        archer,
        monster,
        lich,
        boss,
        demon
    }
    public EnemyType enemyType = EnemyType.skelet;
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
        target = GameObject.Find ( "player" ).transform;
        if(bow.Length != 0){
            bowScript = new Bow[bow.Length];
            for(int i = 0; i < bow.Length; i++){
                bowScript[i] = bow[i].GetComponent<Bow>();
            }
        } 
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
        navMeshAgent = GetComponent<NavMeshAgent> ();
        navMeshAgent.isStopped = true;
        animator = GetComponent<Animator> ();
        parent = transform.parent;
        enemyManager = parent.GetComponent<EnemyManager>();
        randomEnemyScript = parent.GetComponent<RandomEnemy> ();
        playerManager = target.GetComponent<PlayerManager> ();
        enemyState = GetComponent<EnemyState> ();
        enemyAttack = GetComponent<EnemyAttack>();
        enemyDamage = GetComponent<EnemyDamage>();
        gameManager = GameObject.Find ( "GameManager" );
        audioManager = gameManager.GetComponent<AudioManager> ();
        heroInformation = gameManager.GetComponent<HeroInformation>();
        gameObject.SetActive ( false );
    }  
    void OnEnable () {
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
        wait = true;
        navMeshAgent.isStopped = true;
        animator.SetBool("run", false);
    }
    void Chase(){
        wait = false;
        navMeshAgent.isStopped = false;
        animator.SetBool("run", true);
    }
    IEnumerator Delay(){
        animator.SetBool("hit", false);
        Distance();
        RotateToPlayer();
        enemy.EnemySet((int)enemyType);
        enemy.UpdateEnemy();
        attackDistance = enemy.distance;
        yield return new WaitForSeconds ( 0.5f );
        delay = false;
    }
    public void RotateToPlayer(){
        direction = target.position - transform.position;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 1f);
    }
    public void Distance(){
        enemyState.distance = Vector3.Distance(target.position, transform.position);
    }
}



