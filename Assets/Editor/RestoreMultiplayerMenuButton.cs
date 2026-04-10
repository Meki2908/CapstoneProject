using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using Artsystack.ArtsystackGui;

public class RestoreMultiplayerMenuButton : EditorWindow
{
    [MenuItem("Tools/Dungeon Mania/Khôi phục Nút Multiplayer ở Menu")]
    public static void ShowWindow()
    {
        var gmm = Object.FindFirstObjectByType<GameMenuManager>();
        if (gmm == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy GameMenuManager! Hãy mở Scene UI_Game hoặc Canvas_Menu prefab trước.", "OK");
            return;
        }

        // 1. Tìm Nút Continue hoặc NewGame để nhân bản
        Button btnContinue = FindButtonByName("btn_Continue") ?? FindButtonByName("btn_NewGame") ?? FindButtonByName("btn_Play");
        if (btnContinue == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy nút Play hay Continue nào để copy!", "OK");
            return;
        }

        // Tạo Nút Multiplayer
        GameObject btnGo = FindObjectByName("btn_Multiplayer");
        if (btnGo == null)
        {
            btnGo = Instantiate(btnContinue.gameObject, btnContinue.transform.parent);
            btnGo.name = "btn_Multiplayer";
            btnGo.transform.SetSiblingIndex(btnContinue.transform.GetSiblingIndex() + 1);
        }

        var txt = btnGo.GetComponentInChildren<TextMeshProUGUI>();
        if (txt != null) txt.text = "Multiplayer";

        var btn = btnGo.GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        var clickMethod = System.Delegate.CreateDelegate(typeof(UnityEngine.Events.UnityAction), gmm, "ClickPlayMultiplayer") as UnityEngine.Events.UnityAction;
        UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(btn.onClick, clickMethod);


        // 2. Tìm Tab_TabPlay để nhân bản thành Tab_MultiplayerMode
        GameObject tabPlay = FindObjectByName("tab_TabPlay");
        if (tabPlay == null)
        {
            // Trượt fallback
            tabPlay = FindObjectByName("Panel_TabPlay") ?? FindObjectByName("tab_Play");
        }

        if (tabPlay != null)
        {
            GameObject tabMP = FindObjectByName("tab_MultiplayerMode");
            if (tabMP == null)
            {
                tabMP = Instantiate(tabPlay, tabPlay.transform.parent);
                tabMP.name = "tab_MultiplayerMode";
            }
            
            // Xóa hết nút bên trong chỉ chừa lại 2 nút để làm Host và Join
            Button[] buttons = tabMP.GetComponentsInChildren<Button>();
            if (buttons.Length >= 2)
            {
                buttons[0].name = "btn_Host";
                var t0 = buttons[0].GetComponentInChildren<TextMeshProUGUI>();
                if (t0) t0.text = "Host Game (Tạo phòng)";
                buttons[0].onClick.RemoveAllListeners();
                var hostMethod = System.Delegate.CreateDelegate(typeof(UnityEngine.Events.UnityAction), gmm, "OnHostCreateClicked") as UnityEngine.Events.UnityAction;
                UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(buttons[0].onClick, hostMethod);

                buttons[1].name = "btn_Join";
                var t1 = buttons[1].GetComponentInChildren<TextMeshProUGUI>();
                if (t1) t1.text = "Join Game (Vào phòng)";
                buttons[1].onClick.RemoveAllListeners();
                var joinMethod = System.Delegate.CreateDelegate(typeof(UnityEngine.Events.UnityAction), gmm, "OnJoinHostClicked") as UnityEngine.Events.UnityAction;
                UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(buttons[1].onClick, joinMethod);
                
                // Xóa số dư
                for (int i = 2; i < buttons.Length; i++)
                {
                    DestroyImmediate(buttons[i].gameObject);
                }
            }

            tabMP.SetActive(false);

            // Gán biến vào GMM
            var so = new SerializedObject(gmm);
            so.FindProperty("tab_TabPlay").objectReferenceValue = tabPlay;
            so.FindProperty("tab_MultiplayerMode").objectReferenceValue = tabMP;
            so.ApplyModifiedProperties();
        }

        EditorUtility.DisplayDialog("Thành Công", "Đã tạo xong Nút Multiplayer và Tab Multiplayer! Bây giờ hãy nhấn Tools > Setup Multiplayer UI để bơm 3 bảng Join/Host Room vào!", "OK");
    }

    static Button FindButtonByName(string name)
    {
        GameObject go = FindObjectByName(name);
        return go != null ? go.GetComponent<Button>() : null;
    }

    static GameObject FindObjectByName(string name)
    {
        var all = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (var t in all)
        {
            if (t.name == name && !EditorUtility.IsPersistent(t.gameObject))
            {
                return t.gameObject;
            }
        }
        return null;
    }
}
