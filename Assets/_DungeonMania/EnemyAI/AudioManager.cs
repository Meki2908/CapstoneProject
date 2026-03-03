using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class AudioManager : MonoBehaviour {
    [HideInInspector]public AudioSource audioSource;
    public AudioClip[] commonSceneAudio;
    public AudioClip[] commonEnemySound;
    public AudioClip[] swordMagicDamage;
    public AudioClip[] simpleDamage;
    public AudioClip[] doorAudio;
    public AudioClip[] itemAudio;
    public AudioClip[] menuAudio;
    public AudioClip[] winAudio;
    public AudioClip[] playerSkills;
    public AudioClip[] playerSlash;
    public AudioClip[] playerHits;
    public AudioClip[] playerCommonAudio;
    public AudioClip[] playerSteps;
    void OnEnable(){
        EnemyScript.WinAudioEvent += WinAudio;
    }
    void OnDisable(){
         EnemyScript.WinAudioEvent -= WinAudio;
    }
	// Use this for initialization
	void Start () {
        audioSource = GetComponent<AudioSource>();
	}
    public void CommonSceneAudio(int i){
        audioSource.PlayOneShot(commonSceneAudio[i]);
    }
    public void CommonEnemySound(int i){
        audioSource.PlayOneShot(commonEnemySound[i]);
    }
    public void EnemyDamage(){
        audioSource.PlayOneShot(simpleDamage[Random.Range(0, simpleDamage.Length)]);
    }
    public void DoorAudioOpen(){
        audioSource.PlayOneShot(doorAudio[0]);
    }
    public void DoorAudioClose(){
        audioSource.PlayOneShot(doorAudio[1]);
    }
    public void WinAudio(int i){
        audioSource.PlayOneShot(winAudio[i]);
    }
    public void MenuAudio(int i){
        audioSource.PlayOneShot(menuAudio[i]);
    }
    public void ItemAudio(int i){
        audioSource.PlayOneShot(itemAudio[i]);
    }
    public void PlayerSkill(int i){
        audioSource.PlayOneShot(playerSkills[i]);
    }
    public void PlayerHits(){
        audioSource.PlayOneShot(playerHits[Random.Range(0, playerHits.Length)]);
    }
    public void PlayerCommonAudio(int i){
        audioSource.PlayOneShot(playerCommonAudio[i]);
    }
    public void PlayerSteps(){
        audioSource.PlayOneShot(playerSteps[Random.Range(0, playerSteps.Length)]);
    }
    public void SwordMagicDamage(int i){
        audioSource.PlayOneShot(swordMagicDamage[i]);
    }
    public void PlayerSlash(int i){
        audioSource.PlayOneShot(playerSlash[i]);
    }
}
