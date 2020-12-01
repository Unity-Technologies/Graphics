using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        RTHandle m_SSSColor;
        RTHandle m_SSSColorMSAA;
        bool m_SSSReuseGBufferMemory;

        // Disney SSS Model
        ComputeShader m_SubsurfaceScatteringCS;
        int m_SubsurfaceScatteringKernel;
        int m_SubsurfaceScatteringKernelMSAA;
        Material m_CombineLightingPass;
        // End Disney SSS Model

        // Need an extra buffer on some platforms
        RTHandle m_SSSCameraFilteringBuffer;

        // This is use to be able to read stencil value in compute shader
        Material m_SSSCopyStencilForSplitLighting;

        // List of every diffusion profile data we need
        Vector4[]                   m_SSSShapeParamsAndMaxScatterDists;
        Vector4[]                   m_SSSTransmissionTintsAndFresnel0;
        Vector4[]                   m_SSSDisabledTransmissionTintsAndFresnel0;
        Vector4[]                   m_SSSWorldScalesAndFilterRadiiAndThicknessRemaps;
        uint[]                      m_SSSDiffusionProfileHashes;
        int[]                       m_SSSDiffusionProfileUpdate;
        DiffusionProfileSettings[]  m_SSSSetDiffusionProfiles;
        DiffusionProfileSettings    m_SSSDefaultDiffusionProfile;
        int                         m_SSSActiveDiffusionProfileCount;
        uint                        m_SSSTexturingModeFlags;        // 1 bit/profile: 0 = PreAndPostScatter, 1 = PostScatter
        uint                        m_SSSTransmissionFlags;         // 1 bit/profile: 0 = regular, 1 = thin

        void InitSSSBuffers()
        {
            RenderPipelineSettings settings = asset.currentPlatformRenderPipelineSettings;

            if (settings.supportedLitShaderMode == RenderPipelineSettings.SupportedLitShaderMode.ForwardOnly) //forward only
            {
                // In case of full forward we must allocate the render target for forward SSS (or reuse one already existing)
                // TODO: Provide a way to reuse a render target
                m_SSSColor = RTHandles.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R8G8B8A8_SRGB, dimension: TextureXR.dimension, useDynamicScale: true, name: "SSSBuffer");
                m_SSSReuseGBufferMemory = false;
            }

            // We need to allocate the texture if we are in forward or both in case one of the cameras is in enable forward only mode
            if (settings.supportMSAA && settings.supportedLitShaderMode != RenderPipelineSettings.SupportedLitShaderMode.DeferredOnly)
            {
                 m_SSSColorMSAA = RTHandles.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R8G8B8A8_SRGB, dimension: TextureXR.dimension, enableMSAA: true, bindTextureMS: true, useDynamicScale: true, name: "SSSBufferMSAA");
            }

            if ((settings.supportedLitShaderMode & RenderPipelineSettings.SupportedLitShaderMode.DeferredOnly) != 0) //deferred or both
            {
                // In case of deferred, we must be in sync with SubsurfaceScattering.hlsl and lit.hlsl files and setup the correct buffers
                m_SSSColor = m_GbufferManager.GetSubsurfaceScatteringBuffer(0); // Note: This buffer must be sRGB (which is the case with Lit.shader)
                m_SSSReuseGBufferMemory = true;
            }

            if (NeedTemporarySubsurfaceBuffer() || settings.supportMSAA)
            {
                // Caution: must be same format as m_CameraSssDiffuseLightingBuffer
                m_SSSCameraFilteringBuffer = RTHandles.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.B10G11R11_UFloatPack32, dimension: TextureXR.dimension, enableRandomWrite: true, useDynamicScale: true, name: "SSSCameraFiltering"); // Enable UAV
            }
        }

        void DestroySSSBuffers()
        {
            RTHandles.Release(m_SSSColorMSAA);
            RTHandles.Release(m_SSSCameraFilteringBuffer);
            if (!m_SSSReuseGBufferMemory)
            {
                RTHandles.Release(m_SSSColor);
            }
        }

        RTHandle GetSSSBuffer()
        {
            return m_SSSColor;
        }

        RTHandle GetSSSBufferMSAA()
        {
            return m_SSSColorMSAA;
        }

        void InitializeSubsurfaceScattering()
        {
            // Disney SSS (compute + combine)
            string kernelName = "SubsurfaceScattering";
            m_SubsurfaceScatteringCS = defaultResources.shaders.subsurfaceScatteringCS;
            m_SubsurfaceScatteringKernel = m_SubsurfaceScatteringCS.FindKernel(kernelName);
            m_CombineLightingPass = CoreUtils.CreateEngineMaterial(defaultResources.shaders.combineLightingPS);
            m_CombineLightingPass.SetInt(HDShaderIDs._StencilRef, (int)StencilUsage.SubsurfaceScattering);
            m_CombineLightingPass.SetInt(HDShaderIDs._StencilMask, (int)StencilUsage.SubsurfaceScattering);

            m_SSSCopyStencilForSplitLighting = CoreUtils.CreateEngineMaterial(defaultResources.shaders.copyStencilBufferPS);
            m_SSSCopyStencilForSplitLighting.SetInt(HDShaderIDs._StencilRef, (int)StencilUsage.SubsurfaceScattering);
            m_SSSCopyStencilForSplitLighting.SetInt(HDShaderIDs._StencilMask, (int)StencilUsage.SubsurfaceScattering);

            m_SSSDefaultDiffusionProfile = defaultResources.assets.defaultDiffusionProfile;

            // fill the list with the max number of diffusion profile so we dont have
            // the error: exceeds previous array size (5 vs 3). Cap to previous size.
            m_SSSShapeParamsAndMaxScatterDists = new Vector4[DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT];
            m_SSSTransmissionTintsAndFresnel0 = new Vector4[DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT];
            m_SSSDisabledTransmissionTintsAndFresnel0 = new Vector4[DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT];
            m_SSSWorldScalesAndFilterRadiiAndThicknessRemaps = new Vector4[DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT];
            m_SSSDiffusionProfileHashes = new uint[DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT];
            m_SSSDiffusionProfileUpdate = new int[DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT];
            m_SSSSetDiffusionProfiles = new DiffusionProfileSettings[DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT];

            // If ray tracing is supported by the asset, do the initialization
            if (rayTracingSupported)
                InitializeSubsurfaceScatteringRT();
        }

        void CleanupSubsurfaceScattering()
        {
            if (rayTracingSupported)
                CleanupSubsurfaceScatteringRT();

            CoreUtils.Destroy(m_CombineLightingPass);
            CoreUtils.Destroy(m_SSSCopyStencilForSplitLighting);
            DestroySSSBuffers();
        }

        void UpdateCurrentDiffusionProfileSettings(HDCamera hdCamera)
        {
            var currentDiffusionProfiles = HDRenderPipeline.defaultAsset.diffusionProfileSettingsList;
            var diffusionProfileOverride = hdCamera.volumeStack.GetComponent<DiffusionProfileOverride>();

            // If there is a diffusion profile volume override, we merge diffusion profiles that are overwritten
            if (diffusionProfileOverride.active && diffusionProfileOverride.diffusionProfiles.value != null)
            {
                currentDiffusionProfiles = diffusionProfileOverride.diffusionProfiles.value;
            }

            // The first profile of the list is the default diffusion profile, used either when the diffusion profile
            // on the material isn't assigned or when the diffusion profile can't be displayed (too many on the frame)
            SetDiffusionProfileAtIndex(m_SSSDefaultDiffusionProfile, 0);
            m_SSSDiffusionProfileHashes[0] = DiffusionProfileConstants.DIFFUSION_PROFILE_NEUTRAL_ID;

            int i = 1;
            foreach (var v in currentDiffusionProfiles)
            {
                if (v == null)
                    continue;
                SetDiffusionProfileAtIndex(v, i++);
            }

            m_SSSActiveDiffusionProfileCount = i;
        }

        void SetDiffusionProfileAtIndex(DiffusionProfileSettings settings, int index)
        {
            // if the diffusion profile was already set and it haven't changed then there is nothing to upgrade
            if (m_SSSSetDiffusionProfiles[index] == settings && m_SSSDiffusionProfileUpdate[index] == settings.updateCount)
                return;

            // if the settings have not yet been initialized
            if (settings.profile.hash == 0)
                return;

            m_SSSShapeParamsAndMaxScatterDists[index]               = settings.shapeParamAndMaxScatterDist;
            m_SSSTransmissionTintsAndFresnel0[index]                = settings.transmissionTintAndFresnel0;
            m_SSSDisabledTransmissionTintsAndFresnel0[index]        = settings.disabledTransmissionTintAndFresnel0;
            m_SSSWorldScalesAndFilterRadiiAndThicknessRemaps[index] = settings.worldScaleAndFilterRadiusAndThicknessRemap;
            m_SSSDiffusionProfileHashes[index]                      = settings.profile.hash;

            // Erase previous value (This need to be done here individually as in the SSS editor we edit individual component)
            uint mask = 1u << index;
            m_SSSTexturingModeFlags &= ~mask;
            m_SSSTransmissionFlags &= ~mask;

            m_SSSTexturingModeFlags |= (uint)settings.profile.texturingMode    << index;
            m_SSSTransmissionFlags  |= (uint)settings.profile.transmissionMode << index;

            m_SSSSetDiffusionProfiles[index] = settings;
            m_SSSDiffusionProfileUpdate[index] = settings.updateCount;
        }

        unsafe void UpdateShaderVariablesGlobalSubsurface(ref ShaderVariablesGlobal cb, HDCamera hdCamera)
        {
            UpdateCurrentDiffusionProfileSettings(hdCamera);

            cb._DiffusionProfileCount = (uint)m_SSSActiveDiffusionProfileCount;
            cb._EnableSubsurfaceScattering = hdCamera.frameSettings.IsEnabled(FrameSettingsField.SubsurfaceScattering) ? 1u : 0u;
            cb._TexturingModeFlags = m_SSSTexturingModeFlags;
            cb._TransmissionFlags = m_SSSTransmissionFlags;

            for (int i = 0; i < m_SSSActiveDiffusionProfileCount; ++i)
            {
                for (int c = 0; c < 4; ++c) // Vector4 component
                {
                    cb._ShapeParamsAndMaxScatterDists[i * 4 + c] = m_SSSShapeParamsAndMaxScatterDists[i][c];
                    // To disable transmission, we simply nullify the transmissionTint
                    cb._TransmissionTintsAndFresnel0[i * 4 + c] = hdCamera.frameSettings.IsEnabled(FrameSettingsField.Transmission) ? m_SSSTransmissionTintsAndFresnel0[i][c] : m_SSSDisabledTransmissionTintsAndFresnel0[i][c];
                    cb._WorldScalesAndFilterRadiiAndThicknessRemaps[i * 4 + c] = m_SSSWorldScalesAndFilterRadiiAndThicknessRemaps[i][c];
                }

                cb._DiffusionProfileHashTable[i * 4] = m_SSSDiffusionProfileHashes[i];
            }
        }

        static bool NeedTemporarySubsurfaceBuffer()
        {
            // Caution: need to be in sync with SubsurfaceScattering.cs USE_INTERMEDIATE_BUFFER (Can't make a keyword as it is a compute shader)
            // Typed UAV loads from FORMAT_R16G16B16A16_FLOAT is an optional feature of Direct3D 11.
            // Most modern GPUs support it. We can avoid performing a costly copy in this case.
            // TODO: test/implement for other platforms.
            return (SystemInfo.graphicsDeviceType != GraphicsDeviceType.PlayStation4 &&
                SystemInfo.graphicsDeviceType != GraphicsDeviceType.XboxOne &&
                SystemInfo.graphicsDeviceType != GraphicsDeviceType.XboxOneD3D12);
        }

        struct SubsurfaceScatteringParameters
        {
            public ComputeShader    subsurfaceScatteringCS;
            public int              subsurfaceScatteringCSKernel;
            public int              sampleBudget;
            public bool             needTemporaryBuffer;
            public Material         copyStencilForSplitLighting;
            public Material         combineLighting;
            public int              numTilesX;
            public int              numTilesY;
            public int              numTilesZ;
        }

        struct SubsurfaceScatteringResources
        {
            public RTHandle colorBuffer;
            public RTHandle diffuseBuffer;
            public RTHandle depthStencilBuffer;
            public RTHandle depthTexture;

            public RTHandle cameraFilteringBuffer;
            public ComputeBuffer coarseStencilBuffer;
            public RTHandle sssBuffer;
        }

        SubsurfaceScatteringParameters PrepareSubsurfaceScatteringParameters(HDCamera hdCamera)
        {
            var parameters = new SubsurfaceScatteringParameters();

            parameters.subsurfaceScatteringCS = m_SubsurfaceScatteringCS;
            parameters.subsurfaceScatteringCS.shaderKeywords = null;

            if(hdCamera.frameSettings.IsEnabled(FrameSettingsField.MSAA))
            {
                m_SubsurfaceScatteringCS.EnableKeyword("ENABLE_MSAA");
            }

            parameters.subsurfaceScatteringCSKernel = m_SubsurfaceScatteringKernel;
            parameters.needTemporaryBuffer = NeedTemporarySubsurfaceBuffer() || hdCamera.frameSettings.IsEnabled(FrameSettingsField.MSAA);
            parameters.copyStencilForSplitLighting = m_SSSCopyStencilForSplitLighting;
            parameters.combineLighting = m_CombineLightingPass;
            parameters.numTilesX = ((int)hdCamera.screenSize.x + 15) / 16;
            parameters.numTilesY = ((int)hdCamera.screenSize.y + 15) / 16;
            parameters.numTilesZ = hdCamera.viewCount;
            parameters.sampleBudget = hdCamera.frameSettings.sssResolvedSampleBudget;

            return parameters;
        }


        void RenderSubsurfaceScattering(HDCamera hdCamera, CommandBuffer cmd, RTHandle colorBufferRT,
            RTHandle diffuseBufferRT, RTHandle depthStencilBufferRT, RTHandle depthTextureRT, RTHandle normalBuffer)
        {
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.SubsurfaceScattering))
                return;

            BuildCoarseStencilAndResolveIfNeeded(hdCamera, cmd, resolveOnly:false);

            var settings = hdCamera.volumeStack.GetComponent<SubSurfaceScattering>();

            // If ray tracing is enabled for the camera, if the volume override is active and if the RAS is built, we want to do ray traced SSS
            if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing) && settings.rayTracing.value && GetRayTracingState() && hdCamera.frameSettings.IsEnabled(FrameSettingsField.SubsurfaceScattering))
            {
                RenderSubsurfaceScatteringRT(hdCamera, cmd, colorBufferRT, diffuseBufferRT, depthStencilBufferRT, normalBuffer);
            }
            else
            {
                using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.SubsurfaceScattering)))
                {
                    var parameters = PrepareSubsurfaceScatteringParameters(hdCamera);
                    var resources = new SubsurfaceScatteringResources();
                    resources.colorBuffer = colorBufferRT;
                    resources.diffuseBuffer = diffuseBufferRT;
                    resources.depthStencilBuffer = depthStencilBufferRT;
                    resources.depthTexture = depthTextureRT;
                    resources.cameraFilteringBuffer = m_SSSCameraFilteringBuffer;
                    resources.coarseStencilBuffer = m_SharedRTManager.GetCoarseStencilBuffer();
                    resources.sssBuffer = m_SSSColor;

                    // For Jimenez we always need an extra buffer, for Disney it depends on platform
                    if (parameters.needTemporaryBuffer)
                    {
                        // Clear the SSS filtering target
                        using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.ClearSSSFilteringTarget)))
                        {
                            CoreUtils.SetRenderTarget(cmd, m_SSSCameraFilteringBuffer, ClearFlag.Color, Color.clear);
                        }
                    }

                    RenderSubsurfaceScattering(parameters, resources, cmd);
                }
            }
        }


        // Combines specular lighting and diffuse lighting with subsurface scattering.
        // In the case our frame is MSAA, for the moment given the fact that we do not have read/write access to the stencil buffer of the MSAA target; we need to keep this pass MSAA
        // However, the compute can't output and MSAA target so we blend the non-MSAA target into the MSAA one.
        static void RenderSubsurfaceScattering(in SubsurfaceScatteringParameters parameters, in SubsurfaceScatteringResources resources, CommandBuffer cmd)
        {
            cmd.SetComputeIntParam(parameters.subsurfaceScatteringCS, HDShaderIDs._SssSampleBudget, parameters.sampleBudget);

            cmd.SetComputeTextureParam(parameters.subsurfaceScatteringCS, parameters.subsurfaceScatteringCSKernel, HDShaderIDs._DepthTexture, resources.depthTexture);
            cmd.SetComputeTextureParam(parameters.subsurfaceScatteringCS, parameters.subsurfaceScatteringCSKernel, HDShaderIDs._IrradianceSource, resources.diffuseBuffer);
            cmd.SetComputeTextureParam(parameters.subsurfaceScatteringCS, parameters.subsurfaceScatteringCSKernel, HDShaderIDs._SSSBufferTexture, resources.sssBuffer);

            cmd.SetComputeBufferParam(parameters.subsurfaceScatteringCS, parameters.subsurfaceScatteringCSKernel, HDShaderIDs._CoarseStencilBuffer, resources.coarseStencilBuffer);

            if (parameters.needTemporaryBuffer)
            {
                cmd.SetComputeTextureParam(parameters.subsurfaceScatteringCS, parameters.subsurfaceScatteringCSKernel, HDShaderIDs._CameraFilteringBuffer, resources.cameraFilteringBuffer);

                // Perform the SSS filtering pass
                cmd.DispatchCompute(parameters.subsurfaceScatteringCS, parameters.subsurfaceScatteringCSKernel, parameters.numTilesX, parameters.numTilesY, parameters.numTilesZ);

                parameters.combineLighting.SetTexture(HDShaderIDs._IrradianceSource, resources.cameraFilteringBuffer);

                // Additively blend diffuse and specular lighting into the color buffer.
                HDUtils.DrawFullScreen(cmd, parameters.combineLighting, resources.colorBuffer, resources.depthStencilBuffer);
            }
            else
            {
                cmd.SetComputeTextureParam(parameters.subsurfaceScatteringCS, parameters.subsurfaceScatteringCSKernel, HDShaderIDs._CameraColorTexture, resources.colorBuffer);

                // Perform the SSS filtering pass which performs an in-place update of 'colorBuffer'.
                cmd.DispatchCompute(parameters.subsurfaceScatteringCS, parameters.subsurfaceScatteringCSKernel, parameters.numTilesX, parameters.numTilesY, parameters.numTilesZ);
            }
        }
    }
}
