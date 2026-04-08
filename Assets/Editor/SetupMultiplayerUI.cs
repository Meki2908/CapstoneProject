using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Editor tool: tạo 2 panel multiplayer riêng biệt trong Canvas_Menu:
///   Panel_CreateRoom — cho Host (nhập tên → tạo phòng → hiện mã)
///   Panel_JoinRoom   — cho Client (nhập tên → nhập mã → tham gia)
///
/// Cách dùng: Unity menu → Tools → Setup Multiplayer UI
/// Mở prefab Canvas_Menu trước khi chạy!
/// </summary>
public class SetupMultiplayerUI : EditorWindow
{
    [MenuItem("Tools/Setup Multiplayer UI")]
    static void ShowWindow()
    {
        GetWindow<SetupMultiplayerUI>("Setup Multiplayer UI");
    }

    void OnGUI()
    {
        GUILayout.Label("Multiplayer UI Setup", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "Creates 2 separate panels:\n\n" +
            "1. Panel_CreateRoom — Host: enter name → create room → show code\n" +
            "2. Panel_JoinRoom — Client: enter name → enter code → join\n\n" +
            "Open Canvas_Menu prefab before running!",
            MessageType.Info);

        GUILayout.Space(10);

        if (GUILayout.Button("Build Both Panels + Auto Wire", GUILayout.Height(45)))
        {
            CreateBothPanels();
        }
    }

