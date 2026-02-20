using UnityEngine;
using System.Collections;

public class Balls_Moving_SC : MonoBehaviour {

    public float minMaxDistance = 3.5f;
    public float distPerSec = 2.5f;


    float mult = 1.0f;
    Vector3 stPos;
	// Use this for initialization
	void Start () 
    {
        stPos = this.gameObject.transform.position;
	}
	
	// Update is called once per frame
	void Update () 
    {
        float tDist = (this.gameObject.transform.position - stPos).magnitude;
        if (tDist > minMaxDistance)
        {
            if (mult > 0.0f)
            {
                mult = -1.0f;
            }
            else { mult = 1.0f; };
        }


        this.gameObject.transform.Translate(0.0f, 0.0f, mult * Time.deltaTime * distPerSec);
	}
}
