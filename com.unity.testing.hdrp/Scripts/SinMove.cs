using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SinMove : MonoBehaviour
{
    [SerializeField] bool localSpace = false;

    [SerializeField] Vector3 vector = Vector3.right;
    [SerializeField] float frequency = 1f;
    [SerializeField] float fps = 60;

    Vector3 startPosition = Vector3.zero;
    int localFrameCount = 0;

    // Use this for initialization
    void Start()
    {
        startPosition = transform.position;
        localFrameCount = 0;
    }

    // Update is called once per frame
    void Update()
    {
        localFrameCount++;
        transform.position = startPosition + Mathf.Sin(Mathf.PI * frequency * localFrameCount / fps) * (localSpace ? transform.TransformDirection(vector) : vector);
    }
}
