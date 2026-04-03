using UnityEngine;

/// <summary>
/// Simple script to ensure particle effects play when enemy attacks
/// Attach to the same object as Bow script
/// </summary>
public class EnemyAttackEffects : MonoBehaviour
{
    [Header("Attack Effects")]
    [Tooltip("Particle System for bow attack")]
    public ParticleSystem bowParticle;
    [Tooltip("Particle System for skill attack")]
    public ParticleSystem skillParticle;
    [Tooltip("Audio source for attack sounds")]
    public AudioSource attackAudio;

    [Header("Attack Sound")]
    public AudioClip bowSound;
    public AudioClip skillSound;

    [Header("Debug")]
    public bool showDebug = true;

    void Start()
    {
        // Try to find particle systems if not assigned
        if (bowParticle == null)
        {
            bowParticle = GetComponent<ParticleSystem>();
        }

        // Try to find child particle systems
        if (bowParticle == null)
        {
            ParticleSystem[] children = GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in children)
            {
                string name = ps.gameObject.name.ToLower();
                if (name.Contains("bow") || name.Contains("projectile"))
                {
                    bowParticle = ps;
                }
                else if (name.Contains("skill"))
                {
                    skillParticle = ps;
                }
            }
        }

        if (showDebug)
        {
            Debug.Log($"[EnemyAttackEffects] Initialized. Bow PS: {(bowParticle != null ? "found" : "null")}, Skill PS: {(skillParticle != null ? "found" : "null")}");
        }
    }

    /// <summary>
    /// Play bow attack effect - call this from animation event or EnemyAttack
    /// </summary>
    public void PlayBowAttack()
    {
        if (bowParticle != null)
        {
            if (!bowParticle.isPlaying)
            {
                bowParticle.Play();
                if (showDebug) Debug.Log("[EnemyAttackEffects] Bow particle playing");
            }
        }
        else
        {
            if (showDebug) Debug.LogWarning("[EnemyAttackEffects] Bow particle not assigned!");
        }

        if (attackAudio != null && bowSound != null)
        {
            attackAudio.PlayOneShot(bowSound);
        }
    }

    /// <summary>
    /// Play skill attack effect - call this from animation event or EnemyAttack
    /// </summary>
    public void PlaySkillAttack()
    {
        if (skillParticle != null)
        {
            if (!skillParticle.isPlaying)
            {
                skillParticle.Play();
                if (showDebug) Debug.Log("[EnemyAttackEffects] Skill particle playing");
            }
        }
        else if (bowParticle != null)
        {
            // Fallback to bow particle if skill not available
            if (!bowParticle.isPlaying)
            {
                bowParticle.Play();
                if (showDebug) Debug.Log("[EnemyAttackEffects] Using bow particle as fallback for skill");
            }
        }

        if (attackAudio != null && skillSound != null)
        {
            attackAudio.PlayOneShot(skillSound);
        }
    }

    /// <summary>
    /// Stop all attack effects
    /// </summary>
    public void StopAllEffects()
    {
        if (bowParticle != null && bowParticle.isPlaying)
        {
            bowParticle.Stop();
        }
        if (skillParticle != null && skillParticle.isPlaying)
        {
            skillParticle.Stop();
        }
    }

    /// <summary>
    /// Enable/disable particle emission
    /// </summary>
    public void SetEmission(bool enabled)
    {
        if (bowParticle != null)
        {
            if (enabled)
                bowParticle.Play();
            else
                bowParticle.Stop();
        }
        
        if (skillParticle != null)
        {
            if (enabled)
                skillParticle.Play();
            else
                skillParticle.Stop();
        }
    }
}
