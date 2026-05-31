using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Reflection;

namespace GAP_ParticleSystemController
{

	public static class SaveParticleSystemScript{		

        // Runtime safety: avoid writing "OriginalSettings" to disk at play-time.
        // In runtime, prefab folder path is often null (not in PrefabStage), and writing to "/OriginalSettings"
        // becomes "C:\\OriginalSettings" on Windows. Cache in memory instead.
        private static readonly Dictionary<string, List<ParticleSystemOriginalSettings>> RuntimeCache =
            new Dictionary<string, List<ParticleSystemOriginalSettings>>();

        private static string RuntimeKey(GameObject vfx)
        {
            if (vfx == null) return string.Empty;
            // Strip "(Clone)" suffix to share cache across instances.
            return vfx.name.Replace("(Clone)", "").Trim();
        }

		public static void SaveVFX (GameObject prefabVFX, List<ParticleSystemOriginalSettings> psOriginalSettingsList) {
#if !UNITY_EDITOR
            var cacheKey = RuntimeKey(prefabVFX);
            if (!string.IsNullOrEmpty(cacheKey) && psOriginalSettingsList != null)
                RuntimeCache[cacheKey] = psOriginalSettingsList;
            return;
#else
#if UNITY_2018_3_OR_NEWER
             var prefabFolderPath = GetPrefabFolder2018_3 (prefabVFX);
#else
             var prefabFolderPath = GetPrefabFolder (prefabVFX);
#endif

            if (string.IsNullOrEmpty(prefabFolderPath))
            {
                // Not in PrefabStage (e.g. running in scene). Cache only.
                var cacheKey = RuntimeKey(prefabVFX);
                if (!string.IsNullOrEmpty(cacheKey) && psOriginalSettingsList != null)
                    RuntimeCache[cacheKey] = psOriginalSettingsList;
                return;
            }

			if (!Directory.Exists (prefabFolderPath + "/OriginalSettings")) {
				UnityEditor.AssetDatabase.CreateFolder (prefabFolderPath, "OriginalSettings");
				Debug.Log ("Created folder:  " + prefabFolderPath + "/OriginalSettings");
			}
            BinaryFormatter bf = new BinaryFormatter ();			
			FileStream stream = new FileStream (prefabFolderPath + "/OriginalSettings/" + prefabVFX.name + ".dat", FileMode.Create);

			bf.Serialize (stream, psOriginalSettingsList);		
			stream.Close ();

#if UNITY_2018_3_OR_NEWER
            SaveNestedPrefab(prefabVFX);
#endif

            Debug.Log ("Original Settings of '" + prefabVFX.name + "' saved to: " + prefabFolderPath + "/OriginalSettings");
#endif
		}

		public static List<ParticleSystemOriginalSettings> LoadVFX (GameObject prefabVFX) {
#if !UNITY_EDITOR
            var cacheKey = RuntimeKey(prefabVFX);
            if (!string.IsNullOrEmpty(cacheKey) && RuntimeCache.TryGetValue(cacheKey, out var cachedList))
                return cachedList;
            return null;
#else
#if UNITY_2018_3_OR_NEWER
            var prefabFolderPath = GetPrefabFolder2018_3 (prefabVFX);
#else
            var prefabFolderPath = GetPrefabFolder(prefabVFX);
#endif

            if (string.IsNullOrEmpty(prefabFolderPath))
            {
                var cacheKey = RuntimeKey(prefabVFX);
                if (!string.IsNullOrEmpty(cacheKey) && RuntimeCache.TryGetValue(cacheKey, out var cachedList))
                    return cachedList;
                return null;
            }

            if (File.Exists (prefabFolderPath + "/OriginalSettings/" + prefabVFX.name + ".dat")) {
				BinaryFormatter bf = new BinaryFormatter ();
				FileStream stream = new FileStream (prefabFolderPath + "/OriginalSettings/" + prefabVFX.name + ".dat", FileMode.Open);

				List<ParticleSystemOriginalSettings> originalSettingsList = new List<ParticleSystemOriginalSettings> (); 
				originalSettingsList = bf.Deserialize (stream) as List<ParticleSystemOriginalSettings>;

				stream.Close ();
				return originalSettingsList;

			} else {
				Debug.Log ("No saved VFX data found");
				return null;
			}
#endif
		}

		public static bool CheckExistingFile (GameObject prefabVFX){
#if !UNITY_EDITOR
            var cacheKey = RuntimeKey(prefabVFX);
            return !string.IsNullOrEmpty(cacheKey) && RuntimeCache.ContainsKey(cacheKey);
#else
#if UNITY_2018_3_OR_NEWER
            var prefabFolderPath = GetPrefabFolder2018_3 (prefabVFX);
#else
            var prefabFolderPath = GetPrefabFolder(prefabVFX);
#endif
            if (prefabFolderPath != null) {
				if (File.Exists (prefabFolderPath + "/OriginalSettings/" + prefabVFX.name + ".dat"))
					return true;
				else
					return false;
			} else
				return false;
#endif
		}

		static string GetPrefabFolder (GameObject prefabVFX){
#if UNITY_EDITOR
            string prefabPath = UnityEditor.AssetDatabase.GetAssetPath (prefabVFX);
			string prefabFolderPath = Path.GetDirectoryName (prefabPath);
			return prefabFolderPath;
#else
            return null;
#endif
		}

#if UNITY_2018_3_OR_NEWER
        static string GetPrefabFolder2018_3 (GameObject prefabVFX)
        {
#if UNITY_EDITOR
			var stage = UnityEditor.SceneManagement.PrefabStageUtility.GetPrefabStage(prefabVFX);
			if (stage == null)
				return null;
			string prefabPath = stage.assetPath;
			string prefabFolderPath = Path.GetDirectoryName(prefabPath);
			return prefabFolderPath;
#else
            return null;
#endif
        }
#endif

#if UNITY_2018_3_OR_NEWER
        public static void SaveNestedPrefab(GameObject prefab)
        {
#if UNITY_EDITOR
            var prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetPrefabStage(prefab);
            if (prefabStage != null)
                UnityEditor.PrefabUtility.SaveAsPrefabAsset(prefabStage.prefabContentsRoot, prefabStage.assetPath);
#endif
        }
#endif
    }
}