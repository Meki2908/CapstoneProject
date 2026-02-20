using UnityEngine;
using UnityEngine.UI;

public class SetMenuParts : MonoBehaviour
{
    public string[] menuParts;

    void Start ()
    {
        menuParts = LoadTextFiles.Load("testXML",'/');
        int i = 0;
        foreach (Transform child in transform)
        {
            Text textPart = child.GetChild(0).GetComponent<Text>();
            textPart.text = menuParts[i];
            i++;
        }
    }
}
