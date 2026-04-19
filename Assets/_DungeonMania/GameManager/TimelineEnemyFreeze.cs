using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Gắn script này vào một GameObject rỗng hoặc Activation Track trong Timeline.
/// Khi được bt (OnEnable), nó sẽ tự động tìm và đóng băng mọi hoạt động của tất cả quái vật.
/// Khi bị tắt (OnDisable), nó sẽ nhả quái vật ra để tiếp tục đánh.
/// Thích hợp để "đóng băng diễn viên quần chúng" khi Boss đang diễn Intro Cutscene.
/// </summary>
public class TimelineEnemyFreeze : MonoBehaviour
{
    private class EnemyFreezeState
    {
        public EnemyScript enemyScript;
        public EnemyState enemyState;
        public NavMeshAgent navAgent;
        public Animator animator;
        public Rigidbody rb;

        public bool wasEnemyStateEnabled;
        public bool wasNavAgentStopped;
        public float originalAnimSpeed;
        public bool wasKinematic;
    }

    [Tooltip("Nếu true, đóng băng toàn bộ cả những con quái được đánh dấu là isBoss. Thường để false để Boss chính còn diễn Cutscene.")]
    [SerializeField] private bool freezeBosses = false;

    private List<EnemyFreezeState> frozenEnemies = new List<EnemyFreezeState>();

    private void OnEnable()
    {
        frozenEnemies.Clear();

        // Tìm tất cả EnemyScript đang tồn tại trong Scene
        EnemyScript[] allEnemies = FindObjectsByType<EnemyScript>(FindObjectsSortMode.None);

        foreach (var enemy in allEnemies)
        {
            if (enemy == null || !enemy.gameObject.activeInHierarchy) continue;
            
            // Không đóng băng chính Boss nếu freezeBosses = false
            if (enemy.isBoss && !freezeBosses) 
            {
                continue;
            }

            EnemyFreezeState state = new EnemyFreezeState();
            state.enemyScript = enemy;
            state.enemyState = enemy.GetComponent<EnemyState>();
            
            // Dùng các component đã cache sẵn trong EnemyScript nếu có, nếu không thì GetComponent
            state.navAgent = enemy.navMeshAgent != null ? enemy.navMeshAgent : enemy.GetComponent<NavMeshAgent>();
            state.animator = enemy.animator != null ? enemy.animator : enemy.GetComponentInChildren<Animator>();
            state.rb = enemy.GetComponent<Rigidbody>();

            // Lưu trạng thái và khóa lại
            if (state.enemyState != null)
            {
                state.wasEnemyStateEnabled = state.enemyState.enabled;
                state.enemyState.enabled = false;
            }

            if (state.navAgent != null && state.navAgent.isOnNavMesh)
            {
                state.wasNavAgentStopped = state.navAgent.isStopped;
                state.navAgent.isStopped = true;
            }

            if (state.animator != null)
            {
                state.originalAnimSpeed = state.animator.speed;
                state.animator.speed = 0f;
            }

            if (state.rb != null)
            {
                state.wasKinematic = state.rb.isKinematic;
                state.rb.isKinematic = true;
                
                // Mới thêm: Reset velocity nếu không phải kinematic
                if (!state.wasKinematic)
                {
                    state.rb.linearVelocity = Vector3.zero;
                    state.rb.angularVelocity = Vector3.zero;
                }
            }

            frozenEnemies.Add(state);
        }
    }

    private void OnDisable()
    {
        foreach (var state in frozenEnemies)
        {
            if (state.enemyScript == null || !state.enemyScript.gameObject.activeInHierarchy) continue;

            // Khôi phục trạng thái
            if (state.enemyState != null)
                state.enemyState.enabled = state.wasEnemyStateEnabled;

            if (state.navAgent != null && state.navAgent.isOnNavMesh)
                state.navAgent.isStopped = state.wasNavAgentStopped;

            if (state.animator != null)
                state.animator.speed = state.originalAnimSpeed;

            if (state.rb != null)
                state.rb.isKinematic = state.wasKinematic;
        }

        frozenEnemies.Clear();
    }
}
