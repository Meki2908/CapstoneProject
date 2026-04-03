#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// Quest Debug Window — mở bằng menu: Quest → Quest Debug Window
/// Cho phép xem và đặt trạng thái quest/step ngay khi đang Play Mode.
/// </summary>
public class QuestDebugWindow : EditorWindow
{
    // ─── Menu ─────────────────────────────────────────────────────────────
    [MenuItem("Quest/Quest Debug Window")]
    public static void OpenWindow()
    {
        var w = GetWindow<QuestDebugWindow>("Quest Debug");
        w.minSize = new Vector2(340, 420);
        w.Show();
    }

    // ─── State ────────────────────────────────────────────────────────────
    Vector2 _scroll;

    static readonly string[] StateLabels = { "Locked", "Available", "Active", "Completed" };
    static readonly Color[]  StateColors =
    {
        new Color(0.55f, 0.55f, 0.55f), // Locked    – grey
        new Color(0.95f, 0.95f, 0.60f), // Available – yellow
        new Color(0.40f, 0.90f, 0.55f), // Active    – green
        new Color(0.45f, 0.75f, 1.00f), // Completed – blue
    };

    // ─── GUI ──────────────────────────────────────────────────────────────
    void OnGUI()
    {
        // Tiêu đề
        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize  = 14,
            alignment = TextAnchor.MiddleCenter
        };
        GUILayout.Space(6);
        GUILayout.Label("⚔  Quest Debug Panel", titleStyle);
        GUILayout.Space(4);

