using UnityEngine;
using UnityEngine.Rendering;

public class SetCameraDrsFilter : MonoBehaviour
{
    public DynamicResUpscaleFilter DrsFilter = DynamicResUpscaleFilter.Bilinear;
    
    private Camera m_Camera = null;

    void Start()
    {
        m_Camera = GetComponentInParent<Camera>();
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
    }

    void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        if (m_Camera == null || camera != m_Camera)
            return;

        DynamicResolutionHandler.instance.filter = DrsFilter;
    }
}
