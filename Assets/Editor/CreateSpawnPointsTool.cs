using UnityEngine;
using UnityEditor;

public class CreateSpawnPointsTool : EditorWindow
{
    private int spawnCount = 4;
    private float radius = 3f;
    private Vector3 center = new Vector3(0, 0, 0);

    [MenuItem("Tools/Create Spawn Points")]
    public static void ShowWindow()
    {
        GetWindow<CreateSpawnPointsTool>("Create Spawn Points");
    }

    void OnGUI()
    {
        GUILayout.Label("Tao SpawnPoints cho Multiplayer", EditorStyles.boldLabel);
        GUILayout.Space(5);

        spawnCount = EditorGUILayout.IntSlider("So Spawn Points", spawnCount, 2, 8);
        radius = EditorGUILayout.FloatField("Ban kinh", radius);
        center = EditorGUILayout.Vector3Field("Tam spawn XYZ", center);

        GUILayout.Space(5);

        if (GUILayout.Button("Tao Spawn Points", GUILayout.Height(35)))
        {
            CreateSpawnPoints();
        }

        GUILayout.Space(5);

        if (GUILayout.Button("Tao Tag SpawnPoint", GUILayout.Height(25)))
        {
            CreateSpawnPointTag();
        }
    }

    void CreateSpawnPoints()
    {
        if (SpawnPointMarker.spawnPointTag == null)
        {
            EditorUtility.DisplayDialog("Loi", "Vui long tao tag 'SpawnPoint' truoc!", "OK");
            return;
        }

        GameObject parent = new GameObject("SpawnPoints");
        parent.transform.position = center;
        Undo.RegisterCreatedObjectUndo(parent, "Create SpawnPoints Parent");

        for (int i = 0; i < spawnCount; i++)
        {
            float angle = i * (360f / spawnCount);
            float rad = angle * Mathf.Deg2Rad;
            Vector3 pos = center + new Vector3(Mathf.Cos(rad) * radius, 0, Mathf.Sin(rad) * radius);

            GameObject sp = new GameObject("SpawnPoint_" + (i + 1));
            sp.transform.position = pos;
            sp.transform.SetParent(parent.transform);
            sp.tag = "SpawnPoint";
            sp.AddComponent<SpawnPointMarker>();

            Undo.RegisterCreatedObjectUndo(sp, "Create SpawnPoint");
            Debug.Log("Created SpawnPoint_" + (i + 1) + " at " + pos);
        }

        Debug.Log("Da tao " + spawnCount + " SpawnPoints tai " + center);
        Selection.activeGameObject = parent;
    }

    [MenuItem("Tools/Create SpawnPoint Tag")]
    static void CreateSpawnPointTag()
    {
        var tagManager = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0];
        var so = new SerializedObject(tagManager);
        SerializedProperty tags = so.FindProperty("tags");

        bool found = false;
        for (int i = 0; i < tags.arraySize; i++)
        {
            if (tags.GetArrayElementAtIndex(i).stringValue == "SpawnPoint")
            { found = true; break; }
        }

        if (!found)
        {
            tags.InsertArrayElementAtIndex(tags.arraySize);
            tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = "SpawnPoint";
            so.ApplyModifiedProperties();
            Debug.Log("Da tao tag SpawnPoint");
            SpawnPointMarker.spawnPointTag = "SpawnPoint";
            EditorUtility.DisplayDialog("OK", "Da tao tag SpawnPoint", "OK");
        }
        else
        {
            SpawnPointMarker.spawnPointTag = "SpawnPoint";
            Debug.Log("Tag SpawnPoint da ton tai");
            EditorUtility.DisplayDialog("Thong bao", "Tag SpawnPoint da ton tai", "OK");
        }
    }
}

public class SpawnPointMarker : MonoBehaviour
{
    public static string spawnPointTag;

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, 0.8f);

        Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.3f);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 2f);
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.15f);
    }
}