        // Cảnh báo nếu không Play
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Chỉ hoạt động khi đang Play Mode.", MessageType.Warning);
        }

        // QuestManager chưa có
        var mgr = QuestManager.Instance;
        if (mgr == null)
        {
            EditorGUILayout.HelpBox("QuestManager.Instance = null.\nHãy chắc chắn có QuestManager trong scene.", MessageType.Error);
            DrawResetAllButton();
            return;
        }

        GUILayout.Space(4);

        // Nút reset nhanh
        DrawResetAllButton();

        GUILayout.Space(6);
        EditorGUILayout.LabelField("─── Trạng thái từng Quest ───", EditorStyles.boldLabel);
        GUILayout.Space(4);

        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        if (mgr.quests == null || mgr.quests.Length == 0)
        {
            EditorGUILayout.HelpBox("Mảng 'Quests' trong QuestManager đang rỗng!", MessageType.Warning);
        }
        else
        {
            foreach (var qd in mgr.quests)
            {
                if (qd == null) continue;
                DrawQuestCard(mgr, qd);
                GUILayout.Space(6);
            }
        }

        EditorGUILayout.EndScrollView();

        // Tự refresh UI mỗi frame khi đang play
        if (Application.isPlaying) Repaint();
    }

    // ─── Quest Card ───────────────────────────────────────────────────────
    void DrawQuestCard(QuestManager mgr, QuestData qd)
    {
        var state    = mgr.GetState(qd.questID);
        int stepIdx  = mgr.GetStepIndex(qd.questID);
        int stateInt = (int)state;

        Color cardColor = StateColors[Mathf.Clamp(stateInt, 0, StateColors.Length - 1)];

        // Card background
        var rect = EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUI.color = new Color(cardColor.r, cardColor.g, cardColor.b, 0.15f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Header: ID + Title + State badge
        EditorGUILayout.BeginHorizontal();
        GUIStyle idStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 };
        GUILayout.Label($"Quest {qd.questID}  —  {qd.questTitle}", idStyle, GUILayout.ExpandWidth(true));

        GUI.backgroundColor = cardColor;
        GUILayout.Label(StateLabels[stateInt], new GUIStyle(EditorStyles.miniButton)
        {
            fontStyle = FontStyle.Bold,
            normal    = { textColor = Color.black }
        }, GUILayout.Width(90));
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(4);

        if (!Application.isPlaying)
        {
            EditorGUILayout.LabelField("(Chỉ có thể thay đổi khi Play Mode)", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            return;
        }

        // ── Chọn State ────────────────────────────────────────────────────
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Set State:", GUILayout.Width(62));
        for (int i = 0; i < StateLabels.Length; i++)
        {
            bool isCurrent = stateInt == i;
            GUI.backgroundColor = isCurrent ? cardColor : Color.white;
            if (GUILayout.Button(StateLabels[i], EditorStyles.miniButtonMid, GUILayout.Height(20)))
            {
                if (!isCurrent) SetQuestState(mgr, qd.questID, (QuestManager.QuestState)i);
            }
            GUI.backgroundColor = Color.white;
        }
        EditorGUILayout.EndHorizontal();

        // ── Chọn Step (chỉ khi Active) ────────────────────────────────────
        if (state == QuestManager.QuestState.Active && qd.steps != null && qd.steps.Length > 0)
        {
            GUILayout.Space(2);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Set Step:", GUILayout.Width(62));
            for (int s = 0; s < qd.steps.Length; s++)
            {
                bool isCurrent = stepIdx == s;
                GUI.backgroundColor = isCurrent ? new Color(1f, 0.85f, 0.3f) : Color.white;
                string label = $"{s}: {qd.steps[s].stepTitle}";
                if (GUILayout.Button(label, EditorStyles.miniButtonMid, GUILayout.Height(20)))
                {
                    if (!isCurrent) SetQuestStep(mgr, qd.questID, s);
                }
                GUI.backgroundColor = Color.white;
            }
            EditorGUILayout.EndHorizontal();

            // Hiện instruction của step hiện tại
            GUILayout.Space(2);
            int clampedStep = Mathf.Clamp(stepIdx, 0, qd.steps.Length - 1);
            EditorGUILayout.LabelField(
                $"► {qd.steps[clampedStep].instruction}",
                EditorStyles.wordWrappedMiniLabel
            );
        }

        EditorGUILayout.EndVertical();
    }

    // ─── Nút Reset All ────────────────────────────────────────────────────
    void DrawResetAllButton()
    {
        GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
        if (GUILayout.Button("🔄  Reset All Quests (New Game)", GUILayout.Height(28)))
        {
            if (Application.isPlaying && QuestManager.Instance != null)
            {
                if (EditorUtility.DisplayDialog(
                    "Reset Quest",
                    "Reset toàn bộ quest về trạng thái New Game?\nHành động này không thể hoàn tác.",
                    "Reset", "Huỷ"))
                {
                    QuestManager.Instance.ResetAllQuests();
                    Debug.Log("[QuestDebug] All quests reset.");
                }
            }
            else if (!Application.isPlaying)
            {
                // Outside play mode: chỉ xóa PlayerPrefs
                if (EditorUtility.DisplayDialog(
                    "Reset Quest (PlayerPrefs)",
                    "Xóa tất cả PlayerPrefs liên quan đến quest?\n(Không ảnh hưởng khi chưa Play)",
                    "Xóa", "Huỷ"))
                {
                    PlayerPrefs.DeleteKey("QUEST_COUNT");
                    for (int i = 1; i <= 10; i++)
                    {
                        PlayerPrefs.DeleteKey($"QUEST_{i}");
                        PlayerPrefs.DeleteKey($"QUEST_{i}_STEP");
                    }
                    PlayerPrefs.Save();
                    Debug.Log("[QuestDebug] PlayerPrefs quest keys cleared.");
                }
            }
        }
        GUI.backgroundColor = Color.white;
    }

    // ─── Set State Logic ──────────────────────────────────────────────────
    static void SetQuestState(QuestManager mgr, int questID, QuestManager.QuestState newState)
    {
        switch (newState)
        {
            case QuestManager.QuestState.Active:
                mgr.AcceptQuest(questID);
                break;

            case QuestManager.QuestState.Completed:
                // Ensure quest is Active before completing
                if (mgr.GetState(questID) != QuestManager.QuestState.Active)
                    mgr.AcceptQuest(questID);
                mgr.CompleteQuest(questID);
                break;

            default:
                // Locked / Available: ghi trực tiếp vào PlayerPrefs rồi refresh
                int val = (newState == QuestManager.QuestState.Available) ? 0 : 0;
                PlayerPrefs.SetInt($"QUEST_{questID}", val);
                // For Available: ensure QUEST_COUNT is sufficient
                if (newState == QuestManager.QuestState.Available)
                    PlayerPrefs.SetInt("QUEST_COUNT", questID - 1);
                else
                    PlayerPrefs.SetInt("QUEST_COUNT", 0);
                PlayerPrefs.Save();
                mgr.RefreshFromPrefs();
                break;
        }
        Debug.Log($"[QuestDebug] Quest {questID} → {newState}");
    }

    static void SetQuestStep(QuestManager mgr, int questID, int targetStep)
    {
        int current = mgr.GetStepIndex(questID);

        if (targetStep > current)
        {
            // Advance forward
            int diff = targetStep - current;
            for (int i = 0; i < diff; i++)
            {
                if (mgr.GetState(questID) == QuestManager.QuestState.Active)
                {
                    var qd = mgr.GetQuestData(questID);
                    // Dùng PlayerPrefs trực tiếp để skip logic CompleteQuest
                    int nextStep = mgr.GetStepIndex(questID) + 1;
                    if (qd != null && nextStep < qd.steps.Length)
                    {
                        PlayerPrefs.SetInt($"QUEST_{questID}_STEP", nextStep);
                        PlayerPrefs.Save();
                        mgr.RefreshFromPrefs();
                    }
                }
            }
        }
        else if (targetStep < current)
        {
            // Go backward: ghi trực tiếp PlayerPrefs
            PlayerPrefs.SetInt($"QUEST_{questID}_STEP", targetStep);
            PlayerPrefs.Save();
            mgr.RefreshFromPrefs();
        }

        Debug.Log($"[QuestDebug] Quest {questID} step → {targetStep}");
    }
}
#endif
