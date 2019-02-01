using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetLayerDistance : MonoBehaviour
{
    public bool     useSphericalDistanceCulling = false;
    public float[]  layerCullDistances = new float[32];

    private Camera m_Camera;

    void OnEnable()
    {
        m_Camera = GetComponent<Camera>();
    }

    // Update is called once per frame
    void Update()
    {
        if (m_Camera != null)
        {
            m_Camera.layerCullSpherical = useSphericalDistanceCulling;
            m_Camera.layerCullDistances = layerCullDistances;
        }
    }
}
