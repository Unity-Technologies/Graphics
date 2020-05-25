using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RapidEyeMovement : MonoBehaviour
{
	public static GameObject[]	lights;
	public float rotationSpeed = 1;
	public Vector2 rotationRate = new Vector2(0.2f,2f); //Per second
	public float angleSpread = 45; //Per second
	private float currentRotationRate;
	private float lastRotationTime = 0;
	private Vector3 initialRotation;
	private Quaternion lastRotation;
	private Quaternion newRotation;
	
    // Start is called before the first frame update
    void Start()
    {
		initialRotation = this.transform.localEulerAngles;
        lastRotationTime = 0;
		currentRotationRate = Random.Range(rotationRate.x, rotationRate.y);
		
		if (lights == null)
            lights = GameObject.FindGameObjectsWithTag("Light");

    }

    // Update is called once per frame
    void Update()
    {
        if(Time.realtimeSinceStartup - lastRotationTime > currentRotationRate){
			currentRotationRate = Random.Range(rotationRate.x, rotationRate.y);
			lastRotationTime = Time.realtimeSinceStartup;
			lastRotation = this.transform.rotation;
			Vector3 newRot = Vector3.zero;
			newRot.x = initialRotation.x + Random.Range(-1f, 1f) * angleSpread;
			newRot.y = initialRotation.y + Random.Range(-1f, 1f) * angleSpread;
			newRot.z = 0;//initialRotation.z + Random.Range(-1f, 1f) * angleSpread;
			newRotation = Quaternion.Euler(newRot.x, newRot.y, newRot.z);
			
		}
		
		transform.rotation = Quaternion.Slerp (transform.rotation, newRotation, Time.deltaTime * rotationSpeed);
		
		// Debug.Log(lights.Length);
		foreach (GameObject light in lights)
        {
            Vector3 lightDirection = light.transform.forward;
            Vector3 eyeDirection = this.transform.forward;
			Vector3 position = this.transform.position - light.transform.position;
			float dot1 = Mathf.Clamp01(Vector3.Dot(lightDirection.normalized, eyeDirection.normalized));
			float dot2 = Mathf.Clamp01(Vector3.Dot(Vector3.forward, position.normalized));
			// Debug.Log(dot2);
			float dot = dot1 * dot2;
			// float d = map(dot,0f,1f,0.25f,1f);
			// float dot2 = Mathf.Abs(Vector3.Dot(v.normalized, light.transform.eulerAngles.normalized));
			this.transform.GetChild(0).GetComponent<Renderer>().material.SetFloat("PupilRadius", dot);
        }
    }
	
	float map(float s, float a1, float a2, float b1, float b2)
	{
		return b1 + (s-a1)*(b2-b1)/(a2-a1);
	}
}
