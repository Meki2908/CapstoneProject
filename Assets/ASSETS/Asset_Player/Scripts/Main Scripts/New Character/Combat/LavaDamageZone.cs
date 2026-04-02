using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attach to a trigger collider on Lava objects.
/// - Player: takes percent max HP damage over time.
/// - Normal enemies: die instantly on contact.
/// - Boss enemies: immune.
/// </summary>
[RequireComponent(typeof(Collider))]
public class LavaDamageZone : MonoBehaviour
{
    [Header("Player Damage")]
    [SerializeField, Range(0f, 1f)] private float playerPercentPerSecond = 0.10f;
    [SerializeField, Min(0.05f)] private float playerTickInterval = 0.2f;

    // Prevent multiple colliders from applying damage multiple times in the same tick.
    private readonly Dictionary<int, float> nextPlayerTickTime = new Dictionary<int, float>();

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        HandleEnemyLavaContact(other);
    }

    private void OnTriggerStay(Collider other)
    {
        HandleEnemyLavaContact(other);
        HandlePlayerLavaDps(other);
    }

    private void OnTriggerExit(Collider other)
    {
        var playerHealth = other.GetComponentInParent<PlayerHealth>();
        if (playerHealth == null) return;

        int id = playerHealth.GetInstanceID();
        nextPlayerTickTime.Remove(id);
    }

    private void HandlePlayerLavaDps(Collider other)
    {
        var playerHealth = other.GetComponentInParent<PlayerHealth>();
        if (playerHealth == null || !playerHealth.IsAlive) return;

        int id = playerHealth.GetInstanceID();
        if (!nextPlayerTickTime.TryGetValue(id, out float nextTick))
            nextTick = 0f;

        if (Time.time < nextTick) return;

        float damage = playerHealth.MaxHealth * playerPercentPerSecond * playerTickInterval;
        playerHealth.TakeDamage(damage, playerHealth.transform.position, false);
        nextPlayerTickTime[id] = Time.time + playerTickInterval;
    }

    private void HandleEnemyLavaContact(Collider other)
    {
        var enemyHealth = other.GetComponentInParent<TakeDamageTest>();
        if (enemyHealth == null || !enemyHealth.IsAlive()) return;

        var enemyScript = other.GetComponentInParent<EnemyScript>();
        if (enemyScript != null && enemyScript.isBoss) return;

        enemyHealth.TakeDamage(enemyHealth.GetCurrentHealth() + 99999f);
    }
}
