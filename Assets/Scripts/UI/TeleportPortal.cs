using UnityEngine;

public class CloseCanvasButton : MonoBehaviour
{
    public GameObject canvasToClose;

    public void Close()
    {
        if (canvasToClose) canvasToClose.SetActive(false);
    }
}