    void CreateBothPanels()
    {
        // Tìm parent — tab_HostOptions hoặc tab_MultiplayerMode
        var parent = FindObjectByName("tab_HostOptions") ?? FindObjectByName("tab_MultiplayerMode");
        if (parent == null)
        {
            // Fallback: tìm Panel_GUIGame
            parent = FindObjectByName("Panel_GUIGame");
        }
        if (parent == null)
        {
            EditorUtility.DisplayDialog("Error",
                "Cannot find tab_HostOptions / tab_MultiplayerMode / Panel_GUIGame.\n\nOpen Canvas_Menu prefab first!", "OK");
            return;
        }

        var root = parent.transform;

        // Xóa cũ
        DestroyChildByName(root, "Panel_CreateRoom");
        DestroyChildByName(root, "Panel_JoinRoom");

        // ════════════════════════════════════════════
        //  PANEL 1: CREATE ROOM (HOST)
        // ════════════════════════════════════════════
        var createPanel = CreateFullPanel(root, "Panel_CreateRoom");

        // Title
        var titleCreate = CreateText(createPanel.transform, "Text_Title", "CREATE ROOM", 42, TextAlignmentOptions.Center);
        SetRect(titleCreate, 0, 1, 1, 1, 0, -30, 0, 60);

        // Enter name
        var labelName1 = CreateText(createPanel.transform, "Text_LabelName", "Your name:", 28, TextAlignmentOptions.Left);
        SetRect(labelName1, 0.05f, 1, 0.95f, 1, 0, -100, 0, 40);

        var nameInput1 = CreateInputField(createPanel.transform, "InputField_PlayerName_Host", "Enter name...", 20);
        SetRect(nameInput1, 0.05f, 1, 0.95f, 1, 0, -155, 0, 55);

        // Room code after creation
        var labelCode = CreateText(createPanel.transform, "Text_LabelCode", "Room Code:", 28, TextAlignmentOptions.Left);
        SetRect(labelCode, 0.05f, 1, 0.95f, 1, 0, -225, 0, 40);

        var joinCodeDisplay = CreateText(createPanel.transform, "Text_JoinCode", "------", 60, TextAlignmentOptions.Center);
        SetRect(joinCodeDisplay, 0.1f, 1, 0.9f, 1, 0, -290, 0, 65);
        joinCodeDisplay.GetComponent<TextMeshProUGUI>().color = new Color(1f, 0.84f, 0f); // Gold

        // Player count
        var playerCount = CreateText(createPanel.transform, "Text_PlayerCount", "Players: 0/4", 24, TextAlignmentOptions.Left);
        SetRect(playerCount, 0.05f, 1, 0.95f, 1, 0, -345, 0, 35);
        playerCount.GetComponent<TextMeshProUGUI>().color = new Color(0.7f, 0.9f, 1f);

        // Player list frame (dark bg with vertical layout)
        var listFrame = new GameObject("PlayerListFrame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(VerticalLayoutGroup));
        listFrame.transform.SetParent(createPanel.transform, false);
        listFrame.layer = 5;
        SetRect(listFrame, 0.05f, 0, 0.95f, 1, 0, -400, 0, -550);
        listFrame.GetComponent<Image>().color = new Color(0.04f, 0.04f, 0.08f, 0.85f);
        var vlg = listFrame.GetComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.spacing = 4;
        vlg.padding = new RectOffset(10, 10, 8, 8);
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;

        // Placeholder host entry
        var hostEntry = CreateText(listFrame.transform, "Text_HostEntry", "  Waiting...", 26, TextAlignmentOptions.Left);
        var hostRT = hostEntry.GetComponent<RectTransform>();
        hostRT.sizeDelta = new Vector2(0, 40);
        hostEntry.GetComponent<TextMeshProUGUI>().color = new Color(1f, 1f, 1f, 0.3f);
        hostEntry.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Italic;

        // Button: CREATE ROOM
        var btnCreate = CreateStyledButton(createPanel.transform, "Button_CreateRoom", "CREATE ROOM", new Color(0.2f, 0.6f, 0.3f));
        SetRect(btnCreate, 0.15f, 0, 0.85f, 0, 0, 95, 0, 55);

        // Button: ENTER GAME (hidden until room created)
        var btnEnter = CreateStyledButton(createPanel.transform, "Button_EnterGame", "ENTER GAME", new Color(0.8f, 0.5f, 0.1f));
        SetRect(btnEnter, 0.15f, 0, 0.85f, 0, 0, 95, 0, 55);
        btnEnter.SetActive(false);

        // Status
        var statusCreate = CreateText(createPanel.transform, "Text_Status_Create", "", 24, TextAlignmentOptions.Center);
        SetRect(statusCreate, 0.05f, 0, 0.95f, 0, 0, 30, 0, 35);
        statusCreate.GetComponent<TextMeshProUGUI>().color = new Color(0.7f, 0.9f, 1f);

        // Ping display (Host)
        var pingCreate = CreateText(createPanel.transform, "Text_Ping_Host", "Ping: --", 20, TextAlignmentOptions.Left);
        SetRect(pingCreate, 0.05f, 0, 0.5f, 0, 0, 5, 0, 25);
        pingCreate.GetComponent<TextMeshProUGUI>().color = new Color(0.5f, 0.5f, 0.5f);

        // ════════════════════════════════════════════
        //  PANEL 2: JOIN ROOM (CLIENT)
        // ════════════════════════════════════════════
        var joinPanel = CreateFullPanel(root, "Panel_JoinRoom");
        joinPanel.SetActive(false); // Mặc định ẩn

        // Title
        var titleJoin = CreateText(joinPanel.transform, "Text_Title", "JOIN ROOM", 42, TextAlignmentOptions.Center);
        SetRect(titleJoin, 0, 1, 1, 1, 0, -30, 0, 60);

        // Nhập tên
        var labelName2 = CreateText(joinPanel.transform, "Text_LabelName", "Your name:", 28, TextAlignmentOptions.Left);
        SetRect(labelName2, 0.05f, 1, 0.95f, 1, 0, -100, 0, 40);

        var nameInput2 = CreateInputField(joinPanel.transform, "InputField_PlayerName_Join", "Enter name...", 20);
        SetRect(nameInput2, 0.05f, 1, 0.95f, 1, 0, -155, 0, 55);

        // Nhập mã phòng
        var labelJoinCode = CreateText(joinPanel.transform, "Text_LabelCode", "Enter Room Code:", 28, TextAlignmentOptions.Left);
        SetRect(labelJoinCode, 0.05f, 1, 0.95f, 1, 0, -225, 0, 40);

        var joinCodeInput = CreateInputField(joinPanel.transform, "InputField_JoinCode", "Room code (e.g. A1B2C3)", 6);
        SetRect(joinCodeInput, 0.1f, 1, 0.9f, 1, 0, -286, 0, 65);

        // Nút THAM GIA
        var btnJoin = CreateStyledButton(joinPanel.transform, "Button_JoinRoom", "JOIN", new Color(0.2f, 0.4f, 0.8f));
        SetRect(btnJoin, 0.15f, 0, 0.85f, 0, 0, 95, 0, 55);

        // Status
        var statusJoin = CreateText(joinPanel.transform, "Text_Status_Join", "", 24, TextAlignmentOptions.Center);
        SetRect(statusJoin, 0.05f, 0, 0.95f, 0, 0, 30, 0, 35);
        statusJoin.GetComponent<TextMeshProUGUI>().color = new Color(0.7f, 0.9f, 1f);

        // Ping display (Client)
        var pingJoin = CreateText(joinPanel.transform, "Text_Ping_Client", "Ping: --", 20, TextAlignmentOptions.Left);
        SetRect(pingJoin, 0.05f, 0, 0.5f, 0, 0, 5, 0, 25);
        pingJoin.GetComponent<TextMeshProUGUI>().color = new Color(0.5f, 0.5f, 0.5f);

        // ════════════════════════════════════════════
        //  AUTO WIRE → MultiplayerManager
        // ════════════════════════════════════════════
        bool wired = false;
        var mp = Object.FindFirstObjectByType<MultiplayerManager>();
        if (mp != null)
        {
            var so = new SerializedObject(mp);

            SetRef(so, "joinCodeInput", joinCodeInput.GetComponent<TMP_InputField>());
            SetRef(so, "joinCodeDisplay", joinCodeDisplay.GetComponent<TextMeshProUGUI>());
            SetRef(so, "statusText", statusCreate.GetComponent<TextMeshProUGUI>());
            SetRef(so, "playerNameInput", nameInput1.GetComponent<TMP_InputField>());
            // Wire Join panel name input (client uses this when joining)
            SetRef(so, "playerNameInputJoin", nameInput2.GetComponent<TMP_InputField>());
            // Wire Join panel status text
            SetRef(so, "statusTextJoin", statusJoin.GetComponent<TextMeshProUGUI>());
            SetRef(so, "createRoomButton", btnCreate);
            SetRef(so, "enterGameButton", btnEnter);
            SetRef(so, "playerListContainer", listFrame);
            SetRef(so, "playerCountText", playerCount.GetComponent<TextMeshProUGUI>());

            var propRelay = so.FindProperty("useRelay");
            if (propRelay != null) propRelay.boolValue = true;

            so.ApplyModifiedProperties();
            wired = true;

            // Auto-wire button onClick events
            WireButtonOnClick(btnCreate.GetComponent<Button>(), mp, "CreateRoom");
            WireButtonOnClick(btnJoin.GetComponent<Button>(), mp, "StartClientAndLoadGame");

            Debug.Log("[SetupMultiplayerUI] ✅ Auto-wired fields + button onClick to MultiplayerManager!");
        }

        // ════════════════════════════════════════════
        //  AUTO WIRE → PingDisplay
        // ════════════════════════════════════════════
        if (mp != null)
        {
            // Add or get PingDisplay on the same GameObject
            var pingComp = mp.GetComponent<PingDisplay>();
            if (pingComp == null)
                pingComp = mp.gameObject.AddComponent<PingDisplay>();

            var soPing = new SerializedObject(pingComp);
            // Wire to the Host ping text (both panels share the same PingDisplay)
            SetRef(soPing, "pingText", pingCreate.GetComponent<TextMeshProUGUI>());
            soPing.ApplyModifiedProperties();
            Debug.Log("[SetupMultiplayerUI] ✅ Wired PingDisplay component!");
        }

        // ════════════════════════════════════════════
        //  AUTO WIRE → GameMenuManager
        // ════════════════════════════════════════════
        var gmm = Object.FindFirstObjectByType<Artsystack.ArtsystackGui.GameMenuManager>();
        if (gmm != null)
        {
            var so2 = new SerializedObject(gmm);

            var propCreate = so2.FindProperty("panel_CreateRoom");
            if (propCreate != null)
                propCreate.objectReferenceValue = createPanel;

            var propJoin = so2.FindProperty("panel_JoinRoom");
            if (propJoin != null)
                propJoin.objectReferenceValue = joinPanel;

            // Also wire tab_HostOptions if empty
            var tabHost = so2.FindProperty("tab_HostOptions");
            if (tabHost != null && tabHost.objectReferenceValue == null)
                tabHost.objectReferenceValue = createPanel;

            so2.ApplyModifiedProperties();
            Debug.Log("[SetupMultiplayerUI] ✅ Wired panels vào GameMenuManager!");
        }

        Debug.Log("[SetupMultiplayerUI] ✅ Done! Panel_CreateRoom + Panel_JoinRoom");
        EditorUtility.DisplayDialog("Success!",
            "Created 2 separate panels:\n\n" +
            "✅ Panel_CreateRoom (Host)\n" +
            "   - Name input + Create room + Show code\n\n" +
            "✅ Panel_JoinRoom (Client)\n" +
            "   - Name input + Enter code + Join\n\n" +
            (wired ? "✅ Auto-wired to MultiplayerManager!" : "⚠️ Manually wire MultiplayerManager"),
            "OK");
    }

    // ───────────── HELPERS ─────────────

    static void WireButtonOnClick(Button btn, MonoBehaviour target, string methodName)
    {
        if (btn == null || target == null) return;

        // Clear existing listeners
        while (btn.onClick.GetPersistentEventCount() > 0)
            UnityEditor.Events.UnityEventTools.RemovePersistentListener(btn.onClick, 0);

        // Add new persistent listener
        var method = target.GetType().GetMethod(methodName,
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (method != null)
        {
            var action = System.Delegate.CreateDelegate(typeof(UnityEngine.Events.UnityAction), target, method)
                         as UnityEngine.Events.UnityAction;
            UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(btn.onClick, action);
            Debug.Log($"[SetupMultiplayerUI] Wired {btn.name} → {target.GetType().Name}.{methodName}()");
        }
        else
        {
            Debug.LogWarning($"[SetupMultiplayerUI] Method '{methodName}' not found on {target.GetType().Name}!");
        }
    }

    static void SetRef(SerializedObject so, string prop, Object value)
    {
        var p = so.FindProperty(prop);
        if (p != null) p.objectReferenceValue = value;
    }

    static GameObject FindObjectByName(string name)
    {
        foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
            if (t.name == name && t.hideFlags == HideFlags.None)
                return t.gameObject;
        return null;
    }

    static void DestroyChildByName(Transform parent, string name)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
            if (parent.GetChild(i).name == name)
                DestroyImmediate(parent.GetChild(i).gameObject);
    }

    static GameObject CreateFullPanel(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        go.layer = 5;

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.2f, 0.1f);
        rt.anchorMax = new Vector2(0.8f, 0.9f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;

        var img = go.GetComponent<Image>();
        img.color = new Color(0.06f, 0.06f, 0.12f, 0.92f);

        return go;
    }

    static GameObject CreateText(Transform parent, string name, string text, float fontSize, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        go.layer = 5;

        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = align;
        tmp.color = Color.white;
        return go;
    }

    /// <summary>Set RectTransform anchors + offset. Pos = anchoredPosition, Size = sizeDelta.</summary>
    static void SetRect(GameObject go, float aMinX, float aMinY, float aMaxX, float aMaxY, float posX, float posY, float sizeX, float sizeY)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(aMinX, aMinY);
        rt.anchorMax = new Vector2(aMaxX, aMaxY);
        rt.anchoredPosition = new Vector2(posX, posY);
        rt.sizeDelta = new Vector2(sizeX, sizeY);
    }

