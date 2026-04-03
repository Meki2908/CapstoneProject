using System;
using System.Collections;
using System.IO;
using UnityEngine;

[Serializable]
public class MapChinhPlayerPositionJson
{
    public string scene;
    public float px, py, pz;
    public float qx, qy, qz, qw;
}

/// <summary>
/// Đọc/ghi JSON vị trí / rotation root player cho một scene cố định (mặc định Map_Chinh).
/// File: persistentDataPath / map_chinh_player_position.json
/// </summary>
public static class MapChinhPlayerPositionStore
{
    const string FileName = "map_chinh_player_position.json";

    public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

    public static void SaveTransform(Transform t, string sceneName)
    {
        if (t == null || string.IsNullOrEmpty(sceneName)) return;
        try
        {
            var p = t.position;
            var q = t.rotation;
            var data = new MapChinhPlayerPositionJson
            {
                scene = sceneName,
                px = p.x,
                py = p.y,
                pz = p.z,
                qx = q.x,
                qy = q.y,
                qz = q.z,
                qw = q.w
            };
            File.WriteAllText(FilePath, JsonUtility.ToJson(data, true));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[MapChinhPlayerPositionStore] Save failed: {e.Message}");
        }
    }

    public static bool TryLoad(string expectedSceneName, out Vector3 pos, out Quaternion rot)
    {
        pos = default;
        rot = Quaternion.identity;
        if (string.IsNullOrEmpty(expectedSceneName) || !File.Exists(FilePath))
            return false;
        try
        {
            var data = JsonUtility.FromJson<MapChinhPlayerPositionJson>(File.ReadAllText(FilePath));
            if (data == null || data.scene != expectedSceneName)
                return false;
            pos = new Vector3(data.px, data.py, data.pz);
            rot = new Quaternion(data.qx, data.qy, data.qz, data.qw);
            if (rot.x == 0f && rot.y == 0f && rot.z == 0f && rot.w == 0f)
                rot = Quaternion.identity;
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[MapChinhPlayerPositionStore] Load failed: {e.Message}");
            return false;
        }
    }
}

/// <summary>
/// Gắn trên root prefab player (cùng Transform di chuyển trong world). Chỉ hoạt động khi scene hiện tại khớp <see cref="sceneName"/>.
/// </summary>
[DisallowMultipleComponent]
public class MapChinhPlayerPositionPersistence : MonoBehaviour
{
    [SerializeField] string sceneName = "Map_Chinh";

    void Awake()
    {
        Application.quitting += OnAppQuitting;
    }

    void OnDestroy()
    {
        Application.quitting -= OnAppQuitting;
        if (Application.isPlaying)
            TrySave();
    }

    void OnAppQuitting()
    {
        TrySave();
    }

    void Start()
    {
        if (gameObject.scene.name != sceneName)
            return;
        StartCoroutine(ApplyLoadedNextFrame());
    }

    IEnumerator ApplyLoadedNextFrame()
    {
        yield return null;
        if (gameObject.scene.name != sceneName)
            yield break;
        if (!MapChinhPlayerPositionStore.TryLoad(sceneName, out var pos, out var rot))
            yield break;

        var controllers = GetComponentsInChildren<CharacterController>(true);
        foreach (var c in controllers)
            c.enabled = false;

        transform.SetPositionAndRotation(pos, rot);

        foreach (var c in controllers)
            c.enabled = true;

        var ch = GetComponentInChildren<Character>(true);
        if (ch != null)
            ch.playerVelocity = Vector3.zero;
    }

    void TrySave()
    {
        if (gameObject.scene.name != sceneName)
            return;
        MapChinhPlayerPositionStore.SaveTransform(transform, sceneName);
    }
}
