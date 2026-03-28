using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class VariousEffectsScene : MonoBehaviour {

    public Transform[] m_effects;
    public GameObject scaleform;
    public  GameObject[] m_destroyObjects = new GameObject[30];
    public GameObject FriendlyEnemyObject;
    GameObject gm;
    public  int inputLocation;
    public Text m_scalefactor;
    public static float m_gaph_scenesizefactor = 1;
    public Text m_effectName;
    int index = 0;

    void Awake()
    {
        inputLocation = 0;
        m_effectName.text = m_effects[index].name.ToString();
        MakeObject();

    }

	void Update ()
    {
        InputKey();
        if (index < 70)
            FriendlyEnemyObject.SetActive(false);
        else
            FriendlyEnemyObject.SetActive(true);

    }

    void InputKey()
    {
        if (PrevEffectPressed())
        {
            if (index <= 0)
                index = m_effects.Length - 1;
            else
                index--;

            MakeObject();
        }

        if (NextEffectPressed())
        {
            if (index >= m_effects.Length-1)
                index = 0;
            else
                index++;

            MakeObject();
        }

        if (RespawnEffectPressed())
            MakeObject();
    }

    void MakeObject()
    {
        DestroyGameObject();
        gm = Instantiate(m_effects[index],
            m_effects[index].transform.position,
            m_effects[index].transform.rotation).gameObject;
        m_effectName.text = (index+1) +" : "+m_effects[index].name.ToString();
        scaleform.transform.position = gm.transform.position;
        gm.transform.parent = scaleform.transform;
        gm.transform.localScale = new Vector3(1,1,1);
        float submit_scalefactor = m_gaph_scenesizefactor;
        if (index < 70)
            submit_scalefactor *= 0.5f;
        gm.transform.localScale = new Vector3(submit_scalefactor, submit_scalefactor, submit_scalefactor);
        m_destroyObjects[inputLocation] = gm;
        inputLocation++;
    }

    void DestroyGameObject()
    {
        for(int i = 0; i < inputLocation; i++)
        {
            Destroy(m_destroyObjects[i]);
        }
        inputLocation = 0;
    }

    public void GetSizeFactor()
    {
        m_gaph_scenesizefactor = float.Parse(m_scalefactor.text.ToString());
        float submit_scalefactor = m_gaph_scenesizefactor;
        if (index < 70)
            submit_scalefactor *= 0.5f;
        gm.transform.localScale = new Vector3(submit_scalefactor, submit_scalefactor, submit_scalefactor);
    }

    bool PrevEffectPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.zKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Z);
#endif
    }

    bool NextEffectPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.xKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.X);
#endif
    }

    bool RespawnEffectPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.C);
#endif
    }
}
