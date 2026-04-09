using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
public class DungeonInviteSetupEditor
{
    [MenuItem("GameObject/Dungeon Mania/Setup Dungeon Invite UI", false, 10)]
    [MenuItem("Tools/Dungeon Mania/Setup Dungeon Invite UI")]
    public static void SetupDungeonInviteUI(MenuCommand menuCommand)
    {
        GameObject[] selectedObjects = Selection.gameObjects;
        if (selectedObjects.Length == 0)
        {
            EditorUtility.DisplayDialog("Lỗi", "Vui lòng chọn (bôi đen) các Canvas Hầm Ngục (ví dụ: Dungeon Canvas Samac, DamLay, Demon) trong Hierarchy trước khi chạy Tool!", "OK");
            return;
        }

        int processedCount = 0;
        foreach (GameObject obj in selectedObjects)
        {
            DungeonPortalLobbyCoordinator coordinator = obj.GetComponent<DungeonPortalLobbyCoordinator>();
            if (coordinator == null) continue;

            Transform rootCanvas = coordinator.transform;
            Undo.RecordObject(coordinator, "Setup Dungeon Invite UI");

        // 1. Create [Number of players] Panel
        GameObject numPlayersPanel = FindOrCreatePanel(rootCanvas, "Number of players", new Color(0, 0, 0, 0.8f));
        numPlayersPanel.SetActive(false);
        AddText(numPlayersPanel.transform, "Title", "CHỌN CHẾ ĐỘ (Số Người)", 36, new Vector2(0, 150));
        
        CreateButton(numPlayersPanel.transform, "1 player", "1 Người (Solo)", new Vector2(0, 50));
        CreateButton(numPlayersPanel.transform, "2 player", "2 Người", new Vector2(0, -20));
        CreateButton(numPlayersPanel.transform, "3 player", "3 Người", new Vector2(0, -90));
        CreateButton(numPlayersPanel.transform, "4 player", "4 Người", new Vector2(0, -160));
        
        coordinator.numberOfPlayersPanel = numPlayersPanel;

        // 2. Create [Preparation panel]
        GameObject prepPanel = FindOrCreatePanel(rootCanvas, "Preparation panel", new Color(0, 0, 0, 0.9f));
        prepPanel.SetActive(false);
        AddText(prepPanel.transform, "Title", "ĐANG CHỜ TỔ ĐỘI...", 40, new Vector2(0, 100));
        TMP_Text waitText = AddText(rootCanvas, "Joined/Expected", "Trạng thái: 1/1 Người", 30, new Vector2(0, -150));
        TMP_Text countText = AddText(rootCanvas, "Countdown", "Đang chờ Host bắt đầu...", 45, new Vector2(0, -200));
        countText.color = Color.yellow;

        // Add a "Start Now" button for Host to force start
        GameObject startNowBtn = CreateButton(prepPanel.transform, "StartNowButton", "Bắt Đầu Ngay!", new Vector2(0, -200));
        
        coordinator.preparationPanel = prepPanel;
        coordinator.joinedExpectedText = waitText;
        coordinator.countdownText = countText;

            // 3. Create [Invite Notification Panel] (Top Anchored)
            GameObject invitePanel = FindOrCreatePanel(rootCanvas, "InviteNotificationPanel", new Color(0, 0, 0, 0.9f));
            invitePanel.SetActive(false);
            
            RectTransform inviteRect = invitePanel.GetComponent<RectTransform>();
            inviteRect.anchorMin = new Vector2(0.3f, 0.8f);
            inviteRect.anchorMax = new Vector2(0.7f, 1.0f);
            inviteRect.sizeDelta = Vector2.zero;
            inviteRect.anchoredPosition = Vector2.zero;

            TMP_Text inviteMessage = AddText(invitePanel.transform, "MessageText", "Host mời bạn đánh Dungeon. Tham gia?", 30, new Vector2(0, 20));
            
            GameObject btnYes = CreateButton(invitePanel.transform, "BtnYes", "CÓ (YES)", new Vector2(-120, -40));
            btnYes.GetComponent<Image>().color = new Color(0.2f, 0.8f, 0.2f);
            
            GameObject btnNo = CreateButton(invitePanel.transform, "BtnNo", "KHÔNG (NO)", new Vector2(120, -40));
            btnNo.GetComponent<Image>().color = new Color(0.8f, 0.2f, 0.2f);

            // 4. Create [Party Ready Room] Panel
            GameObject readyRoomPanel = FindOrCreatePanel(rootCanvas, "PartyReadyRoomPanel", new Color(0.1f, 0.1f, 0.1f, 0.95f));
            readyRoomPanel.SetActive(false);
            AddText(readyRoomPanel.transform, "Title", "PHÒNG CHỜ HẦM NGỤC", 45, new Vector2(0, 200));

            TMP_Text memberListText = AddText(readyRoomPanel.transform, "MemberListText", "Người chơi 1 - Sẵn sàng\nNgười chơi 2 - Đang chờ", 28, new Vector2(0, 0));
            memberListText.alignment = TextAlignmentOptions.TopLeft;
            memberListText.rectTransform.sizeDelta = new Vector2(800, 300);

            GameObject btnReady = CreateButton(readyRoomPanel.transform, "BtnReady", "SẴN SÀNG", new Vector2(-150, -250));
            btnReady.GetComponent<Image>().color = new Color(0.2f, 0.6f, 1f);

            GameObject btnExit = CreateButton(readyRoomPanel.transform, "BtnExit", "THOÁT", new Vector2(150, -250));
            btnExit.GetComponent<Image>().color = new Color(0.8f, 0.2f, 0.2f);

            // Wire up variables
            coordinator.inviteNotificationPanel = invitePanel;
            coordinator.inviteMessageText = inviteMessage;
            coordinator.inviteYesButton = btnYes.GetComponent<Button>();
            coordinator.inviteNoButton = btnNo.GetComponent<Button>();
            coordinator.startNowButton = startNowBtn.GetComponent<Button>();

            coordinator.partyReadyRoomPanel = readyRoomPanel;
            coordinator.partyMemberListText = memberListText;
            coordinator.partyReadyButton = btnReady.GetComponent<Button>();
            coordinator.partyExitButton = btnExit.GetComponent<Button>();

            EditorUtility.SetDirty(coordinator);
            processedCount++;
        }

        if (processedCount > 0)
        {
            EditorUtility.DisplayDialog("Thành công", $"Đã khởi tạo xong Canvas Tổ Đội cho {processedCount} Cổng Hầm Ngục được chọn!", "Tuyệt vời");
        }
        else
        {
            EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy Script DungeonPortalLobbyCoordinator trên các Object bạn vừa chọn. Hãy đảm bảo bạn chọn đúng Canvas chứa script mở cổng nhé!", "Đã hiểu");
        }
    }

