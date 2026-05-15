using UnityEngine;

/// <summary>
/// Gắn trên Dungeon Canvas (DamLay/Samac/Demon): sau khi chọn độ khó → hiện Panel_Pre-Start.
/// </summary>
public class DungeonGateUiFlow : MonoBehaviour
{
    [Header("Invite defaults (dùng khi bấm Invite)")]
    public string targetSceneName = "MapDamLay";
    public int mapType = 1;

    [Header("Debug")]
    public bool enableDebugLogs = true;

    GameObject _difficultyRoot;
    GameObject _preStartPanel;

    void Awake()
    {
        ResolveUiRefs();
    }

    void OnDisable()
    {
        ScenePortal.NotifyDungeonCanvasClosed(gameObject);
    }

    /// <summary>Gắn vào nút X / Cancel trên dungeon canvas để đóng UI và cho phép chạm cổng lại.</summary>
    public void CloseDungeonUi()
    {
        ResetToDifficultySelect();
        if (gameObject.activeSelf)
            gameObject.SetActive(false);
    }

    void ResolveUiRefs()
    {
        if (_difficultyRoot == null)
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "Button" && t.parent == transform)
                {
                    _difficultyRoot = t.gameObject;
                    break;
                }
            }
        }

        if (_preStartPanel == null)
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "Panel_Pre-Start")
                {
                    _preStartPanel = t.gameObject;
                    break;
                }
            }
        }
    }

    public void ShowPreStartPanel(DungeonDifficulty difficulty)
    {
        ResolveUiRefs();

        DungeonConfig.SelectedDifficulty = difficulty;
        DungeonConfig.SelectedMapType = mapType;
        DungeonPartyRuntime.SetPendingInvite(targetSceneName, difficulty, mapType);

        if (_difficultyRoot != null)
            _difficultyRoot.SetActive(false);

        if (_preStartPanel != null)
        {
            _preStartPanel.SetActive(true);
            Dbg($"ShowPreStartPanel | difficulty={difficulty} scene={targetSceneName} mapType={mapType}");
        }
        else
            Debug.LogWarning($"[DungeonGateUiFlow] Không tìm thấy Panel_Pre-Start dưới '{name}'.", this);
    }

    public void ResetToDifficultySelect()
    {
        ResolveUiRefs();
        if (_preStartPanel != null)
            _preStartPanel.SetActive(false);
        if (_difficultyRoot != null)
            _difficultyRoot.SetActive(true);
    }

    void Dbg(string msg)
    {
        if (!enableDebugLogs)
            return;
        Debug.Log($"[DungeonGateUiFlow] {msg}", this);
    }

    public static DungeonGateUiFlow FindForCanvas(GameObject dungeonCanvasRoot)
    {
        if (dungeonCanvasRoot == null)
            return null;
        var flow = dungeonCanvasRoot.GetComponent<DungeonGateUiFlow>();
        if (flow != null)
            return flow;
        return dungeonCanvasRoot.AddComponent<DungeonGateUiFlow>();
    }
}
