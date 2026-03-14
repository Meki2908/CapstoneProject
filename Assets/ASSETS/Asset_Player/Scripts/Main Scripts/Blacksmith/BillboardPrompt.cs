using UnityEngine;

/// <summary>
/// Makes a world-space Canvas always face the main camera (billboard effect).
/// </summary>
public class BillboardPrompt : MonoBehaviour
{
    private Camera _cam;

    void Start()
    {
        _cam = Camera.main;
    }

    void LateUpdate()
    {
        if (_cam == null)
        {
            _cam = Camera.main;
            if (_cam == null) return;
        }

        // Face the camera
        transform.forward = _cam.transform.forward;
    }
}
