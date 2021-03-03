using System.Collections.Generic;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Universal
{
    public class DBufferRenderPass : ScriptableRenderPass
    {
        public static readonly RenderQueueRange k_RenderQueue_AllOpaque = new RenderQueueRange { lowerBound = (int)RenderQueue.Geometry, upperBound = (int)RenderQueue.GeometryLast };

        FilteringSettings m_FilteringSettings;
        ProfilingSampler m_ProfilingSampler;
        List<ShaderTagId> m_ShaderTagIdList = new List<ShaderTagId>();

        public DecalDrawIntoDBufferSystem m_DecalDrawIntoDBufferSystem;

        ProfilingSampler renderIntoDBuffer;

        public void SetDetphState(bool writeEnabled, CompareFunction function = CompareFunction.Less)
        {
            m_RenderStateBlock.mask |= RenderStateMask.Depth;
            m_RenderStateBlock.depthState = new DepthState(writeEnabled, function);
        }

        public void SetStencilState(int reference, CompareFunction compareFunction, StencilOp passOp, StencilOp failOp, StencilOp zFailOp)
        {
            StencilState stencilState = StencilState.defaultValue;
            stencilState.enabled = true;
            stencilState.SetCompareFunction(compareFunction);
            stencilState.SetPassOperation(passOp);
            stencilState.SetFailOperation(failOp);
            stencilState.SetZFailOperation(zFailOp);

            m_RenderStateBlock.mask |= RenderStateMask.Stencil;
            m_RenderStateBlock.stencilReference = reference;
            m_RenderStateBlock.stencilState = stencilState;
        }

        RenderStateBlock m_RenderStateBlock;

        public DBufferRenderPass(string profilerTag, RenderPassEvent renderPassEvent, string[] shaderTags)
        {
            this.renderPassEvent = RenderPassEvent.AfterRenderingPrePasses + 1;

            m_ProfilingSampler = new ProfilingSampler(profilerTag);

            RenderQueueRange renderQueueRange = k_RenderQueue_AllOpaque;
            m_FilteringSettings = new FilteringSettings(renderQueueRange, -1);

            if (shaderTags != null && shaderTags.Length > 0)
            {
                foreach (var passName in shaderTags)
                    m_ShaderTagIdList.Add(new ShaderTagId(passName));
            }

            // Require depth
            this.ConfigureInput(ScriptableRenderPassInput.Depth);

            m_RenderStateBlock = new RenderStateBlock(RenderStateMask.Everything);
            m_RenderStateBlock.depthState = new DepthState(true, CompareFunction.Always);

            renderIntoDBuffer = new ProfilingSampler("V1.DecalSystem.RenderIntoDBuffer");
        }

        static string[] s_DBufferNames = { "_DBufferTexture0", "_DBufferTexture1", "_DBufferTexture2", "_DBufferTexture3" };
        static string s_DBufferDepthName = "DBufferDepth";

        static GraphicsFormat[] m_RTFormat = { GraphicsFormat.R8G8B8A8_SRGB, GraphicsFormat.R8G8B8A8_UNorm, GraphicsFormat.R8G8B8A8_UNorm, GraphicsFormat.R8G8_UNorm };

        static public void GetMaterialDBufferDescription(out GraphicsFormat[] RTFormat)
        {
            RTFormat = m_RTFormat;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            bool use4RTs = false;

            int dBufferCount = use4RTs ? 4 : 3;

            for (int dbufferIndex = 0; dbufferIndex < dBufferCount; ++dbufferIndex)
            {
                var desc = renderingData.cameraData.cameraTargetDescriptor;
                desc.graphicsFormat = m_RTFormat[dbufferIndex];
                desc.depthBufferBits = 0;

                cmd.GetTemporaryRT(Shader.PropertyToID(s_DBufferNames[dbufferIndex]), desc);
            }

            // for alpha compositing, color is cleared to 0, alpha to 1
            // https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html

            // this clears the targets
            // TODO: Once we move to render graph, move this to render targets initialization parameters and remove rtHandles parameters
            Color clearColor = new Color(0.0f, 0.0f, 0.0f, 1.0f);
            Color clearColorNormal = new Color(0.5f, 0.5f, 0.5f, 1.0f); // for normals 0.5 is neutral
            Color clearColorAOSBlend = new Color(1.0f, 1.0f, 1.0f, 1.0f);

            var colorAttachments = new RenderTargetIdentifier[dBufferCount];
            for (int dbufferIndex = 0; dbufferIndex < dBufferCount; ++dbufferIndex)
            {
                colorAttachments[dbufferIndex] = new RenderTargetIdentifier(s_DBufferNames[dbufferIndex]);
            }

            var depthDesc = renderingData.cameraData.cameraTargetDescriptor;
            depthDesc.graphicsFormat = GraphicsFormat.DepthAuto;
            depthDesc.depthBufferBits = 24;

            cmd.GetTemporaryRT(Shader.PropertyToID(s_DBufferDepthName), depthDesc);

            ConfigureTarget(colorAttachments, new RenderTargetIdentifier(s_DBufferDepthName));
            ConfigureClear(ClearFlag.Color, new Color(0, 0, 0, 1));
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            SortingCriteria sortingCriteria = renderingData.cameraData.defaultOpaqueSortFlags;

            DrawingSettings drawingSettings = CreateDrawingSettings(m_ShaderTagIdList, ref renderingData, sortingCriteria);

            // NOTE: Do NOT mix ProfilingScope with named CommandBuffers i.e. CommandBufferPool.Get("name").
            // Currently there's an issue which results in mismatched markers.
            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                cmd.EnableShaderKeyword("_DECAL");

                float width = renderingData.cameraData.pixelWidth;
                float height = renderingData.cameraData.pixelHeight;
                cmd.SetGlobalVector("_ScreenSize", new Vector4(width, height, 1f / width, 1f / height));

                if (m_DecalDrawIntoDBufferSystem == null)
                {
                    using (new ProfilingScope(cmd, renderIntoDBuffer))
                    {
                        DecalSystem.instance.RenderIntoDBuffer(cmd);
                    }
                }

                if (m_DecalDrawIntoDBufferSystem != null)
                    m_DecalDrawIntoDBufferSystem.Execute(cmd);

                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref m_FilteringSettings, ref m_RenderStateBlock);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            bool use4RTs = false;

            int dBufferCount = use4RTs ? 4 : 3;

            for (int dbufferIndex = 0; dbufferIndex < dBufferCount; ++dbufferIndex)
            {
                cmd.ReleaseTemporaryRT(Shader.PropertyToID(s_DBufferNames[dbufferIndex]));
            }

            cmd.ReleaseTemporaryRT(Shader.PropertyToID(s_DBufferDepthName));
        }
    }
}
