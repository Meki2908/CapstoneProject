using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class InteractShowCanvas : MonoBehaviour
{
    [Header("UI")]
    public GameObject promptFCanvas;   // UI "Press F"
    public GameObject targetCanvas;    // UI muốn mở khi bấm F

    [Header("Input")]
    public KeyCode interactKey = KeyCode.F;

    [Header("Player Detect")]
    public string playerTag = "Player";

    [Header("Options")]
    public bool hidePromptWhenOpened = true;
    public bool closeTargetWithSameKey = false; // nếu muốn bấm F lần nữa để đóng

    bool _canInteract = false;

    void Awake()
    {
        if (promptFCanvas) promptFCanvas.SetActive(false);
        if (targetCanvas) targetCanvas.SetActive(false);
    }

    void Update()
    {
        if (!_canInteract) return;

        if (IsInteractPressedThisFrame())
        {
            if (!targetCanvas) return;

            bool next = !targetCanvas.activeSelf;
            targetCanvas.SetActive(next);

            if (hidePromptWhenOpened && promptFCanvas)
                promptFCanvas.SetActive(!next);

            // Nếu không muốn toggle mà chỉ mở 1 lần:
            if (!closeTargetWithSameKey && next)
                _canInteract = false; // mở xong thì không nghe F nữa (tuỳ bạn)
        }
    }

    bool IsInteractPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
            return true;
#endif
        return Input.GetKeyDown(interactKey);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        _canInteract = true;
        if (promptFCanvas) promptFCanvas.SetActive(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        _canInteract = false;
        if (promptFCanvas) promptFCanvas.SetActive(false);

        // Tuỳ: ra khỏi vùng thì đóng luôn UI
        // if (targetCanvas) targetCanvas.SetActive(false);
    }
}
