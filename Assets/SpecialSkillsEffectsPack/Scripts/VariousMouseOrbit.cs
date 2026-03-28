using UnityEngine;
using System.Collections;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class VariousMouseOrbit : MonoBehaviour
{

    Transform Target;
    public Transform[] Targets;
    int i = 0;
    public float distance;

    public float xSpeed = 250.0f;
    public float ySpeed = 120.0f;

    public float yMinLimit = -20.0f;
    public float yMaxLimit = 80.0f;

    private float x = 0.0f;
    private float y = 0.0f;
    public float CameraDist = 10;

    // Use this for initialization
    void Start()
    {
        Vector3 angles = transform.eulerAngles;
        x = angles.x+50;
        y = angles.y;
        distance = 30;
        Target = Targets[0];
        if (this.GetComponent<Rigidbody>() == true)
            GetComponent<Rigidbody>().freezeRotation = true;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (SwitchTargetPressed())
        {
            if (i < Targets.Length-1)
                i++;
            else if (i >= Targets.Length-1)
                i = 0;
            Target = Targets[i];       
        }


            if (OrbitHeld())
             {
                if (Target)
                {
                    Vector2 mouseDelta = GetMouseDelta();
                    x += mouseDelta.x * xSpeed * 0.02f;
                    y += mouseDelta.y * ySpeed * 0.05f;

                    y = ClampAngle(y, yMinLimit, yMaxLimit);

                    Quaternion rotation = Quaternion.Euler(y, x, 0);
                    Vector3 position = rotation * new Vector3(0, 0, -distance) + Target.position;

                    transform.rotation = rotation;
                    transform.position = position;
                    distance = CameraDist;

                    if (ZoomInHeld())
                    {
                        CameraDist -= Time.deltaTime * 20f;
                        CameraDist = Mathf.Clamp(CameraDist,2,80);
                    }
                    if (ZoomOutHeld())
                    {
                        CameraDist += Time.deltaTime * 20f;
                        CameraDist = Mathf.Clamp(CameraDist, 2, 80);
                    }
              }
        }
    }

    bool SwitchTargetPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.V);
#endif
    }

    bool OrbitHeld()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.rightButton.isPressed;
#else
        return Input.GetKey(KeyCode.Mouse1);
#endif
    }

    Vector2 GetMouseDelta()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current == null)
            return Vector2.zero;
        return Mouse.current.delta.ReadValue();
#else
        return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
#endif
    }

    bool ZoomInHeld()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.wKey.isPressed;
#else
        return Input.GetKey(KeyCode.W);
#endif
    }

    bool ZoomOutHeld()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.sKey.isPressed;
#else
        return Input.GetKey(KeyCode.S);
#endif
    }

    float ClampAngle(float ag, float min, float max)
    {
        if (ag < -360)
            ag += 360;
        if (ag > 360)
            ag -= 360;
        return Mathf.Clamp(ag, min, max);
    }
}
