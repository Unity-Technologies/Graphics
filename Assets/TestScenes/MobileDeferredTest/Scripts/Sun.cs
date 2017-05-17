using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sun : MonoBehaviour {

	public Transform eyePosition;
	Light sunlight;

	public Color daytimeSkyColor = new Color(0.31f, 0.88f, 1f);
	public Color nighttimeSkyColor = new Color(0.04f, 0.19f, 0.27f);

	// Use this for initialization
	void Start () {
		sunlight = GetComponent<Light> ();
		sunlight.color = daytimeSkyColor;
	}

	public float radius = 6;
	public float daySeconds = 1200;
	public float speed = 0.01f;
	public float blend = 0.25f;

	private float timeAnim = 0;

	// Update is called once per frame
	void Update () {

		timeAnim = (timeAnim + speed * Time.deltaTime) % daySeconds;

		Vector3 midpoint = eyePosition.position; midpoint.y -= 0.5f;
		float sunangle = timeAnim * 360;

		sunlight.transform.position = midpoint + Quaternion.Euler(0,0,sunangle)*(radius*Vector3.right);
		sunlight.transform.LookAt (midpoint);

		//sunlight.color = Color.Lerp(daytimeSkyColor, nighttimeSkyColor, timeAnim/blend);

	}
}
