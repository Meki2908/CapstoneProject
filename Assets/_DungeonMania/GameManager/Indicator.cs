using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class Indicator : MonoBehaviour{
    public Transform indicator;
    public Sprite[] sprites;
    GameObject indicatorGameObject;
    Image indicatorImage;
    Text indicatorText;
    void Start(){
        indicatorGameObject = indicator.gameObject;
        indicatorImage = indicator.GetChild(0).GetComponent<Image>();
        indicatorText = indicator.GetChild(1).GetComponent<Text>();
        indicatorGameObject.SetActive(false);
    }
    public void ShowIndicator(int i, int ii){
        indicatorGameObject.SetActive(true);
        indicatorImage.sprite = sprites[i];
        indicatorText.text = ii.ToString();
    }
    public void HiddenIndicator(){
        indicatorGameObject.SetActive(false);
    }
}
