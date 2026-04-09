using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Editor tool: tạo 3 panel multiplayer trong Canvas_Menu:
///   Panel_CreateRoom    — cho Host (nhập tên → tạo phòng → hiện mã + player list)
///   Panel_JoinRoom      — cho Client (nhập tên → nhập mã → tham gia)
///   Panel_ConnectedRoom — cho Client sau khi join thành công (hiện room code, chờ host)
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
            "Creates 3 panels:\n\n" +
            "1. Panel_CreateRoom — Host: enter name → create room → show code + player list\n" +
            "2. Panel_JoinRoom — Client: enter name → enter code → join\n" +
            "3. Panel_ConnectedRoom — Client after join: room code + status + leave\n\n" +
            "Open Canvas_Menu prefab before running!",
            MessageType.Info);

        GUILayout.Space(10);

        if (GUILayout.Button("Build All Panels + Auto Wire", GUILayout.Height(45)))
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
        DestroyChildByName(root, "Panel_ConnectedRoom");

        // ════════════════════════════════════════════
        //  PANEL 1: CREATE ROOM (HOST) — Simplified
        // ════════════════════════════════════════════
        var createPanel = CreateFullPanel(root, "Panel_CreateRoom");

        // Title
        var titleCreate = CreateText(createPanel.transform, "Text_Title", "CREATE ROOM", 35, TextAlignmentOptions.Center);
        SetRect(titleCreate, 0, 1, 1, 1, 0, -40, 0, 100);

        // Enter name
        var labelName1 = CreateText(createPanel.transform, "Text_LabelName", "Your name:", 35, TextAlignmentOptions.Left);
        SetRect(labelName1, 0.05f, 1, 0.95f, 1, 0, -160, 0, 80);

        var nameInput1 = CreateInputField(createPanel.transform, "InputField_PlayerName_Host", "Enter name...", 20);
        SetRect(nameInput1, 0.05f, 1, 0.95f, 1, 0, -270, 0, 110);

        // Button: CREATE ROOM
        var btnCreate = CreateStyledButton(createPanel.transform, "Button_CreateRoom", "CREATE ROOM", new Color(0.2f, 0.6f, 0.3f));
        SetRect(btnCreate, 0.1f, 0, 0.9f, 0, 0, 200, 0, 110);

        // Button: BACK (quay về tab_HostOptions)
        var btnBackCreate = CreateStyledButton(createPanel.transform, "Button_Back", "← BACK", new Color(0.4f, 0.4f, 0.4f));
        SetRect(btnBackCreate, 0.1f, 0, 0.9f, 0, 0, 70, 0, 90);

        // Status
        var statusCreate = CreateText(createPanel.transform, "Text_Status_Create", "", 35, TextAlignmentOptions.Center);
        SetRect(statusCreate, 0.05f, 0, 0.95f, 0, 0, -20, 0, 70);
        statusCreate.GetComponent<TextMeshProUGUI>().color = new Color(0.7f, 0.9f, 1f);

        // ════════════════════════════════════════════
        //  PANEL 2: JOIN ROOM (CLIENT)
        // ════════════════════════════════════════════
        var joinPanel = CreateFullPanel(root, "Panel_JoinRoom");
        joinPanel.SetActive(false); // Mặc định ẩn

        // Title
        var titleJoin = CreateText(joinPanel.transform, "Text_Title", "JOIN ROOM", 35, TextAlignmentOptions.Center);
        SetRect(titleJoin, 0, 1, 1, 1, 0, -40, 0, 100);

        // Nhập tên
        var labelName2 = CreateText(joinPanel.transform, "Text_LabelName", "Your name:", 35, TextAlignmentOptions.Left);
        SetRect(labelName2, 0.05f, 1, 0.95f, 1, 0, -160, 0, 80);

        var nameInput2 = CreateInputField(joinPanel.transform, "InputField_PlayerName_Join", "Enter name...", 20);
        SetRect(nameInput2, 0.05f, 1, 0.95f, 1, 0, -270, 0, 110);

        // Nhập mã phòng
        var labelJoinCode = CreateText(joinPanel.transform, "Text_LabelCode", "Enter Room Code:", 35, TextAlignmentOptions.Left);
        SetRect(labelJoinCode, 0.05f, 1, 0.95f, 1, 0, -400, 0, 80);

        var joinCodeInput = CreateInputField(joinPanel.transform, "InputField_JoinCode", "Room code (e.g. A1B2C3)", 6);
        SetRect(joinCodeInput, 0.05f, 1, 0.95f, 1, 0, -520, 0, 130);

        // Nút THAM GIA
        var btnJoin = CreateStyledButton(joinPanel.transform, "Button_JoinRoom", "JOIN", new Color(0.2f, 0.4f, 0.8f));
        SetRect(btnJoin, 0.1f, 0, 0.9f, 0, 0, 200, 0, 110);

        // Status
        var statusJoin = CreateText(joinPanel.transform, "Text_Status_Join", "", 35, TextAlignmentOptions.Center);
        SetRect(statusJoin, 0.05f, 0, 0.95f, 0, 0, 70, 0, 70);
        statusJoin.GetComponent<TextMeshProUGUI>().color = new Color(0.7f, 0.9f, 1f);

        // Ping display (Client)
        var pingJoin = CreateText(joinPanel.transform, "Text_Ping_Client", "Ping: --", 35, TextAlignmentOptions.Left);
        SetRect(pingJoin, 0.05f, 0, 0.5f, 0, 0, 5, 0, 50);
        pingJoin.GetComponent<TextMeshProUGUI>().color = new Color(0.5f, 0.5f, 0.5f);

        // Button: BACK (quay về tab_HostOptions)
        var btnBackJoin = CreateStyledButton(joinPanel.transform, "Button_Back", "← BACK", new Color(0.4f, 0.4f, 0.4f));
        SetRect(btnBackJoin, 0.1f, 0, 0.9f, 0, 0, 70, 0, 90);

        // ════════════════════════════════════════════
        //  PANEL 3: CONNECTED ROOM (CLIENT after join success)
        // ════════════════════════════════════════════
        var connectedPanel = CreateFullPanel(root, "Panel_ConnectedRoom");
        connectedPanel.SetActive(false);

        // Title
        var titleConnected = CreateText(connectedPanel.transform, "Text_Title", "CONNECTED", 35, TextAlignmentOptions.Center);
        SetRect(titleConnected, 0, 1, 1, 1, 0, -40, 0, 100);
        titleConnected.GetComponent<TextMeshProUGUI>().color = new Color(0.4f, 1f, 0.5f);

        // Room code
        var labelConnCode = CreateText(connectedPanel.transform, "Text_LabelCode", "Room Code:", 35, TextAlignmentOptions.Left);
        SetRect(labelConnCode, 0.05f, 1, 0.95f, 1, 0, -160, 0, 80);

        var connRoomCode = CreateText(connectedPanel.transform, "Text_ConnectedRoomCode", "------", 50, TextAlignmentOptions.Center);
        SetRect(connRoomCode, 0.05f, 1, 0.68f, 1, 0, -280, 0, 120);
        connRoomCode.GetComponent<TextMeshProUGUI>().color = new Color(1f, 0.84f, 0f);

        // Button: COPY room code
        var btnCopy = CreateStyledButton(connectedPanel.transform, "Button_CopyCode", "📋 COPY", new Color(0.25f, 0.45f, 0.7f));
        SetRect(btnCopy, 0.70f, 1, 0.95f, 1, 0, -275, 0, 100);

        // Player count
        var connPlayerCount = CreateText(connectedPanel.transform, "Text_ConnectedPlayerCount", "", 35, TextAlignmentOptions.Left);
        SetRect(connPlayerCount, 0.05f, 1, 0.95f, 1, 0, -410, 0, 70);
        connPlayerCount.GetComponent<TextMeshProUGUI>().color = new Color(0.7f, 0.9f, 1f);

        // Player list frame
        var connListFrame = new GameObject("ConnectedPlayerListFrame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(VerticalLayoutGroup));
        connListFrame.transform.SetParent(connectedPanel.transform, false);
        connListFrame.layer = 5;
        SetRect(connListFrame, 0.05f, 0, 0.95f, 1, 0, -500, 0, -680);
        connListFrame.GetComponent<Image>().color = new Color(0.04f, 0.04f, 0.08f, 0.85f);
        var connVlg = connListFrame.GetComponent<VerticalLayoutGroup>();
        connVlg.childAlignment = TextAnchor.UpperLeft;
        connVlg.spacing = 8;
        connVlg.padding = new RectOffset(15, 15, 12, 12);
        connVlg.childControlWidth = true;
        connVlg.childControlHeight = false;
        connVlg.childForceExpandWidth = true;

        // Status
        var connStatus = CreateText(connectedPanel.transform, "Text_Status_Connected", "Waiting for host to start game...", 35, TextAlignmentOptions.Center);
        SetRect(connStatus, 0.05f, 0, 0.95f, 0, 0, 210, 0, 70);
        connStatus.GetComponent<TextMeshProUGUI>().color = new Color(0.7f, 0.9f, 1f);

        // Button: START GAME (chỉ host thấy, ẩn mặc định)
        var btnStartGame = CreateStyledButton(connectedPanel.transform, "Button_StartGame", "START GAME", new Color(0.2f, 0.7f, 0.3f));
        SetRect(btnStartGame, 0.1f, 0, 0.9f, 0, 0, 210, 0, 110);
        btnStartGame.SetActive(false);

        // Button: LEAVE ROOM
        var btnLeave = CreateStyledButton(connectedPanel.transform, "Button_LeaveRoom", "LEAVE ROOM", new Color(0.7f, 0.2f, 0.2f));
        SetRect(btnLeave, 0.1f, 0, 0.9f, 0, 0, 70, 0, 100);

        // Ping (Client)
        var pingConn = CreateText(connectedPanel.transform, "Text_Ping_Connected", "Ping: --", 35, TextAlignmentOptions.Left);
        SetRect(pingConn, 0.05f, 0, 0.5f, 0, 0, 5, 0, 50);
        pingConn.GetComponent<TextMeshProUGUI>().color = new Color(0.5f, 0.5f, 0.5f);

        // ════════════════════════════════════════════
        //  AUTO WIRE → MultiplayerManager
        // ════════════════════════════════════════════
        bool wired = false;
        var mp = Object.FindFirstObjectByType<MultiplayerManager>();
        if (mp != null)
        {
            var so = new SerializedObject(mp);

            // Panel_CreateRoom fields
            SetRef(so, "joinCodeInput", joinCodeInput.GetComponent<TMP_InputField>());
            SetRef(so, "statusText", statusCreate.GetComponent<TextMeshProUGUI>());
            SetRef(so, "playerNameInput", nameInput1.GetComponent<TMP_InputField>());
            SetRef(so, "playerNameInputJoin", nameInput2.GetComponent<TMP_InputField>());
            SetRef(so, "statusTextJoin", statusJoin.GetComponent<TextMeshProUGUI>());
            SetRef(so, "createRoomButton", btnCreate);

            // Panel references (for switching)
            SetRef(so, "panelCreateRoom", createPanel);
            SetRef(so, "panelJoinRoom", joinPanel);
            SetRef(so, "panelConnectedRoom", connectedPanel);

            // Panel_ConnectedRoom fields
            SetRef(so, "connectedRoomCodeText", connRoomCode.GetComponent<TextMeshProUGUI>());
            SetRef(so, "connectedStatusText", connStatus.GetComponent<TextMeshProUGUI>());
            SetRef(so, "connectedPlayerCountText", connPlayerCount.GetComponent<TextMeshProUGUI>());
            SetRef(so, "connectedPlayerListContainer", connListFrame);
            SetRef(so, "startGameButton", btnStartGame);
            SetRef(so, "leaveRoomButton", btnLeave);
            SetRef(so, "copyCodeButton", btnCopy);

            so.ApplyModifiedProperties();
            wired = true;

            // Auto-wire button onClick events
            WireButtonOnClick(btnCreate.GetComponent<Button>(), mp, "CreateRoom");
            WireButtonOnClick(btnJoin.GetComponent<Button>(), mp, "StartClientAndLoadGame");
            WireButtonOnClick(btnStartGame.GetComponent<Button>(), mp, "LoadGameAsHost");
            WireButtonOnClick(btnLeave.GetComponent<Button>(), mp, "LeaveRoom");
            WireButtonOnClick(btnCopy.GetComponent<Button>(), mp, "CopyRoomCode");

            Debug.Log("[SetupMultiplayerUI] ✅ Auto-wired all fields + buttons!");
        }

        // ════════════════════════════════════════════
        //  AUTO WIRE → PingDisplay
        // ════════════════════════════════════════════
        if (mp != null)
        {
            var pingComp2 = mp.GetComponent<PingDisplay>();
            if (pingComp2 == null)
                pingComp2 = mp.gameObject.AddComponent<PingDisplay>();
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

            var propConnected = so2.FindProperty("panel_ConnectedRoom");
            if (propConnected != null)
                propConnected.objectReferenceValue = connectedPanel;

            var tabHost = so2.FindProperty("tab_HostOptions");
            if (tabHost != null && tabHost.objectReferenceValue == null)
                tabHost.objectReferenceValue = createPanel;

            so2.ApplyModifiedProperties();

            // Wire BACK buttons → GameMenuManager.BackToHostOptions
            WireButtonOnClick(btnBackCreate.GetComponent<Button>(), gmm, "BackToHostOptions");
            WireButtonOnClick(btnBackJoin.GetComponent<Button>(), gmm, "BackToHostOptions");
            Debug.Log("[SetupMultiplayerUI] ✅ Wired panels + BACK buttons vào GameMenuManager!");
        }

        Debug.Log("[SetupMultiplayerUI] ✅ Done! All 3 panels created.");
        EditorUtility.DisplayDialog("Success!",
            "Created 3 panels:\n\n" +
            "✅ Panel_CreateRoom (Host)\n" +
            "   - Name input + Create room + Show code + Player list\n\n" +
            "✅ Panel_JoinRoom (Client)\n" +
            "   - Name input + Enter code + Join\n\n" +
            "✅ Panel_ConnectedRoom (Client after join)\n" +
            "   - Room code + Status + Leave button\n\n" +
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
        rt.anchorMin = new Vector2(0.1f, 0.02f);
        rt.anchorMax = new Vector2(0.9f, 0.98f);
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

        var txtGo = CreateText(go.transform, "Text", label, 35, TextAlignmentOptions.Center);
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
        var phGo = CreateText(textArea.transform, "Placeholder", placeholderText, 35, TextAlignmentOptions.Center);
        var phRt = phGo.GetComponent<RectTransform>();
        phRt.anchorMin = Vector2.zero; phRt.anchorMax = Vector2.one; phRt.sizeDelta = Vector2.zero;
        phGo.GetComponent<TextMeshProUGUI>().color = new Color(1, 1, 1, 0.35f);
        phGo.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Italic;

        // Text
        var txtGo = CreateText(textArea.transform, "Text", "", 35, TextAlignmentOptions.Center);
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
