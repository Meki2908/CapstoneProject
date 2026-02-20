using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
public class Menu : MonoBehaviour {
    GameObject gameManager;
    AudioManager audioManager;
    public GameObject[] menuButtons;
    public GameObject[] stateMenu;
    void Awake(){
        gameManager = GameObject.Find("GameManager");
        audioManager = gameManager.GetComponent<AudioManager>();
    }
    void Start() {
        foreach (GameObject gO in menuButtons) {
            gO.gameObject.SetActive(true);
        }
        StateButtons(0);
    }
    public void StateButtons(int idButton) {
        audioManager.MenuAudio(0);
        for (int i = 0; i <  stateMenu.Length; i++) {
            if (i == idButton) {
                stateMenu[idButton].SetActive(true);
            } else stateMenu[i].SetActive(false);
        }
    }
}

