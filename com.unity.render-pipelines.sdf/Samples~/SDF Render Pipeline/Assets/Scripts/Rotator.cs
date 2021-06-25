using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotator : MonoBehaviour
{
    public float xAxisSpeed = 1.0f;
    public float yAxisSpeed = 1.0f;
    public float zAxisSpeed = 1.0f;

    // Update is called once per frame
    void Update()
    {
        Vector3 euler = this.transform.eulerAngles;
        euler.x += (xAxisSpeed * Time.deltaTime);
        euler.y += (yAxisSpeed * Time.deltaTime);
        euler.z += (zAxisSpeed * Time.deltaTime);

        this.transform.rotation = Quaternion.Euler(euler);
    }
}
