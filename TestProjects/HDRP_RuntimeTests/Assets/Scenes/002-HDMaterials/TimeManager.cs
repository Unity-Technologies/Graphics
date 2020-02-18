using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// [ExecuteAlways]
public class TimeManager : MonoBehaviour
{
    public Material[]   materials;
    public Wave         wave;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float t = (float)Time.frameCount / 60.0f;

        foreach (var mat in materials)
            mat.SetFloat("_CustomTime", t);

        wave.time = t;
    }
}
