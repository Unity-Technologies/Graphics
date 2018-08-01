using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Loook at main camera.
/// </summary>

public class LookAt : MonoBehaviour {

    private Transform cam;

	// Use this for initialization
	void Start () {
        cam = Camera.main.transform;
	}
	
	// Update is called once per frame
	void Update () {
        transform.LookAt(cam);
	}
}
