using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public static class RenderTargetHandle
    {
        public static int Color;
        public static int Depth;
        public static int DepthMS;
        public static int OpaqueColor;
        public static int DirectionalShadowmap;
        public static int LocalShadowmap;
        public static int ScreenSpaceOcclusion;
    }


    public class ForwardRenderer
    {
        private Dictionary<int, RenderTargetIdentifier> m_ResourceMap = new Dictionary<int, RenderTargetIdentifier>();
        private List<ScriptableRenderPass> m_Passes = new List<ScriptableRenderPass>();
        private List<ScriptableRenderPass> m_Graph = new List<ScriptableRenderPass>();

        public ForwardRenderer()
        {
            // RenderTexture format depends on camera and pipeline (HDR, non HDR, etc)
            // Samples (MSAA) depend on camera and pipeline
            RenderTargetHandle.Color = Shader.PropertyToID("_CameraColorTexture");
            RenderTargetHandle.Depth = Shader.PropertyToID("_CameraDepthTexture");
            RenderTargetHandle.DepthMS = Shader.PropertyToID("_CameraDepthMSTexture");
            RenderTargetHandle.OpaqueColor = Shader.PropertyToID("_CameraOpaqueTexture");
            RenderTargetHandle.DirectionalShadowmap = Shader.PropertyToID("_DirectionalShadowmapTexture");
            RenderTargetHandle.LocalShadowmap = Shader.PropertyToID("_LocalShadowmapTexture");
            RenderTargetHandle.ScreenSpaceOcclusion = Shader.PropertyToID("_ScreenSpaceShadowMapTexture");

            m_ResourceMap.Add(RenderTargetHandle.Color, new RenderTargetIdentifier(RenderTargetHandle.Color));
            m_ResourceMap.Add(RenderTargetHandle.Depth, new RenderTargetIdentifier(RenderTargetHandle.Depth));
            m_ResourceMap.Add(RenderTargetHandle.DepthMS, new RenderTargetIdentifier(RenderTargetHandle.DepthMS));
            m_ResourceMap.Add(RenderTargetHandle.OpaqueColor, new RenderTargetIdentifier(RenderTargetHandle.OpaqueColor));
            m_ResourceMap.Add(RenderTargetHandle.DirectionalShadowmap, new RenderTargetIdentifier(RenderTargetHandle.DirectionalShadowmap));
            m_ResourceMap.Add(RenderTargetHandle.LocalShadowmap, new RenderTargetIdentifier(RenderTargetHandle.LocalShadowmap));
            m_ResourceMap.Add(RenderTargetHandle.ScreenSpaceOcclusion, new RenderTargetIdentifier(RenderTargetHandle.ScreenSpaceOcclusion));
        }

        public void AddPass(ScriptableRenderPass pass)
        {
            m_Passes.Add(pass);
        }

        public List<ScriptableRenderPass> BuildRenderGraph()
        {
            m_Graph.Clear();
            return m_Graph;
        }

        public RenderTargetIdentifier GetSurface(int handle)
        {
            return m_ResourceMap[handle];
        }
    }
}
