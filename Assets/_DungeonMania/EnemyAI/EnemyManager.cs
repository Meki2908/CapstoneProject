using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class EnemyManager : MonoBehaviour{
    public GameObject[] chestBoss;
    //public SwordsList swordsList;
    public ParticleSystem[] generalEffects;
    public ParticleSystem[] hitParticle;
    public ParticleSystem[] magicHit;
    public ParticleSystem[] spellHit;
    public ParticleSystem[] bossExpl;
    public AudioClip skullSound, goldSound, hitSkillAudio;
}