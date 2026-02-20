using UnityEngine;
public class GameController : MonoBehaviour {
    public PlayerResurection playerResurection;
    public static bool pause;
    AudioManager audioManager;
    public delegate void SkillButton1();
    public static event SkillButton1 ContinueUpdateSkillButton;
    public static event SkillButton1 PauseShieldFX;
    public static event SkillButton1 ContinueShieldFX;
    public static event SkillButton1 StopFX;
    public AdsManager ads;
    public static AudioSource audioListener;
    void Start() {
        audioManager = GetComponent<AudioManager>();
        //ads = GetComponent<AdsManager>();
        audioListener = Camera.main.GetComponent<AudioSource>();
    }
    void OnEnable(){
        SkillButton.EventSkillButton += Pause;
    }
    void OnDisable(){
        SkillButton.EventSkillButton -= Pause;
    }
    public void Exit(bool b){
        if (b) {
            print(HeroInformation.isShield);
            audioManager.MenuAudio(0);
            if (HeroInformation.isShield) Resurection(true);
            else {
                int percent = Random.Range(1, 101);
                int pPercent = HeroInformation.player.jesus.value + HeroInformation.player.lucky.value;
                if (pPercent > 55) pPercent = 55;
                if (percent <= pPercent) Resurection(true);
                else {
                    PlayerSave();
                    if (GamePlayManager.level.levelType == Level.LevelType.bossLevel || GamePlayManager.level.levelType == Level.LevelType.demonLevel ||
                        GamePlayManager.level.levelType == Level.LevelType.arena) {
                        GamePlayManager g = GetComponent<GamePlayManager>();
                        g.SetBossSlider(false, 0);
                    }
                    if (GamePlayManager.level.levelType == Level.LevelType.demonLevel) {
                        HeroInformation.player.dungeonLevel--;
                        SetStars.Set();
                    } 
                    Resurection(false);
                }
            }
        }
        else{
            PlayerSave();
            if (IapManager.CheckNoAds()) {
                GamePlayManager.canResurection = false;
                Resurection(true);
            }
            else {
                GamePlayManager.canResurection = false;
               // ads.ShowAd(AdsManager.Earned.resurrection);
            }
        } 
    }
    public void Repeat(){
        audioManager.MenuAudio(0);
        PlayerSave();
        GetComponent<GamePlayManager>().SetBossSlider(false, 0);
        Resurection(false);
    }
    public void Quit(){
        audioManager.MenuAudio(0);
        PlayerSave();
        Application.Quit();
    }
    public void Pause(){
        audioManager.MenuAudio(2);
        PlayerSave();
        if (!pause){
            PauseShieldFX();
            pause = true;
            EnemyEvent.EnemyEventSystem(1);
        }
        else{
            ContinueUpdateSkillButton();
            ContinueShieldFX();
            pause = false;
            if(GamePlayManager.inside)EnemyEvent.EnemyEventSystem(2);
        }
    }
    public void Resurection(bool b){
        HeroInformation.Resurection();
        if (b) playerResurection.Resurection();
        else playerResurection.NoResurection();
    }
    public static void PlayerSave() {
        ObjectSerialization.Save("GameSave", HeroInformation.player);
    }
    public static void StopShieldFX() {
        StopFX();
    }
}
