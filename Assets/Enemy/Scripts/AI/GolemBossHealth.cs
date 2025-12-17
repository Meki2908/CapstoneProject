using UnityEngine;
using DamageNumbersPro;

/// <summary>
/// GOLEM BOSS HEALTH SYSTEM
/// Quản lý máu, nhận damage, visual effects, và phase transitions
/// </summary>
public class GolemBossHealth : MonoBehaviour
{
    [Header("=== HEALTH SETTINGS ===")]
    [Tooltip("Tổng máu của boss")]
    public float maxHealth = 1000f;
    [HideInInspector] public float currentHealth;
    
    [Header("=== DEFENSE ===")]
    [Tooltip("% damage reduction")]
    [Range(0f, 0.9f)] public float damageReduction = 0.2f;
    
    [Tooltip("Boss bất tử trong thời gian này sau khi spawn")]
    public float invulnerabilityDuration = 3f;
    private float invulnerabilityTimer = 0f;
    
    [Header("=== PHASE ARMOR ===")]
    [Tooltip("Giảm damage thêm khi ở Phase 1")]
    [Range(0f, 0.5f)] public float phase1ExtraArmor = 0.1f;
    
    [Tooltip("Giảm damage thêm khi ở Phase 3 (rage mode)")]
    [Range(0f, 0.5f)] public float phase3ExtraArmor = 0.15f;
    
    [Header("=== VISUAL FEEDBACK ===")]
    [Tooltip("Damage number prefab")]
    public DamageNumber damageNumberPrefab;
    
    [Tooltip("Hit effect prefab")]
    public GameObject hitEffectPrefab;
    
    [Tooltip("Critical hit effect")]
    public GameObject criticalHitEffectPrefab;
    
    [Tooltip("Phase transition effect")]
    public GameObject phaseTransitionEffectPrefab;
    
    [Tooltip("Death effect")]
    public GameObject deathEffectPrefab;
    
    [Header("=== HEALTH BAR ===")]
    [Tooltip("World space health bar transform")]
    public Transform healthBarTransform;
    
    [Tooltip("Health bar fill image")]
    public UnityEngine.UI.Image healthBarFill;
    
    [Tooltip("Health bar background (phase color indicator)")]
    public UnityEngine.UI.Image healthBarBackground;
    
    [Header("=== PHASE COLORS ===")]
    public Color phase1Color = Color.green;
    public Color phase2Color = Color.yellow;
    public Color phase3Color = Color.red;
    
    [Header("=== REFERENCES ===")]
    public GolemBossAI bossAI;
    public GolemBossAnimator bossAnimator;
    
    [Header("=== DEBUG ===")]
    public bool showDebugLogs = true;
    public bool showDebugGUI = true;
    
    // Internal
    private bool isDead = false;
    private float lastDamageTime = 0f;
    private float damageFlashDuration = 0.1f;
    private Renderer[] bossRenderers;
    private Color originalColor;
    
    private void Awake()
    {
        // Auto-find components
        if (bossAI == null) bossAI = GetComponent<GolemBossAI>();
        if (bossAnimator == null) bossAnimator = GetComponent<GolemBossAnimator>();
        
        // Get all renderers for damage flash
        bossRenderers = GetComponentsInChildren<Renderer>();
        if (bossRenderers.Length > 0)
        {
            originalColor = bossRenderers[0].material.color;
        }
    }
    
    private void Start()
    {
        currentHealth = maxHealth;
        
        // Sync with AI
        if (bossAI != null)
        {
            bossAI.currentHealth = currentHealth;
            bossAI.maxHealth = maxHealth;
        }
        
        // Start with invulnerability
        invulnerabilityTimer = invulnerabilityDuration;
        
        UpdateHealthBar();
        UpdatePhaseColor();
        
        if (showDebugLogs)
        {
            Debug.Log($"[GolemBossHealth] ❤️ Health initialized: {currentHealth}/{maxHealth}");
        }
    }
    
    private void Update()
    {
        // Countdown invulnerability
        if (invulnerabilityTimer > 0f)
        {
            invulnerabilityTimer -= Time.deltaTime;
            
            // Flash effect during invulnerability
            if (Time.frameCount % 10 == 0)
            {
                FlashColor(Color.yellow, 0.05f);
            }
        }
        
        // Sync health with AI
        if (bossAI != null && bossAI.currentHealth != currentHealth)
        {
            currentHealth = bossAI.currentHealth;
            UpdateHealthBar();
        }
    }
    
    #region DAMAGE SYSTEM
    
