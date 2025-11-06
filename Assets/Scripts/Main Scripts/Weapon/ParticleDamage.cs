using UnityEngine;

public class ProjectileDamage : MonoBehaviour
{
    [SerializeField] float damage = 10f;
    [SerializeField] bool debugMode = false;

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.TryGetComponent(out TakeDamageTest enemy))
        {
            if (debugMode) Debug.Log($"[ProjectileDamage] Collision hit: {enemy.name} for {damage} damage");
            enemy.TakeDamage(damage);
        }
    }

    private void OnParticleCollision(GameObject other)
    {
        if (other.TryGetComponent(out TakeDamageTest enemy))
        {
            if (debugMode) Debug.Log($"[ProjectileDamage] Particle hit: {enemy.name} for {damage} damage");
            enemy.TakeDamage(damage);
        }
    }
}