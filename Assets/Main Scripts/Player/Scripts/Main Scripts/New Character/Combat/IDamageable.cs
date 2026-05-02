using UnityEngine;

public interface IDamageable
{
    void TakeDamage(int damage, Vector3 hitPosition);
    void TakeDamage(int damage);
    bool IsAlive();
    Transform GetTransform();
}
