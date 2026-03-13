using UnityEngine;

/// <summary>
/// Gắn script này lên Parent (luôn bật).
/// mapCanvas = Canvas Map (tắt trong Editor).
/// Ấn M → bật map + chuột. Ấn M lại → tắt.
/// </summary>
[DisallowMultipleComponent]
public class WorldMapController : MonoBehaviour
{
    public GameObject mapCanvas;  // Kéo Canvas Map vào đây

    void Start()
    {
        if (mapCanvas != null) mapCanvas.SetActive(false);
        else Debug.LogError("[WorldMap] mapCanvas chưa gán!");

        Cursor.visible   = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
            Toggle();
    }

    void Toggle()
    {
        if (mapCanvas == null) return;

        bool open = !mapCanvas.activeSelf;
        mapCanvas.SetActive(open);
        Cursor.visible   = open;
        Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
    }
}
