// Enemy Scaling Guide for 10+ Enemies
// This file contains best practices and configuration recommendations

/*
=== ENEMY SCALING GUIDE ===

1. BASEENEMYAI OPTIMIZATIONS:
   - ANIMATOR_UPDATE_INTERVAL: 0.2f (from default 0.1f)
   - STATE_CHANGE_COOLDOWN: 1.0f (from default 0.5f)
   - Distance-based update throttling: implemented
   - Debug logging: disabled in production

2. ENEMYCONTACTDAMAGE OPTIMIZATIONS:
   - rangeCheckInterval: 0.1f (check every 0.1s instead of every frame)
   - Use useDamageRange = true for better performance than raycast
   - Consider disabling range damage on distant enemies

3. TAKEDAMAGETEST OPTIMIZATIONS:
   - enableRaycastDamage: false (disabled to prevent conflicts)
   - Detection range: reasonable values (not too large)

4. GENERAL OPTIMIZATIONS:
   - Use object pooling for enemy spawning/despawning
   - Implement frustum culling for off-screen enemies
   - Consider Level of Detail (LOD) system
   - Disable unnecessary colliders/rigidbodies

5. PERFORMANCE TARGETS:
   - 1-5 enemies: 60+ FPS (optimal)
   - 6-10 enemies: 50+ FPS (good)
   - 11-15 enemies: 40+ FPS (acceptable)
   - 15+ enemies: Consider optimizations or reduce count

6. MONITORING:
   - Use EnemyPerformanceMonitor to track FPS and enemy count
   - Monitor physics raycast count in Profiler
   - Watch for excessive state changes

7. SPAWNING STRATEGIES:
   - Spawn enemies in waves, not all at once
   - Use distance-based spawning (closer to player first)
   - Implement enemy caps based on performance
   - Consider procedural enemy placement

8. MEMORY OPTIMIZATIONS:
   - Reuse enemy prefabs
   - Pool damage numbers and effects
   - Avoid excessive string operations in Update()
   - Use object pooling for projectiles/effects

9. PHYSICS OPTIMIZATIONS:
   - Reduce raycast frequency
   - Use simpler colliders where possible
   - Disable physics on distant enemies
   - Consider physics layer optimizations

10. DEBUGGING PERFORMANCE:
    - Use Unity Profiler to identify bottlenecks
    - Monitor GC spikes (garbage collection)
    - Check for excessive Update() calls
    - Profile AI decision making time

=== IMPLEMENTATION CHECKLIST ===

□ BaseEnemyAI.ANIMATOR_UPDATE_INTERVAL = 0.2f
□ BaseEnemyAI.STATE_CHANGE_COOLDOWN = 1.0f
□ EnemyContactDamage.rangeCheckInterval = 0.1f
□ TakeDamageTest.enableRaycastDamage = false
□ EnemyPerformanceMonitor attached to scene
□ Debug logging disabled in production
□ Object pooling implemented for enemies
□ Distance culling for far enemies
□ Physics optimizations applied
□ Performance tested with target enemy count
*/

