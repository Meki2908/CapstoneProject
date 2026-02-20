using System.Collections;
using UnityEngine;
using UnityEngine.UI;
public class GamePlayManager : MonoBehaviour {
    public Slider slider;
    public Image sliderIcon;
    public Sprite[] sliderIcons;
    public static Slider sliderStatic;
    public static Level level;
    public static int lich, archers, monsteres, boss, demon;
    public Text textCurrentLevel;
    public static int leftEnemiesForDoor;
    public static int leftEnemiesArena;
    public static int countDoor;
    public static bool isDoorOpen;
    public static bool isBerserk;
    public Door[] doors;
    public static bool inTeleport;
    public GameObject[] teleports;
    public static  bool inside;
    public static bool isLevel;
    //public static int dungeonLevel;
    //public static int gameLevel;
    public static bool canResurection;
    public static int waveOfArena;
    public static int checkAreneEnemys;
    public static int enemysOfWave;
    public Transform npc;
    public static GameObject npcIndicator;
    public GameObject npcObject;
    public static GameObject staticNpcObject;
    GameController gameController;
    StartGame startGame;
    public Transform player;
    public Transform cameras;
    public GameObject arenaInfo;
    private void Awake(){
        gameController = GetComponent<GameController>();
        startGame = GetComponent<StartGame>();
        staticNpcObject = npcObject;
        sliderStatic = slider;
        SetBossSlider(false, 0);
        npcIndicator = npc.transform.GetChild(4).gameObject;
        Ini();
    }
    private void OnEnable() {
        SetEnemyRoom.BossSlider += SetBossSlider;
    }
    private void OnDisable() {
        SetEnemyRoom.BossSlider -= SetBossSlider;
    }
    public static void Ini(){
        level = new Level {
            enemyType = new int[] { 0, 0, 0, 0, 0 }
        };
        leftEnemiesForDoor = 10;
        isLevel = false;
        inTeleport = false;
        canResurection = true;
        SetNpcIndicator();
    }

    public static void SetNpcIndicator() {
        if (PlayerPrefs.GetInt("QUEST_COUNT") == 0 || PlayerPrefs.GetInt("QUEST_" + PlayerPrefs.GetInt("QUEST_COUNT").ToString()) == 2) npcIndicator.SetActive(true);
        else npcIndicator.SetActive(false);
    }

    public void ActivateTeleport(int i) {
        teleports[i].SetActive(true);
        teleports[i].SendMessage("EnableTeleport");
    }
    public static IEnumerator UpdateValue(float value) {
        float fillAmount = sliderStatic.value;
        float elapsed = 0f;
        while (elapsed < 0.5f) {
            elapsed += Time.deltaTime;
            sliderStatic.value = Mathf.Lerp(fillAmount, value, elapsed / 0.5f);
            yield return null;
        }
        sliderStatic.value = value;
    }
    public void SetBossSlider(bool on, int i) {
        if (on) {
            slider.gameObject.SetActive(true);
            sliderIcon.sprite = sliderIcons[i];
            StartCoroutine(UpdateValue(1f));
        } else {
            slider.gameObject.SetActive(false);
            StartCoroutine(UpdateValue(0f));
        }
    }
    public static void Arenalevel() {
        leftEnemiesArena = 0;
        checkAreneEnemys = 0;
        waveOfArena++;
        enemysOfWave = Random.Range(25, 30) + waveOfArena * 2;
        switch (waveOfArena) {
            case 0:
            level.enemyType = new int[] { 0, 0, 0, 0, 0 };
            break;
            case 1:
            level.enemyType = new int[] { 2, 0, 0, 0, 0 };
            break;
            case 2:
            level.enemyType = new int[] { Random.Range(2, 4), 1, 0, 0, 0 };
            break;
            case 3:
            level.enemyType = new int[] { Random.Range(2, 4), 2, 1, 0, 0 };
            break;
            case 4:
            level.enemyType = new int[] { Random.Range(2, 4), 3, Random.Range(1, 3), 0, 0 };
            break;
            default:
            level.enemyType = new int[] { 3, 5, 2, 0, 0 };
            break;
        }
    }

    public void UpdateArenaSlider() {
        StartCoroutine(UpdateValue((float)enemysOfWave / (float)(enemysOfWave - leftEnemiesArena)));
    }

    public void EndOfArenaWave() {
        gameController.Pause();
        startGame.pauseObject.SetActive(false);
        arenaInfo.SetActive(true);
        if (LocalizationManager.localizationIndex == 0) arenaInfo.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "Волна врагов уничтожена! Продолжить?";
        else arenaInfo.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "The wave of enemies has been destroyed! Continue?";
        player.GetComponent<PlayerManager>().animator.SetBool("attack1", false);
        arenaInfo.transform.localPosition = new Vector3(0, 0, 0);
        SetBossSlider(false, 0);
    }

    public void ContinueArena() {
        gameController.Pause();
        startGame.pauseObject.SetActive(true);
        arenaInfo.SetActive(false);
        Arenalevel();
        EnemyEvent.EnemyEventSystem(0);
        SetBossSlider(true, 0);
    }

    public void ExitArena() {
        gameController.Pause();
        startGame.pauseObject.SetActive(true);
        arenaInfo.SetActive(false);
        player.GetComponent<PlayerManager>().animator.Play("in");
        Vector3 startPos = new Vector3(2, 0, 10);
        player.position = startPos;
        cameras.position = startPos;
        EnemyEvent.DoorClose();
        EnemyEvent.ChestClose(true);
        Ini();
        EnemyEvent.EnemyEventSystem(3);
    }
}
