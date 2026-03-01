using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
public class HeroInformation : MonoBehaviour {
    static EndGame endGameScript;
    public GameObject textPlayerGold;
    static Text gold;
    public GameObject textPlayerScore;
    static Text score;
    //public GameObject textPlayerKeys;
    //static Text keys;
    public GameObject textPlayerExp;
    static Text  experiencePoint;
    public GameObject playerLevel;
    static Text playerLevelText;
    public static PlayerClass player;
    public static bool alive = true; // Mặc định là true để enemy hoạt động khi không dùng HeroInformation của DungeonMania
    public static bool isSkill;
    public static bool isShield;
    static GameObject playerStat;
    GameObject menuUI;
    static GameObject endGame;
    //public SwordsList swordsList;
    public static int resurectionK;
    public UpdateCharacteristicsInfo updateCharacteristicsInfo;
    GameController gameController;
    public ItemDataBase itemDataBase;
    public delegate void Button(bool b);
    public static event Button SetButton;
    public delegate void NullButton();
    public static event NullButton nullButton;
    public static void ButtonSet(bool i){
        SetButton(i);
    }
    void NullButtonVoid() {
        nullButton();
    }
    public PlayerManager playerManager;
    public Traning training;

    void Awake() {
        // Tìm EndGame với kiểm tra null
        endGame = GameObject.Find( "EndGame" );
        if (endGame != null) {
        endGameScript = endGame.GetComponent<EndGame>();
            if (endGameScript != null) {
        endGame.SetActive(false);
            }
        } else {
            Debug.LogWarning("[HeroInformation] EndGame not found in scene");
        }
        
        playerStat = GameObject.Find( "GameUI" );
        menuUI = GameObject.Find("MenuUi");
        
        if (textPlayerExp != null)
        experiencePoint = textPlayerExp.GetComponent<Text>();
        if (textPlayerGold != null)
        gold = textPlayerGold.GetComponent<Text>();
        if (textPlayerScore != null)
        score = textPlayerScore.GetComponent<Text>();
        if (playerLevel != null)
        playerLevelText = playerLevel.GetComponent<Text>();
            
        gameController = GetComponent<GameController>();
        CreatePlayer();
        UpdateInformation();
    }
    public void CreatePlayer(){
        // ✅ PRODUCTION: Load save game nếu có, tạo mới nếu không
        // ⚠️ TEST MODE: Uncomment dòng dưới để xóa save mỗi lần khởi động
        // PlayerPrefs.DeleteKey("GameSave");
        // PlayerPrefs.DeleteKey("PlayerSave1");

        if (PlayerPrefs.HasKey("GameSave")) {
            player = (PlayerClass)ObjectSerialization.Load("GameSave");
            if (itemDataBase != null && itemDataBase.swords != null && itemDataBase.swords.Count > 0 && player.currentSwordIndex < itemDataBase.swords.Count)
                player.sword = itemDataBase.swords[player.currentSwordIndex];
            player.score = 0;
            alive = true;
        }
        else if (PlayerPrefs.HasKey("PlayerSave1")) {
            player = (PlayerClass)ObjectSerialization.Load("PlayerSave1");
            if (player != null) {
                player.inventorySword.Clear();
                player.indicatorSwordList = 1;
                if (itemDataBase != null && itemDataBase.swords != null && itemDataBase.swords.Count > 0) {
                    player.inventorySword.Add(itemDataBase.swords[0].index);
                    player.sword = itemDataBase.swords[0];
                }
                player.score = 0;
                alive = true;
                player.dungeonLevel = 0;
                player.gameLevel = 1;

                player.statePlayerForDialogue = CharactersClass.StatePlayerForDialogue.FirstMeet;
                player.itemMeet = false;
                player.storyMeet = false;
                player.bossMeet = false;
                player.demonMeet = false;
                player.firstBoss = false;
                player.firstDemon = false;
                player.firstDead = false;

                SetGlobalVar();

                ObjectSerialization.Save("GameSave", player);
                PlayerPrefs.DeleteKey("PlayerSave1");
            }
        }
        else{
            player = new PlayerClass("Player");
            SetGlobalVar();
            if (itemDataBase != null && itemDataBase.swords != null && itemDataBase.swords.Count > 0) {
                player.inventorySword.Add(itemDataBase.swords[0].index);
                player.sword = itemDataBase.swords[0];
            }
            alive = true;
            player.UpdateAbilitys();
        }
    }

    void SetGlobalVar() {
        PlayerPrefs.SetInt("QUEST_COUNT", 0);
        PlayerPrefs.SetInt("QUEST_1", 0);
        PlayerPrefs.SetInt("QUEST_2", 0);
        PlayerPrefs.SetInt("QUEST_3", 0);
        PlayerPrefs.SetInt("QUEST_4", 0);
        PlayerPrefs.SetInt("QUEST_5", 0);
    }

    public void Reset(){
        StopAllCoroutines();
        PlayerPrefs.DeleteKey("GameSave");
        CreatePlayer();
        SelectSword.SetSword();
        NullButtonVoid();
        playerManager.sliderExperiance.value = 0f;
        playerManager.sliderHelth.value = 0;
        playerManager.GetComponent<PlayerResurection>().NoResurection();
        StartCoroutine(GetComponent<StartGame>().StartBeginGame());
        SetStars.Set();
        GetComponent<GamePlayManager>().SetBossSlider(false, 0);
    }
    public IEnumerator UpdateStat(int goldValue, int scoreValue){
        int currentGoldValue = player.gold;
        int nextGoldValue = currentGoldValue + goldValue;
        int currentScoreValue = player.score;
        int nextScoreValue = currentScoreValue + scoreValue;
        float lerp = 0f;
        while(lerp < 0.5f){
            lerp += Time.deltaTime;
            player.score = (int)Mathf.Lerp(player.score, nextScoreValue, lerp/0.5f);
            player.gold = (int)Mathf.Lerp(player.gold, nextGoldValue, lerp/0.5f);    
            if(GameController.pause)updateCharacteristicsInfo.Info();       
            UpdateInformation();
            yield return null;
        }
    } 
    public static void UpdateInformation() {
        gold.text = player.gold.ToString();
        experiencePoint.text = player.experiencePoint.ToString();
        score.text = player.score.ToString();
        playerLevelText.text = player.playerLevel.ToString();
    }
    public static void EndGame() {
        //подгружаем рекламу
        HeroInformation.ButtonSet(false);
        playerStat.SetActive(false);
        GameController.PlayerSave();
        endGame.SetActive(true);
        endGameScript.ResurectionMenu();
    }
    public static void Resurection(){
        endGame.SetActive(false);
        playerStat.SetActive(true);
    }
}