    static GameObject CreateStyledButton(Transform parent, string name, string label, Color bgColor)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        go.layer = 5;

        go.GetComponent<Image>().color = bgColor;

        var txtGo = CreateText(go.transform, "Text", label, 28, TextAlignmentOptions.Center);
        var txtRt = txtGo.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.sizeDelta = Vector2.zero;

        return go;
    }

    static GameObject CreateInputField(Transform parent, string name, string placeholderText, int charLimit)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_InputField));
        go.transform.SetParent(parent, false);
        go.layer = 5;

        go.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.18f, 1f);

        // Text Area
        var textArea = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
        textArea.transform.SetParent(go.transform, false);
        textArea.layer = 5;
        var taRt = textArea.GetComponent<RectTransform>();
        taRt.anchorMin = Vector2.zero;
        taRt.anchorMax = Vector2.one;
        taRt.offsetMin = new Vector2(15, 5);
        taRt.offsetMax = new Vector2(-15, -5);

        // Placeholder
        var phGo = CreateText(textArea.transform, "Placeholder", placeholderText, 30, TextAlignmentOptions.Center);
        var phRt = phGo.GetComponent<RectTransform>();
        phRt.anchorMin = Vector2.zero; phRt.anchorMax = Vector2.one; phRt.sizeDelta = Vector2.zero;
        phGo.GetComponent<TextMeshProUGUI>().color = new Color(1, 1, 1, 0.35f);
        phGo.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Italic;

        // Text
        var txtGo = CreateText(textArea.transform, "Text", "", 30, TextAlignmentOptions.Center);
        var txtRt = txtGo.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one; txtRt.sizeDelta = Vector2.zero;

        // Wire
        var input = go.GetComponent<TMP_InputField>();
        input.textViewport = taRt;
        input.textComponent = txtGo.GetComponent<TextMeshProUGUI>();
        input.placeholder = phGo.GetComponent<TextMeshProUGUI>();
        input.characterLimit = charLimit;

        return go;
    }
}
