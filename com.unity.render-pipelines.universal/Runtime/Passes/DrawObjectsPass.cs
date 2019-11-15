using System.Collections.Generic;

namespace UnityEngine.Rendering.Universal.Internal
{
    /// <summary>
    /// Draw  objects into the given color and depth target
    ///
    /// You can use this pass to render objects that have a material and/or shader
    /// with the pass names UniversalForward or SRPDefaultUnlit.
    /// </summary>
    public class DrawObjectsPass : ScriptableRenderPass
    {
        FilteringSettings m_FilteringSettings;
        RenderStateBlock m_RenderStateBlock;
        List<ShaderTagId> m_ShaderTagIdList = new List<ShaderTagId>();
        string m_ProfilerTag;
        bool m_IsOpaque;

        public DrawObjectsPass(string profilerTag, bool opaque, RenderPassEvent evt, RenderQueueRange renderQueueRange, LayerMask layerMask, StencilState stencilState, int stencilReference)
        {
            m_ProfilerTag = profilerTag;
            m_ShaderTagIdList.Add(new ShaderTagId("UniversalForward"));
            m_ShaderTagIdList.Add(new ShaderTagId("LightweightForward"));
            m_ShaderTagIdList.Add(new ShaderTagId("SRPDefaultUnlit"));
            renderPassEvent = evt;
            m_FilteringSettings = new FilteringSettings(renderQueueRange, layerMask);
            m_RenderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);
            m_IsOpaque = opaque;

            if (stencilState.enabled)
            {
                m_RenderStateBlock.stencilReference = stencilReference;
                m_RenderStateBlock.mask = RenderStateMask.Stencil;
                m_RenderStateBlock.stencilState = stencilState;
            }
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);
            using (new ProfilingSample(cmd, m_ProfilerTag))
            {
                if (URPCameraMode.isPureURP)
                {
                    // XRTODO: Enable pure mode globally in UniversalRenderPipeline.cs
                    cmd.EnableShaderKeyword("UNITY_PURE_URP_ON");
                    ref CameraData cameraData = ref renderingData.cameraData;
                    if (isFinalPassToGameViewBackBuffer)
                    {
                        bool isFinalPassToGameViewBackBuffer = isFinalBackBufferWrite && cameraData.camera.targetTexture == null
                                           && !(cameraData.camera.cameraType == CameraType.SceneView || cameraData.camera.cameraType == CameraType.Preview);
                        Matrix4x4 projMatrix = GL.GetGPUProjectionMatrix(renderingData.cameraData.camera.projectionMatrix, !isFinalPassToGameViewBackBuffer);
                        Matrix4x4 viewMatrix = renderingData.cameraData.camera.worldToCameraMatrix;
                        Matrix4x4 viewProjMatrix = projMatrix * viewMatrix;
                        Matrix4x4 invViewProjMatrix = Matrix4x4.Inverse(viewProjMatrix);

                        cmd.SetGlobalMatrix(Shader.PropertyToID("_ViewMatrix"), viewMatrix);
                        cmd.SetGlobalMatrix(Shader.PropertyToID("_InvViewMatrix"), Matrix4x4.Inverse(viewMatrix));
                        cmd.SetGlobalMatrix(Shader.PropertyToID("_ProjMatrix"), projMatrix);
                        cmd.SetGlobalMatrix(Shader.PropertyToID("_InvProjMatrix"), Matrix4x4.Inverse(projMatrix));
                        cmd.SetGlobalMatrix(Shader.PropertyToID("_ViewProjMatrix"), viewProjMatrix);
                        cmd.SetGlobalMatrix(Shader.PropertyToID("_InvViewProjMatrix"), Matrix4x4.Inverse(viewProjMatrix));
                    }
                }

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                Camera camera = renderingData.cameraData.camera;
                var sortFlags = (m_IsOpaque) ? renderingData.cameraData.defaultOpaqueSortFlags : SortingCriteria.CommonTransparent;
                var drawSettings = CreateDrawingSettings(m_ShaderTagIdList, ref renderingData, sortFlags);
                context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref m_FilteringSettings, ref m_RenderStateBlock);

                // Render objects that did not match any shader pass with error shader
                RenderingUtils.RenderObjectsWithError(context, ref renderingData.cullResults, camera, m_FilteringSettings, SortingCriteria.None);

                if (URPCameraMode.isPureURP)
                {
                    // XRTODO: Remove this once pure mode is globally on 
                    cmd.DisableShaderKeyword("UNITY_PURE_URP_ON");
                }
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
