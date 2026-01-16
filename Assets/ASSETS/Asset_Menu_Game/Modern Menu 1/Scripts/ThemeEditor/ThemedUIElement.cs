using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SlimUI.ModernMenu{
	[System.Serializable]
	public class ThemedUIElement : ThemedUI {
		[Header("Parameters")]
		Color outline;
		Image image;
		GameObject message;
		public enum OutlineStyle {solidThin, solidThick, dottedThin, dottedThick};
		public bool hasImage = false;
		public bool isText = false;
		
		[Header("Custom Color Override")]
		[Tooltip("Tick nếu muốn giữ màu tùy chỉnh, không dùng theme color")]
		public bool useCustomColor = false;

		protected override void OnSkinUI(){
			base.OnSkinUI();

			if(hasImage && !useCustomColor){
				image = GetComponent<Image>();
				image.color = themeController.currentColor;
			}

			message = gameObject;

			if(isText && !useCustomColor){
				message.GetComponent<TextMeshPro>().color = themeController.textColor;
			}
		}
	}
}