using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class InstObject : MonoBehaviour{
    public Transform thisObject;
    public int number;
    public bool isItem;
    public bool crystall;
    public Transform[] items;
    Srystall[] scriptes;
    GameObject gameManager;
    [HideInInspector]public AudioManager audioManager;
    void OnEnable(){
        if(isItem) SetEnemyRoom.itemPositionEvent += SetItem;
    }
    void OnDisable(){
        if(isItem) SetEnemyRoom.itemPositionEvent -= SetItem;
    }
    void Awake(){
        gameManager = GameObject.Find("GameManager");
        audioManager = gameManager.GetComponent<AudioManager>();
        for(int i = 0; i < number; i++){
            Instantiate(thisObject, transform);
        }
        if(isItem){
            items = new Transform[transform.childCount];
            if(crystall) scriptes = new Srystall[transform.childCount];
            for(int i = 0; i < transform.childCount; i++){
                items[i] = transform.GetChild(i);
                if(crystall) scriptes[i] = items[i].GetComponent<Srystall>();
            }
        }
    }
    void SetItem(Vector3[] v, Transform[] v1){
        if(crystall){
            for(int i = 0; i < items.Length; i++){
                items[i].position = v[i];
                scriptes[i].SetObject();
            }
        }else{
            for(int i = 0; i < items.Length; i++){
                items[i].position = v1[i].position;
                items[i].rotation = v1[i].rotation;
                EnemyEvent.ChestClose(true);
            }
        }
    }
}
