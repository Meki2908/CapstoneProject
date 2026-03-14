namespace ModularFootstepSystem.Extensions
{
    using System.Collections.Generic;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Rendering.Universal;
    
    /// <summary>
    /// Enables the Decal feature in the current Render Pipeline Asset to ensure proper rendering of decals such as footprints.
    /// </summary>
    public static class DecalEnabler
    {
        /// <summary>
        /// Checks if the decal feature function is enabled.
        /// </summary>
        public static bool IsDecalsEnabled()
        {
            UniversalRenderPipelineAsset asset = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
            ScriptableRendererData data = GetDefaultRenderer(asset);
            
            foreach (var feature in data.rendererFeatures)
            {
                if (feature is DecalRendererFeature)
                {
                    return true;
                }
            }

            return false;
        }
        
        /// <summary>
        /// Enables the decals feature in the current Render Pipeline Asset.
        /// </summary>
        public static void EnableDecals() => AddRendererFeature<DecalRendererFeature>();

        private static void ConfigureDecals(DecalRendererFeature feature)
        {
            SerializedObject featureObject = new SerializedObject(feature);
            SerializedProperty decalSettingsProperty = featureObject.FindProperty("m_Settings");
            SerializedProperty decalLayers = decalSettingsProperty.FindPropertyRelative("decalLayers");
            decalLayers.boolValue = true;
            featureObject.ApplyModifiedProperties();
        }
        
        private static void AddRendererFeature<T>() where T : ScriptableRendererFeature
        {
            var handledDataObjects = new List<ScriptableRendererData>();

            int levels = QualitySettings.names.Length;
            for (int level = 0; level < levels; level++)
            {
                UniversalRenderPipelineAsset asset = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
                ScriptableRendererData data = GetDefaultRenderer(asset);

                if (handledDataObjects.Contains(data))
                {
                    continue;
                }
                handledDataObjects.Add(data);

                bool found = false;
                foreach (var feature in data.rendererFeatures)
                {
                    if (feature is T)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    var feature = ScriptableObject.CreateInstance<T>();
                    feature.name = typeof(T).Name;
                    
                    if (feature is DecalRendererFeature decalFeature)
                    {
                        ConfigureDecals(decalFeature);
                    }
                    
                    AddRenderFeature(data, feature);
                }
            }
        }
        
        private static int GetDefaultRendererIndex(UniversalRenderPipelineAsset asset)
            => (int)typeof(UniversalRenderPipelineAsset).GetField("m_DefaultRendererIndex", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(asset);
        
        private static ScriptableRendererData GetDefaultRenderer(UniversalRenderPipelineAsset asset)
        {
            if (asset)
            {
                ScriptableRendererData[] rendererDataList = (ScriptableRendererData[])typeof(UniversalRenderPipelineAsset)
                        .GetField("m_RendererDataList", BindingFlags.NonPublic | BindingFlags.Instance)
                        .GetValue(asset);
                int defaultRendererIndex = GetDefaultRendererIndex(asset);

                return rendererDataList[defaultRendererIndex];
            }
            
            return null;
        }
        
        private static void AddRenderFeature(ScriptableRendererData data, ScriptableRendererFeature feature)
        {
            var serializedObject = new SerializedObject(data);

            var renderFeaturesProp = serializedObject.FindProperty("m_RendererFeatures"); // Let's hope they don't change these.
            var renderFeaturesMapProp = serializedObject.FindProperty("m_RendererFeatureMap");

            serializedObject.Update();
            
            if (EditorUtility.IsPersistent(data))
                AssetDatabase.AddObjectToAsset(feature, data);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out var guid, out long localId);

            renderFeaturesProp.arraySize++;
            var componentProp = renderFeaturesProp.GetArrayElementAtIndex(renderFeaturesProp.arraySize - 1);
            componentProp.objectReferenceValue = feature;

            renderFeaturesMapProp.arraySize++;
            var guidProp = renderFeaturesMapProp.GetArrayElementAtIndex(renderFeaturesMapProp.arraySize - 1);
            guidProp.longValue = localId;

            if (EditorUtility.IsPersistent(data))
            {
                AssetDatabase.SaveAssetIfDirty(data);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}