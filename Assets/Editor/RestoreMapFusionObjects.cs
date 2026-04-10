using UnityEngine;
using UnityEditor;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;

public class RestoreMapFusionObjects : EditorWindow
{
    [MenuItem("Tools/Dungeon Mania/Phục hồi toàn bộ Object Mạng (Fusion) vào Scene")]
    public static void ShowWindow()
    {
        string sceneName = EditorSceneManager.GetActiveScene().name;

        // 1. Phục hồi cho UI_Game hoặc menu
        if (sceneName.Contains("UI_Game") || sceneName.Contains("Menu"))
        {
            var mp = Object.FindFirstObjectByType<MultiplayerManager>();
            if (mp == null)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/_NetworkLobbyManager.prefab");
                if (prefab != null)
                {
                    PrefabUtility.InstantiatePrefab(prefab);
                    EditorUtility.DisplayDialog("Thành công", "Đã tìm thấy Scene Menu!\nĐã chèn bổ sung cục '_NetworkLobbyManager' vào thành công!", "OK");
                    EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                }
                else
                {
                    EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy prefab _NetworkLobbyManager!", "OK");
                }
            }
            else
            {
                EditorUtility.DisplayDialog("Thông báo", "Scene này đã có MultiplayerManager rồi, không cần chèn thêm!", "OK");
            }
        }
        // 2. Phục hồi cho Cảnh Gameplay (Map_Chinh)
        else
        {
            var spawner = Object.FindFirstObjectByType<FusionSpawnPointSetter>();
            if (spawner == null)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/FusionSpawnPointSetter.prefab");
                if (prefab != null)
                {
                    GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    
                    // Tạo 2 điểm hồi sinh mẫu cho người chơi dễ kéo
                    GameObject sp1 = new GameObject("SpawnPoint_1");
                    GameObject sp2 = new GameObject("SpawnPoint_2");
                    sp1.transform.position = new Vector3(0, 1, 0);
                    sp2.transform.position = new Vector3(2, 1, 0);

                    // Gán vào Script
                    var comp = go.GetComponent<FusionSpawnPointSetter>();
                    if (comp != null)
                    {
                        var so = new SerializedObject(comp);
                        var prop = so.FindProperty("_SpawnPoints");
                        prop.arraySize = 2;
                        prop.GetArrayElementAtIndex(0).objectReferenceValue = sp1.transform;
                        prop.GetArrayElementAtIndex(1).objectReferenceValue = sp2.transform;
                        so.ApplyModifiedProperties();
                    }

                    EditorUtility.DisplayDialog("Thành công", "Đã chèn bổ sung 'FusionSpawnPointSetter' vào Map!\nTôi đã tạo sẵn 2 cái SpawnPoint mẫu (nằm ở tọa độ 0,0,0).\n\nBạn chỉ việc cầm 2 cục đó kéo tới cổng Làng hoặc chỗ nào bạn thích là xong!", "OK");
                    EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                }
                else
                {
                    EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy prefab FusionSpawnPointSetter!", "OK");
                }
            }
            else
            {
                EditorUtility.DisplayDialog("Thông báo", "Map này đã gắn sẵn hệ thống đẻ người chơi của Fusion rồi!", "OK");
            }
        }
    }
}
#endif
