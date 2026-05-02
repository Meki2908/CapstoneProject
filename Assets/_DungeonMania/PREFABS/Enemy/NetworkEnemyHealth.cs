using Fusion;
using UnityEngine;

/// <summary>
/// Quản lý máu của Enemy thông qua mạng Fusion 2.
/// Kế thừa NetworkBehaviour và biến máu thành biến [Networked].
/// </summary>
public class NetworkEnemyHealth : NetworkBehaviour
{
    [Networked, OnChangedRender(nameof(OnHealthChanged))]
    public float NetworkedHP { get; set; }

    private TakeDamageTest _localDamageTest;
    private EnemyScript _localEnemyScript;

    void Awake()
    {
        _localDamageTest = GetComponent<TakeDamageTest>();
        _localEnemyScript = GetComponent<EnemyScript>();
    }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            // Host thiết lập máu ban đầu
            if (_localDamageTest != null)
            {
                NetworkedHP = _localDamageTest.MaxHealth;
                _localDamageTest.CurrentHealth = NetworkedHP;
            }
        }
        else
        {
            // Client đồng bộ theo Host
            if (_localDamageTest != null)
            {
                _localDamageTest.CurrentHealth = NetworkedHP;
            }

            // Tắt AI và di chuyển máy Client để cho NetworkTransform tự kéo nó đi theo Server
            var agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null) agent.enabled = false;

            if (_localEnemyScript != null) _localEnemyScript.enabled = false;
            
            var baseAI = GetComponent<BaseEnemyAI>();
            if (baseAI != null) baseAI.enabled = false;

            var attackAI = GetComponent<EnemyAttack>();
            if (attackAI != null) attackAI.enabled = false;
        }
    }

    /// <summary>
    /// Bất cứ khi nào Máy Local đánh trúng, ta gọi API này thay vì trừ thẳng.
    /// RPC này chỉ Host mới có quyền xử lý.
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_TakeDamage(float damage, int originPlayerId, Vector3 hitPoint)
    {
        if (!Object.IsValid || !HasStateAuthority) return;

        // Chỉ Host trừ máu
        NetworkedHP -= damage;

        // Xử lý cạn máu
        if (NetworkedHP <= 0f)
        {
            NetworkedHP = 0f;
            // Gọi hàm Die cục bộ trên Host (Host sẽ phát hoạt ảnh chết, rớt đồ...)
        }
    }

    /// <summary>
    /// Biến máu thay đổi từ Host -> Đồng bộ xuống tất cả Client
    /// </summary>
    void OnHealthChanged()
    {
        if (_localDamageTest != null)
        {
            // Check nếu máu tụt -> chạy hoạt ảnh dính đạn
            float oldHealth = _localDamageTest.CurrentHealth;
            _localDamageTest.CurrentHealth = NetworkedHP;

            if (NetworkedHP < oldHealth && NetworkedHP > 0)
            {
                // Play Hit Animation local
                if (_localEnemyScript != null && _localEnemyScript.animator != null)
                {
                    _localEnemyScript.animator.SetBool("hit", true);
                    _localEnemyScript.animator.SetBool("attack", false);
                }
            }
            else if (NetworkedHP <= 0 && oldHealth > 0)
            {
                // Gọi chết local
                var enemyDamage = GetComponent<EnemyDamage>();
                if (enemyDamage != null)
                {
                    enemyDamage.StartCoroutine(enemyDamage.Death());
                }
            }
        }
    }
}
