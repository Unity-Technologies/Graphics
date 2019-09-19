using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class Sin_ChangeFOV : MonoBehaviour
{
    [SerializeField] float min = 45f;
    [SerializeField] float max = 90f;

    [SerializeField] float frequency = 1f;
    [SerializeField] float fps = 60;

    Camera targetCamera;

    // Use this for initialization
    void Start()
    {
        targetCamera = GetComponent<Camera>();
    }

    // Update is called once per frame
    void Update()
    {
        targetCamera.fieldOfView = min + (max - min) * Mathf.Sin(Mathf.PI * frequency * Time.frameCount / fps);
    }
}
