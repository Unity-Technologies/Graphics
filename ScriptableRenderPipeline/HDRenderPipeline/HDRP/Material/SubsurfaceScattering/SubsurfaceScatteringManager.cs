using UnityEngine.Rendering;
using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class SubsurfaceScatteringManager
    {
        // Currently we only support SSSBuffer with one buffer. If the shader code change, it may require to update the shader manager
        public const int k_MaxSSSBuffer = 1;

        readonly int m_SSSBuffer0;
        readonly RenderTargetIdentifier m_SSSBuffer0RT;

        public int sssBufferCount { get { return k_MaxSSSBuffer; } }

        RenderTargetIdentifier[] m_ColorMRTs;
        RenderTargetIdentifier[] m_RTIDs = new RenderTargetIdentifier[k_MaxSSSBuffer];

        // Disney SSS Model
        ComputeShader m_SubsurfaceScatteringCS;
        int m_SubsurfaceScatteringKernel;
        Material m_CombineLightingPass;

        RenderTexture m_HTile;
        RenderTargetIdentifier m_HTileRT;
        // End Disney SSS Model

        // Jimenez SSS Model
        Material m_SssVerticalFilterPass;
        Material m_SssHorizontalFilterAndCombinePass;
        // End Jimenez SSS Model

        // Jimenez need an extra buffer and Disney need one for some platform
        readonly int m_CameraFilteringBuffer;
        readonly RenderTargetIdentifier m_CameraFilteringBufferRT;

        // This is use to be able to read stencil value in compute shader
        Material m_CopyStencilForSplitLighting;

        public SubsurfaceScatteringManager()
        {
            m_SSSBuffer0 = HDShaderIDs._SSSBufferTexture[0];
            m_SSSBuffer0RT = new RenderTargetIdentifier(m_SSSBuffer0);

            // Use with Jimenez
            m_CameraFilteringBuffer = HDShaderIDs._CameraFilteringBuffer;
            m_CameraFilteringBufferRT = new RenderTargetIdentifier(m_CameraFilteringBuffer);
        }

        // In case of deferred, we must be in sync with SubsurfaceScattering.hlsl and lit.hlsl files and setup the correct buffers
        // for SSS
        public void InitSSSBuffersFromGBuffer(GBufferManager gbufferManager)
        {
            m_RTIDs[0] = gbufferManager.GetGBuffers()[0]; // Note: This buffer must be sRGB (which is the case with Lit.shader)
        }

        // In case of full forward we must allocate the render target for forward SSS (or reuse one already existing)
        // TODO: Provide a way to reuse a render target
        public void InitSSSBuffers(RenderTextureDescriptor desc, CommandBuffer cmd)
        {
            m_RTIDs[0] = m_SSSBuffer0RT;

            desc.depthBufferBits = 0;
            desc.colorFormat = RenderTextureFormat.ARGB32;
            desc.sRGB = true;  // Note: This buffer must be sRGB to match deferred case (which is the case with Lit.shader)

            cmd.ReleaseTemporaryRT(m_SSSBuffer0);
            cmd.GetTemporaryRT(m_SSSBuffer0, desc, FilterMode.Point);
        }

        public RenderTargetIdentifier GetSSSBuffers(int index)
        {
            Debug.Assert(index < sssBufferCount);
            return m_RTIDs[index];
        }

        public void Build(HDRenderPipelineAsset hdAsset)
        {
            // Disney SSS (compute + combine)
            m_SubsurfaceScatteringCS = hdAsset.renderPipelineResources.subsurfaceScatteringCS;
            m_SubsurfaceScatteringKernel = m_SubsurfaceScatteringCS.FindKernel("SubsurfaceScattering");
            m_CombineLightingPass = CoreUtils.CreateEngineMaterial(hdAsset.renderPipelineResources.combineLighting);
            m_CombineLightingPass.SetInt(HDShaderIDs._StencilMask, (int)HDRenderPipeline.StencilBitMask.LightingMask);

            // Jimenez SSS Model (shader)
            m_SssVerticalFilterPass = CoreUtils.CreateEngineMaterial(hdAsset.renderPipelineResources.subsurfaceScattering);
            m_SssVerticalFilterPass.DisableKeyword("SSS_FILTER_HORIZONTAL_AND_COMBINE");
            m_SssVerticalFilterPass.SetFloat(HDShaderIDs._DstBlend, (float)BlendMode.Zero);
            m_SssVerticalFilterPass.SetInt(HDShaderIDs._StencilMask, (int)HDRenderPipeline.StencilBitMask.LightingMask);

            m_SssHorizontalFilterAndCombinePass = CoreUtils.CreateEngineMaterial(hdAsset.renderPipelineResources.subsurfaceScattering);
            m_SssHorizontalFilterAndCombinePass.EnableKeyword("SSS_FILTER_HORIZONTAL_AND_COMBINE");
            m_SssHorizontalFilterAndCombinePass.SetFloat(HDShaderIDs._DstBlend, (float)BlendMode.One);
            m_SssHorizontalFilterAndCombinePass.SetInt(HDShaderIDs._StencilMask, (int)HDRenderPipeline.StencilBitMask.LightingMask);

            m_CopyStencilForSplitLighting = CoreUtils.CreateEngineMaterial(hdAsset.renderPipelineResources.copyStencilBuffer);
            m_CopyStencilForSplitLighting.SetInt(HDShaderIDs._StencilRef, (int)StencilLightingUsage.SplitLighting);
            m_CopyStencilForSplitLighting.SetInt(HDShaderIDs._StencilMask, (int)HDRenderPipeline.StencilBitMask.LightingMask);
        }

        public void Cleanup()
        {
            CoreUtils.Destroy(m_CombineLightingPass);
            CoreUtils.Destroy(m_SssVerticalFilterPass);
            CoreUtils.Destroy(m_SssHorizontalFilterAndCombinePass);
            CoreUtils.Destroy(m_CopyStencilForSplitLighting);
        }

        public void Resize(HDCamera hdCamera)
        {
            // We must use a RenderTexture and not GetTemporaryRT() as currently Unity only aloow to bind a RenderTexture for a UAV in a pixel shader
            // We use 8x8 tiles in order to match the native GCN HTile as closely as possible.
            var desc = hdCamera.renderTextureDesc;
            desc.width = (desc.width + 7) / 8;
            desc.height = (desc.height + 7) / 8;
            m_HTile = CoreUtils.CreateRenderTexture(desc, 0, RenderTextureFormat.R8, RenderTextureReadWrite.Linear); // DXGI_FORMAT_R8_UINT is not supported by Unity
            m_HTile.filterMode = FilterMode.Point;
            m_HTile.enableRandomWrite = true;
            m_HTile.Create();
            m_HTileRT = new RenderTargetIdentifier(m_HTile);
        }

        public void PushGlobalParams(CommandBuffer cmd, DiffusionProfileSettings sssParameters, FrameSettings frameSettings)
        {
            // Broadcast SSS parameters to all shaders.
            cmd.SetGlobalInt(HDShaderIDs._EnableSubsurfaceScattering, frameSettings.enableSubsurfaceScattering ? 1 : 0);
            unsafe
            {
                // Warning: Unity is not able to losslessly transfer integers larger than 2^24 to the shader system.
                // Therefore, we bitcast uint to float in C#, and bitcast back to uint in the shader.
                uint texturingModeFlags = sssParameters.texturingModeFlags;
                uint transmissionFlags = sssParameters.transmissionFlags;
                cmd.SetGlobalFloat(HDShaderIDs._TexturingModeFlags, *(float*)&texturingModeFlags);
                cmd.SetGlobalFloat(HDShaderIDs._TransmissionFlags, *(float*)&transmissionFlags);
            }
            cmd.SetGlobalVectorArray(HDShaderIDs._ThicknessRemaps, sssParameters.thicknessRemaps);
            cmd.SetGlobalVectorArray(HDShaderIDs._ShapeParams, sssParameters.shapeParams);
            cmd.SetGlobalVectorArray(HDShaderIDs._HalfRcpVariancesAndWeights, sssParameters.halfRcpVariancesAndWeights);
            // To disable transmission, we simply nullify the transmissionTint
            cmd.SetGlobalVectorArray(HDShaderIDs._TransmissionTintsAndFresnel0, frameSettings.enableTransmission ? sssParameters.transmissionTintsAndFresnel0 : sssParameters.disabledTransmissionTintsAndFresnel0);
            cmd.SetGlobalVectorArray(HDShaderIDs._WorldScales, sssParameters.worldScales);
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
        public void SubsurfaceScatteringPass(HDCamera hdCamera, CommandBuffer cmd, DiffusionProfileSettings sssParameters, FrameSettings frameSettings,
                                            RenderTargetIdentifier colorBufferRT, RenderTargetIdentifier diffuseBufferRT, RenderTargetIdentifier depthStencilBufferRT, RenderTargetIdentifier depthTextureRT)
        {
            if (sssParameters == null || !frameSettings.enableSubsurfaceScattering)
                return;

            using (new ProfilingSample(cmd, "Subsurface Scattering", CustomSamplerId.SubsurfaceScattering.GetSampler()))
            {
                // For Jimenez we always need an extra buffer, for Disney it depends on platform
                if (ShaderConfig.k_UseDisneySSS == 0 || NeedTemporarySubsurfaceBuffer())
                {
                    // Caution: must be same format as m_CameraSssDiffuseLightingBuffer
                    cmd.ReleaseTemporaryRT(m_CameraFilteringBuffer);
                    CoreUtils.CreateCmdTemporaryRT(cmd, m_CameraFilteringBuffer, hdCamera.renderTextureDesc, 0, FilterMode.Point, RenderTextureFormat.RGB111110Float, RenderTextureReadWrite.Linear, 1, true); // Enable UAV

                    // Clear the SSS filtering target
                    using (new ProfilingSample(cmd, "Clear SSS filtering target", CustomSamplerId.ClearSSSFilteringTarget.GetSampler()))
                    {
                        CoreUtils.SetRenderTarget(cmd, m_CameraFilteringBufferRT, ClearFlag.Color, CoreUtils.clearColorAllBlack);
                    }
                }

                if (ShaderConfig.s_UseDisneySSS == 1) // use static here to quiet the compiler warning
                {
                    using (new ProfilingSample(cmd, "HTile for SSS", CustomSamplerId.HTileForSSS.GetSampler()))
                    {
                        // Currently, Unity does not offer a way to access the GCN HTile even on PS4 and Xbox One.
                        // Therefore, it's computed in a pixel shader, and optimized to only contain the SSS bit.
                        CoreUtils.SetRenderTarget(cmd, m_HTileRT, ClearFlag.Color, CoreUtils.clearColorAllBlack);

                        cmd.SetRandomWriteTarget(1, m_HTile);
                        // Generate HTile for the split lighting stencil usage. Don't write into stencil texture (shaderPassId = 2)
                        // Use ShaderPassID 1 => "Pass 2 - Export HTILE for stencilRef to output"
                        CoreUtils.SetRenderTarget(cmd, depthStencilBufferRT); // No need for color buffer here
                        CoreUtils.DrawFullScreen(cmd, m_CopyStencilForSplitLighting, null, 2);
                        cmd.ClearRandomWriteTargets();
                    }

                    // TODO: Remove this once fix, see comment inside the function
                    hdCamera.SetupComputeShader(m_SubsurfaceScatteringCS, cmd);

                    unsafe
                    {
                        // Warning: Unity is not able to losslessly transfer integers larger than 2^24 to the shader system.
                        // Therefore, we bitcast uint to float in C#, and bitcast back to uint in the shader.
                        uint texturingModeFlags = sssParameters.texturingModeFlags;
                        cmd.SetComputeFloatParam(m_SubsurfaceScatteringCS, HDShaderIDs._TexturingModeFlags, *(float*)&texturingModeFlags);
                    }

                    cmd.SetComputeVectorArrayParam(m_SubsurfaceScatteringCS, HDShaderIDs._WorldScales,        sssParameters.worldScales);
                    cmd.SetComputeVectorArrayParam(m_SubsurfaceScatteringCS, HDShaderIDs._FilterKernels,      sssParameters.filterKernels);
                    cmd.SetComputeVectorArrayParam(m_SubsurfaceScatteringCS, HDShaderIDs._ShapeParams,        sssParameters.shapeParams);

                    cmd.SetComputeTextureParam(m_SubsurfaceScatteringCS, m_SubsurfaceScatteringKernel, HDShaderIDs._DepthTexture,       depthTextureRT);
                    cmd.SetComputeTextureParam(m_SubsurfaceScatteringCS, m_SubsurfaceScatteringKernel, HDShaderIDs._SSSHTile,           m_HTileRT);
                    cmd.SetComputeTextureParam(m_SubsurfaceScatteringCS, m_SubsurfaceScatteringKernel, HDShaderIDs._IrradianceSource,   diffuseBufferRT);

                    for (int i = 0; i < sssBufferCount; ++i)
                    {
                        cmd.SetComputeTextureParam(m_SubsurfaceScatteringCS, m_SubsurfaceScatteringKernel, HDShaderIDs._SSSBufferTexture[i], GetSSSBuffers(i));
                    }

                    if (NeedTemporarySubsurfaceBuffer())
                    {
                        cmd.SetComputeTextureParam(m_SubsurfaceScatteringCS, m_SubsurfaceScatteringKernel, HDShaderIDs._CameraFilteringBuffer, m_CameraFilteringBufferRT);

                        // Perform the SSS filtering pass which fills 'm_CameraFilteringBufferRT'.
                        // We dispatch 4x swizzled 16x16 groups per a 32x32 macro tile.
                        cmd.DispatchCompute(m_SubsurfaceScatteringCS, m_SubsurfaceScatteringKernel, 4, ((int)hdCamera.screenSize.x + 31) / 32, ((int)hdCamera.screenSize.y + 31) / 32);

                        cmd.SetGlobalTexture(HDShaderIDs._IrradianceSource, m_CameraFilteringBufferRT);  // Cannot set a RT on a material

                        // Additively blend diffuse and specular lighting into 'm_CameraColorBufferRT'.
                        CoreUtils.DrawFullScreen(cmd, m_CombineLightingPass, colorBufferRT, depthStencilBufferRT);
                    }
                    else
                    {
                        cmd.SetComputeTextureParam(m_SubsurfaceScatteringCS, m_SubsurfaceScatteringKernel, HDShaderIDs._CameraColorTexture, colorBufferRT);

                        // Perform the SSS filtering pass which performs an in-place update of 'colorBuffer'.
                        // We dispatch 4x swizzled 16x16 groups per a 32x32 macro tile.
                        cmd.DispatchCompute(m_SubsurfaceScatteringCS, m_SubsurfaceScatteringKernel, 4, ((int)hdCamera.screenSize.x + 31) / 32, ((int)hdCamera.screenSize.y + 31) / 32);
                    }
                }
                else
                {
                    for (int i = 0; i < sssBufferCount; ++i)
                    {
                        cmd.SetGlobalTexture(HDShaderIDs._SSSBufferTexture[i], GetSSSBuffers(i));
                    }

                    cmd.SetGlobalTexture(HDShaderIDs._IrradianceSource, diffuseBufferRT);  // Cannot set a RT on a material
                    m_SssVerticalFilterPass.SetVectorArray(HDShaderIDs._FilterKernelsBasic,       sssParameters.filterKernelsBasic);
                    m_SssVerticalFilterPass.SetVectorArray(HDShaderIDs._HalfRcpWeightedVariances, sssParameters.halfRcpWeightedVariances);
                    // Perform the vertical SSS filtering pass which fills 'm_CameraFilteringBufferRT'.
                    CoreUtils.DrawFullScreen(cmd, m_SssVerticalFilterPass, m_CameraFilteringBufferRT, depthStencilBufferRT);

                    cmd.SetGlobalTexture(HDShaderIDs._IrradianceSource, m_CameraFilteringBufferRT);  // Cannot set a RT on a material
                    m_SssHorizontalFilterAndCombinePass.SetVectorArray(HDShaderIDs._FilterKernelsBasic,       sssParameters.filterKernelsBasic);
                    m_SssHorizontalFilterAndCombinePass.SetVectorArray(HDShaderIDs._HalfRcpWeightedVariances, sssParameters.halfRcpWeightedVariances);
                    // Perform the horizontal SSS filtering pass, and combine diffuse and specular lighting into 'm_CameraColorBufferRT'.
                    CoreUtils.DrawFullScreen(cmd, m_SssHorizontalFilterAndCombinePass, colorBufferRT, depthStencilBufferRT);
                }
            }
        }
    }
}
