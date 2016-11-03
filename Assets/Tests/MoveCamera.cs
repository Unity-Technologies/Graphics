using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveCamera : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        Vector3 newPos = transform.position;
        newPos.x = -3.0f + (0.5f + Mathf.Sin(Time.realtimeSinceStartup * 0.4f) * 0.5f) * (-40.0f + 3.0f);
        transform.position = newPos;
	}
}
