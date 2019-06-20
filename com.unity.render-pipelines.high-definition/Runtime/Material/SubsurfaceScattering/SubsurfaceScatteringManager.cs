using UnityEngine.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    using RTHandle = RTHandleSystem.RTHandle;

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

        RTHandle m_SSSHTile;
        // End Disney SSS Model

        // Need an extra buffer on some platforms
        RTHandle m_SSSCameraFilteringBuffer;

        // This is use to be able to read stencil value in compute shader
        Material m_SSSCopyStencilForSplitLighting;

        // List of every diffusion profile data we need
        Vector4[]                   m_SSSThicknessRemaps;
        Vector4[]                   m_SSSShapeParams;
        Vector4[]                   m_SSSTransmissionTintsAndFresnel0;
        Vector4[]                   m_SSSDisabledTransmissionTintsAndFresnel0;
        Vector4[]                   m_SSSWorldScales;
        Vector4[]                   m_SSSFilterKernels;
        float[]                     m_SSSDiffusionProfileHashes;
        int[]                       m_SSSDiffusionProfileUpdate;
        DiffusionProfileSettings[]  m_SSSSetDiffusionProfiles;
        DiffusionProfileSettings    m_SSSDefaultDiffusionProfile;
        int                         m_SSSActiveDiffusionProfileCount;
        uint                        m_SSSTexturingModeFlags;        // 1 bit/profile: 0 = PreAndPostScatter, 1 = PostScatter
        uint                        m_SSSTransmissionFlags;         // 1 bit/profile: 0 = regular, 1 = thin

        public void InitSSSBuffers()
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
            if (settings.supportMSAA)
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

            // We use 8x8 tiles in order to match the native GCN HTile as closely as possible.
            m_SSSHTile = RTHandles.Alloc(size => new Vector2Int((size.x + 7) / 8, (size.y + 7) / 8), TextureXR.slices, colorFormat: GraphicsFormat.R8_UNorm, dimension: TextureXR.dimension, enableRandomWrite: true, useDynamicScale: true, name: "SSSHtile"); // Enable UAV

            // fill the list with the max number of diffusion profile so we dont have
            // the error: exceeds previous array size (5 vs 3). Cap to previous size.
            m_SSSThicknessRemaps = new Vector4[DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT];
            m_SSSShapeParams = new Vector4[DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT];
            m_SSSTransmissionTintsAndFresnel0 = new Vector4[DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT];
            m_SSSDisabledTransmissionTintsAndFresnel0 = new Vector4[DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT];
            m_SSSWorldScales = new Vector4[DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT];
            m_SSSFilterKernels = new Vector4[DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT * DiffusionProfileConstants.SSS_N_SAMPLES_NEAR_FIELD];
            m_SSSDiffusionProfileHashes = new float[DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT];
            m_SSSDiffusionProfileUpdate = new int[DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT];
            m_SSSSetDiffusionProfiles = new DiffusionProfileSettings[DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT];
        }

        public RTHandle GetSSSBuffer()
        {
            return m_SSSColor;
        }

        public RTHandle GetSSSBufferMSAA()
        {
            return m_SSSColorMSAA;
        }

        public void InitializeSubsurfaceScattering()
        {
            // Disney SSS (compute + combine)
            string kernelName = asset.currentPlatformRenderPipelineSettings.increaseSssSampleCount ? "SubsurfaceScatteringHQ" : "SubsurfaceScatteringMQ";
            string kernelNameMSAA = asset.currentPlatformRenderPipelineSettings.increaseSssSampleCount ? "SubsurfaceScatteringHQ_MSAA" : "SubsurfaceScatteringMQ_MSAA";
            m_SubsurfaceScatteringCS = asset.renderPipelineResources.shaders.subsurfaceScatteringCS;
            m_SubsurfaceScatteringKernel = m_SubsurfaceScatteringCS.FindKernel(kernelName);
            m_SubsurfaceScatteringKernelMSAA = m_SubsurfaceScatteringCS.FindKernel(kernelNameMSAA);
            m_CombineLightingPass = CoreUtils.CreateEngineMaterial(asset.renderPipelineResources.shaders.combineLightingPS);
            m_CombineLightingPass.SetInt(HDShaderIDs._StencilMask, (int)HDRenderPipeline.StencilBitMask.LightingMask);

            m_SSSCopyStencilForSplitLighting = CoreUtils.CreateEngineMaterial(asset.renderPipelineResources.shaders.copyStencilBufferPS);
            m_SSSCopyStencilForSplitLighting.SetInt(HDShaderIDs._StencilRef, (int)StencilLightingUsage.SplitLighting);
            m_SSSCopyStencilForSplitLighting.SetInt(HDShaderIDs._StencilMask, (int)HDRenderPipeline.StencilBitMask.LightingMask);

            m_SSSDefaultDiffusionProfile = asset.renderPipelineResources.assets.defaultDiffusionProfile;
        }

        public void CleanupSubsurfaceScattering()
        {
            CoreUtils.Destroy(m_CombineLightingPass);
            CoreUtils.Destroy(m_SSSCopyStencilForSplitLighting);
            if (!m_SSSReuseGBufferMemory)
            {
                RTHandles.Release(m_SSSColor);
            }
            RTHandles.Release(m_SSSColorMSAA);
            RTHandles.Release(m_SSSCameraFilteringBuffer);
            RTHandles.Release(m_SSSHTile);
        }

        void UpdateCurrentDiffusionProfileSettings()
        {
            var currentDiffusionProfiles = asset.diffusionProfileSettingsList;
            var diffusionProfileOverride = VolumeManager.instance.stack.GetComponent<DiffusionProfileOverride>();

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
            if (settings.profile.filterKernelNearField == null)
                return;

            m_SSSThicknessRemaps[index] = settings.thicknessRemaps;
            m_SSSShapeParams[index] = settings.shapeParams;
            m_SSSTransmissionTintsAndFresnel0[index] = settings.transmissionTintsAndFresnel0;
            m_SSSDisabledTransmissionTintsAndFresnel0[index] = settings.disabledTransmissionTintsAndFresnel0;
            m_SSSWorldScales[index] = settings.worldScales;
            for (int j = 0, n = DiffusionProfileConstants.SSS_N_SAMPLES_NEAR_FIELD; j < n; j++)
            {
                m_SSSFilterKernels[n * index + j].x = settings.profile.filterKernelNearField[j].x;
                m_SSSFilterKernels[n * index + j].y = settings.profile.filterKernelNearField[j].y;

                if (j < DiffusionProfileConstants.SSS_N_SAMPLES_FAR_FIELD)
                {
                    m_SSSFilterKernels[n * index + j].z = settings.profile.filterKernelFarField[j].x;
                    m_SSSFilterKernels[n * index + j].w = settings.profile.filterKernelFarField[j].y;
                }
            }
            m_SSSDiffusionProfileHashes[index] = HDShadowUtils.Asfloat(settings.profile.hash);

            // Erase previous value (This need to be done here individually as in the SSS editor we edit individual component)
            uint mask = 1u << index;
            m_SSSTexturingModeFlags &= ~mask;
            m_SSSTransmissionFlags &= ~mask;

            m_SSSTexturingModeFlags |= (uint)settings.profile.texturingMode    << index;
            m_SSSTransmissionFlags  |= (uint)settings.profile.transmissionMode << index;

            m_SSSSetDiffusionProfiles[index] = settings;
            m_SSSDiffusionProfileUpdate[index] = settings.updateCount;
        }

        public void PushSubsurfaceScatteringGlobalParams(HDCamera hdCamera, CommandBuffer cmd)
        {
            UpdateCurrentDiffusionProfileSettings();

            cmd.SetGlobalInt(HDShaderIDs._DiffusionProfileCount, m_SSSActiveDiffusionProfileCount);

            if (m_SSSActiveDiffusionProfileCount == 0)
                return ;

            // Broadcast SSS parameters to all shaders.
            cmd.SetGlobalInt(HDShaderIDs._EnableSubsurfaceScattering, hdCamera.frameSettings.IsEnabled(FrameSettingsField.SubsurfaceScattering) ? 1 : 0);
            unsafe
            {
                // Warning: Unity is not able to losslessly transfer integers larger than 2^24 to the shader system.
                // Therefore, we bitcast uint to float in C#, and bitcast back to uint in the shader.
                uint texturingModeFlags = this.m_SSSTexturingModeFlags;
                uint transmissionFlags = this.m_SSSTransmissionFlags;
                cmd.SetGlobalFloat(HDShaderIDs._TexturingModeFlags, *(float*)&texturingModeFlags);
                cmd.SetGlobalFloat(HDShaderIDs._TransmissionFlags, *(float*)&transmissionFlags);
            }
            cmd.SetGlobalVectorArray(HDShaderIDs._ThicknessRemaps, m_SSSThicknessRemaps);
            cmd.SetGlobalVectorArray(HDShaderIDs._ShapeParams, m_SSSShapeParams);
            // To disable transmission, we simply nullify the transmissionTint
            cmd.SetGlobalVectorArray(HDShaderIDs._TransmissionTintsAndFresnel0, hdCamera.frameSettings.IsEnabled(FrameSettingsField.Transmission) ? m_SSSTransmissionTintsAndFresnel0 : m_SSSDisabledTransmissionTintsAndFresnel0);
            cmd.SetGlobalVectorArray(HDShaderIDs._WorldScales, m_SSSWorldScales);
            cmd.SetGlobalFloatArray(HDShaderIDs._DiffusionProfileHashTable, m_SSSDiffusionProfileHashes);
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
            public bool             needTemporaryBuffer;
            public Material         copyStencilForSplitLighting;
            public Material         combineLighting;
            public uint             texturingModeFlags;
            public int              numTilesX;
            public int              numTilesY;
            public int              numTilesZ;
            public Vector4[]        worldScales;
            public Vector4[]        filterKernels;
            public Vector4[]        shapeParams;
            public float[]          diffusionProfileHashes;

        }

        struct SubsurfaceScatteringResources
        {
            public RTHandle colorBuffer;
            public RTHandle diffuseBuffer;
            public RTHandle depthStencilBuffer;
            public RTHandle depthTexture;

            public RTHandle cameraFilteringBuffer;
            public RTHandle hTileBuffer;
            public RTHandle sssBuffer;
        }

        SubsurfaceScatteringParameters PrepareSubsurfaceScatteringParameters(HDCamera hdCamera)
        {
            var parameters = new SubsurfaceScatteringParameters();

            parameters.subsurfaceScatteringCS = m_SubsurfaceScatteringCS;
            parameters.subsurfaceScatteringCSKernel = hdCamera.frameSettings.IsEnabled(FrameSettingsField.MSAA) ? m_SubsurfaceScatteringKernelMSAA : m_SubsurfaceScatteringKernel;
            parameters.needTemporaryBuffer = NeedTemporarySubsurfaceBuffer() || hdCamera.frameSettings.IsEnabled(FrameSettingsField.MSAA);
            parameters.copyStencilForSplitLighting = m_SSSCopyStencilForSplitLighting;
            parameters.combineLighting = m_CombineLightingPass;
            parameters.texturingModeFlags = m_SSSTexturingModeFlags;
            parameters.numTilesX = ((int)hdCamera.screenSize.x + 15) / 16;
            parameters.numTilesY = ((int)hdCamera.screenSize.y + 15) / 16;
            parameters.numTilesZ = hdCamera.viewCount;

            parameters.worldScales = m_SSSWorldScales;
            parameters.filterKernels = m_SSSFilterKernels;
            parameters.shapeParams = m_SSSShapeParams;
            parameters.diffusionProfileHashes = m_SSSDiffusionProfileHashes;

            return parameters;
        }

        public void RenderSubsurfaceScattering(HDCamera hdCamera, CommandBuffer cmd, RTHandle colorBufferRT,
            RTHandle diffuseBufferRT, RTHandle depthStencilBufferRT, RTHandle depthTextureRT)
        {
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.SubsurfaceScattering))
                return;

            using (new ProfilingSample(cmd, "Subsurface Scattering", CustomSamplerId.SubsurfaceScattering.GetSampler()))
            {
                var parameters = PrepareSubsurfaceScatteringParameters(hdCamera);
                var resources = new SubsurfaceScatteringResources();
                resources.colorBuffer = colorBufferRT;
                resources.diffuseBuffer = diffuseBufferRT;
                resources.depthStencilBuffer = depthStencilBufferRT;
                resources.depthTexture = depthTextureRT;
                resources.cameraFilteringBuffer = m_SSSCameraFilteringBuffer;
                resources.hTileBuffer = m_SSSHTile;
                resources.sssBuffer = m_SSSColor;

                // For Jimenez we always need an extra buffer, for Disney it depends on platform
                if (parameters.needTemporaryBuffer)
                {
                    // Clear the SSS filtering target
                    using (new ProfilingSample(cmd, "Clear SSS filtering target", CustomSamplerId.ClearSSSFilteringTarget.GetSampler()))
                    {
                        HDUtils.SetRenderTarget(cmd, m_SSSCameraFilteringBuffer, ClearFlag.Color, Color.clear);
                    }
                }

                RenderSubsurfaceScattering(parameters, resources, cmd);
            }
        }

        // Combines specular lighting and diffuse lighting with subsurface scattering.
        // In the case our frame is MSAA, for the moment given the fact that we do not have read/write access to the stencil buffer of the MSAA target; we need to keep this pass MSAA
        // However, the compute can't output and MSAA target so we blend the non-MSAA target into the MSAA one.
        static void RenderSubsurfaceScattering(in SubsurfaceScatteringParameters parameters, in SubsurfaceScatteringResources resources, CommandBuffer cmd)
        {
            // TODO: For MSAA, at least initially, we can only support Jimenez, because we can't
            // create MSAA + UAV render targets.
            using (new ProfilingSample(cmd, "HTile for SSS", CustomSamplerId.HTileForSSS.GetSampler()))
            {
                // Currently, Unity does not offer a way to access the GCN HTile even on PS4 and Xbox One.
                // Therefore, it's computed in a pixel shader, and optimized to only contain the SSS bit.

                // Clear the HTile texture. TODO: move this to ClearBuffers(). Clear operations must be batched!
                HDUtils.SetRenderTarget(cmd, resources.hTileBuffer, ClearFlag.Color, Color.clear);

                HDUtils.SetRenderTarget(cmd, resources.depthStencilBuffer); // No need for color buffer here
                cmd.SetRandomWriteTarget(1, resources.hTileBuffer); // This need to be done AFTER SetRenderTarget
                // Generate HTile for the split lighting stencil usage. Don't write into stencil texture (shaderPassId = 2)
                // Use ShaderPassID 1 => "Pass 2 - Export HTILE for stencilRef to output"
                CoreUtils.DrawFullScreen(cmd, parameters.copyStencilForSplitLighting, null, 2);
                cmd.ClearRandomWriteTargets();
            }

            unsafe
            {
                // Warning: Unity is not able to losslessly transfer integers larger than 2^24 to the shader system.
                // Therefore, we bitcast uint to float in C#, and bitcast back to uint in the shader.
                uint textureingModeFlags = parameters.texturingModeFlags;
                cmd.SetComputeFloatParam(parameters.subsurfaceScatteringCS, HDShaderIDs._TexturingModeFlags, *(float*)&textureingModeFlags);
            }

            cmd.SetComputeVectorArrayParam(parameters.subsurfaceScatteringCS, HDShaderIDs._WorldScales, parameters.worldScales);
            cmd.SetComputeVectorArrayParam(parameters.subsurfaceScatteringCS, HDShaderIDs._FilterKernels, parameters.filterKernels);
            cmd.SetComputeVectorArrayParam(parameters.subsurfaceScatteringCS, HDShaderIDs._ShapeParams, parameters.shapeParams);
            cmd.SetComputeFloatParams(parameters.subsurfaceScatteringCS, HDShaderIDs._DiffusionProfileHashTable, parameters.diffusionProfileHashes);

            cmd.SetComputeTextureParam(parameters.subsurfaceScatteringCS, parameters.subsurfaceScatteringCSKernel, HDShaderIDs._DepthTexture, resources.depthTexture);
            cmd.SetComputeTextureParam(parameters.subsurfaceScatteringCS, parameters.subsurfaceScatteringCSKernel, HDShaderIDs._SSSHTile, resources.hTileBuffer);
            cmd.SetComputeTextureParam(parameters.subsurfaceScatteringCS, parameters.subsurfaceScatteringCSKernel, HDShaderIDs._IrradianceSource, resources.diffuseBuffer);
            cmd.SetComputeTextureParam(parameters.subsurfaceScatteringCS, parameters.subsurfaceScatteringCSKernel, HDShaderIDs._SSSBufferTexture, resources.sssBuffer);

            if (parameters.needTemporaryBuffer)
            {
                cmd.SetComputeTextureParam(parameters.subsurfaceScatteringCS, parameters.subsurfaceScatteringCSKernel, HDShaderIDs._CameraFilteringBuffer, resources.cameraFilteringBuffer);

                // Perform the SSS filtering pass
                cmd.DispatchCompute(parameters.subsurfaceScatteringCS, parameters.subsurfaceScatteringCSKernel, parameters.numTilesX, parameters.numTilesY, parameters.numTilesZ);

                parameters.combineLighting.SetTexture(HDShaderIDs._IrradianceSource, resources.cameraFilteringBuffer);

                // Additively blend diffuse and specular lighting into the color buffer.
                HDUtils.DrawFullScreen(cmd, parameters.combineLighting, resources.colorBuffer);
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
