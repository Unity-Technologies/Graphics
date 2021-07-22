using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;


[RequireComponent(typeof(Camera))]
[ExecuteAlways]
public class CameraMipBias : MonoBehaviour
{
    public float MipBias = 0.0f;
    private HDAdditionalCameraData m_camData = null;

    public void Awake()
    {
        if (m_camData != null)
            return;

        m_camData = GetComponentInParent<HDAdditionalCameraData>();
    }

    public void Update()
    {
        if (m_camData == null)
            return;

        m_camData.materialMipBias = MipBias;
    }
}
