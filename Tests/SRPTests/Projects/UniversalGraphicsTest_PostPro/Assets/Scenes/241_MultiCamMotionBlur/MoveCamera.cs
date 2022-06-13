using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR;

public class MoveCamera : MonoBehaviour
{
    [SerializeField] Vector3 m_Direction = Vector3.zero;
    [SerializeField] Vector3 m_Rotate = Vector3.zero;
    bool toogle = true;
    // Start is called before the first frame update
    void Start()
    {
        RenderPipelineManager.beginContextRendering += OnBeginContextRendering;
    }

    void OnBeginContextRendering(ScriptableRenderContext context, List<Camera> cameras)
    {
        var rot = transform.rotation;
        if (XRSettings.isDeviceActive)
        {
            toogle = !toogle;
            if (toogle) return;
            transform.position += m_Direction;
            rot.eulerAngles += m_Rotate;
        }
        else
        {
            transform.position += m_Direction;
            rot.eulerAngles += m_Rotate;
        }
        transform.rotation = rot;
    }

    void OnDestroy()
    {
        RenderPipelineManager.beginContextRendering -= OnBeginContextRendering;
    }
}
