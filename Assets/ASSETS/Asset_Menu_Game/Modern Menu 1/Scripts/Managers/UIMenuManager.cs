using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

namespace SlimUI.ModernMenu{
	public class UIMenuManager : MonoBehaviour {
		private Animator CameraObject;

		// campaign button sub menu
        [Header("MENUS")]
        [Tooltip("The Menu for when the MAIN menu buttons")]
        public GameObject mainMenu;
        [Tooltip("THe first list of buttons")]
        public GameObject firstMenu;
        [Tooltip("The Menu for when the PLAY button is clicked")]
        public GameObject playMenu;
        [Tooltip("The Menu for when the EXIT button is clicked")]
        public GameObject exitMenu;
        [Tooltip("Optional 4th Menu")]
        public GameObject extrasMenu;

        public enum Theme {custom1, custom2, custom3};
        [Header("THEME SETTINGS")]
        public Theme theme;
        private int themeIndex;
        public ThemedUIData themeController;

        [Header("PANELS")]
        [Tooltip("The UI Panel parenting all sub menus")]
        public GameObject mainCanvas;
        [Tooltip("The UI Panel that holds the CONTROLS window tab")]
        public GameObject PanelControls;
        [Tooltip("The UI Panel that holds the VIDEO window tab")]
        public GameObject PanelVideo;
        [Tooltip("The UI Panel that holds the GAME window tab")]
        public GameObject PanelGame;
        [Tooltip("The UI Panel that holds the KEY BINDINGS window tab")]
        public GameObject PanelKeyBindings;
        [Tooltip("The UI Sub-Panel under KEY BINDINGS for MOVEMENT")]
        public GameObject PanelMovement;
        [Tooltip("The UI Sub-Panel under KEY BINDINGS for COMBAT")]
        public GameObject PanelCombat;
        [Tooltip("The UI Sub-Panel under KEY BINDINGS for GENERAL")]
        public GameObject PanelGeneral;
        

        // highlights in settings screen
        [Header("SETTINGS SCREEN")]
        [Tooltip("Highlight Image for when GAME Tab is selected in Settings")]
        public GameObject lineGame;
        [Tooltip("Highlight Image for when VIDEO Tab is selected in Settings")]
        public GameObject lineVideo;
        [Tooltip("Highlight Image for when CONTROLS Tab is selected in Settings")]
        public GameObject lineControls;
        [Tooltip("Highlight Image for when KEY BINDINGS Tab is selected in Settings")]
        public GameObject lineKeyBindings;
        [Tooltip("Highlight Image for when MOVEMENT Sub-Tab is selected in KEY BINDINGS")]
        public GameObject lineMovement;
        [Tooltip("Highlight Image for when COMBAT Sub-Tab is selected in KEY BINDINGS")]
        public GameObject lineCombat;
        [Tooltip("Highlight Image for when GENERAL Sub-Tab is selected in KEY BINDINGS")]
        public GameObject lineGeneral;

        [Header("LOADING SCREEN")]
		[Tooltip("If this is true, the loaded scene won't load until receiving user input")]
		public bool waitForInput = true;
        public GameObject loadingMenu;
		[Tooltip("The loading bar Slider UI element in the Loading Screen")]
        public Slider loadingBar;
        public TMP_Text loadPromptText;
		public KeyCode userPromptKey;

		[Header("SFX")]
        [Tooltip("The GameObject holding the Audio Source component for the HOVER SOUND")]
        public AudioSource hoverSound;
        [Tooltip("The GameObject holding the Audio Source component for the AUDIO SLIDER")]
        public AudioSource sliderSound;
        [Tooltip("The GameObject holding the Audio Source component for the SWOOSH SOUND when switching to the Settings Screen")]
        public AudioSource swooshSound;

		void Start(){
			CameraObject = transform.GetComponent<Animator>();
            if (CameraObject != null)
            {
                CameraObject.updateMode = AnimatorUpdateMode.UnscaledTime;
            }

            // Null checks for AudioSources
            if (playMenu != null) playMenu.SetActive(false);
			if (exitMenu != null) exitMenu.SetActive(false);
			if(extrasMenu) extrasMenu.SetActive(false);
			if (firstMenu != null) firstMenu.SetActive(true);
			if (mainMenu != null) mainMenu.SetActive(true);

			SetThemeColors();
		}

		void SetThemeColors()
		{
            if (themeController == null) return;

			switch (theme)
			{
				case Theme.custom1:
                    if (themeController.custom1 != null) {
					    themeController.currentColor = themeController.custom1.graphic1;
					    themeController.textColor = themeController.custom1.text1;
                    }
					themeIndex = 0;
					break;
				case Theme.custom2:
                    if (themeController.custom2 != null) {
					    themeController.currentColor = themeController.custom2.graphic2;
					    themeController.textColor = themeController.custom2.text2;
                    }
					themeIndex = 1;
					break;
				case Theme.custom3:
                    if (themeController.custom3 != null) {
					    themeController.currentColor = themeController.custom3.graphic3;
					    themeController.textColor = themeController.custom3.text3;
                    }
					themeIndex = 2;
					break;
				default:
					Debug.Log("Invalid theme selected.");
					break;
			}
		}

		public void PlayCampaign(){
			if (exitMenu) exitMenu.SetActive(false);
			if(extrasMenu) extrasMenu.SetActive(false);
			if (playMenu) playMenu.SetActive(true);
		}
		
