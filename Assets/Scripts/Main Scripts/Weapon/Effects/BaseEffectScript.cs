using UnityEngine;

public abstract class BaseEffectScript : MonoBehaviour
{
    [Header("Base Settings")]
    [SerializeField] protected float damage = 10f;
    [SerializeField] protected bool debugMode = false;

    protected virtual void OnParticleCollision(GameObject other)
    {
        if (other.TryGetComponent(out TakeDamageTest enemy))
        {
            if (debugMode) Debug.Log($"[{GetType().Name}] Particle hit: {enemy.name} for {damage} damage");

            // Apply damage first
            enemy.TakeDamage(damage);

            // Apply specific effect
            ApplyEffect(enemy);
        }
    }

    protected virtual void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.TryGetComponent(out TakeDamageTest enemy))
        {
            if (debugMode) Debug.Log($"[{GetType().Name}] Collision hit: {enemy.name} for {damage} damage");

            // Apply damage first
            enemy.TakeDamage(damage);

            // Apply specific effect
            ApplyEffect(enemy);
        }
    }

    protected abstract void ApplyEffect(TakeDamageTest enemy);
}
