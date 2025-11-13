using System;
using System.Runtime.CompilerServices;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    // The Screen Space Ambient Occlusion (SSAO) Pass
    internal class ScreenSpaceAmbientOcclusionPass : ScriptableRenderPass
    {
        // Private Variables
        private readonly bool m_SupportsR8RenderTextureFormat = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.R8);
        private int m_BlueNoiseTextureIndex = 0;
        private Material m_Material;
        private Texture2D[] m_BlueNoiseTextures;
        private Vector4[] m_CameraTopLeftCorner = new Vector4[2];
        private Vector4[] m_CameraXExtent = new Vector4[2];
        private Vector4[] m_CameraYExtent = new Vector4[2];
        private Vector4[] m_CameraZExtent = new Vector4[2];
        private BlurTypes m_BlurType = BlurTypes.Bilateral;
        private Matrix4x4[] m_CameraViewProjections = new Matrix4x4[2];
        private ProfilingSampler m_ProfilingSampler = ProfilingSampler.Get(URPProfileId.SSAO);
        private RenderTextureDescriptor m_AOPassDescriptor;
        private ScreenSpaceAmbientOcclusionSettings m_CurrentSettings;

        // Constants
        private const string k_SSAOTextureName = "_ScreenSpaceOcclusionTexture";
        private const string k_AmbientOcclusionParamName = "_AmbientOcclusionParam";

        // Statics
        internal static readonly int s_AmbientOcclusionParamID = Shader.PropertyToID(k_AmbientOcclusionParamName);
        private static readonly int s_SSAOParamsID = Shader.PropertyToID("_SSAOParams");
        private static readonly int s_SSAOBlueNoiseParamsID = Shader.PropertyToID("_SSAOBlueNoiseParams");
        private static readonly int s_BlueNoiseTextureID = Shader.PropertyToID("_BlueNoiseTexture");
        private static readonly int s_SSAOFinalTextureID = Shader.PropertyToID(k_SSAOTextureName);
        private static readonly int s_CameraViewXExtentID = Shader.PropertyToID("_CameraViewXExtent");
        private static readonly int s_CameraViewYExtentID = Shader.PropertyToID("_CameraViewYExtent");
        private static readonly int s_CameraViewZExtentID = Shader.PropertyToID("_CameraViewZExtent");
        private static readonly int s_ProjectionParams2ID = Shader.PropertyToID("_ProjectionParams2");
        private static readonly int s_CameraViewProjectionsID = Shader.PropertyToID("_CameraViewProjections");
        private static readonly int s_CameraViewTopLeftCornerID = Shader.PropertyToID("_CameraViewTopLeftCorner");
        private static readonly int s_CameraNormalsTextureID = Shader.PropertyToID("_CameraNormalsTexture");

        // Enums
        private enum BlurTypes
        {
            Bilateral,
            Gaussian,
            Kawase,
        }

        private enum ShaderPasses
        {
            AmbientOcclusion = 0,

            BilateralBlurHorizontal = 1,
            BilateralBlurVertical = 2,
            BilateralBlurFinal = 3,
            BilateralAfterOpaque = 4,

            GaussianBlurHorizontal = 5,
            GaussianBlurVertical = 6,
            GaussianAfterOpaque = 7,

            KawaseBlur = 8,
            KawaseAfterOpaque = 9,
        }

        // Structs
        private struct SSAOMaterialParams
        {
            internal bool orthographicCamera;
            internal bool aoBlueNoise;
            internal bool aoInterleavedGradient;
            internal bool sampleCountHigh;
            internal bool sampleCountMedium;
            internal bool sampleCountLow;
            internal bool sourceDepthNormals;
            internal bool sourceDepthHigh;
            internal bool sourceDepthMedium;
            internal bool sourceDepthLow;
            internal Vector4 ssaoParams;

            internal SSAOMaterialParams(ref ScreenSpaceAmbientOcclusionSettings settings, bool isOrthographic)
            {
                bool isUsingDepthNormals = settings.Source == ScreenSpaceAmbientOcclusionSettings.DepthSource.DepthNormals;
                float radiusMultiplier = settings.AOMethod == ScreenSpaceAmbientOcclusionSettings.AOMethodOptions.BlueNoise ? 1.5f : 1;
                orthographicCamera = isOrthographic;
                aoBlueNoise = settings.AOMethod == ScreenSpaceAmbientOcclusionSettings.AOMethodOptions.BlueNoise;
                aoInterleavedGradient = settings.AOMethod == ScreenSpaceAmbientOcclusionSettings.AOMethodOptions.InterleavedGradient;
                sampleCountHigh = settings.Samples == ScreenSpaceAmbientOcclusionSettings.AOSampleOption.High;
                sampleCountMedium = settings.Samples == ScreenSpaceAmbientOcclusionSettings.AOSampleOption.Medium;
                sampleCountLow = settings.Samples == ScreenSpaceAmbientOcclusionSettings.AOSampleOption.Low;
                sourceDepthNormals = settings.Source == ScreenSpaceAmbientOcclusionSettings.DepthSource.DepthNormals;
                sourceDepthHigh = !isUsingDepthNormals && settings.NormalSamples == ScreenSpaceAmbientOcclusionSettings.NormalQuality.High;
                sourceDepthMedium = !isUsingDepthNormals && settings.NormalSamples == ScreenSpaceAmbientOcclusionSettings.NormalQuality.Medium;
                sourceDepthLow = !isUsingDepthNormals && settings.NormalSamples == ScreenSpaceAmbientOcclusionSettings.NormalQuality.Low;
                ssaoParams = new Vector4(
                    settings.Intensity, // Intensity
                    settings.Radius * radiusMultiplier, // Radius
                    1.0f / (settings.Downsample ? 2 : 1), // Downsampling
                    settings.Falloff // Falloff
                );
            }

            internal bool Equals(ref SSAOMaterialParams other)
            {
                return orthographicCamera == other.orthographicCamera
                       && aoBlueNoise == other.aoBlueNoise
                       && aoInterleavedGradient == other.aoInterleavedGradient
                       && sampleCountHigh == other.sampleCountHigh
                       && sampleCountMedium == other.sampleCountMedium
                       && sampleCountLow == other.sampleCountLow
                       && sourceDepthNormals == other.sourceDepthNormals
                       && sourceDepthHigh == other.sourceDepthHigh
                       && sourceDepthMedium == other.sourceDepthMedium
                       && sourceDepthLow == other.sourceDepthLow
                       && ssaoParams == other.ssaoParams
                       ;
            }
        }
        private SSAOMaterialParams m_SSAOParamsPrev = new SSAOMaterialParams();

        internal ScreenSpaceAmbientOcclusionPass()
        {
            m_CurrentSettings = new ScreenSpaceAmbientOcclusionSettings();
        }

        internal bool Setup(ref ScreenSpaceAmbientOcclusionSettings featureSettings, ref ScriptableRenderer renderer, ref Material material, ref Texture2D[] blueNoiseTextures)
        {
            m_BlueNoiseTextures = blueNoiseTextures;
            m_Material = material;
            m_CurrentSettings = featureSettings;

            // RenderPass Event + Source Settings (Depth / Depth&Normals
            if (renderer is UniversalRenderer { usesDeferredLighting: true })
            {
                renderPassEvent = m_CurrentSettings.AfterOpaque ? RenderPassEvent.AfterRenderingOpaques : RenderPassEvent.AfterRenderingGbuffer;

                m_CurrentSettings.Source = ScreenSpaceAmbientOcclusionSettings.DepthSource.DepthNormals;
            }
            else
            {
                // Rendering after PrePasses is usually correct except when depth priming is in play:
                // then we rely on a depth resolve taking place after the PrePasses in order to have it ready for SSAO.
                // Hence we set the event to RenderPassEvent.AfterRenderingPrePasses + 1 at the earliest.
                renderPassEvent = m_CurrentSettings.AfterOpaque ? RenderPassEvent.BeforeRenderingTransparents : RenderPassEvent.AfterRenderingPrePasses + 1;
            }

            // Ask for a Depth or Depth + Normals textures
            switch (m_CurrentSettings.Source)
            {
                case ScreenSpaceAmbientOcclusionSettings.DepthSource.Depth:
                    ConfigureInput(ScriptableRenderPassInput.Depth);
                    break;
                case ScreenSpaceAmbientOcclusionSettings.DepthSource.DepthNormals:
                    ConfigureInput(ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal); // need depthNormal prepass for forward-only geometry
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // Blur settings
            switch (m_CurrentSettings.BlurQuality)
            {
                case ScreenSpaceAmbientOcclusionSettings.BlurQualityOptions.High:
                    m_BlurType = BlurTypes.Bilateral;
                    break;
                case ScreenSpaceAmbientOcclusionSettings.BlurQualityOptions.Medium:
                    m_BlurType = BlurTypes.Gaussian;
                    break;
                case ScreenSpaceAmbientOcclusionSettings.BlurQualityOptions.Low:
                    m_BlurType = BlurTypes.Kawase;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return m_Material != null
                   && m_CurrentSettings.Intensity > 0.0f
                   && m_CurrentSettings.Radius > 0.0f
                   && m_CurrentSettings.Falloff > 0.0f;
        }

        private void SetupKeywordsAndParameters(ref ScreenSpaceAmbientOcclusionSettings settings, ref UniversalCameraData cameraData)
        {
            #if ENABLE_VR && ENABLE_XR_MODULE
                int eyeCount = cameraData.xr.enabled && cameraData.xr.singlePassEnabled ? 2 : 1;
            #else
                int eyeCount = 1;
            #endif

            for (int eyeIndex = 0; eyeIndex < eyeCount; eyeIndex++)
            {
                Matrix4x4 view = cameraData.GetViewMatrix(eyeIndex);
                Matrix4x4 proj = cameraData.GetProjectionMatrix(eyeIndex);
                m_CameraViewProjections[eyeIndex] = proj * view;

                // camera view space without translation, used by SSAO.hlsl ReconstructViewPos() to calculate view vector.
                Matrix4x4 cview = view;
                cview.SetColumn(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
                Matrix4x4 cviewProj = proj * cview;
                Matrix4x4 cviewProjInv = cviewProj.inverse;

                Vector4 topLeftCorner = cviewProjInv.MultiplyPoint(new Vector4(-1, 1, -1, 1));
                Vector4 topRightCorner = cviewProjInv.MultiplyPoint(new Vector4(1, 1, -1, 1));
                Vector4 bottomLeftCorner = cviewProjInv.MultiplyPoint(new Vector4(-1, -1, -1, 1));
                Vector4 farCentre = cviewProjInv.MultiplyPoint(new Vector4(0, 0, 1, 1));
                m_CameraTopLeftCorner[eyeIndex] = topLeftCorner;
                m_CameraXExtent[eyeIndex] = topRightCorner - topLeftCorner;
                m_CameraYExtent[eyeIndex] = bottomLeftCorner - topLeftCorner;
                m_CameraZExtent[eyeIndex] = farCentre;
            }

            m_Material.SetVector(s_ProjectionParams2ID, new Vector4(1.0f / cameraData.camera.nearClipPlane, 0.0f, 0.0f, 0.0f));
            m_Material.SetMatrixArray(s_CameraViewProjectionsID, m_CameraViewProjections);
            m_Material.SetVectorArray(s_CameraViewTopLeftCornerID, m_CameraTopLeftCorner);
            m_Material.SetVectorArray(s_CameraViewXExtentID, m_CameraXExtent);
            m_Material.SetVectorArray(s_CameraViewYExtentID, m_CameraYExtent);
            m_Material.SetVectorArray(s_CameraViewZExtentID, m_CameraZExtent);

            if (settings.AOMethod == ScreenSpaceAmbientOcclusionSettings.AOMethodOptions.BlueNoise)
            {
                m_BlueNoiseTextureIndex = (m_BlueNoiseTextureIndex + 1) % m_BlueNoiseTextures.Length;
                Texture2D noiseTexture = m_BlueNoiseTextures[m_BlueNoiseTextureIndex];
                Vector4 blueNoiseParams = new Vector4(
                    cameraData.pixelWidth / (float)m_BlueNoiseTextures[m_BlueNoiseTextureIndex].width, // X Scale
                    cameraData.pixelHeight / (float)m_BlueNoiseTextures[m_BlueNoiseTextureIndex].height, // Y Scale
                    Random.value, // X Offset
                    Random.value // Y Offset
                );

                // For testing we use a single blue noise texture and a single set of blue noise params.
                #if UNITY_INCLUDE_TESTS
                    noiseTexture = m_BlueNoiseTextures[0];
                    blueNoiseParams.z = 1;
                    blueNoiseParams.w = 1;
                #endif

                m_Material.SetTexture(s_BlueNoiseTextureID, noiseTexture);
                m_Material.SetVector(s_SSAOBlueNoiseParamsID, blueNoiseParams);
            }

            // Setting keywords can be somewhat expensive on low-end platforms.
            // Previous params are cached to avoid setting the same keywords every frame.
            SSAOMaterialParams matParams = new SSAOMaterialParams(ref settings, cameraData.camera.orthographic);
            bool ssaoParamsDirty = !m_SSAOParamsPrev.Equals(ref matParams);    // Checks if the parameters have changed.
            bool isParamsPropertySet = m_Material.HasProperty(s_SSAOParamsID); // Checks if the parameters have been set on the material.
            if (!ssaoParamsDirty && isParamsPropertySet)
                return;

            m_SSAOParamsPrev = matParams;
            CoreUtils.SetKeyword(m_Material, ScreenSpaceAmbientOcclusion.k_OrthographicCameraKeyword,    matParams.orthographicCamera);
            CoreUtils.SetKeyword(m_Material, ScreenSpaceAmbientOcclusion.k_AOBlueNoiseKeyword,           matParams.aoBlueNoise);
            CoreUtils.SetKeyword(m_Material, ScreenSpaceAmbientOcclusion.k_AOInterleavedGradientKeyword, matParams.aoInterleavedGradient);
            CoreUtils.SetKeyword(m_Material, ScreenSpaceAmbientOcclusion.k_SampleCountHighKeyword,       matParams.sampleCountHigh);
            CoreUtils.SetKeyword(m_Material, ScreenSpaceAmbientOcclusion.k_SampleCountMediumKeyword,     matParams.sampleCountMedium);
            CoreUtils.SetKeyword(m_Material, ScreenSpaceAmbientOcclusion.k_SampleCountLowKeyword,        matParams.sampleCountLow);
            CoreUtils.SetKeyword(m_Material, ScreenSpaceAmbientOcclusion.k_SourceDepthNormalsKeyword,    matParams.sourceDepthNormals);
            CoreUtils.SetKeyword(m_Material, ScreenSpaceAmbientOcclusion.k_SourceDepthHighKeyword,       matParams.sourceDepthHigh);
            CoreUtils.SetKeyword(m_Material, ScreenSpaceAmbientOcclusion.k_SourceDepthMediumKeyword,     matParams.sourceDepthMedium);
            CoreUtils.SetKeyword(m_Material, ScreenSpaceAmbientOcclusion.k_SourceDepthLowKeyword,        matParams.sourceDepthLow);
            m_Material.SetVector(s_SSAOParamsID, matParams.ssaoParams);
        }

        /*----------------------------------------------------------------------------------------------------------------------------------------
         ------------------------------------------------------------- RENDER-GRAPH --------------------------------------------------------------
         ----------------------------------------------------------------------------------------------------------------------------------------*/

        private class SSAOPassData
        {
            internal bool afterOpaque;
            internal ScreenSpaceAmbientOcclusionSettings.BlurQualityOptions BlurQuality;
            internal Material material;
            internal float directLightingStrength;
            internal TextureHandle cameraColor;
            internal TextureHandle AOTexture;
            internal TextureHandle finalTexture;
            internal TextureHandle blurTexture;
            internal TextureHandle cameraNormalsTexture;
            internal UniversalCameraData cameraData;
        }

        private void InitSSAOPassData(ref SSAOPassData data)
        {
            data.material = m_Material;
            data.BlurQuality = m_CurrentSettings.BlurQuality;
            data.afterOpaque = m_CurrentSettings.AfterOpaque;
            data.directLightingStrength = m_CurrentSettings.DirectLightingStrength;
        }

        private static Vector4 ComputeScaleBias(in UnsafeGraphContext context, in TextureHandle source, in TextureHandle destination)
        {
            RTHandle srcRTHandle = source;
            Vector2 viewportScale;
            if (srcRTHandle is { useScaling: true })
            {
                viewportScale.x = srcRTHandle.rtHandleProperties.rtHandleScale.x;
                viewportScale.y = srcRTHandle.rtHandleProperties.rtHandleScale.y;
            }
            else
            {
                viewportScale = Vector2.one;
            }

            bool yFlip = context.GetTextureUVOrigin(in source) != context.GetTextureUVOrigin(in destination);
            if (yFlip)
                return new Vector4(viewportScale.x, -viewportScale.y, 0, viewportScale.y);
            else
                return new Vector4(viewportScale.x, viewportScale.y, 0, 0);
        }

        /// <inheritdoc cref="IRenderGraphRecorder.RecordRenderGraph"/>
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

            // Create the texture handles...
            CreateRenderTextureHandles(renderGraph,
                                       resourceData,
                                       cameraData,
                                       out TextureHandle aoTexture,
                                       out TextureHandle blurTexture,
                                       out TextureHandle finalTexture);

            // Get the resources
            TextureHandle cameraDepthTexture = resourceData.cameraDepthTexture;
            TextureHandle cameraNormalsTexture = resourceData.cameraNormalsTexture;

            // Update keywords and other shader params
            SetupKeywordsAndParameters(ref m_CurrentSettings, ref cameraData);

            using (IUnsafeRenderGraphBuilder builder = renderGraph.AddUnsafePass<SSAOPassData>("Blit SSAO", out var passData, m_ProfilingSampler))
            {
                // Shader keyword changes are considered as global state modifications
                builder.AllowGlobalStateModification(true);

                // Fill in the Pass data...
                InitSSAOPassData(ref passData);
                passData.cameraColor = resourceData.cameraColor;
                passData.AOTexture = aoTexture;
                passData.finalTexture = finalTexture;
                passData.blurTexture = blurTexture;
                passData.cameraData = cameraData;

                // Declare input textures
                builder.UseTexture(passData.AOTexture, AccessFlags.ReadWrite);

                // TODO: Refactor to eliminate the need for 'UseTexture'.
                // Currently required only because 'PostProcessUtils.SetSourceSize' allocates an RTHandle,
                // which expects a valid graphicsResource. Without this call, 'cameraColor.graphicsResource'
                // may be null if it wasn't initialized in an earlier pass (e.g., DrawOpaque).
                if (resourceData.cameraColor.IsValid())
                    builder.UseTexture(resourceData.cameraColor, AccessFlags.Read);

                if (passData.BlurQuality != ScreenSpaceAmbientOcclusionSettings.BlurQualityOptions.Low)
                    builder.UseTexture(passData.blurTexture, AccessFlags.ReadWrite);

                if (cameraDepthTexture.IsValid())
                    builder.UseTexture(cameraDepthTexture, AccessFlags.Read);

                if (m_CurrentSettings.Source == ScreenSpaceAmbientOcclusionSettings.DepthSource.DepthNormals && cameraNormalsTexture.IsValid())
                {
                    builder.UseTexture(cameraNormalsTexture, AccessFlags.Read);
                    passData.cameraNormalsTexture = cameraNormalsTexture;
                }

                // The global SSAO texture only needs to be set if After Opaque is disabled...
                if (!passData.afterOpaque && finalTexture.IsValid())
                {
                    builder.UseTexture(passData.finalTexture, AccessFlags.Write);
                    builder.SetGlobalTextureAfterPass(finalTexture, s_SSAOFinalTextureID);
                }

                builder.SetRenderFunc(static (SSAOPassData data, UnsafeGraphContext rgContext) =>
                {
                    CommandBuffer cmd = CommandBufferHelpers.GetNativeCommandBuffer(rgContext.cmd);
                    RenderBufferLoadAction finalLoadAction = data.afterOpaque ? RenderBufferLoadAction.Load : RenderBufferLoadAction.DontCare;

                    // Setup
                    PostProcessUtils.SetGlobalShaderSourceSize(cmd, data.cameraData.cameraTargetDescriptor.width, data.cameraData.cameraTargetDescriptor.height, data.cameraColor);

                    if (data.cameraNormalsTexture.IsValid())
                        data.material.SetTexture(s_CameraNormalsTextureID, data.cameraNormalsTexture);

                    // AO Pass
                    Blitter.BlitCameraTexture(cmd, data.AOTexture, data.AOTexture, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, data.material,  (int) ShaderPasses.AmbientOcclusion);

                    // Blur passes
                    Vector4 viewScaleBias;
                    switch (data.BlurQuality)
                    {
                        // Bilateral
                        case ScreenSpaceAmbientOcclusionSettings.BlurQualityOptions.High:
                            Blitter.BlitCameraTexture(cmd, data.AOTexture, data.blurTexture, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, data.material, (int) ShaderPasses.BilateralBlurHorizontal);
                            Blitter.BlitCameraTexture(cmd, data.blurTexture, data.AOTexture, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, data.material, (int) ShaderPasses.BilateralBlurVertical);
                            viewScaleBias = ComputeScaleBias(in rgContext, in data.AOTexture, in data.finalTexture);
                            Blitter.BlitCameraTexture(cmd, data.AOTexture, data.finalTexture, viewScaleBias, finalLoadAction, RenderBufferStoreAction.Store, data.material, (int) (data.afterOpaque ? ShaderPasses.BilateralAfterOpaque : ShaderPasses.BilateralBlurFinal));
                            break;

                        // Gaussian
                        case ScreenSpaceAmbientOcclusionSettings.BlurQualityOptions.Medium:
                            Blitter.BlitCameraTexture(cmd, data.AOTexture, data.blurTexture, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store, data.material, (int) ShaderPasses.GaussianBlurHorizontal);
                            viewScaleBias = ComputeScaleBias(in rgContext, in data.blurTexture, in data.finalTexture);
                            Blitter.BlitCameraTexture(cmd, data.blurTexture, data.finalTexture, viewScaleBias, finalLoadAction, RenderBufferStoreAction.Store, data.material, (int) (data.afterOpaque ? ShaderPasses.GaussianAfterOpaque : ShaderPasses.GaussianBlurVertical));
                            break;

                        // Kawase
                        case ScreenSpaceAmbientOcclusionSettings.BlurQualityOptions.Low:
                            viewScaleBias = ComputeScaleBias(in rgContext, in data.AOTexture, in data.finalTexture);
                            Blitter.BlitCameraTexture(cmd, data.AOTexture, data.finalTexture, viewScaleBias, finalLoadAction, RenderBufferStoreAction.Store, data.material, (int) (data.afterOpaque ? ShaderPasses.KawaseAfterOpaque : ShaderPasses.KawaseBlur));
                            break;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    // We only want URP shaders to sample SSAO if After Opaque is disabled...
                    if (!data.afterOpaque)
                    {
                        rgContext.cmd.SetKeyword(ShaderGlobalKeywords.ScreenSpaceOcclusion, true);
                        rgContext.cmd.SetGlobalVector(s_AmbientOcclusionParamID, new Vector4(1f, 0f, 0f, data.directLightingStrength));
                    }
                });
            }
        }

        private void CreateRenderTextureHandles(RenderGraph renderGraph, UniversalResourceData resourceData,
            UniversalCameraData cameraData, out TextureHandle aoTexture, out TextureHandle blurTexture, out TextureHandle finalTexture)
        {
            // Descriptor for the final blur pass
            RenderTextureDescriptor finalTextureDescriptor = cameraData.cameraTargetDescriptor;
            finalTextureDescriptor.colorFormat = m_SupportsR8RenderTextureFormat ? RenderTextureFormat.R8 : RenderTextureFormat.ARGB32;
            finalTextureDescriptor.depthStencilFormat = GraphicsFormat.None;
            finalTextureDescriptor.msaaSamples = 1;

            // Descriptor for the AO and Blur passes
            int downsampleDivider = m_CurrentSettings.Downsample ? 2 : 1;
            bool useRedComponentOnly = m_SupportsR8RenderTextureFormat && m_BlurType > BlurTypes.Bilateral;

            RenderTextureDescriptor aoBlurDescriptor = finalTextureDescriptor;
            aoBlurDescriptor.colorFormat = useRedComponentOnly ? RenderTextureFormat.R8 : RenderTextureFormat.ARGB32;
            aoBlurDescriptor.width /= downsampleDivider;
            aoBlurDescriptor.height /= downsampleDivider;

            // Handles
            aoTexture = UniversalRenderer.CreateRenderGraphTexture(renderGraph, aoBlurDescriptor, "_SSAO_OcclusionTexture0", false, FilterMode.Bilinear);
            finalTexture = m_CurrentSettings.AfterOpaque ? resourceData.activeColorTexture : UniversalRenderer.CreateRenderGraphTexture(renderGraph, finalTextureDescriptor, k_SSAOTextureName, false, FilterMode.Bilinear);

            if (m_CurrentSettings.BlurQuality != ScreenSpaceAmbientOcclusionSettings.BlurQualityOptions.Low)
                blurTexture = UniversalRenderer.CreateRenderGraphTexture(renderGraph, aoBlurDescriptor, "_SSAO_OcclusionTexture1", false, FilterMode.Bilinear);
            else
                blurTexture = TextureHandle.nullHandle;

            if (!m_CurrentSettings.AfterOpaque)
                resourceData.ssaoTexture = finalTexture;
        }

        /// <inheritdoc/>
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            if (cmd == null)
                throw new ArgumentNullException("cmd");

            if (!m_CurrentSettings.AfterOpaque)
                cmd.SetKeyword(ShaderGlobalKeywords.ScreenSpaceOcclusion, false);
        }

        public void Dispose()
        {
            m_SSAOParamsPrev = default;
        }
    }
}
