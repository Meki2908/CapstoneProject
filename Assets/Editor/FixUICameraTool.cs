using UnityEngine;
using UnityEditor;

/// <summary>
/// Quick fix tool to configure UI_Camera correctly
/// Run from: Tools > Fix UI Camera
/// </summary>
public class FixUICameraTool : EditorWindow
{
    [MenuItem("Tools/Fix UI Camera")]
    public static void FixUICamera()
    {
        // Find UI_Camera
        var uiCameraObj = GameObject.Find("UI_Camera");
        if (uiCameraObj == null)
        {
            EditorUtility.DisplayDialog("Not Found", "UI_Camera not found in scene!", "OK");
            return;
        }

        var uiCamera = uiCameraObj.GetComponent<Camera>();
        if (uiCamera == null)
        {
            EditorUtility.DisplayDialog("Error", "UI_Camera has no Camera component!", "OK");
            return;
        }

        // Fix Camera settings
        uiCamera.clearFlags = CameraClearFlags.Nothing; // DON'T CLEAR - critical!
        uiCamera.cullingMask = 1 << 5; // UI layer only
        uiCamera.depth = 100; // Render after everything
        uiCamera.orthographic = true;
        uiCamera.orthographicSize = 5;
        uiCamera.nearClipPlane = 0.3f;
        uiCamera.farClipPlane = 1000f;

        // Move UI_Camera out of Canv_Options if it's inside
        var canvOptions = uiCameraObj.transform.parent;
        if (canvOptions != null && canvOptions.name.Contains("Canv"))
        {
            // Move to PlayerRoot level
            var playerRoot = GameObject.Find("PlayerRoot");
            if (playerRoot != null)
            {
                uiCameraObj.transform.SetParent(playerRoot.transform);
                Debug.Log("Moved UI_Camera to PlayerRoot");
            }
            else
            {
                // Move to root
                uiCameraObj.transform.SetParent(null);
                Debug.Log("Moved UI_Camera to scene root");
            }
        }

        // Position camera
        uiCameraObj.transform.localPosition = Vector3.zero;
        uiCameraObj.transform.localRotation = Quaternion.identity;

        // Find and configure Canvas
        var canvas = FindCanvasWithCamera(uiCamera);
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = uiCamera;
            canvas.planeDistance = 10f;
            canvas.sortingOrder = 100;
            
            // Ensure Canvas is on UI layer
            SetLayerRecursively(canvas.gameObject, 5); // UI layer
            
            Debug.Log("Canvas configured: " + canvas.gameObject.name);
        }

        EditorUtility.SetDirty(uiCameraObj);
        
        EditorUtility.DisplayDialog("Fixed!", 
            "UI_Camera configured:\n\n" +
            "✓ Clear Flags = Nothing (no blue screen)\n" +
            "✓ Culling Mask = UI only\n" +
            "✓ Depth = 100\n" +
            "✓ Moved outside Canv_Options\n\n" +
            "Try playing now!", 
            "OK");
    }

    private static Canvas FindCanvasWithCamera(Camera camera)
    {
        // Find all canvases
        var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (var canvas in canvases)
        {
            if (canvas.renderMode == RenderMode.ScreenSpaceCamera && canvas.worldCamera == camera)
                return canvas;
            
            // Or find Canv_Options
            if (canvas.gameObject.name.Contains("Canv_Options") || 
                canvas.transform.parent?.name.Contains("Canv_Options") == true)
                return canvas;
        }
        
        // Find by name
        var canvObj = GameObject.Find("Canv_Options");
        if (canvObj != null)
        {
            var c = canvObj.GetComponent<Canvas>();
            if (c != null) return c;
            
            c = canvObj.GetComponentInParent<Canvas>();
            if (c != null) return c;
        }
        
        return null;
    }

    private static void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
}