    /// <summary>
    /// Take damage from attacks
    /// </summary>
    public void TakeDamage(float rawDamage, bool isCritical = false, Vector3? hitPosition = null)
    {
        if (isDead) return;
        
        // Check invulnerability
        if (invulnerabilityTimer > 0f)
        {
            if (showDebugLogs)
            {
                Debug.Log("[GolemBossHealth] 🛡️ INVULNERABLE!");
            }
            return;
        }
        
        // Calculate actual damage with armor
        float actualDamage = CalculateDamageWithArmor(rawDamage, isCritical);
        
        // Apply damage
        currentHealth -= actualDamage;
        currentHealth = Mathf.Max(0f, currentHealth);
        
        // Sync with AI
        if (bossAI != null)
        {
            bossAI.currentHealth = currentHealth;
            bossAI.UpdatePhase();
        }
        
        // Visual feedback
        OnDamaged(actualDamage, isCritical, hitPosition);
        
        // Check death
        if (currentHealth <= 0f && !isDead)
        {
            Die();
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"[GolemBossHealth] 💔 Took {actualDamage:F1} damage ({rawDamage:F1} raw) | HP: {currentHealth:F0}/{maxHealth:F0}");
        }
    }
    
    /// <summary>
    /// Calculate damage with armor reduction
    /// </summary>
    private float CalculateDamageWithArmor(float rawDamage, bool isCritical)
    {
        float totalReduction = damageReduction;
        
        // Phase-based armor
        if (bossAI != null)
        {
            switch (bossAI.currentPhase)
            {
                case GolemBossAI.BossPhase.Phase1_Normal:
                    totalReduction += phase1ExtraArmor;
                    break;
                    
                case GolemBossAI.BossPhase.Phase3_Enraged:
                    totalReduction += phase3ExtraArmor;
                    break;
            }
        }
        
        // Critical hits ignore some armor
        if (isCritical)
        {
            totalReduction *= 0.5f;
        }
        
        // Apply reduction
        float actualDamage = rawDamage * (1f - totalReduction);
        
        return actualDamage;
    }
    
    /// <summary>
    /// Heal boss
    /// </summary>
    public void Heal(float amount)
    {
        if (isDead) return;
        
        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        
        // Sync with AI
        if (bossAI != null)
        {
            bossAI.currentHealth = currentHealth;
        }
        
        UpdateHealthBar();
        
        // Spawn heal effect
        if (hitEffectPrefab != null)
        {
            var effect = Instantiate(hitEffectPrefab, transform.position + Vector3.up * 2f, Quaternion.identity);
            Destroy(effect, 2f);
        }
        
        // Show heal number
        if (damageNumberPrefab != null)
        {
            var healNumber = damageNumberPrefab.Spawn(transform.position + Vector3.up * 3f, amount);
            healNumber.SetColor(Color.green);
            // Note: DamageNumbersPro doesn't have SetPrefix, use SetText or customize prefab instead
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"[GolemBossHealth] 💚 Healed {amount:F0} HP | HP: {currentHealth:F0}/{maxHealth:F0}");
        }
    }
    
    #endregion
    
    #region VISUAL FEEDBACK
    
    /// <summary>
    /// Visual feedback when damaged
    /// </summary>
    private void OnDamaged(float damage, bool isCritical, Vector3? hitPosition)
    {
        lastDamageTime = Time.time;
        
        // Flash red
        FlashColor(Color.red, damageFlashDuration);
        
        // Damage animation
        if (bossAnimator != null && Random.value > 0.7f) // 30% chance to play damage reaction
        {
            bossAnimator.PlayDamage();
        }
        
        // Spawn damage number
        if (damageNumberPrefab != null)
        {
            Vector3 spawnPos = hitPosition ?? (transform.position + Vector3.up * 3f);
            var damageNum = damageNumberPrefab.Spawn(spawnPos, damage);
            
            if (isCritical)
            {
                damageNum.SetColor(Color.yellow);
                damageNum.SetScale(1.5f);
                // Note: Use SetText if you want "CRITICAL!" prefix
                // damageNum.SetText("CRITICAL! " + damage.ToString("F0"));
            }
            else
            {
                damageNum.SetColor(Color.white);
            }
        }
        
        // Hit effect
        GameObject effectPrefab = isCritical ? criticalHitEffectPrefab : hitEffectPrefab;
        if (effectPrefab != null)
        {
            Vector3 effectPos = hitPosition ?? (transform.position + Vector3.up * 2f);
            var effect = Instantiate(effectPrefab, effectPos, Quaternion.identity);
            Destroy(effect, 2f);
        }
        
        // Update health bar
        UpdateHealthBar();
    }
    
    /// <summary>
    /// Flash boss color
    /// </summary>
    private void FlashColor(Color color, float duration)
    {
        if (bossRenderers.Length == 0) return;
        
        foreach (var renderer in bossRenderers)
        {
            if (renderer != null && renderer.material != null)
            {
                renderer.material.color = color;
            }
        }
        
        // Reset to original after duration
        Invoke(nameof(ResetColor), duration);
    }
    
    private void ResetColor()
    {
        foreach (var renderer in bossRenderers)
        {
            if (renderer != null && renderer.material != null)
            {
                renderer.material.color = originalColor;
            }
        }
    }
    
    /// <summary>
    /// Update health bar UI
    /// </summary>
    private void UpdateHealthBar()
    {
        if (healthBarFill != null)
        {
            float fillAmount = currentHealth / maxHealth;
            healthBarFill.fillAmount = fillAmount;
        }
    }
    
    /// <summary>
    /// Update health bar color based on phase
    /// </summary>
    private void UpdatePhaseColor()
    {
        if (healthBarBackground == null || bossAI == null) return;
        
        Color phaseColor = phase1Color;
        
        switch (bossAI.currentPhase)
        {
            case GolemBossAI.BossPhase.Phase2_Aggressive:
                phaseColor = phase2Color;
                break;
                
            case GolemBossAI.BossPhase.Phase3_Enraged:
                phaseColor = phase3Color;
                break;
        }
        
        healthBarBackground.color = phaseColor;
    }
    
    /// <summary>
    /// Show phase transition effect
    /// </summary>
    public void ShowPhaseTransitionEffect()
    {
        UpdatePhaseColor();
        
        if (phaseTransitionEffectPrefab != null)
        {
            var effect = Instantiate(phaseTransitionEffectPrefab, transform.position, Quaternion.identity);
            effect.transform.SetParent(transform);
            Destroy(effect, 3f);
        }
    }
    
    #endregion
    
    #region DEATH
    
    /// <summary>
    /// Boss death
    /// </summary>
    private void Die()
    {
        if (isDead) return;
        
        isDead = true;
        
        if (showDebugLogs)
        {
            Debug.Log("[GolemBossHealth] 💀 BOSS DEFEATED!");
        }
        
        // Death animation
        if (bossAnimator != null)
        {
            bossAnimator.PlayDeath();
        }
        
        // Death effect
        if (deathEffectPrefab != null)
        {
            var effect = Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 5f);
        }
        
        // Disable AI
        if (bossAI != null)
        {
            bossAI.enabled = false;
        }
        
        // Destroy after animation
        Destroy(gameObject, 5f);
    }
    
    #endregion
    
    #region DEBUG
    
    private void OnGUI()
    {
        if (!showDebugGUI) return;
        
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;
        
        // Boss health bar
        GUI.Box(new Rect(screenWidth / 2 - 200, 20, 400, 40), "");
        
        // Background
        GUI.color = Color.red;
        GUI.Box(new Rect(screenWidth / 2 - 195, 25, 390, 30), "");
        
        // Health fill
        float healthPercent = currentHealth / maxHealth;
        Color phaseColor = phase1Color;
        if (bossAI != null)
        {
            switch (bossAI.currentPhase)
            {
                case GolemBossAI.BossPhase.Phase2_Aggressive:
                    phaseColor = phase2Color;
                    break;
                case GolemBossAI.BossPhase.Phase3_Enraged:
                    phaseColor = phase3Color;
                    break;
            }
        }
        
        GUI.color = phaseColor;
        GUI.Box(new Rect(screenWidth / 2 - 195, 25, 390 * healthPercent, 30), "");
        
        // Text
        GUI.color = Color.white;
        string phaseText = bossAI != null ? bossAI.currentPhase.ToString() : "Unknown";
        GUI.Label(
            new Rect(screenWidth / 2 - 190, 25, 380, 30),
            $"GOLEM BOSS - {phaseText}\nHP: {currentHealth:F0} / {maxHealth:F0} ({healthPercent * 100:F1}%)"
        );
        
        // Invulnerability indicator
        if (invulnerabilityTimer > 0f)
        {
            GUI.color = Color.yellow;
            GUI.Label(
                new Rect(screenWidth / 2 - 100, 65, 200, 20),
                $"🛡️ INVULNERABLE: {invulnerabilityTimer:F1}s"
            );
        }
    }
    
    #endregion
}
