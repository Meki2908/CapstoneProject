using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Linq;

public class SetupPlayerSilhouette
{
    [MenuItem("Tools/Setup Player Silhouette (Cách 1)")]
    public static void Setup()
    {
        // 1. Get current Pipeline asset
        var pipelineAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (pipelineAsset == null)
        {
            /// Try default QualitySettings
            pipelineAsset = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
        }

        if (pipelineAsset == null)
        {
            Debug.LogError("Current Render Pipeline is not URP. Please ensure URP is assigned in Graphics/Quality list.");
            return;
        }

        // 2. We need to find the RendererData.
        ScriptableRendererData rendererData = null;
        SerializedObject pipelineSO = new SerializedObject(pipelineAsset);
        SerializedProperty rendererDataList = pipelineSO.FindProperty("m_RendererDataList");
        SerializedProperty defaultRendererIndex = pipelineSO.FindProperty("m_DefaultRendererIndex");
        
        if (rendererDataList != null && rendererDataList.arraySize > 0)
        {
            int index = defaultRendererIndex != null ? defaultRendererIndex.intValue : 0;
            rendererData = rendererDataList.GetArrayElementAtIndex(index).objectReferenceValue as ScriptableRendererData;
        }

        if (rendererData == null)
        {
            Debug.LogError("Active RendererData could not be found.");
            return;
        }

        // 3. Create or find Material
        string matPath = "Assets/Settings/PlayerSilhouetteMaterial.mat";
        Material silhouetteMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (silhouetteMat == null)
        {
            // Create a simple unlit material
            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlitShader == null) unlitShader = Shader.Find("Hidden/Universal Render Pipeline/Unlit"); 
            silhouetteMat = new Material(unlitShader);
            silhouetteMat.SetColor("_BaseColor", new Color(0.1f, 0.7f, 1f, 1f)); // cyan
            
            // Note: URP RenderObjects override applies Depth state properly.
            // But we ensure the material doesn't force ZWrite
            if (silhouetteMat.HasProperty("_ZWrite")) silhouetteMat.SetFloat("_ZWrite", 0);
            
            if (!System.IO.Directory.Exists("Assets/Settings")) System.IO.Directory.CreateDirectory("Assets/Settings");
            AssetDatabase.CreateAsset(silhouetteMat, matPath);
        }

        // 4. Check if feature already exists
        bool hasFeature = false;
        foreach (var feature in rendererData.rendererFeatures)
        {
            if (feature != null && feature.name == "Player Silhouette")
            {
                hasFeature = true;
                break;
            }
        }

        if (hasFeature)
        {
            Debug.Log("Player Silhouette feature already exists on the active renderer.");
            EditorUtility.DisplayDialog("Thành công", "Tính năng Player Silhouette đã được thêm từ trước rồi nhé!", "OK");
            return;
        }

        // 5. Add RenderObjects Feature
#if UNITY_2021_1_OR_NEWER
        RenderObjects renderObjects = ScriptableObject.CreateInstance<RenderObjects>();
        renderObjects.name = "Player Silhouette";
        
        // Use SerializedObject to set values without relying on private/changed properties
        SerializedObject roSO = new SerializedObject(renderObjects);
        
        // Event = AfterRenderingTransparents
        SerializedProperty eventProp = roSO.FindProperty("settings.Event");
        if(eventProp != null) eventProp.intValue = (int)RenderPassEvent.AfterRenderingTransparents;
        
        // LayerMask (Find Player Layer)
        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer == -1) playerLayer = LayerMask.NameToLayer("Character");
        if (playerLayer == -1) playerLayer = LayerMask.NameToLayer("Default"); // fallback
        
        SerializedProperty filterLayerProp = roSO.FindProperty("settings.filterSettings.LayerMask");
        if(filterLayerProp != null) filterLayerProp.intValue = (1 << playerLayer);
        
        // Override Material
        SerializedProperty matProp = roSO.FindProperty("settings.overrideMaterial");
        if(matProp != null) matProp.objectReferenceValue = silhouetteMat;
        
        SerializedProperty matPassProp = roSO.FindProperty("settings.overrideMaterialPassIndex");
        if(matPassProp != null) matPassProp.intValue = 0;
        
        // Depth Override -> Greater and Write = false
        SerializedProperty depthOverrideProp = roSO.FindProperty("settings.overrideDepthState");
        if(depthOverrideProp != null) depthOverrideProp.boolValue = true;
        
        SerializedProperty depthCompareProp = roSO.FindProperty("settings.depthCompareFunction");
        if(depthCompareProp != null) depthCompareProp.intValue = (int)CompareFunction.Greater;
        
        SerializedProperty depthWriteProp = roSO.FindProperty("settings.enableWrite");
        if(depthWriteProp != null) depthWriteProp.boolValue = false;
        
        roSO.ApplyModifiedProperties();

        // Add to renderer asset
        AssetDatabase.AddObjectToAsset(renderObjects, rendererData);
        
        // Add to renderer features list
        SerializedObject rdSO = new SerializedObject(rendererData);
        SerializedProperty m_RendererFeatures = rdSO.FindProperty("m_RendererFeatures");
        if (m_RendererFeatures != null)
        {
            m_RendererFeatures.arraySize++;
            m_RendererFeatures.GetArrayElementAtIndex(m_RendererFeatures.arraySize - 1).objectReferenceValue = renderObjects;
            rdSO.ApplyModifiedProperties();
        }
        
        EditorUtility.SetDirty(rendererData);
        AssetDatabase.SaveAssets();
        
        Debug.Log("Successfully added Player Silhouette to URP Renderer!");
        EditorUtility.DisplayDialog("Thành công!", $"Đã thêm Player Silhouette X-Ray vào hệ thống URP (Target Layer: {LayerMask.LayerToName(playerLayer)}).", "OK");
#else
        Debug.LogError("Cannot automatically add Render Feature on this Unity version.");
#endif
    }
}
