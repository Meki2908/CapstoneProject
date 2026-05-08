using UnityEngine;

public static class CombatAimHelper
{
    // Static NonAlloc buffer (Unity main thread only).
    private static readonly Collider[] AutoAimColliders = new Collider[64];

    public static void SnapToTarget(Character character, EquipmentSystem equipment, EnemyDetection enemyDetection, Vector2 rawMoveInput)
    {
        if (character == null) return;

        // Fusion authority guard: only rotate on the owning side.
        if (!character.HasStateAuthority && !character.HasInputAuthority) return;

        float searchRange = 5f;
        LayerMask mask;

        if (enemyDetection != null)
        {
            mask = enemyDetection.EnemyLayerMask;
            var weapon = equipment != null ? equipment.GetCurrentWeapon() : null;

            // Mage: keep identical range to combat attack range; melee skills: allow a bit farther.
            if (weapon != null && weapon.weaponType == WeaponType.Mage)
                searchRange = enemyDetection.MageAttackRange;
            else
                searchRange = enemyDetection.GetCurrentWeaponAttackRangePublic() * 1.5f;
        }
        else
        {
            mask = LayerMask.GetMask("Enemy");
        }

        Vector3 charPos = character.transform.position;
        Vector3 charForward = character.transform.forward;
        charForward.y = 0f;
        if (charForward.sqrMagnitude > 1e-6f) charForward.Normalize();
        else charForward = Vector3.forward;

        int count = Physics.OverlapSphereNonAlloc(charPos, searchRange, AutoAimColliders, mask);

        Transform nearest = null;
        float minSqrDist = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            Collider col = AutoAimColliders[i];
            if (col == null) continue;

            TakeDamageTest hp = col.GetComponentInParent<TakeDamageTest>();
            if (hp != null && !hp.IsAlive()) continue;

            Transform targetRoot = hp != null ? hp.transform : col.transform.root;
            if (targetRoot == null || !targetRoot.gameObject.activeInHierarchy) continue;

            Vector3 dirToTarget = targetRoot.position - charPos;
            dirToTarget.y = 0f;
            if (dirToTarget.sqrMagnitude < 0.001f) continue;

            // Cone check: ignore targets behind the player.
            float dot = Vector3.Dot(charForward, dirToTarget.normalized);
            // if (dot < 0f) continue;

            float sqrDist = dirToTarget.sqrMagnitude;
            if (sqrDist < minSqrDist)
            {
                minSqrDist = sqrDist;
                nearest = targetRoot;
            }
        }

        // Priority 1: snap to nearest enemy in front.
        if (nearest != null)
        {
            Vector3 dir = nearest.position - charPos;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
            {
                character.transform.rotation = Quaternion.LookRotation(dir.normalized);
                return;
            }
        }

        // Priority 2: fallback to raw move input direction (camera-relative).
        if (rawMoveInput.sqrMagnitude > 0.01f)
        {
            Transform camTransform = character.cameraTransform != null
                ? character.cameraTransform
                : (Camera.main ? Camera.main.transform : null);

            if (camTransform != null)
            {
                Vector3 camForward = camTransform.forward;
                camForward.y = 0f;
                camForward.Normalize();

                Vector3 camRight = camTransform.right;
                camRight.y = 0f;
                camRight.Normalize();

                Vector3 moveDir = (camForward * rawMoveInput.y + camRight * rawMoveInput.x).normalized;
                if (moveDir.sqrMagnitude > 0.001f)
                {
                    character.transform.rotation = Quaternion.LookRotation(moveDir);
                }
            }
        }
    }
}
