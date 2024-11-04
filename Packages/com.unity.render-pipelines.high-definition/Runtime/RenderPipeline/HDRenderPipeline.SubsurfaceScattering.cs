using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        // Disney SSS Model
        ComputeShader m_SubsurfaceScatteringCS;
        ComputeShader m_SubsurfaceScatteringDownsampleCS;
        int m_SubsurfaceScatteringKernel;
        int m_PackDiffusionProfileKernel;
        int m_SubsurfaceScatteringKernelMSAA;
        int m_SubsurfaceScatteringDownsampleKernel;
        Material m_CombineLightingPass;
        // End Disney SSS Model

        // This is use to be able to read stencil value in compute shader
        Material m_SSSCopyStencilForSplitLighting;

        // List of every diffusion profile data we need
        Vector4[] m_SSSShapeParamsAndMaxScatterDists;
        Vector4[] m_SSSTransmissionTintsAndFresnel0;
        Vector4[] m_SSSDisabledTransmissionTintsAndFresnel0;
        Vector4[] m_SSSWorldScalesAndFilterRadiiAndThicknessRemaps;
        Vector4[] m_SSSDualLobeAndDiffusePower;
        Vector4[] m_SSSBorderAttenuationColor;
        uint[] m_SSSDiffusionProfileHashes;
        int[] m_SSSDiffusionProfileUpdate;
        DiffusionProfileSettings[] m_SSSSetDiffusionProfiles;
        DiffusionProfileSettings m_SSSDefaultDiffusionProfile;
        int m_SSSActiveDiffusionProfileCount;
        uint m_SSSTexturingModeFlags;        // 1 bit/profile: 0 = PreAndPostScatter, 1 = PostScatter
        uint m_SSSTransmissionFlags;         // 1 bit/profile: 0 = regular, 1 = thin

        void InitializeSubsurfaceScattering()
        {
            // Disney SSS (compute + combine)
            string kernelName = "SubsurfaceScattering";
            m_SubsurfaceScatteringCS = runtimeShaders.subsurfaceScatteringCS;
            m_SubsurfaceScatteringKernel = m_SubsurfaceScatteringCS.FindKernel(kernelName);
            m_PackDiffusionProfileKernel = m_SubsurfaceScatteringCS.FindKernel("PackDiffusionProfile");

            m_SubsurfaceScatteringDownsampleCS = runtimeShaders.subsurfaceScatteringDownsampleCS;
            m_SubsurfaceScatteringDownsampleKernel = m_SubsurfaceScatteringDownsampleCS.FindKernel("Downsample");
            m_CombineLightingPass = CoreUtils.CreateEngineMaterial(runtimeShaders.combineLightingPS);
            m_CombineLightingPass.SetInt(HDShaderIDs._StencilRef, (int)StencilUsage.SubsurfaceScattering);
            m_CombineLightingPass.SetInt(HDShaderIDs._StencilMask, (int)StencilUsage.SubsurfaceScattering);

            m_SSSCopyStencilForSplitLighting = CoreUtils.CreateEngineMaterial(runtimeShaders.copyStencilBufferPS);
            m_SSSCopyStencilForSplitLighting.SetInt(HDShaderIDs._StencilRef, (int)StencilUsage.SubsurfaceScattering);
            m_SSSCopyStencilForSplitLighting.SetInt(HDShaderIDs._StencilMask, (int)StencilUsage.SubsurfaceScattering);

            m_SSSDefaultDiffusionProfile = runtimeAssets.defaultDiffusionProfile;

            // fill the list with the max number of diffusion profile so we dont have
            // the error: exceeds previous array size (5 vs 3). Cap to previous size.
            m_SSSShapeParamsAndMaxScatterDists = new Vector4[DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT];
            m_SSSTransmissionTintsAndFresnel0 = new Vector4[DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT];
            m_SSSDisabledTransmissionTintsAndFresnel0 = new Vector4[DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT];
            m_SSSWorldScalesAndFilterRadiiAndThicknessRemaps = new Vector4[DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT];
            m_SSSDualLobeAndDiffusePower = new Vector4[DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT];
            m_SSSBorderAttenuationColor = new Vector4[DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT];
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
        }

        void UpdateCurrentDiffusionProfileSettings(HDCamera hdCamera)
        {
            // The first profile of the list is the default diffusion profile, used either when the diffusion profile
            // on the material isn't assigned or when the diffusion profile can't be displayed (too many on the frame)
            SetDiffusionProfileAtIndex(m_SSSDefaultDiffusionProfile, 0);
            m_SSSDiffusionProfileHashes[0] = DiffusionProfileConstants.DIFFUSION_PROFILE_NEUTRAL_ID;

            int profileCount = 1;
            var diffusionProfiles = hdCamera.volumeStack.GetComponent<DiffusionProfileList>().diffusionProfiles;
            if (diffusionProfiles.value != null)
            {
                profileCount = diffusionProfiles.accumulatedCount;
                for (int i = 1; i < diffusionProfiles.accumulatedCount; i++)
                    SetDiffusionProfileAtIndex(diffusionProfiles.value[i], i);
            }

            m_SSSActiveDiffusionProfileCount = profileCount;
        }

        void SetDiffusionProfileAtIndex(DiffusionProfileSettings settings, int index)
        {
            // if the diffusion profile was already set and it haven't changed then there is nothing to upgrade
            if (m_SSSSetDiffusionProfiles[index] == settings && m_SSSDiffusionProfileUpdate[index] == settings.updateCount)
                return;

            // if the settings have not yet been initialized
            if (settings.profile.hash == 0)
                return;

            m_SSSShapeParamsAndMaxScatterDists[index] = settings.shapeParamAndMaxScatterDist;
            m_SSSTransmissionTintsAndFresnel0[index] = settings.transmissionTintAndFresnel0;
            m_SSSDisabledTransmissionTintsAndFresnel0[index] = settings.disabledTransmissionTintAndFresnel0;
            m_SSSWorldScalesAndFilterRadiiAndThicknessRemaps[index] = settings.worldScaleAndFilterRadiusAndThicknessRemap;
            m_SSSDualLobeAndDiffusePower[index] = settings.dualLobeAndDiffusePower;
            m_SSSBorderAttenuationColor[index] = settings.borderAttenuationColor;
            m_SSSDiffusionProfileHashes[index] = settings.profile.hash;

            // Erase previous value (This need to be done here individually as in the SSS editor we edit individual component)
            uint mask = 1u << index;
            m_SSSTexturingModeFlags &= ~mask;
            m_SSSTransmissionFlags &= ~mask;

            m_SSSTexturingModeFlags |= (uint)settings.profile.texturingMode << index;
            m_SSSTransmissionFlags |= (uint)settings.profile.transmissionMode << index;

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
                    cb._DualLobeAndDiffusePower[i * 4 + c] = m_SSSDualLobeAndDiffusePower[i][c];
                    cb._BorderAttenuationColor[i * 4 + c] = m_SSSBorderAttenuationColor[i][c];
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
                SystemInfo.graphicsDeviceType != GraphicsDeviceType.PlayStation5 &&
                SystemInfo.graphicsDeviceType != GraphicsDeviceType.PlayStation5NGGC &&
                SystemInfo.graphicsDeviceType != GraphicsDeviceType.XboxOne &&
                SystemInfo.graphicsDeviceType != GraphicsDeviceType.XboxOneD3D12 &&
                SystemInfo.graphicsDeviceType != GraphicsDeviceType.GameCoreXboxOne &&
                SystemInfo.graphicsDeviceType != GraphicsDeviceType.GameCoreXboxSeries);
        }

        // Albedo + SSS Profile and mask / Specular occlusion (when no SSS)
        // This will be used during GBuffer and/or forward passes.
        TextureHandle CreateSSSBuffer(RenderGraph renderGraph, HDCamera hdCamera, MSAASamples msaaSamples)
        {
            bool msaa = msaaSamples != MSAASamples.None;
#if UNITY_2020_2_OR_NEWER
            FastMemoryDesc fastMemDesc;
            fastMemDesc.inFastMemory = true;
            fastMemDesc.residencyFraction = 1.0f;
            fastMemDesc.flags = FastMemoryFlags.SpillTop;
#endif

            return renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
            {
                format = GraphicsFormat.R8G8B8A8_SRGB,
                enableRandomWrite = !msaa,
                bindTextureMS = msaa,
                msaaSamples = msaaSamples,
                clearBuffer = NeedClearGBuffer(hdCamera),
                clearColor = Color.clear,
                name = msaa ? "SSSBufferMSAA" : "SSSBuffer"
#if UNITY_2020_2_OR_NEWER
                , fastMemoryDesc = fastMemDesc
#endif
            });
        }

        class SubsurfaceScaterringPassData
        {
            public ComputeShader subsurfaceScatteringCS;
            public ComputeShader subsurfaceScatteringDownsampleCS;
            public int subsurfaceScatteringCSKernel;
            public int packDiffusionProfileKernel;
            public int subsurfaceScatteringDownsampleCSKernel;
            public int sampleBudget;
            public int downsampleSteps;
            public bool needTemporaryBuffer;
            public Material copyStencilForSplitLighting;
            public Material combineLighting;
            public int numTilesX;
            public int numTilesY;
            public int numTilesZ;
            public bool useOcclusion;
            public Vector2 viewportSize;

            public TextureHandle colorBuffer;
            public TextureHandle diffuseBuffer;
            public TextureHandle depthStencilBuffer;
            public TextureHandle depthTexture;
            public TextureHandle cameraFilteringBuffer;
            public TextureHandle downsampleBuffer;
            public TextureHandle sssBuffer;
            public TextureHandle diffusionProfileIndex;
            public BufferHandle  coarseStencilBuffer;
        }

        TextureHandle RenderSubsurfaceScatteringScreenSpace(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle colorBuffer, in LightingBuffers lightingBuffers, ref PrepassOutput prepassOutput)
        {
            BuildCoarseStencilAndResolveIfNeeded(renderGraph, hdCamera, resolveOnly: false, ref prepassOutput);

            TextureHandle depthStencilBuffer = prepassOutput.depthBuffer;
            TextureHandle depthTexture = prepassOutput.depthPyramidTexture;

            using (var builder = renderGraph.AddRenderPass<SubsurfaceScaterringPassData>("Subsurface Scattering", out var passData, ProfilingSampler.Get(HDProfileId.SubsurfaceScattering)))
            {
                passData.useOcclusion = currentAsset.currentPlatformRenderPipelineSettings.subsurfaceScatteringAttenuation;

                CoreUtils.SetKeyword(m_SubsurfaceScatteringCS, "ENABLE_MSAA", hdCamera.msaaEnabled);
                CoreUtils.SetKeyword(m_SubsurfaceScatteringCS, "USE_SSS_OCCLUSION", passData.useOcclusion);

                passData.subsurfaceScatteringCS = m_SubsurfaceScatteringCS;
                passData.subsurfaceScatteringCSKernel = m_SubsurfaceScatteringKernel;
                passData.packDiffusionProfileKernel = m_PackDiffusionProfileKernel;
                passData.subsurfaceScatteringDownsampleCS = m_SubsurfaceScatteringDownsampleCS;
                passData.subsurfaceScatteringDownsampleCSKernel = m_SubsurfaceScatteringDownsampleKernel;
                passData.needTemporaryBuffer = NeedTemporarySubsurfaceBuffer() || hdCamera.msaaEnabled;
                passData.copyStencilForSplitLighting = m_SSSCopyStencilForSplitLighting;
                passData.combineLighting = m_CombineLightingPass;
                passData.numTilesX = ((int)hdCamera.screenSize.x + 15) / 16;
                passData.numTilesY = ((int)hdCamera.screenSize.y + 15) / 16;
                passData.numTilesZ = hdCamera.viewCount;
                passData.sampleBudget = hdCamera.frameSettings.sssResolvedSampleBudget;
                passData.downsampleSteps = hdCamera.frameSettings.sssResolvedDownsampleSteps;

                passData.colorBuffer = builder.WriteTexture(colorBuffer);
                passData.diffuseBuffer = builder.ReadTexture(lightingBuffers.diffuseLightingBuffer);
                passData.depthStencilBuffer = builder.ReadTexture(depthStencilBuffer);
                passData.depthTexture = builder.ReadTexture(depthTexture);
                passData.sssBuffer = builder.ReadTexture(lightingBuffers.sssBuffer);
                passData.coarseStencilBuffer = builder.ReadBuffer(prepassOutput.coarseStencilBuffer);

                if (passData.useOcclusion)
                {
                    passData.diffusionProfileIndex = builder.CreateTransientTexture(new TextureDesc(new Vector2(0.5f, 1f), true, true)
                    {
                       colorFormat = GraphicsFormat.R8_UInt,
                       enableRandomWrite = true,
                       clearBuffer = false,
                       name = "Packed Diffusion Profile Index",
                    });
                    passData.viewportSize = hdCamera.screenSize;
                }

                if (passData.downsampleSteps > 0)
                {
                    float scale = 1.0f / (1u << passData.downsampleSteps);
                    passData.downsampleBuffer = builder.CreateTransientTexture(
                        new TextureDesc(Vector2.one * scale, true, true)
                        { format = GraphicsFormat.B10G11R11_UFloatPack32, enableRandomWrite = true, clearBuffer = true, clearColor = Color.clear, name = "SSSDownsampled" });
                }

                if (passData.needTemporaryBuffer)
                {
                    passData.cameraFilteringBuffer = builder.CreateTransientTexture(
                        new TextureDesc(Vector2.one, true, true)
                        { format = GraphicsFormat.B10G11R11_UFloatPack32, enableRandomWrite = true, clearBuffer = true, clearColor = Color.clear, name = "SSSCameraFiltering" });
                }
                else
                {
                    // We need to set this as otherwise it will still be using an handle that is potentially coming from another rendergraph execution.
                    // For example if we have two cameras, if  NeedTemporarySubsurfaceBuffer() is false, but one camera has MSAA and one hasn't, only one camera
                    // will have passData.parameters.needTemporaryBuffer true and the other that doesn't, without explicit setting to null handle will try to use handle of the other camera.
                    passData.cameraFilteringBuffer = TextureHandle.nullHandle;
                }

                builder.SetRenderFunc(
                    (SubsurfaceScaterringPassData data, RenderGraphContext ctx) =>
                    {
                        CoreUtils.SetKeyword(ctx.cmd, "USE_DOWNSAMPLE", data.downsampleSteps > 0);

                        if (data.downsampleSteps > 0)
                        {
                            // The downsample workgroup size is half of the subsurface scattering main pass
                            int shift = data.downsampleSteps - 1;

                            ctx.cmd.SetComputeIntParam(data.subsurfaceScatteringDownsampleCS, HDShaderIDs._SssDownsampleSteps, data.downsampleSteps);
                            ctx.cmd.SetComputeTextureParam(data.subsurfaceScatteringDownsampleCS, data.subsurfaceScatteringDownsampleCSKernel, HDShaderIDs._SourceTexture, data.diffuseBuffer);
                            ctx.cmd.SetComputeTextureParam(data.subsurfaceScatteringDownsampleCS, data.subsurfaceScatteringDownsampleCSKernel, HDShaderIDs._OutputTexture, data.downsampleBuffer);
                            ctx.cmd.DispatchCompute(data.subsurfaceScatteringDownsampleCS, data.subsurfaceScatteringDownsampleCSKernel, data.numTilesX >> shift, data.numTilesY >> shift, data.numTilesZ);
                        }

                        // Combines specular lighting and diffuse lighting with subsurface scattering.
                        // In the case our frame is MSAA, for the moment given the fact that we do not have read/write access to the stencil buffer of the MSAA target; we need to keep this pass MSAA
                        // However, the compute can't output and MSAA target so we blend the non-MSAA target into the MSAA one.
                        ctx.cmd.SetComputeIntParam(data.subsurfaceScatteringCS, HDShaderIDs._SssSampleBudget, data.sampleBudget);
                        ctx.cmd.SetComputeIntParam(data.subsurfaceScatteringCS, HDShaderIDs._SssDownsampleSteps, data.downsampleSteps);

                        ctx.cmd.SetComputeTextureParam(data.subsurfaceScatteringCS, data.subsurfaceScatteringCSKernel, HDShaderIDs._DepthTexture, data.depthTexture);
                        ctx.cmd.SetComputeTextureParam(data.subsurfaceScatteringCS, data.subsurfaceScatteringCSKernel, HDShaderIDs._IrradianceSource, data.diffuseBuffer);
                        if (data.downsampleSteps > 0)
                            ctx.cmd.SetComputeTextureParam(data.subsurfaceScatteringCS, data.subsurfaceScatteringCSKernel, HDShaderIDs._IrradianceSourceDownsampled, data.downsampleBuffer);
                        ctx.cmd.SetComputeTextureParam(data.subsurfaceScatteringCS, data.subsurfaceScatteringCSKernel, HDShaderIDs._SSSBufferTexture, data.sssBuffer);

                        ctx.cmd.SetComputeBufferParam(data.subsurfaceScatteringCS, data.subsurfaceScatteringCSKernel, HDShaderIDs._CoarseStencilBuffer, data.coarseStencilBuffer);

                        // When occlusion is enabled, we pack the 2 diffusion profile indices into a single 8bit texel to improve the bandwidth when fetching the buffer.
                        // We couldn't pack this data into the lighting buffer because of the MSAA resolve and the precision loss.
                        if (data.useOcclusion)
                        {
                            ctx.cmd.SetComputeTextureParam(data.subsurfaceScatteringCS, data.packDiffusionProfileKernel, HDShaderIDs._DiffusionProfileIndexTexture, data.diffusionProfileIndex);
                            ctx.cmd.SetComputeTextureParam(data.subsurfaceScatteringCS, data.packDiffusionProfileKernel, HDShaderIDs._SSSBufferTexture, data.sssBuffer);
                            int xGroupCount = HDUtils.DivRoundUp(Mathf.CeilToInt(data.viewportSize.x / 2.0f), 8);
                            ctx.cmd.DispatchCompute(data.subsurfaceScatteringCS, data.packDiffusionProfileKernel, xGroupCount, HDUtils.DivRoundUp((int)data.viewportSize.y, 8), data.numTilesZ);

                            ctx.cmd.SetComputeTextureParam(data.subsurfaceScatteringCS, data.subsurfaceScatteringCSKernel, HDShaderIDs._DiffusionProfileIndexTexture, data.diffusionProfileIndex);
                        }
                        else
                        {
                            ctx.cmd.SetComputeTextureParam(data.subsurfaceScatteringCS, data.subsurfaceScatteringCSKernel, HDShaderIDs._DiffusionProfileIndexTexture, TextureXR.GetBlackTexture());
                        }

                        if (data.needTemporaryBuffer)
                        {
                            ctx.cmd.SetComputeTextureParam(data.subsurfaceScatteringCS, data.subsurfaceScatteringCSKernel, HDShaderIDs._CameraFilteringBuffer, data.cameraFilteringBuffer);

                            // Perform the SSS filtering pass
                            ctx.cmd.DispatchCompute(data.subsurfaceScatteringCS, data.subsurfaceScatteringCSKernel, data.numTilesX, data.numTilesY, data.numTilesZ);

                            data.combineLighting.SetTexture(HDShaderIDs._IrradianceSource, data.cameraFilteringBuffer);

                            // Additively blend diffuse and specular lighting into the color buffer.
                            HDUtils.DrawFullScreen(ctx.cmd, data.combineLighting, data.colorBuffer, data.depthStencilBuffer);
                        }
                        else
                        {
                            ctx.cmd.SetComputeTextureParam(data.subsurfaceScatteringCS, data.subsurfaceScatteringCSKernel, HDShaderIDs._CameraColorTexture, data.colorBuffer);

                            // Perform the SSS filtering pass which performs an in-place update of 'colorBuffer'.
                            ctx.cmd.DispatchCompute(data.subsurfaceScatteringCS, data.subsurfaceScatteringCSKernel, data.numTilesX, data.numTilesY, data.numTilesZ);
                        }
                    });

                return passData.colorBuffer;
            }
        }

        void RenderSubsurfaceScattering(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle colorBuffer, TextureHandle historyValidationTexture, ref LightingBuffers lightingBuffers, ref PrepassOutput prepassOutput)
        {
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.SubsurfaceScattering))
                return;

            lightingBuffers.diffuseLightingBuffer = ResolveMSAAColor(m_RenderGraph, hdCamera, lightingBuffers.diffuseLightingBuffer);
            lightingBuffers.sssBuffer = ResolveMSAAColor(m_RenderGraph, hdCamera, lightingBuffers.sssBuffer);

            // If ray tracing is enabled for the camera, if the volume override is active and if the RAS is built, we want to do ray traced SSS
            var settings = hdCamera.volumeStack.GetComponent<SubSurfaceScattering>();
            if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing) && settings.rayTracing.value && GetRayTracingState() && GetRayTracingClusterState())
            {
                RenderSubsurfaceScatteringRT(m_RenderGraph, hdCamera, prepassOutput.depthBuffer, prepassOutput.normalBuffer, colorBuffer,
                    lightingBuffers.sssBuffer, lightingBuffers.diffuseLightingBuffer, prepassOutput.motionVectorsBuffer, historyValidationTexture, lightingBuffers.ssgiLightingBuffer);
            }
            else
            {
                RenderSubsurfaceScatteringScreenSpace(m_RenderGraph, hdCamera, colorBuffer, lightingBuffers, ref prepassOutput);
            }
        }
    }
}
