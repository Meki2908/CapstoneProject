using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Bow : MonoBehaviour {
    ParticleSystem ps;
    Damage damage;
    public bool isSkill;
    Transform player;
    PlayerHelth playerHealth;
    int hit;
	void Awake () {
        ps = GetComponent<ParticleSystem>();
        player = GameObject.Find("player").transform;
        playerHealth = player.GetComponent<PlayerHelth>();
	}
    private void OnParticleCollision(GameObject go){
        playerHealth.PlayerDamage(damage, hit);
    }
    public void DamageBow(Damage d, int h){
        damage = d;
        hit = h;
        //if(isSkill)transform.position = new Vector3(player.position.x, 6f, player.position.z);
        ps.Play();
    }
}
