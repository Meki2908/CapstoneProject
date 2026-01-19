using UnityEngine;

public class GolemDamageHandler : MonoBehaviour
{
    [Header("Golem Health Settings")]
    [SerializeField] private float maxHealth = 1000f;
    [SerializeField] private float currentHealth;

    [Header("References")]
    private TakeDamageTest takeDamageTest;

    void Awake()
    {
        // Initialize health first
        currentHealth = maxHealth;

        // Try to get TakeDamageTest component for compatibility
        takeDamageTest = GetComponent<TakeDamageTest>();
    }

    void Start()
    {
        // Sync TakeDamageTest with our health values (not the other way around)
        if (takeDamageTest != null)
        {
            takeDamageTest.MaxHealth = maxHealth;
            takeDamageTest.CurrentHealth = currentHealth;
        }
    } // Force recompile

    // Public properties that GolemAI expects
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public float HealthPercentage => maxHealth > 0 ? currentHealth / maxHealth : 0f;

    // Damage handling methods
    public void TakeDamage(float damage)
    {
        currentHealth = Mathf.Max(0f, currentHealth - damage);

        // Sync health to TakeDamageTest if it exists for compatibility
        if (takeDamageTest != null)
        {
            takeDamageTest.CurrentHealth = currentHealth;
        }
    }

    public void TakeDamage(float damage, WeaponType weaponType, bool isCrit)
    {
        currentHealth = Mathf.Max(0f, currentHealth - damage);

        // Sync health to TakeDamageTest if it exists for compatibility
        if (takeDamageTest != null)
        {
            takeDamageTest.CurrentHealth = currentHealth;
        }
    }
}
