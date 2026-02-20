using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chest : MonoBehaviour {
    public bool firstLevel;
    GameObject gameManager;
    AudioManager audioManager;
    GameController gameController;
    HeroInformation heroInformation;
    GameObject chest;
    GameObject goldPlane;
    public ParticleSystem spark;
    public enum ChestType{
        ChestWood,
        ChestSteampunk,
        ChestSkull,
    }
    public ChestType chestType = ChestType.ChestWood;
    bool open;
    bool isPlayerEnter;
    public ParticleSystem goldParticle;
    public GameObject bat;
    public ParticleSystem smoke;
    Animator animator;
    void OnEnable(){
        if((int)chestType == 0) EnemyEvent.CloseChestes += CloseChests;
        else{
            EnemyEvent.DisableEvent += Reload;
            spark.Play();
        }
    }
    void OnDisable(){
        if((int)chestType == 0) EnemyEvent.CloseChestes -= CloseChests;
        else EnemyEvent.DisableEvent -= Reload;
    }
	void Start () {
        gameManager = GameObject.Find("GameManager");
        audioManager = gameManager.GetComponent<AudioManager>();
        heroInformation = gameManager.GetComponent<HeroInformation>();
        gameController = gameManager.GetComponent<GameController>();
        spark.Stop();
        for( int i = 0; i < 3; i++) {
            transform.GetChild(i).gameObject.SetActive(false);
        }
        chest = transform.GetChild((int)chestType).gameObject;
        goldPlane = transform.GetChild(6).gameObject;
        goldPlane.SetActive(false);
        if((int)chestType != 0) gameObject.SetActive(false);
        chest.SetActive(true);
        animator = chest.GetComponent<Animator>();
        bat.SetActive(false);
        CloseChests(true);  
	}
    private void OnTriggerEnter(Collider other) {
        if (open) return;
        else {
            if (other.name == "player") {
                isPlayerEnter = true;
                other.SendMessage( "Chest", true );
                other.SendMessage( "ChestObject", this.gameObject );
                other.SendMessage("CheckChest", chestType);
            }
        }
    }
    private void OnTriggerExit(Collider other) {
        if (open) return;
        else {
            if (other.name == "player") {
                isPlayerEnter = false;
                other.SendMessage( "Chest", false );
                other.SendMessage( "ChestObject", this.gameObject );
            }
        }
    }
    public IEnumerator Open() {
        open = true;
        audioManager.ItemAudio(0);
        animator.Play("open");
        yield return new WaitForSeconds(0.1f);
        StartBonus();
        if (chestType != ChestType.ChestWood) {
            yield return new WaitForSeconds(5f);
            animator.Play("close");
            open = false;
            isPlayerEnter = false;
            goldParticle.Stop();
            animator.Play("Shake");
            smoke.Play();
            yield return new WaitForSeconds(0.4f);
            spark.Stop();
            gameObject.SetActive(false);
        }
    }

    void StartBonus() {
        if (IapManager.CheckNoAds()) {
            CheckBonus();
        } else {
            if (Application.internetReachability != NetworkReachability.NotReachable) {
                if(/*AdsManager.CheckAds()*/ 1==2) CheckBonus();
                else StartCoroutine(Gold());
            } 
            else StartCoroutine(Gold());
        }
    }

    void CheckBonus() {
        int percenBonusGold = Random.Range(1, 101);
        int lucky = HeroInformation.player.lucky.value + HeroInformation.player.playerLevel;
        if (chestType == ChestType.ChestSteampunk) lucky += 5;
        else if (chestType == ChestType.ChestSkull) lucky += 10;
        else if (chestType == ChestType.ChestWood) {
            if (HeroInformation.player.dungeonLevel == 0) lucky = 25;
        }
        if (lucky > 25) lucky = 25;
        if (percenBonusGold < lucky) {
            audioManager.WinAudio(1);
            //yield return new WaitForSeconds(0.1f);
            int percentArtifacte = Random.Range(1, 101);
            int currentLucky = HeroInformation.player.lucky.value + HeroInformation.player.playerLevel; //+ 5;
            if (chestType == ChestType.ChestSteampunk) currentLucky += 5;
            else if (chestType == ChestType.ChestSkull) currentLucky += 10;
            else if (chestType == ChestType.ChestWood) {
                if (HeroInformation.player.dungeonLevel == 0) currentLucky = 40;
            }
            if (currentLucky > 40) currentLucky = 40;
            //if (percentArtifacte < currentLucky & gameController.ads.CheckAds())
            if (percentArtifacte < currentLucky)
            {

            }
                //gameController.ads.AdInfo(AdsManager.Earned.artifacte, 1);
            else {
                int amount;
                if (HeroInformation.player.miner.value == 0) amount = 300;
                else amount = ((HeroInformation.player.playerLevel + HeroInformation.player.miner.value + HeroInformation.player.lucky.value) * 10) + 300;
                amount += Random.Range(0, HeroInformation.player.miner.value + 10);
               // gameController.ads.AdInfo(AdsManager.Earned.gold, amount);
            }
        }
        else {
            StartCoroutine(Gold());
        }
    }
    IEnumerator Gold() {
        yield return new WaitForSeconds(0.55f);
        int percentGold;
        if(chestType != ChestType.ChestWood) percentGold = 0;
        else percentGold = Random.Range( 1, 101 );
        if (percentGold > HeroInformation.player.lucky.value + 60) {
            audioManager.ItemAudio(1);
            bat.SetActive(true);
        }
        else {
            goldPlane.SetActive(true);
            goldParticle.Play();
            StartCoroutine(StopParticle());
            audioManager.ItemAudio(2);
            int goldInformation = Random.Range( 10, 16 );
            goldInformation += (HeroInformation.player.miner.value + HeroInformation.player.playerLevel);
            goldInformation += (HeroInformation.player.dungeonLevel * 2 * HeroInformation.player.gameLevel);
            if((int)chestType == 1) goldInformation *= 5;
            if((int)chestType == 2) goldInformation *= 10;
            if(GamePlayManager.level.index == 9) goldInformation *= 5;
            StartCoroutine(heroInformation.UpdateStat(goldInformation, 0));
        }
    }
    public void CloseChests(bool b) {
        if(b){
            animator.Play("close");
            open = false;
            isPlayerEnter = false;
            goldParticle.Stop();
        }
        else{
            if(firstLevel){
            animator.Play("close");
            open = false;
            isPlayerEnter = false;
            goldParticle.Stop();                
            }
        }
    }
    IEnumerator StopParticle(){
        yield return new WaitForSeconds (1.4f);
        goldParticle.Stop();
        goldPlane.SetActive(false);
    }
    public void Reload(){
        animator.Play("close");
        open = false;
        isPlayerEnter = false;
        goldParticle.Stop();
        gameObject.SetActive(false);
    }
    public void Damage(){
        Open();
    }
}

