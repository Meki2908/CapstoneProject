using UnityEngine;
using System.Collections;

/// <summary>
/// Marker chỉ đường hiển thị trên đầu NPC/cổng mục tiêu hiện tại.
/// Script này tự động bật/tắt dựa trên trạng thái quest.
/// 
/// Cách setup:
///   - Tạo 1 child GameObject "QuestMarker" dưới NPC Leona,
///     gắn script này và gán prefab/sprite mũi tên.
///   - Làm tương tự trên cổng dungeon đầu tiên.
/// </summary>
public class QuestMarker : MonoBehaviour
{
    [Header("Quest & Bước sẽ hiện marker này")]
    public int questID = 1;

    [Tooltip("Marker này hiện khi quest đang ở bước này (0-based).")]
    public int showAtStep = 0;      // 0 = Bước 'Tìm Leona', 1 = Bước 'Đến cổng'

    [Header("Marker visuals (bật/tắt theo quest state)")]
    public GameObject markerObject;  // Gán icon hoặc mũi tên (có thể là sprite, particle...)

    [Header("Bobbing animation (tuỳ chọn)")]
    public bool enableBobbing = false;
    public float bobbingHeight = 0.3f;
    public float bobbingSpeed  = 2f;

    Vector3 _startPos;

    IEnumerator Start()
    {
        _startPos = transform.localPosition;
        yield return null;  // Wait 1 frame: ensure QuestManager.Awake() has run
        Refresh(-1);
    }

    void OnEnable()
    {
        QuestManager.OnQuestAccepted     += Refresh;
        QuestManager.OnQuestStepAdvanced += Refresh;
        QuestManager.OnQuestCompleted    += Refresh;
    }

    void OnDisable()
    {
        QuestManager.OnQuestAccepted     -= Refresh;
        QuestManager.OnQuestStepAdvanced -= Refresh;
        QuestManager.OnQuestCompleted    -= Refresh;
    }

    void Update()
    {
        if (!enableBobbing || markerObject == null || !markerObject.activeSelf) return;
        float y = _startPos.y + Mathf.Sin(Time.time * bobbingSpeed) * bobbingHeight;
        transform.localPosition = new Vector3(_startPos.x, y, _startPos.z);
    }

    // ─── Logic ────────────────────────────────────────────────────────────

    void Refresh(int _)
    {
        if (QuestManager.Instance == null) { SetVisible(false); return; }

        var state = QuestManager.Instance.GetState(questID);
        int step  = QuestManager.Instance.GetStepIndex(questID);

        bool shouldShow = state == QuestManager.QuestState.Active && step == showAtStep;
        SetVisible(shouldShow);
    }

    void SetVisible(bool visible)
    {
        if (markerObject == null) return;

        // Nếu markerObject là chính GameObject này → KHÔNG dùng SetActive
        // (vì sẽ disable script và mất event subscription!)
        // Thay vào đó toggle Renderer, Light, ParticleSystem
        if (markerObject == gameObject)
        {
            foreach (var r  in GetComponentsInChildren<Renderer>(true))       r.enabled = visible;
            foreach (var l  in GetComponentsInChildren<Light>(true))          l.enabled = visible;
            foreach (var ps in GetComponentsInChildren<ParticleSystem>(true))
            {
                if (visible) ps.Play();
                else         { ps.Stop(); ps.Clear(); }
            }
        }
        else
        {
            // markerObject là child riêng → SetActive bình thường
            markerObject.SetActive(visible);
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.4f);
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.6f,
            $"QuestMarker\nQuestID={questID} Step={showAtStep}");
    }
#endif
}
