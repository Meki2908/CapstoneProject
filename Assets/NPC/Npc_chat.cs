using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class TriggerShowTwoAndFOpenOther : MonoBehaviour
{
    [Header("Player Detect")]
    public string playerTag = "Player";

    [Header("On Enter: show these 2 UI")]
    public GameObject canvasA;
    public GameObject canvasB;

    [Header("Press F: open this UI")]
    public GameObject canvasOnF;

    [Header("Input")]
    public KeyCode key = KeyCode.F;

    [Header("Options")]
    public bool hideAandBAfterF = true;     // bật canvasOnF thì tắt A/B
    public bool closeOnExit = true;        // ra khỏi vùng thì tắt tất cả
    public bool allowToggleOnF = false;    // F bật/tắt canvasOnF

    bool _inside;

    void Awake()
    {
        // an toàn: tắt hết lúc start
        if (canvasA) canvasA.SetActive(false);
        if (canvasB) canvasB.SetActive(false);
        if (canvasOnF) canvasOnF.SetActive(false);
    }

    void Update()
    {
        if (!_inside) return;

        if (IsFPressedThisFrame())
        {
            if (!canvasOnF) return;

            if (allowToggleOnF)
            {
                bool next = !canvasOnF.activeSelf;
                canvasOnF.SetActive(next);

                if (hideAandBAfterF && next)
                {
                    if (canvasA) canvasA.SetActive(false);
                    if (canvasB) canvasB.SetActive(false);
                }
            }
            else
            {
                canvasOnF.SetActive(true);

                if (hideAandBAfterF)
                {
                    if (canvasA) canvasA.SetActive(false);
                    if (canvasB) canvasB.SetActive(false);
                }
            }
        }
    }

    bool IsFPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
            return true;
#endif
        return Input.GetKeyDown(key);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        _inside = true;

        if (canvasA) canvasA.SetActive(true);
        if (canvasB) canvasB.SetActive(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        _inside = false;

        if (!closeOnExit) return;

        if (canvasA) canvasA.SetActive(false);
        if (canvasB) canvasB.SetActive(false);
        if (canvasOnF) canvasOnF.SetActive(false);
    }
}
