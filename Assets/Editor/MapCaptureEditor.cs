using UnityEngine;
using UnityEditor;
using System.IO;

public class MapCaptureEditor : EditorWindow
{
    Camera cam;
    int width = 4096;
    int height = 4096;
    string outputPath = "Assets/WorldMap.png";

    [MenuItem("Tools/Map/Capture World Map PNG")]
    static void Open() => GetWindow<MapCaptureEditor>("Map Capture");

    void OnGUI()
    {
        cam = (Camera)EditorGUILayout.ObjectField("Map Camera", cam, typeof(Camera), true);
        width = EditorGUILayout.IntField("Width", width);
        height = EditorGUILayout.IntField("Height", height);
        outputPath = EditorGUILayout.TextField("Output Path", outputPath);

        EditorGUILayout.HelpBox("Chọn MapCaptureCam (Orthographic, top-down) rồi bấm Capture.", MessageType.Info);

        GUI.enabled = cam != null && width > 0 && height > 0;
        if (GUILayout.Button("Capture PNG"))
        {
            Capture();
        }
        GUI.enabled = true;
    }

    void Capture()
    {
        var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        var prevRT = RenderTexture.active;
        var prevTarget = cam.targetTexture;

        cam.targetTexture = rt;
        RenderTexture.active = rt;

        cam.Render();

        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();

        byte[] png = tex.EncodeToPNG();
        File.WriteAllBytes(outputPath, png);

        cam.targetTexture = prevTarget;
        RenderTexture.active = prevRT;
        rt.Release();

        DestroyImmediate(rt);
        DestroyImmediate(tex);

        AssetDatabase.Refresh();
        Debug.Log($"Saved map: {outputPath}");
    }
}
