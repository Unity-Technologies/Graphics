using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    class SharedRTManager
    {
        // The render target used when we do not support MSAA
        RTHandle m_NormalRT = null;
        RTHandle m_MotionVectorsRT = null;
        RTHandle m_CameraDepthStencilBuffer = null;
        // Needed in case passes will need to read stencil per pixel rather than per sample
        // The best we can do for resolve is an OR of all samples, however this is inaccurate by nature.
        RTHandle m_StencilBufferResolved;
        RTHandle m_CameraDepthBufferMipChain;
        RTHandle m_CameraHalfResDepthBuffer = null;
        HDUtils.PackedMipChainInfo m_CameraDepthBufferMipChainInfo; // This is metadata

        // The two render targets that should be used when we render in MSAA
        RTHandle m_NormalMSAART = null;
        RTHandle m_MotionVectorsMSAART = null;
        // This texture must be used because reading directly from an MSAA Depth buffer is way to expensive. The solution that we went for is writing the depth in an additional color buffer (10x cheaper to solve on ps4)
        RTHandle m_DepthAsColorMSAART = null;
        RTHandle m_CameraDepthStencilMSAABuffer;
        // This texture stores a set of depth values that are required for evaluating a bunch of effects in MSAA mode (R = Samples Max Depth, G = Samples Min Depth, G =  Samples Average Depth)
        RTHandle m_CameraDepthValuesBuffer = null;

        // Buffer used for quad overshading and vertex density debug modes
        // Should be a texture but metal doesn't support texture atomics
        ComputeBuffer m_FullScreenDebugBuffer = null;

        ComputeBuffer m_CoarseStencilBuffer = null;
        RTHandle m_DecalPrePassBuffer = null;
        RTHandle m_DecalPrePassBufferMSAA = null;

        // MSAA resolve materials
        Material m_DepthResolveMaterial  = null;
        Material m_ColorResolveMaterial = null;
        Material m_MotionVectorResolve = null;

        // Flags that defines if we are using a local texture or external
        bool m_ReuseGBufferMemory = false;
        bool m_MotionVectorsSupport = false;
        bool m_MSAASupported = false;
        bool m_DecalLayersSupported;
        MSAASamples m_MSAASamples = MSAASamples.None;

        // Arrays of RTIDs that are used to set render targets (when MSAA and when not MSAA)
        // Note: we have two set because as we can have both MRT for deferred and MRT for forward
        // at the same time in a pass, so we can't reuse the same array in this case
        // Firt set is used for Forward case
        protected RenderTargetIdentifier[] m_RTIDs1 = new RenderTargetIdentifier[1];
        protected RenderTargetIdentifier[] m_RTIDs2 = new RenderTargetIdentifier[2];
        protected RenderTargetIdentifier[] m_RTIDs3 = new RenderTargetIdentifier[3];
        protected RenderTargetIdentifier[] m_RTIDs4 = new RenderTargetIdentifier[4];
        // Second set is use for Deferred case
        protected RenderTargetIdentifier[] m_RTIDs1Deferred = new RenderTargetIdentifier[1];

        // Property block used for the resolves
        MaterialPropertyBlock m_PropertyBlock = new MaterialPropertyBlock();

        public SharedRTManager()
        {
        }

        public void InitSharedBuffers(GBufferManager gbufferManager, RenderPipelineSettings settings, RenderPipelineResources resources)
        {
            // Set the flags
            m_MSAASupported = settings.supportMSAA && settings.supportedLitShaderMode != RenderPipelineSettings.SupportedLitShaderMode.DeferredOnly;
            m_MSAASamples = m_MSAASupported ? settings.msaaSampleCount : MSAASamples.None;
            m_MotionVectorsSupport = settings.supportMotionVectors;
            m_ReuseGBufferMemory = settings.supportedLitShaderMode != RenderPipelineSettings.SupportedLitShaderMode.ForwardOnly;
            m_DecalLayersSupported = settings.supportDecals && settings.supportDecalLayers;

            // Create the depth/stencil buffer
            m_CameraDepthStencilBuffer = RTHandles.Alloc(Vector2.one, TextureXR.slices, DepthBits.Depth32, dimension: TextureXR.dimension, useDynamicScale: true, name: "CameraDepthStencil");

            // Create the mip chain buffer
            m_CameraDepthBufferMipChainInfo = new HDUtils.PackedMipChainInfo();
            m_CameraDepthBufferMipChainInfo.Allocate();
            m_CameraDepthBufferMipChain = RTHandles.Alloc(ComputeDepthBufferMipChainSize, TextureXR.slices, colorFormat: GraphicsFormat.R32_SFloat, dimension: TextureXR.dimension, enableRandomWrite: true, useDynamicScale: true, name: "CameraDepthBufferMipChain");

            if(settings.lowresTransparentSettings.enabled)
            {
                // Create the half res depth buffer used for low resolution transparency
                m_CameraHalfResDepthBuffer = RTHandles.Alloc(Vector2.one * 0.5f, TextureXR.slices, DepthBits.Depth32, dimension: TextureXR.dimension, useDynamicScale: true, name: "LowResDepthBuffer");
            }

            if (m_MotionVectorsSupport)
            {
                m_MotionVectorsRT = RTHandles.Alloc(Vector2.one, TextureXR.slices, colorFormat: Builtin.GetMotionVectorFormat(), dimension: TextureXR.dimension, useDynamicScale: true, name: "MotionVectors");
                if (m_MSAASupported)
                {
                    m_MotionVectorsMSAART = RTHandles.Alloc(Vector2.one, TextureXR.slices, colorFormat: Builtin.GetMotionVectorFormat(), dimension: TextureXR.dimension, enableMSAA: true, bindTextureMS: true, useDynamicScale: true, name: "MotionVectorsMSAA");
                }
            }

            // Allocate the additional textures only if MSAA is supported
            if (m_MSAASupported)
            {
                // Let's create the MSAA textures
                m_CameraDepthStencilMSAABuffer = RTHandles.Alloc(Vector2.one, TextureXR.slices, DepthBits.Depth24, dimension: TextureXR.dimension, bindTextureMS: true, enableMSAA: true, useDynamicScale: true, name: "CameraDepthStencilMSAA");
                m_CameraDepthValuesBuffer = RTHandles.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R32G32B32A32_SFloat, dimension: TextureXR.dimension, useDynamicScale: true, name: "DepthValuesBuffer");
                m_DepthAsColorMSAART = RTHandles.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R32_SFloat, dimension: TextureXR.dimension, bindTextureMS: true, enableMSAA: true, useDynamicScale: true, name: "DepthAsColorMSAA");
                m_StencilBufferResolved = RTHandles.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R8G8_UInt, dimension: TextureXR.dimension, enableRandomWrite: true, useDynamicScale: true, name: "StencilBufferResolved");

                // We need to allocate this texture as long as msaa is supported because on both mode, one of the cameras can be forward only using the framesettings
                m_NormalMSAART = RTHandles.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R8G8B8A8_UNorm, dimension: TextureXR.dimension, enableMSAA: true, bindTextureMS: true, useDynamicScale: true, name: "NormalBufferMSAA");

                // Create the required resolve materials
                m_DepthResolveMaterial = CoreUtils.CreateEngineMaterial(resources.shaders.depthValuesPS);
                m_ColorResolveMaterial = CoreUtils.CreateEngineMaterial(resources.shaders.colorResolvePS);
                m_MotionVectorResolve = CoreUtils.CreateEngineMaterial(resources.shaders.resolveMotionVecPS);

                if (m_DecalLayersSupported)
                    m_DecalPrePassBufferMSAA = RTHandles.Alloc(Vector2.one, TextureXR.slices, dimension: TextureXR.dimension, colorFormat: GraphicsFormat.R8G8B8A8_UNorm, enableMSAA: true, useDynamicScale: true, name: "Decal PrePass Buffer MSAA");

                CoreUtils.SetKeyword(m_DepthResolveMaterial, "_HAS_MOTION_VECTORS", m_MotionVectorsSupport);
            }

            // TODO: try to save this memory allocation. We can't reuse GBuffer for now as it require an additional clear before the GBuffer pass, otherwise the buffer can contain garbage that can be misinterpreted
            // if forward object are render (see test in HDRP_Test DecalNormalPatch buffer)
            if (m_DecalLayersSupported)
                m_DecalPrePassBuffer = RTHandles.Alloc(Vector2.one, TextureXR.slices, dimension: TextureXR.dimension, colorFormat: GraphicsFormat.R8G8B8A8_UNorm, useDynamicScale: true, name: "Decal PrePass Buffer");

            // If we are in the forward only mode
            if (!m_ReuseGBufferMemory)
            {
                // In case of full forward we must allocate the render target for normal buffer (or reuse one already existing)
                // TODO: Provide a way to reuse a render target
                m_NormalRT = RTHandles.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R8G8B8A8_UNorm, dimension: TextureXR.dimension, enableRandomWrite: true, useDynamicScale: true, name: "NormalBuffer");
            }
            else
            {
                // When not forward only we should are using the normal buffer of the gbuffer
                // In case of deferred, we must be in sync with NormalBuffer.hlsl and lit.hlsl files and setup the correct buffers
                m_NormalRT = gbufferManager.GetNormalBuffer(0); // Normal + Roughness
            }
        }

        public bool IsConsolePlatform()
        {
            return SystemInfo.graphicsDeviceType == GraphicsDeviceType.PlayStation4 ||
                SystemInfo.graphicsDeviceType == GraphicsDeviceType.XboxOne ||
                SystemInfo.graphicsDeviceType == GraphicsDeviceType.XboxOneD3D12 ||
                SystemInfo.graphicsDeviceType == GraphicsDeviceType.GameCoreXboxOne ||
                SystemInfo.graphicsDeviceType == GraphicsDeviceType.GameCoreXboxSeries;
        }

        // Function that will return the set of buffers required for the prepass (depending on if msaa is enabled or not)
        public RenderTargetIdentifier[] GetDepthPrepassForwardRTI(FrameSettings frameSettings)
        {
            // Note: Hardware allow to not write in all RT bind only if all the previous RT have been written (i.e there is no bubble)
            // So here we need to guarantee that all RTs but last are written by the shader.
            // Material could enable decal or not in Material so it need to be the last target
            using (ListPool<RenderTargetIdentifier>.Get(out var mrts))
            {
                if (frameSettings.IsEnabled(FrameSettingsField.MSAA))
                {
                    Debug.Assert(m_MSAASupported);
                    mrts.Add(m_DepthAsColorMSAART.nameID);
                    mrts.Add(m_NormalMSAART.nameID);
                    if (frameSettings.IsEnabled(FrameSettingsField.DecalLayers))
                        mrts.Add(m_DecalPrePassBufferMSAA);
                }
                else
                {
                    mrts.Add(m_NormalRT.nameID);
                    if (frameSettings.IsEnabled(FrameSettingsField.DecalLayers))
                        mrts.Add(m_DecalPrePassBuffer);
                }

                switch (mrts.Count)
                {
                    case 1:
                        mrts.CopyTo(m_RTIDs1);
                        return m_RTIDs1;
                    case 2:
                        mrts.CopyTo(m_RTIDs2);
                        return m_RTIDs2;
                    case 3:
                        mrts.CopyTo(m_RTIDs3);
                        return m_RTIDs3;
                    default:
                        return null;
                }
            }
        }

        // Function that will return the set of buffers required for the prepass (depending on if msaa is enabled or not)
        public RenderTargetIdentifier[] GetDepthPrepassDeferredRTI(FrameSettings frameSettings)
        {
            // In deferred we did nothing if decal aren't enabled
            if (!frameSettings.IsEnabled(FrameSettingsField.DecalLayers))
                return null;

            // Note: In deferred we can't have MSAA
            m_RTIDs1Deferred[0] = m_DecalPrePassBuffer.nameID;
            return m_RTIDs1Deferred;
        }

        // Function that will return the set of buffers required for the motion vector pass
        public RenderTargetIdentifier[] GetMotionVectorsPassRTI(FrameSettings frameSettings)
        {
            // Note: Hardware allow to not write in all RT bind only if all the previous RT have been written (i.e there is no bubble)
            // So here we need to guarantee that all RTs but last are written by the shader.
            // Material could enable decal or not in Material so it should be last target
            // However with motion vector pass, unlike for depth prepass we have no way to no if we are deferred (don't write normal buffer)
            // or forward (write normal buffer). As we have a single function to setup the target, we are forcing
            // disabled decals to still write 0 in DecalBuffer while render in the Motion vector pass
            // For this reasons we render normal last, as now decal is permanent
            Debug.Assert(m_MotionVectorsSupport);
            using (ListPool<RenderTargetIdentifier>.Get(out var mrts))
            {
                if (frameSettings.IsEnabled(FrameSettingsField.MSAA))
                {
                    Debug.Assert(m_MSAASupported);
                    mrts.Add(m_DepthAsColorMSAART.nameID);
                    mrts.Add(m_MotionVectorsMSAART.nameID);
                    if (frameSettings.IsEnabled(FrameSettingsField.DecalLayers))
                        mrts.Add(m_DecalPrePassBufferMSAA.nameID);
                    mrts.Add(m_NormalMSAART.nameID);
                }
                else
                {
                    mrts.Add(m_MotionVectorsRT.nameID);
                    if (frameSettings.IsEnabled(FrameSettingsField.DecalLayers))
                        mrts.Add(m_DecalPrePassBuffer.nameID);
                    mrts.Add(m_NormalRT.nameID);
                }

                switch (mrts.Count)
                {
                    case 1:
                        mrts.CopyTo(m_RTIDs1);
                        return m_RTIDs1;
                    case 2:
                        mrts.CopyTo(m_RTIDs2);
                        return m_RTIDs2;
                    case 3:
                        mrts.CopyTo(m_RTIDs3);
                        return m_RTIDs3;
                    case 4:
                        mrts.CopyTo(m_RTIDs4);
                        return m_RTIDs4;
                    default:
                        return null;
                }
            }
        }

        // Request the normal buffer (MSAA or not)
        public RTHandle GetDecalPrepassBuffer(bool isMSAA = false)
        {
            if (isMSAA)
            {
                Debug.Assert(m_MSAASupported);
                return m_DecalPrePassBufferMSAA;
            }
            else
            {
                return m_DecalPrePassBuffer;
            }
        }

        // Request the normal buffer (MSAA or not)
        public RTHandle GetNormalBuffer(bool isMSAA = false)
        {
            if (isMSAA)
            {
                Debug.Assert(m_MSAASupported);
                return m_NormalMSAART;
            }
            else
            {
                return m_NormalRT;
            }
        }

        // Request the motion vectors buffer (MSAA or not)
        public RTHandle GetMotionVectorsBuffer(bool isMSAA = false)
        {
            Debug.Assert(m_MotionVectorsSupport);
            if (isMSAA)
            {
                Debug.Assert(m_MSAASupported);
                return m_MotionVectorsMSAART;
            }
            else
            {
                return m_MotionVectorsRT;
            }
        }

        // Request the depth stencil buffer (MSAA or not)
        public RTHandle GetDepthStencilBuffer(bool isMSAA = false)
        {
            if (isMSAA)
            {
                Debug.Assert(m_MSAASupported);
                return m_CameraDepthStencilMSAABuffer;
            }
            else
            {
                return m_CameraDepthStencilBuffer;
            }
        }

        public RTHandle GetStencilBuffer(bool isMSAA = false)
        {
            if (isMSAA)
            {
                Debug.Assert(m_MSAASupported);
                return m_StencilBufferResolved;
            }
            else
            {
                return m_CameraDepthStencilBuffer;
            }
        }

        public ComputeBuffer GetCoarseStencilBuffer()
        {
            return m_CoarseStencilBuffer;
        }

        public RTHandle GetLowResDepthBuffer()
        {
            return m_CameraHalfResDepthBuffer;
        }

        // Request the depth texture (MSAA or not)
        public RTHandle GetDepthTexture(bool isMSAA = false)
        {
            if (isMSAA)
            {
                Debug.Assert(m_MSAASupported);
                return m_DepthAsColorMSAART;
            }
            else
            {
                return m_CameraDepthBufferMipChain;
            }
        }

        public RTHandle GetDepthValuesTexture()
        {
            Debug.Assert(m_MSAASupported);
            return m_CameraDepthValuesBuffer;
        }

        public ComputeBuffer GetFullScreenDebugBuffer()
        {
            return m_FullScreenDebugBuffer;
        }

        public void SetNumMSAASamples(MSAASamples msaaSamples)
        {
            m_MSAASamples = msaaSamples;
        }

        public Vector2Int ComputeDepthBufferMipChainSize(Vector2Int screenSize)
        {
            m_CameraDepthBufferMipChainInfo.ComputePackedMipChainInfo(screenSize);
            return m_CameraDepthBufferMipChainInfo.textureSize;
        }

        public HDUtils.PackedMipChainInfo GetDepthBufferMipChainInfo()
        {
            return m_CameraDepthBufferMipChainInfo;
        }

        public void Build(HDRenderPipelineAsset hdAsset)
        {
        }

        public void AllocateCoarseStencilBuffer(int width, int height, int viewCount)
        {
            if(width > 8 && height > 8)
                m_CoarseStencilBuffer = new ComputeBuffer(HDUtils.DivRoundUp(width, 8) * HDUtils.DivRoundUp(height, 8) * viewCount, sizeof(uint));
        }

        public void AllocateFullScreenDebugBuffer(int width, int height, int viewCount)
        {
            m_FullScreenDebugBuffer = new ComputeBuffer(width * height * viewCount, sizeof(uint));
        }

        public void DisposeCoarseStencilBuffer()
        {
            CoreUtils.SafeRelease(m_CoarseStencilBuffer);
        }

        public void DisposeFullScreenDebugBuffer()
        {
            CoreUtils.SafeRelease(m_FullScreenDebugBuffer);
        }

        public void Cleanup()
        {
            if (m_DecalLayersSupported)
                RTHandles.Release(m_DecalPrePassBuffer);

            if (!m_ReuseGBufferMemory)
            {
                RTHandles.Release(m_NormalRT);
            }

            if (m_MotionVectorsSupport)
            {
                RTHandles.Release(m_MotionVectorsRT);
                if (m_MSAASupported)
                {
                    RTHandles.Release(m_MotionVectorsMSAART);
                }
            }

            RTHandles.Release(m_CameraDepthStencilBuffer);
            RTHandles.Release(m_CameraDepthBufferMipChain);
            RTHandles.Release(m_CameraHalfResDepthBuffer);

            if (m_MSAASupported)
            {
                RTHandles.Release(m_CameraDepthStencilMSAABuffer);
                RTHandles.Release(m_CameraDepthValuesBuffer);
                RTHandles.Release(m_StencilBufferResolved);

                RTHandles.Release(m_NormalMSAART);
                RTHandles.Release(m_DepthAsColorMSAART);

                 // Do not forget to release the materials
                CoreUtils.Destroy(m_DepthResolveMaterial);
                CoreUtils.Destroy(m_ColorResolveMaterial);
                CoreUtils.Destroy(m_MotionVectorResolve);

                if (m_DecalLayersSupported)
                    RTHandles.Release(m_DecalPrePassBufferMSAA);
            }
        }

        public static int SampleCountToPassIndex(MSAASamples samples)
        {
            switch (samples)
            {
                case MSAASamples.None:
                    return 0;
                case MSAASamples.MSAA2x:
                    return 1;
                case MSAASamples.MSAA4x:
                    return 2;
                case MSAASamples.MSAA8x:
                    return 3;
            };
            return 0;
        }


        // Bind the normal buffer that is needed
        public void BindNormalBuffer(CommandBuffer cmd, bool isMSAA = false)
        {
            // NormalBuffer can be access in forward shader, so need to set global texture
            cmd.SetGlobalTexture(HDShaderIDs._NormalBufferTexture, GetNormalBuffer(isMSAA));
        }

        public void ResolveSharedRT(CommandBuffer cmd, HDCamera hdCamera)
        {
            if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.MSAA))
            {
                Debug.Assert(m_MSAASupported);
                using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.ResolveMSAADepth)))
                {
                    if (m_MotionVectorsSupport)
                    {
                        // Grab the RTIs and set the output render targets
                        m_RTIDs3[0] = m_CameraDepthValuesBuffer.nameID;
                        m_RTIDs3[1] = m_NormalRT.nameID;
                        m_RTIDs3[2] = m_MotionVectorsRT.nameID;
                        CoreUtils.SetRenderTarget(cmd, m_RTIDs3, m_CameraDepthStencilBuffer);

                        // Set the motion vector input texture
                        Shader.SetGlobalTexture(HDShaderIDs._MotionVectorTextureMS, m_MotionVectorsMSAART);
                    }
                    else
                    {
                        // Grab the RTIs and set the output render targets
                        m_RTIDs2[0] = m_CameraDepthValuesBuffer.nameID;
                        m_RTIDs2[1] = m_NormalRT.nameID;
                        CoreUtils.SetRenderTarget(cmd, m_RTIDs2, m_CameraDepthStencilBuffer);
                    }

                    // Set the depth and normal input textures
                    Shader.SetGlobalTexture(HDShaderIDs._NormalTextureMS, m_NormalMSAART);
                    Shader.SetGlobalTexture(HDShaderIDs._DepthTextureMS, m_DepthAsColorMSAART);

                    // Resolve the buffers
                    cmd.DrawProcedural(Matrix4x4.identity, m_DepthResolveMaterial, SampleCountToPassIndex(m_MSAASamples), MeshTopology.Triangles, 3, 1);
                }
            }
        }

        public void ResolveMotionVectorTexture(CommandBuffer cmd, HDCamera hdCamera)
        {
            if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.MSAA) && m_MotionVectorsSupport)
            {
                using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.ResolveMSAAMotionVector)))
                {
                    CoreUtils.SetRenderTarget(cmd, m_MotionVectorsRT);
                    Shader.SetGlobalTexture(HDShaderIDs._MotionVectorTextureMS, m_MotionVectorsMSAART);
                    cmd.DrawProcedural(Matrix4x4.identity, m_MotionVectorResolve, SampleCountToPassIndex(m_MSAASamples), MeshTopology.Triangles, 3, 1);
                }
            }
        }

        public void ResolveMSAAColor(CommandBuffer cmd, HDCamera hdCamera, RTHandle msaaTarget, RTHandle simpleTarget)
        {
            if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.MSAA))
            {
                Debug.Assert(m_MSAASupported);
                using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.ResolveMSAAColor)))
                {
                    // Grab the RTIs and set the output render targets
                    CoreUtils.SetRenderTarget(cmd, simpleTarget);

                    // Set the input textures
                    m_PropertyBlock.SetTexture(HDShaderIDs._ColorTextureMS, msaaTarget);

                    // Resolve the depth and normal buffers
                    cmd.DrawProcedural(Matrix4x4.identity, m_ColorResolveMaterial, SampleCountToPassIndex(m_MSAASamples), MeshTopology.Triangles, 3, 1, m_PropertyBlock);
                }
            }
        }
    }
}
