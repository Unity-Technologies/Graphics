using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SinMove : MonoBehaviour
{
    [SerializeField] bool localSpace = false;

    [SerializeField] Vector3 vector = Vector3.right;
    [SerializeField] float frequency = 1f;

    Vector3 startPosition = Vector3.zero;

	// Use this for initialization
	void Start ()
    {
        startPosition = transform.position;
	}
	
	// Update is called once per frame
	void Update ()
    {
        transform.position = startPosition + Mathf.Sin(Mathf.PI * Time.time * frequency) * (localSpace?transform.TransformDirection(vector):vector);
	}
}
