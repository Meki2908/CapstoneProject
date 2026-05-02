using UnityEngine;

/// <summary>
/// ScriptableObject định nghĩa các loại projectile khác nhau
/// </summary>
[CreateAssetMenu(fileName = "ProjectileType", menuName = "Scriptable Objects/ProjectileType")]
public class ProjectileTypeSO : ScriptableObject
{
    [Header("Projectile Identity")]
    public string projectileName = "Basic Projectile";
    public string description = "A basic projectile";

    [Header("Visual")]
    public GameObject projectilePrefab;
    public GameObject hitEffect;
    public GameObject trailEffect;

    [Header("Physics")]
    [Tooltip("Projectile speed")]
    public float speed = 20f;
    [Tooltip("Projectile lifetime before auto-destroy")]
    public float lifetime = 3f;
    [Tooltip("Gravity effect (0 = no gravity, 1 = full gravity)")]
    public float gravity = 0f;
    [Tooltip("Projectile size multiplier")]
    public float sizeMultiplier = 1f;

    [Header("Auto-Aim")]
    [Tooltip("Auto-aim range")]
    public float autoAimRange = 15f;
    [Tooltip("Auto-aim strength (0 = no aim, 1 = perfect aim)")]
    public float autoAimStrength = 1f;
    [Tooltip("Auto-aim update rate (times per second)")]
    public float autoAimUpdateRate = 10f;

    [Header("Damage")]
    [Tooltip("Base damage")]
    public float damage = 50f;
    [Tooltip("Damage type (Physical/Magical)")]
    public string damageType = "Physical";
    [Tooltip("Can pierce through enemies")]
    public bool canPierce = false;
    [Tooltip("Max pierce count (0 = infinite)")]
    public int maxPierceCount = 0;

    [Header("Special Effects")]
    [Tooltip("Explode on hit")]
    public bool explodeOnHit = false;
    [Tooltip("Explosion radius")]
    public float explosionRadius = 5f;
    [Tooltip("Explosion damage")]
    public float explosionDamage = 25f;
    [Tooltip("Bounce off surfaces")]
    public bool canBounce = false;
    [Tooltip("Max bounce count")]
    public int maxBounceCount = 0;
    [Tooltip("Bounce force multiplier")]
    public float bounceForceMultiplier = 0.8f;

    [Header("Audio")]
    public AudioClip fireSound;
    public AudioClip hitSound;
    public AudioClip trailSound;

    [Header("Particle Effects")]
    public GameObject muzzleFlash;
    public GameObject impactParticles;
    public GameObject trailParticles;
}
