using UnityEngine.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class SubsurfaceScatteringManager
    {
        // Currently we only support SSSBuffer with one buffer. If the shader code change, it may require to update the shader manager
        public const int k_MaxSSSBuffer = 1;

        public int sssBufferCount { get { return k_MaxSSSBuffer; } }

        RTHandleSystem.RTHandle[] m_ColorMRTs = new RTHandleSystem.RTHandle[k_MaxSSSBuffer];
        RTHandleSystem.RTHandle[] m_ColorMSAAMRTs = new RTHandleSystem.RTHandle[k_MaxSSSBuffer];
        bool[] m_ReuseGBufferMemory  = new bool[k_MaxSSSBuffer];

        // Disney SSS Model
        ComputeShader m_SubsurfaceScatteringCS;
        int m_SubsurfaceScatteringKernel;
        int m_SubsurfaceScatteringKernelMSAA;
        Material m_CombineLightingPass;

        RTHandleSystem.RTHandle m_HTile;
        // End Disney SSS Model

        // Need an extra buffer on some platforms
        RTHandleSystem.RTHandle m_CameraFilteringBuffer;

        // This is use to be able to read stencil value in compute shader
        Material m_CopyStencilForSplitLighting;

        bool m_MSAASupport = false;

        // List of every diffusion profile data we need
        Vector4[]                   thicknessRemaps;
        Vector4[]                   shapeParams;
        Vector4[]                   transmissionTintsAndFresnel0;
        Vector4[]                   disabledTransmissionTintsAndFresnel0;
        Vector4[]                   worldScales;
        Vector4[]                   filterKernels;
        float[]                     diffusionProfileHashes;
        int[]                       diffusionProfileUpdate;
        DiffusionProfileSettings[]  setDiffusionProfiles;
        HDRenderPipelineAsset       hdAsset;
        DiffusionProfileSettings    defaultDiffusionProfile;
        int                         activeDiffusionProfileCount;
        public uint                 texturingModeFlags;        // 1 bit/profile: 0 = PreAndPostScatter, 1 = PostScatter
        public uint                 transmissionFlags;         // 1 bit/profile: 0 = regular, 1 = thin

        public SubsurfaceScatteringManager()
        {
        }

        public void InitSSSBuffers(GBufferManager gbufferManager, RenderPipelineSettings settings)
        {
            // Reset the msaa flag
            m_MSAASupport = settings.supportMSAA;

            if (settings.supportedLitShaderMode == RenderPipelineSettings.SupportedLitShaderMode.ForwardOnly) //forward only
            {
                // In case of full forward we must allocate the render target for forward SSS (or reuse one already existing)
                // TODO: Provide a way to reuse a render target
                m_ColorMRTs[0] = RTHandles.Alloc(Vector2.one, filterMode: FilterMode.Point, colorFormat: GraphicsFormat.R8G8B8A8_SRGB, xrInstancing: true, useDynamicScale: true, name: "SSSBuffer");
                m_ReuseGBufferMemory [0] = false;
            }

            // We need to allocate the texture if we are in forward or both in case one of the cameras is in enable forward only mode
            if (m_MSAASupport)
            {
                 m_ColorMSAAMRTs[0] = RTHandles.Alloc(Vector2.one, filterMode: FilterMode.Point, colorFormat: GraphicsFormat.R8G8B8A8_SRGB, enableMSAA: true, bindTextureMS: true, xrInstancing: true, useDynamicScale: true, name: "SSSBufferMSAA");
            }

            if ((settings.supportedLitShaderMode & RenderPipelineSettings.SupportedLitShaderMode.DeferredOnly) != 0) //deferred or both
            {
                // In case of deferred, we must be in sync with SubsurfaceScattering.hlsl and lit.hlsl files and setup the correct buffers
                m_ColorMRTs[0] = gbufferManager.GetSubsurfaceScatteringBuffer(0); // Note: This buffer must be sRGB (which is the case with Lit.shader)
                m_ReuseGBufferMemory [0] = true;
            }

            if (NeedTemporarySubsurfaceBuffer() || settings.supportMSAA)
            {
                // Caution: must be same format as m_CameraSssDiffuseLightingBuffer
                m_CameraFilteringBuffer = RTHandles.Alloc(Vector2.one, filterMode: FilterMode.Point, colorFormat: GraphicsFormat.B10G11R11_UFloatPack32, enableRandomWrite: true, xrInstancing: true, useDynamicScale: true, name: "SSSCameraFiltering"); // Enable UAV
            }

            // We use 8x8 tiles in order to match the native GCN HTile as closely as possible.
            m_HTile = RTHandles.Alloc(size => new Vector2Int((size.x + 7) / 8, (size.y + 7) / 8), filterMode: FilterMode.Point, colorFormat: GraphicsFormat.R8_UNorm, enableRandomWrite: true, xrInstancing: true, useDynamicScale: true, name: "SSSHtile"); // Enable UAV

            // fill the list with the max number of diffusion profile so we dont have
            // the error: exceeds previous array size (5 vs 3). Cap to previous size.
            thicknessRemaps = new Vector4[DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT];
            shapeParams = new Vector4[DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT];
            transmissionTintsAndFresnel0 = new Vector4[DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT];
            disabledTransmissionTintsAndFresnel0 = new Vector4[DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT];
            worldScales = new Vector4[DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT];
            filterKernels = new Vector4[DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT * DiffusionProfileConstants.SSS_N_SAMPLES_NEAR_FIELD];
            diffusionProfileHashes = new float[DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT];
            diffusionProfileUpdate = new int[DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT];
            setDiffusionProfiles = new DiffusionProfileSettings[DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT];
        }

        public RTHandleSystem.RTHandle GetSSSBuffer(int index)
        {
            Debug.Assert(index < sssBufferCount);
            return m_ColorMRTs[index];
        }

        public RTHandleSystem.RTHandle GetSSSBufferMSAA(int index)
        {
            Debug.Assert(index < sssBufferCount);
            return m_ColorMSAAMRTs[index];
        }

        public void Build(HDRenderPipelineAsset hdAsset)
        {
            // Disney SSS (compute + combine)
            string kernelName = hdAsset.currentPlatformRenderPipelineSettings.increaseSssSampleCount ? "SubsurfaceScatteringHQ" : "SubsurfaceScatteringMQ";
            string kernelNameMSAA = hdAsset.currentPlatformRenderPipelineSettings.increaseSssSampleCount ? "SubsurfaceScatteringHQ_MSAA" : "SubsurfaceScatteringMQ_MSAA";
            m_SubsurfaceScatteringCS = hdAsset.renderPipelineResources.shaders.subsurfaceScatteringCS;
            m_SubsurfaceScatteringKernel = m_SubsurfaceScatteringCS.FindKernel(kernelName);
            m_SubsurfaceScatteringKernelMSAA = m_SubsurfaceScatteringCS.FindKernel(kernelNameMSAA);
            m_CombineLightingPass = CoreUtils.CreateEngineMaterial(hdAsset.renderPipelineResources.shaders.combineLightingPS);
            m_CombineLightingPass.SetInt(HDShaderIDs._StencilMask, (int)HDRenderPipeline.StencilBitMask.LightingMask);

            m_CopyStencilForSplitLighting = CoreUtils.CreateEngineMaterial(hdAsset.renderPipelineResources.shaders.copyStencilBufferPS);
            m_CopyStencilForSplitLighting.SetInt(HDShaderIDs._StencilRef, (int)StencilLightingUsage.SplitLighting);
            m_CopyStencilForSplitLighting.SetInt(HDShaderIDs._StencilMask, (int)HDRenderPipeline.StencilBitMask.LightingMask);
            
            this.hdAsset = hdAsset;
            defaultDiffusionProfile = hdAsset.defaultDiffusionProfileSettings;
        }

        public void Cleanup()
        {
            CoreUtils.Destroy(m_CombineLightingPass);
            CoreUtils.Destroy(m_CopyStencilForSplitLighting);

            for (int i = 0; i < k_MaxSSSBuffer; ++i)
            {
                if (!m_ReuseGBufferMemory [i])
                {
                    RTHandles.Release(m_ColorMRTs[i]);
                }

                if (m_MSAASupport)
                {
                    RTHandles.Release(m_ColorMSAAMRTs[i]);
                }
            }

            RTHandles.Release(m_CameraFilteringBuffer);
            RTHandles.Release(m_HTile);
        }

        void UpdateCurrentDiffusionProfileSettings()
        {
            var currentDiffusionProfiles = hdAsset.diffusionProfileSettingsList;
            var diffusionProfileOverride = VolumeManager.instance.stack.GetComponent<DiffusionProfileOverride>();

            // If there is a diffusion profile volume override, we merge diffusion profiles that are overwritten
            if (diffusionProfileOverride.active && diffusionProfileOverride.diffusionProfiles.value != null)
            {
                currentDiffusionProfiles = diffusionProfileOverride.diffusionProfiles.value;
            }

            // The first profile of the list is the default diffusion profile, used either when the diffusion profile
            // on the material isn't assigned or when the diffusion profile can't be displayed (too many on the frame)
            SetDiffusionProfileAtIndex(defaultDiffusionProfile, 0);
            diffusionProfileHashes[0] = DiffusionProfileConstants.DIFFUSION_PROFILE_NEUTRAL_ID;

            int i = 1;
            foreach (var v in currentDiffusionProfiles)
            {
                if (v == null)
                    continue;
                SetDiffusionProfileAtIndex(v, i++);
            }

            activeDiffusionProfileCount = i;
        }

        void SetDiffusionProfileAtIndex(DiffusionProfileSettings settings, int index)
        {
            // if the diffusion profile was already set and it haven't changed then there is nothing to upgrade
            if (setDiffusionProfiles[index] == settings && diffusionProfileUpdate[index] == settings.updateCount)
                return;
            
            // if the settings have not yet been initialized
            if (settings.profile.filterKernelNearField == null)
                return;

            thicknessRemaps[index] = settings.thicknessRemaps;
            shapeParams[index] = settings.shapeParams;
            transmissionTintsAndFresnel0[index] = settings.transmissionTintsAndFresnel0;
            disabledTransmissionTintsAndFresnel0[index] = settings.disabledTransmissionTintsAndFresnel0;
            worldScales[index] = settings.worldScales;
            for (int j = 0, n = DiffusionProfileConstants.SSS_N_SAMPLES_NEAR_FIELD; j < n; j++)
            {
                filterKernels[n * index + j].x = settings.profile.filterKernelNearField[j].x;
                filterKernels[n * index + j].y = settings.profile.filterKernelNearField[j].y;

                if (j < DiffusionProfileConstants.SSS_N_SAMPLES_FAR_FIELD)
                {
                    filterKernels[n * index + j].z = settings.profile.filterKernelFarField[j].x;
                    filterKernels[n * index + j].w = settings.profile.filterKernelFarField[j].y;
                }
            }
            diffusionProfileHashes[index] = HDShadowUtils.Asfloat(settings.profile.hash);

            // Erase previous value (This need to be done here individually as in the SSS editor we edit individual component)
            uint mask = 1u << index;
            texturingModeFlags &= ~mask;
            transmissionFlags &= ~mask;
            
            texturingModeFlags |= (uint)settings.profile.texturingMode    << index;
            transmissionFlags  |= (uint)settings.profile.transmissionMode << index;

            setDiffusionProfiles[index] = settings;
            diffusionProfileUpdate[index] = settings.updateCount;
        }

        public void PushGlobalParams(HDCamera hdCamera, CommandBuffer cmd)
        {
            UpdateCurrentDiffusionProfileSettings();

            cmd.SetGlobalInt(HDShaderIDs._DiffusionProfileCount, activeDiffusionProfileCount);

            if (activeDiffusionProfileCount == 0)
                return ;

            // Broadcast SSS parameters to all shaders.
            cmd.SetGlobalInt(HDShaderIDs._EnableSubsurfaceScattering, hdCamera.frameSettings.IsEnabled(FrameSettingsField.SubsurfaceScattering) ? 1 : 0);
            unsafe
            {
                // Warning: Unity is not able to losslessly transfer integers larger than 2^24 to the shader system.
                // Therefore, we bitcast uint to float in C#, and bitcast back to uint in the shader.
                uint texturingModeFlags = this.texturingModeFlags;
                uint transmissionFlags = this.transmissionFlags;
                cmd.SetGlobalFloat(HDShaderIDs._TexturingModeFlags, *(float*)&texturingModeFlags);
                cmd.SetGlobalFloat(HDShaderIDs._TransmissionFlags, *(float*)&transmissionFlags);
            }
            cmd.SetGlobalVectorArray(HDShaderIDs._ThicknessRemaps, thicknessRemaps);
            cmd.SetGlobalVectorArray(HDShaderIDs._ShapeParams, shapeParams);
            // To disable transmission, we simply nullify the transmissionTint
            cmd.SetGlobalVectorArray(HDShaderIDs._TransmissionTintsAndFresnel0, hdCamera.frameSettings.IsEnabled(FrameSettingsField.Transmission) ? transmissionTintsAndFresnel0 : disabledTransmissionTintsAndFresnel0);
            cmd.SetGlobalVectorArray(HDShaderIDs._WorldScales, worldScales);
            cmd.SetGlobalFloatArray(HDShaderIDs._DiffusionProfileHashTable, diffusionProfileHashes);
        }

        bool NeedTemporarySubsurfaceBuffer()
        {
            // Caution: need to be in sync with SubsurfaceScattering.cs USE_INTERMEDIATE_BUFFER (Can't make a keyword as it is a compute shader)
            // Typed UAV loads from FORMAT_R16G16B16A16_FLOAT is an optional feature of Direct3D 11.
            // Most modern GPUs support it. We can avoid performing a costly copy in this case.
            // TODO: test/implement for other platforms.
            return SystemInfo.graphicsDeviceType != GraphicsDeviceType.PlayStation4 &&
                SystemInfo.graphicsDeviceType != GraphicsDeviceType.XboxOne &&
                SystemInfo.graphicsDeviceType != GraphicsDeviceType.XboxOneD3D12;
        }

        // Combines specular lighting and diffuse lighting with subsurface scattering.
        // In the case our frame is MSAA, for the moment given the fact that we do not have read/write access to the stencil buffer of the MSAA target; we need to keep this pass MSAA
        // However, the compute can't output and MSAA target so we blend the non-MSAA target into the MSAA one.
        public void SubsurfaceScatteringPass(HDCamera hdCamera, CommandBuffer cmd, RTHandleSystem.RTHandle colorBufferRT,
        RTHandleSystem.RTHandle diffuseBufferRT, RTHandleSystem.RTHandle depthStencilBufferRT, RTHandleSystem.RTHandle depthTextureRT)
        {
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.SubsurfaceScattering))
                return;

            // TODO: For MSAA, at least initially, we can only support Jimenez, because we can't
            // create MSAA + UAV render targets.

            using (new ProfilingSample(cmd, "Subsurface Scattering", CustomSamplerId.SubsurfaceScattering.GetSampler()))
            {
                // For Jimenez we always need an extra buffer, for Disney it depends on platform
                if (NeedTemporarySubsurfaceBuffer() || hdCamera.frameSettings.IsEnabled(FrameSettingsField.MSAA))
                {
                    // Clear the SSS filtering target
                    using (new ProfilingSample(cmd, "Clear SSS filtering target", CustomSamplerId.ClearSSSFilteringTarget.GetSampler()))
                    {
                        HDUtils.SetRenderTarget(cmd, hdCamera, m_CameraFilteringBuffer, ClearFlag.Color, Color.clear);
                    }
                }

                using (new ProfilingSample(cmd, "HTile for SSS", CustomSamplerId.HTileForSSS.GetSampler()))
                {
                    // Currently, Unity does not offer a way to access the GCN HTile even on PS4 and Xbox One.
                    // Therefore, it's computed in a pixel shader, and optimized to only contain the SSS bit.

                    // Clear the HTile texture. TODO: move this to ClearBuffers(). Clear operations must be batched!
                    HDUtils.SetRenderTarget(cmd, hdCamera, m_HTile, ClearFlag.Color, Color.clear);

                    HDUtils.SetRenderTarget(cmd, hdCamera, depthStencilBufferRT); // No need for color buffer here
                    cmd.SetRandomWriteTarget(1, m_HTile); // This need to be done AFTER SetRenderTarget
                    // Generate HTile for the split lighting stencil usage. Don't write into stencil texture (shaderPassId = 2)
                    // Use ShaderPassID 1 => "Pass 2 - Export HTILE for stencilRef to output"
                    CoreUtils.DrawFullScreen(cmd, m_CopyStencilForSplitLighting, null, 2);
                    cmd.ClearRandomWriteTargets();
                }

                unsafe
                {
                    // Warning: Unity is not able to losslessly transfer integers larger than 2^24 to the shader system.
                    // Therefore, we bitcast uint to float in C#, and bitcast back to uint in the shader.
                    uint texturingModeFlags = this.texturingModeFlags;
                    cmd.SetComputeFloatParam(m_SubsurfaceScatteringCS, HDShaderIDs._TexturingModeFlags, *(float*)&texturingModeFlags);
                }

                cmd.SetComputeVectorArrayParam(m_SubsurfaceScatteringCS, HDShaderIDs._WorldScales,          worldScales);
                cmd.SetComputeVectorArrayParam(m_SubsurfaceScatteringCS, HDShaderIDs._FilterKernels,        filterKernels);
                cmd.SetComputeVectorArrayParam(m_SubsurfaceScatteringCS, HDShaderIDs._ShapeParams,          shapeParams);
                cmd.SetComputeFloatParams(m_SubsurfaceScatteringCS, HDShaderIDs._DiffusionProfileHashTable, diffusionProfileHashes);

                int sssKernel = hdCamera.frameSettings.IsEnabled(FrameSettingsField.MSAA) ? m_SubsurfaceScatteringKernelMSAA : m_SubsurfaceScatteringKernel;

                cmd.SetComputeTextureParam(m_SubsurfaceScatteringCS, sssKernel, HDShaderIDs._DepthTexture,       depthTextureRT);
                cmd.SetComputeTextureParam(m_SubsurfaceScatteringCS, sssKernel, HDShaderIDs._SSSHTile,           m_HTile);
                cmd.SetComputeTextureParam(m_SubsurfaceScatteringCS, sssKernel, HDShaderIDs._IrradianceSource,   diffuseBufferRT);

                for (int i = 0; i < sssBufferCount; ++i)
                {
                    cmd.SetComputeTextureParam(m_SubsurfaceScatteringCS, sssKernel, HDShaderIDs._SSSBufferTexture[i], GetSSSBuffer(i));
                }
                
                int numTilesX = ((int)(hdCamera.textureWidthScaling.x * hdCamera.screenSize.x) + 15) / 16;
                int numTilesY = ((int)hdCamera.screenSize.y + 15) / 16;
                int numTilesZ = XRGraphics.computePassCount;

                if (NeedTemporarySubsurfaceBuffer() || hdCamera.frameSettings.IsEnabled(FrameSettingsField.MSAA))
                {
                    cmd.SetComputeTextureParam(m_SubsurfaceScatteringCS, sssKernel, HDShaderIDs._CameraFilteringBuffer, m_CameraFilteringBuffer);

                    // Perform the SSS filtering pass which fills 'm_CameraFilteringBufferRT'.
                    cmd.DispatchCompute(m_SubsurfaceScatteringCS, sssKernel, numTilesX, numTilesY, numTilesZ);

                    cmd.SetGlobalTexture(HDShaderIDs._IrradianceSource, m_CameraFilteringBuffer);  // Cannot set a RT on a material

                    // Additively blend diffuse and specular lighting into 'm_CameraColorBufferRT'.
                    HDUtils.DrawFullScreen(cmd, hdCamera, m_CombineLightingPass, colorBufferRT, depthStencilBufferRT);
                }
                else
                {
                    cmd.SetComputeTextureParam(m_SubsurfaceScatteringCS, m_SubsurfaceScatteringKernel, HDShaderIDs._CameraColorTexture, colorBufferRT);

                    // Perform the SSS filtering pass which performs an in-place update of 'colorBuffer'.
                    cmd.DispatchCompute(m_SubsurfaceScatteringCS, m_SubsurfaceScatteringKernel, numTilesX, numTilesY, numTilesZ);
                }
            }
        }
    }
}