    private static GameObject FindOrCreatePanel(Transform parent, string name, Color bgColor)
    {
        Transform child = parent.Find(name);
        if (child != null) return child.gameObject;

        GameObject panel = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(panel, "Create Panel");
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;

        Image img = panel.AddComponent<Image>();
        img.color = bgColor;

        return panel;
    }

    private static GameObject CreateButton(Transform parent, string name, string label, Vector2 position)
    {
        Transform existing = parent.Find(name);
        if (existing != null) return existing.gameObject;

        GameObject btnObj = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(btnObj, "Create Button");
        btnObj.transform.SetParent(parent, false);

        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(250, 60);
        rect.anchoredPosition = position;

        Image img = btnObj.AddComponent<Image>();
        img.color = new Color(0.3f, 0.3f, 0.3f, 1f);

        Button btn = btnObj.AddComponent<Button>();

        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(btnObj.transform, false);

        RectTransform txtRect = txtObj.AddComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.sizeDelta = Vector2.zero;

        TMP_Text tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 24;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        return btnObj;
    }

    private static TMP_Text AddText(Transform parent, string name, string content, float size, Vector2 position)
    {
        Transform existing = parent.Find(name);
        if (existing != null) return existing.GetComponent<TMP_Text>();

        GameObject txtObj = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(txtObj, "Create Text");
        txtObj.transform.SetParent(parent, false);

        RectTransform rect = txtObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(600, 100);
        rect.anchoredPosition = position;

        TMP_Text tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontSize = size;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        return tmp;
    }
}
#endif
