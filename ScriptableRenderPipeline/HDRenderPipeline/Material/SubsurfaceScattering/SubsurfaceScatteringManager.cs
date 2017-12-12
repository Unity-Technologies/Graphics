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

        public SubsurfaceScatteringManager()
        {
            m_SSSBuffer0RT = new RenderTargetIdentifier(m_SSSBuffer0);
        }

        // In case of deferred, we must be in sync with SubsurfaceScattering.hlsl and lit.hlsl files and setup the correct buffers
        // for SSS
        public void InitGBuffers(int width, int height, GBufferManager gbufferManager, CommandBuffer cmd)
        {
            m_RTIDs[0] = gbufferManager.GetGBuffers()[0];
        }

        // In case of full forward we must allocate the render target for forward SSS (or reuse one already existing)
        // TODO: Provide a way to reuse a render target
        public void InitGBuffers(int width, int height, CommandBuffer cmd)
        {
            m_RTIDs[0] = m_SSSBuffer0RT;

            cmd.ReleaseTemporaryRT(m_SSSBuffer0);
            cmd.GetTemporaryRT(m_SSSBuffer0, width, height, 0, FilterMode.Point, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);            
        }

        public RenderTargetIdentifier GetSSSBuffers(int index)
        {
            Debug.Assert(index < sssBufferCount);
            return m_RTIDs[index];
        }


        public void CreateSssMaterials()
        {
            m_SubsurfaceScatteringKernel = m_SubsurfaceScatteringCS.FindKernel("SubsurfaceScattering");

            CoreUtils.Destroy(m_CombineLightingPass);
            m_CombineLightingPass = CoreUtils.CreateEngineMaterial("Hidden/HDRenderPipeline/CombineLighting");

            // Old SSS Model >>>
            CoreUtils.Destroy(m_SssVerticalFilterPass);
            m_SssVerticalFilterPass = CoreUtils.CreateEngineMaterial("Hidden/HDRenderPipeline/SubsurfaceScattering");
            m_SssVerticalFilterPass.DisableKeyword("SSS_FILTER_HORIZONTAL_AND_COMBINE");
            m_SssVerticalFilterPass.SetFloat(HDShaderIDs._DstBlend, (float)BlendMode.Zero);

            CoreUtils.Destroy(m_SssHorizontalFilterAndCombinePass);
            m_SssHorizontalFilterAndCombinePass = CoreUtils.CreateEngineMaterial("Hidden/HDRenderPipeline/SubsurfaceScattering");
            m_SssHorizontalFilterAndCombinePass.EnableKeyword("SSS_FILTER_HORIZONTAL_AND_COMBINE");
            m_SssHorizontalFilterAndCombinePass.SetFloat(HDShaderIDs._DstBlend, (float)BlendMode.One);
            // <<< Old SSS Model
        }

        // Combines specular lighting and diffuse lighting with subsurface scattering.
        void SubsurfaceScatteringPass(HDCamera hdCamera, CommandBuffer cmd, SubsurfaceScatteringSettings sssParameters)
        {
            if (!m_CurrentDebugDisplaySettings.renderingDebugSettings.enableSSSAndTransmission)
                return;

            using (new ProfilingSample(cmd, "Subsurface Scattering", GetSampler(CustomSamplerId.SubsurfaceScattering)))
            {
                if (sssSettings.useDisneySSS)
                {
                    using (new ProfilingSample(cmd, "HTile for SSS", GetSampler(CustomSamplerId.HTileForSSS)))
                    {
                        CoreUtils.SetRenderTarget(cmd, m_HTileRT, ClearFlag.Color, CoreUtils.clearColorAllBlack);

                        cmd.SetRandomWriteTarget(1, GetHTile());
                        // Generate HTile for the split lighting stencil usage. Don't write into stencil texture (shaderPassId = 2)
                        // Use ShaderPassID 1 => "Pass 2 - Export HTILE for stencilRef to output"
                        CoreUtils.DrawFullScreen(cmd, m_CopyStencilForSplitLighting, m_CameraStencilBufferCopyRT, m_CameraDepthStencilBufferRT, null, 2);
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

                    cmd.SetComputeTextureParam(m_SubsurfaceScatteringCS, m_SubsurfaceScatteringKernel, HDShaderIDs._DepthTexture,     GetDepthTexture());
                    cmd.SetComputeTextureParam(m_SubsurfaceScatteringCS, m_SubsurfaceScatteringKernel, HDShaderIDs._HTile,            GetHTile());
                    cmd.SetComputeTextureParam(m_SubsurfaceScatteringCS, m_SubsurfaceScatteringKernel, HDShaderIDs._IrradianceSource, m_CameraSssDiffuseLightingBufferRT);

                    for (int i = 0; i < m_SSSBufferManager.sssBufferCount; ++i)
                    {
                        cmd.SetComputeTextureParam(m_SubsurfaceScatteringCS, m_SubsurfaceScatteringKernel, HDShaderIDs._SSSBufferTexture[i], m_SSSBufferManager.GetSSSBuffers(i));
                    }

                    if (NeedTemporarySubsurfaceBuffer())
                    {
                        cmd.SetComputeTextureParam(m_SubsurfaceScatteringCS, m_SubsurfaceScatteringKernel, HDShaderIDs._CameraFilteringBuffer, m_CameraFilteringBufferRT);

                        // Perform the SSS filtering pass which fills 'm_CameraFilteringBufferRT'.
                        // We dispatch 4x swizzled 16x16 groups per a 32x32 macrotile.
                        cmd.DispatchCompute(m_SubsurfaceScatteringCS, m_SubsurfaceScatteringKernel, 4, ((int)hdCamera.screenSize.x + 31) / 32, ((int)hdCamera.screenSize.y + 31) / 32);

                        cmd.SetGlobalTexture(HDShaderIDs._IrradianceSource, m_CameraFilteringBufferRT);  // Cannot set a RT on a material

                        // Additively blend diffuse and specular lighting into 'm_CameraColorBufferRT'.
                        CoreUtils.DrawFullScreen(cmd, m_CombineLightingPass, m_CameraColorBufferRT, m_CameraDepthStencilBufferRT);
                    }
                    else
                    {
                        cmd.SetComputeTextureParam(m_SubsurfaceScatteringCS, m_SubsurfaceScatteringKernel, HDShaderIDs._CameraColorTexture, m_CameraColorBufferRT);

                        // Perform the SSS filtering pass which performs an in-place update of 'm_CameraColorBufferRT'.
                        // We dispatch 4x swizzled 16x16 groups per a 32x32 macrotile.
                        cmd.DispatchCompute(m_SubsurfaceScatteringCS, m_SubsurfaceScatteringKernel, 4, ((int)hdCamera.screenSize.x + 31) / 32, ((int)hdCamera.screenSize.y + 31) / 32);
                    }
                }
                else
                {
                    for (int i = 0; i < m_SSSBufferManager.sssBufferCount; ++i)
                    {
                        cmd.SetGlobalTexture(HDShaderIDs._SSSBufferTexture[i], m_SSSBufferManager.GetSSSBuffers(i));
                    }

                    cmd.SetGlobalTexture(HDShaderIDs._IrradianceSource, m_CameraSssDiffuseLightingBufferRT);  // Cannot set a RT on a material
                    m_SssVerticalFilterPass.SetVectorArray(HDShaderIDs._FilterKernelsBasic,       sssParameters.filterKernelsBasic);
                    m_SssVerticalFilterPass.SetVectorArray(HDShaderIDs._HalfRcpWeightedVariances, sssParameters.halfRcpWeightedVariances);
                    // Perform the vertical SSS filtering pass which fills 'm_CameraFilteringBufferRT'.
                    CoreUtils.DrawFullScreen(cmd, m_SssVerticalFilterPass, m_CameraFilteringBufferRT, m_CameraDepthStencilBufferRT);

                    cmd.SetGlobalTexture(HDShaderIDs._IrradianceSource, m_CameraFilteringBufferRT);  // Cannot set a RT on a material
                    m_SssHorizontalFilterAndCombinePass.SetVectorArray(HDShaderIDs._FilterKernelsBasic,       sssParameters.filterKernelsBasic);
                    m_SssHorizontalFilterAndCombinePass.SetVectorArray(HDShaderIDs._HalfRcpWeightedVariances, sssParameters.halfRcpWeightedVariances);
                    // Perform the horizontal SSS filtering pass, and combine diffuse and specular lighting into 'm_CameraColorBufferRT'.
                    CoreUtils.DrawFullScreen(cmd, m_SssHorizontalFilterAndCombinePass, m_CameraColorBufferRT, m_CameraDepthStencilBufferRT);
                }
            }
        }
    }
}
