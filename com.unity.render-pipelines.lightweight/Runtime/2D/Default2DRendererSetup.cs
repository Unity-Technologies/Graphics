using UnityEngine.Rendering.LWRP;

namespace UnityEngine.Experimental.Rendering.LWRP
{
    internal class _2DRenderer : ScriptableRenderer
    {
        Default2DRendererData m_RendererData;
        Render2DLightingPass m_Render2DLightingPass;

        // TODO: Create intermediate color texture as needed.

        public _2DRenderer(Default2DRendererData data) : base(data)
        {
            m_RendererData = data;
            m_Render2DLightingPass = new Render2DLightingPass(data);
        }

        public override void Setup(ref RenderingData renderingData)
        {
            EnqueuePass(m_Render2DLightingPass);
        }
    }
}
