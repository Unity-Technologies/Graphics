using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class AllocRTHandleInCreate_ScriptableRendererFeature : ScriptableRendererFeature
{
    class AllocRTHandleInCreate_ScriptableRenderPass : ScriptableRenderPass
    {
        RTHandle m_RTHandle;

        public AllocRTHandleInCreate_ScriptableRenderPass()
        {
            RTHandleAllocInfo allocInfo = new RTHandleAllocInfo("DummyScriptableRendererPass RTHandle");
            m_RTHandle = RTHandles.Alloc(Vector2.one, allocInfo);
        }

        public void Dispose()
        {
            m_RTHandle.Release();
        }
    }

    AllocRTHandleInCreate_ScriptableRenderPass m_Pass;

    public override void Create()
    {
        m_Pass = new AllocRTHandleInCreate_ScriptableRenderPass();
    }

    protected override void Dispose(bool disposing)
    {
        m_Pass?.Dispose();
        m_Pass = null;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
    }
}
