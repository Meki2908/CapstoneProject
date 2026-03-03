using UnityEngine;

namespace SlimUI.ModernMenu{
	[ExecuteInEditMode()]
	[System.Serializable]
	public class ThemedUI : MonoBehaviour {

		public ThemedUIData themeController;

	protected virtual void OnSkinUI(){
		// Check if themeController is assigned
		if (themeController == null) {
			Debug.LogWarning("ThemedUI: themeController is not assigned on " + gameObject.name);
			return;
		}
	}

		public virtual void Awake(){
			OnSkinUI();
		}

		public virtual void Update(){
			OnSkinUI();
		}
	}
}
