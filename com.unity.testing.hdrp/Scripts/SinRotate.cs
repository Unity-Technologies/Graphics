using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SinRotate : MonoBehaviour
{
    [SerializeField] bool localSpace = false;

    [SerializeField] Vector3 angles = new Vector3(45f, 0f, 0f);
    [SerializeField] float frequency = 1f;
    [SerializeField] float fps = 60;

    Vector3 startAngles = Vector3.zero;
    int localFrameCount = 0;

    // Use this for initialization
    void Start()
    {
        startAngles = transform.eulerAngles;
    }

    // Update is called once per frame
    void Update()
    {
        localFrameCount++;
        if (localSpace)
        {
            transform.eulerAngles = startAngles;
            transform.Rotate(angles * Mathf.Sin(Mathf.PI * frequency * localFrameCount / fps), Space.Self);
        }
        else
        {
            transform.eulerAngles = startAngles + Mathf.Sin(Mathf.PI * frequency * localFrameCount / fps) * angles;
        }
    }
}
