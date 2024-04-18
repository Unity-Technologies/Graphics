using UnityEngine;
using UnityEngine.Rendering.Universal;

// This Renderer Feature sets up the BlitToRTHandlePass pass.
public class BlitToRTHandleRendererFeature : ScriptableRendererFeature
{
    private BlitToRTHandlePass m_CopyColorPass;
    private RenderPassEvent m_CopyColorEvent = RenderPassEvent.AfterRenderingTransparents;
    public Material blitMaterial;

    public override void Create()
    {
        m_CopyColorPass = new BlitToRTHandlePass(m_CopyColorEvent, blitMaterial);
    }
    
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType != CameraType.Game)
            return;
        
        renderer.EnqueuePass(m_CopyColorPass);
    }

    protected override void Dispose(bool disposing)
    {
        m_CopyColorPass?.Dispose();
        m_CopyColorPass = null;
    }
}