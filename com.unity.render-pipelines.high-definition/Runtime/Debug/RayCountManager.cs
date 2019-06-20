using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
#if ENABLE_RAYTRACING
    public class RayCountManager
    {
        // Ray count UAV
        RTHandleSystem.RTHandle m_RayCountTex = null;
        static Texture2D s_DebugFontTex = null;
        static ComputeBuffer s_TotalRayCountBuffer = null;

        // Material used to blit the output texture into the camera render target
        Material m_Blit;
        Material m_DrawRayCount;
        MaterialPropertyBlock m_DrawRayCountProperties = new MaterialPropertyBlock();
        // Raycount shader
        ComputeShader m_RayCountCompute;
        bool m_RayCountEnabled;

        int _TotalRayCountBuffer = Shader.PropertyToID("_TotalRayCountBuffer");
        int _FontColor = Shader.PropertyToID("_FontColor");
        
        public void Init(RenderPipelineResources renderPipelineResources)
        {
            m_Blit = CoreUtils.CreateEngineMaterial(renderPipelineResources.shaders.blitPS);
            m_DrawRayCount = CoreUtils.CreateEngineMaterial(renderPipelineResources.shaders.debugViewRayCountPS);
            m_RayCountCompute = renderPipelineResources.shaders.countTracedRays;
            s_DebugFontTex = renderPipelineResources.textures.debugFontTex;
            // UINT textures must use UINT32, since groupshared uint used to synchronize counts is allocated as a UINT32
            m_RayCountTex = RTHandles.Alloc(Vector2.one, filterMode: FilterMode.Point, colorFormat: GraphicsFormat.R32G32B32A32_UInt, enableRandomWrite: true, useMipMap: false, name: "RayCountTex");
            s_TotalRayCountBuffer = new ComputeBuffer(3, sizeof(uint));
        }

        public void Release()
        {
            CoreUtils.Destroy(m_Blit);
            CoreUtils.Destroy(m_DrawRayCount);

            RTHandles.Release(m_RayCountTex);
            CoreUtils.SafeRelease(s_TotalRayCountBuffer);
        }

        public RTHandleSystem.RTHandle rayCountTex
        {
            get
            {
                return m_RayCountTex;
            }
        }

        public int rayCountEnabled
        {
            get
            {
                return m_RayCountEnabled ? 1 : 0;
            }
        }

        public void ClearRayCount(CommandBuffer cmd, HDCamera camera)
        {
            if (m_RayCountEnabled)
            {
                int clearBufferKernelIdx = m_RayCountCompute.FindKernel("CS_Clear");
                cmd.SetComputeBufferParam(m_RayCountCompute, clearBufferKernelIdx, _TotalRayCountBuffer, s_TotalRayCountBuffer);
                cmd.DispatchCompute(m_RayCountCompute, clearBufferKernelIdx, 1, 1, 1);

                HDUtils.SetRenderTarget(cmd, camera, m_RayCountTex, ClearFlag.Color);
            }
        }

        public void Update(CommandBuffer cmd, HDCamera camera, bool rayCountEnabled)
        {
            m_RayCountEnabled = rayCountEnabled;
            ClearRayCount(cmd, camera);
        }

        public void RenderRayCount(CommandBuffer cmd, HDCamera camera, Color fontColor)
        {
            if (m_RayCountEnabled)
            {
                using (new ProfilingSample(cmd, "Raytracing Debug Overlay", CustomSamplerId.RaytracingDebug.GetSampler()))
                {
                    int width = camera.actualWidth;
                    int height = camera.actualHeight;

                    // Sum across all rays per pixel
                    int countKernelIdx = m_RayCountCompute.FindKernel("CS_CountRays");
                    uint groupSizeX = 0, groupSizeY = 0, groupSizeZ = 0;
                    m_RayCountCompute.GetKernelThreadGroupSizes(countKernelIdx, out groupSizeX, out groupSizeY, out groupSizeZ);
                    int dispatchWidth = 0, dispatchHeight = 0;
                    dispatchWidth = (int)((width + groupSizeX - 1) / groupSizeX);
                    dispatchHeight = (int)((height + groupSizeY - 1) / groupSizeY);
                    cmd.SetComputeTextureParam(m_RayCountCompute, countKernelIdx, HDShaderIDs._RayCountTexture, m_RayCountTex);
                    cmd.SetComputeBufferParam(m_RayCountCompute, countKernelIdx, _TotalRayCountBuffer, s_TotalRayCountBuffer);
                    cmd.DispatchCompute(m_RayCountCompute, countKernelIdx, dispatchWidth, dispatchHeight, 1);

                    // Draw overlay
                    m_DrawRayCountProperties.SetTexture(HDShaderIDs._DebugFont, s_DebugFontTex);
                    m_DrawRayCountProperties.SetColor(_FontColor, fontColor);
                    m_DrawRayCount.SetBuffer(_TotalRayCountBuffer, s_TotalRayCountBuffer);
                    CoreUtils.DrawFullScreen(cmd, m_DrawRayCount, m_DrawRayCountProperties);
                }
            }
        }
    }
#endif
}
