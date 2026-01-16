Enemy setup guidelines (quick)

- Purpose
  - Standardize enemy prefabs: separate physics collider and trigger hurtbox, add centralized death controller, and support animation-driven damage windows.

- Recommended prefab structure
  - Root (has Animator, NavMeshAgent, Rigidbody, main Collider (non-trigger))
    - Hurtbox (child) -> Collider (isTrigger = true), layer `EnemyHurtbox`

- Steps to run Editor utility (batch)
  1. In Project window select enemy prefab assets you want to modify.
  2. Menu -> Tools -> Enemy Setup -> Add Hurtbox & Death Controller to Selected Prefabs
  3. If you want layer assignment, create a layer named `EnemyHurtbox` in Project Settings -> Tags & Layers before running.

- Animation Events (melee attacks)
  - For each attack clip, add an Animation Event at the hit frame:
    - Call `AttemptDealContactDamage` on the enemy (this applies contact damage instantly at that frame).
    - Optionally call `BeginContactDamage` a few frames before and `EndContactDamage` after to create a hit window.
  - Configure `damagePoint` on the `EnemyContactDamage` component to the Transform where the hit should originate (e.g., weapon tip).
  - Tune `damageRadius` so hits only connect when appropriate.

- Projectile/Skill setup
  - For projectile prefabs with `SkillDamageHelper`, set `requireHurtboxLayer = true` and set `enemyHurtboxLayer` to `EnemyHurtbox`. This ensures projectiles only hit designated hurtboxes.
  - Alternatively, projectiles will still damage enemies if they hit any collider that has a `TakeDamageTest` in parent.

- Death behaviour
  - Add `EnemyDeathController` to enemy prefab; on death `Die()` will set Animator Die param, disable NavMeshAgent, snap to ground and destroy object after delay.

If anything here should be changed to fit your prefab layout, tell me and I will adjust the editor tool to match.











