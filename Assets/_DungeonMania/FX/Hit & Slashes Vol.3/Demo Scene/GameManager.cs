using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour {
	public TextMesh text_fx_name;
	public GameObject[] fx_prefabs;
	public int index_fx = 0;
	private Ray ray;
	private RaycastHit2D ray_cast_hit;

	void Start () 
	{
		if (fx_prefabs == null || fx_prefabs.Length == 0)
		{
			if (text_fx_name != null) text_fx_name.text = "[0] (no FX assigned)";
			return;
		}
		index_fx = Mathf.Clamp(index_fx, 0, fx_prefabs.Length - 1);
		text_fx_name.text = "[" + (index_fx + 1) + "] " + fx_prefabs[ index_fx ].name;
	}

	void Update () 
	{
		if (fx_prefabs == null || fx_prefabs.Length == 0)
			return;

		if (Input.GetMouseButtonDown (0)) 
		{
			ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			ray_cast_hit = Physics2D.Raycast(new Vector2(ray.origin.x, ray.origin.y), new Vector2(0,0));
			if (ray_cast_hit) 
			{
				switch(ray_cast_hit.transform.name){
				case "BG":
					Instantiate(fx_prefabs[ index_fx ], new Vector3(ray.origin.x, ray.origin.y, 0), Quaternion.identity);
					break;
				case "UI-arrow-right":
					ray_cast_hit.transform.SendMessage("Go");
					index_fx++;
					if(index_fx >= fx_prefabs.Length)
						index_fx = 0;
					index_fx = Mathf.Clamp(index_fx, 0, fx_prefabs.Length - 1);
					text_fx_name.text = "[" + (index_fx + 1) + "] " + fx_prefabs[ index_fx ].name;
					break;
				case "UI-arrow-left":
					ray_cast_hit.transform.SendMessage("Go");
					index_fx--;
					if(index_fx <= -1)
						index_fx = fx_prefabs.Length - 1;
					index_fx = Mathf.Clamp(index_fx, 0, fx_prefabs.Length - 1);
					text_fx_name.text = "[" + (index_fx + 1) + "] " + fx_prefabs[ index_fx ].name;
					break;
				case "Instructions":
					Destroy(ray_cast_hit.transform.gameObject);
					break;
				}
			}
		}
		//Change-FX keyboard..	
		if ( Input.GetKeyDown("z") || Input.GetKeyDown("left") ){
			var left = GameObject.Find("UI-arrow-left");
			if (left != null) left.SendMessage("Go");
			index_fx--;
			if(index_fx <= -1)
				index_fx = fx_prefabs.Length - 1;
			index_fx = Mathf.Clamp(index_fx, 0, fx_prefabs.Length - 1);
			text_fx_name.text = "[" + (index_fx + 1) + "] " + fx_prefabs[ index_fx ].name;	
		}

		if ( Input.GetKeyDown("x") || Input.GetKeyDown("right")){
			var right = GameObject.Find("UI-arrow-right");
			if (right != null) right.SendMessage("Go");
			index_fx++;
			if(index_fx >= fx_prefabs.Length)
				index_fx = 0;
			index_fx = Mathf.Clamp(index_fx, 0, fx_prefabs.Length - 1);
			text_fx_name.text = "[" + (index_fx + 1) + "] " + fx_prefabs[ index_fx ].name;
		}

		if ( Input.GetKeyDown("space") ){
			Instantiate(fx_prefabs[ index_fx ], new Vector3(0, 0, 0), Quaternion.identity);	
		}
	}

}