		public void PlayCampaignMobile(){
			if (exitMenu) exitMenu.SetActive(false);
			if(extrasMenu) extrasMenu.SetActive(false);
			if (playMenu) playMenu.SetActive(true);
			if (mainMenu) mainMenu.SetActive(false);
		}

		public void ReturnMenu(){
			if (playMenu) playMenu.SetActive(false);
			if(extrasMenu) extrasMenu.SetActive(false);
			if (exitMenu) exitMenu.SetActive(false);
			if (mainMenu) mainMenu.SetActive(true);
		}

		public void LoadScene(string scene){
			if(scene != ""){
				StartCoroutine(LoadAsynchronously(scene));
			}
		}

		public void  DisablePlayCampaign(){
			if (playMenu) playMenu.SetActive(false);
		}

		public void Position2(){
			DisablePlayCampaign();
            if (CameraObject != null)
			    CameraObject.SetFloat("Animate",1);
		}

		public void Position1(){
            if (CameraObject != null)
			    CameraObject.SetFloat("Animate",0);
		}

		void DisablePanels(){
			if (PanelControls) PanelControls.SetActive(false);
			if (PanelVideo) PanelVideo.SetActive(false);
			if (PanelGame) PanelGame.SetActive(false);
			if (PanelKeyBindings) PanelKeyBindings.SetActive(false);

			if (lineGame) lineGame.SetActive(false);
			if (lineControls) lineControls.SetActive(false);
			if (lineVideo) lineVideo.SetActive(false);
			if (lineKeyBindings) lineKeyBindings.SetActive(false);

			if (PanelMovement) PanelMovement.SetActive(false);
			if (lineMovement) lineMovement.SetActive(false);
			if (PanelCombat) PanelCombat.SetActive(false);
			if (lineCombat) lineCombat.SetActive(false);
			if (PanelGeneral) PanelGeneral.SetActive(false);
			if (lineGeneral) lineGeneral.SetActive(false);
		}

		public void GamePanel(){
			DisablePanels();
			if (PanelGame) PanelGame.SetActive(true);
			if (lineGame) lineGame.SetActive(true);
		}

		public void VideoPanel(){
			DisablePanels();
			if (PanelVideo) PanelVideo.SetActive(true);
			if (lineVideo) lineVideo.SetActive(true);
		}

		public void ControlsPanel(){
			DisablePanels();
			if (PanelControls) PanelControls.SetActive(true);
			if (lineControls) lineControls.SetActive(true);
		}

		public void KeyBindingsPanel(){
			DisablePanels();
			MovementPanel();
			if (PanelKeyBindings) PanelKeyBindings.SetActive(true);
			if (lineKeyBindings) lineKeyBindings.SetActive(true);
		}

		public void MovementPanel(){
			DisablePanels();
			if (PanelKeyBindings) PanelKeyBindings.SetActive(true);
			if (PanelMovement) PanelMovement.SetActive(true);
			if (lineMovement) lineMovement.SetActive(true);
		}

		public void CombatPanel(){
			DisablePanels();
			if (PanelKeyBindings) PanelKeyBindings.SetActive(true);
			if (PanelCombat) PanelCombat.SetActive(true);
			if (lineCombat) lineCombat.SetActive(true);
		}

		public void GeneralPanel(){
			DisablePanels();
			if (PanelKeyBindings) PanelKeyBindings.SetActive(true);
			if (PanelGeneral) PanelGeneral.SetActive(true);
			if (lineGeneral) lineGeneral.SetActive(true);
		}

		public void PlayHover(){
            if (hoverSound) hoverSound.Play();
		}

		public void PlaySFXHover(){
            if (sliderSound) sliderSound.Play();
		}

		public void PlaySwoosh(){
            if (swooshSound) swooshSound.Play();
		}

		// Are You Sure - Quit Panel Pop Up
		public void AreYouSure(){
			exitMenu.SetActive(true);
			if(extrasMenu) extrasMenu.SetActive(false);
			DisablePlayCampaign();
		}

		public void AreYouSureMobile(){
			exitMenu.SetActive(true);
			if(extrasMenu) extrasMenu.SetActive(false);
			mainMenu.SetActive(false);
			DisablePlayCampaign();
		}

		public void ExtrasMenu(){
			playMenu.SetActive(false);
			if(extrasMenu) extrasMenu.SetActive(true);
			exitMenu.SetActive(false);
		}

		public void QuitGame(){
			#if UNITY_EDITOR
				UnityEditor.EditorApplication.isPlaying = false;
			#else
				Application.Quit();
			#endif
		}

		// Load Bar synching animation
		IEnumerator LoadAsynchronously(string sceneName){ // scene name is just the name of the current scene being loaded
			AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
			operation.allowSceneActivation = false;
			mainCanvas.SetActive(false);
			loadingMenu.SetActive(true);

			while (!operation.isDone){
				float progress = Mathf.Clamp01(operation.progress / .95f);
				loadingBar.value = progress;

				if (operation.progress >= 0.9f && waitForInput){
					loadPromptText.text = "Press " + userPromptKey.ToString().ToUpper() + " to continue";
					loadingBar.value = 1;

					if (Input.GetKeyDown(userPromptKey)){
						operation.allowSceneActivation = true;
					}
                }else if(operation.progress >= 0.9f && !waitForInput){
					operation.allowSceneActivation = true;
				}

				yield return null;
			}
		}
	}
}