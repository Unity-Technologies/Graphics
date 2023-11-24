using System;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    // The Screen Space Ambient Occlusion (SSAO) Pass
    internal class ScreenSpaceAmbientOcclusionPass : ScriptableRenderPass
    {
        // Properties
        private bool isRendererDeferred => m_Renderer != null
                                           && m_Renderer is UniversalRenderer
                                           && ((UniversalRenderer)m_Renderer).renderingModeRequested == RenderingMode.Deferred;

        // Internal Variables
        internal string profilerTag;

        // Private Variables
        private bool m_SupportsR8RenderTextureFormat = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.R8);
        private int m_BlueNoiseTextureIndex = 0;
        private float m_BlurRandomOffsetX = 0f;
        private float m_BlurRandomOffsetY = 0f;
        private Material m_Material;
        private SetupPassData m_PassData;
        private Texture2D[] m_BlueNoiseTextures;
        private Vector4[] m_CameraTopLeftCorner = new Vector4[2];
        private Vector4[] m_CameraXExtent = new Vector4[2];
        private Vector4[] m_CameraYExtent = new Vector4[2];
        private Vector4[] m_CameraZExtent = new Vector4[2];
        private RTHandle[] m_SSAOTextures = new RTHandle[4];
        private BlurTypes m_BlurType = BlurTypes.Bilateral;
        private Matrix4x4[] m_CameraViewProjections = new Matrix4x4[2];
        private ProfilingSampler m_ProfilingSampler = ProfilingSampler.Get(URPProfileId.SSAO);
        private ScriptableRenderer m_Renderer = null;
        private RenderTextureDescriptor m_AOPassDescriptor;
        private ScreenSpaceAmbientOcclusionSettings m_CurrentSettings;

        // Constants
        private const string k_SSAOTextureName = "_ScreenSpaceOcclusionTexture";
        private const string k_AmbientOcclusionParamName = "_AmbientOcclusionParam";

        // Statics
        private static readonly int s_SSAOParamsID = Shader.PropertyToID("_SSAOParams");
        private static readonly int s_AmbientOcclusionParamID = Shader.PropertyToID(k_AmbientOcclusionParamName);
        private static readonly int s_SSAOBlueNoiseParamsID = Shader.PropertyToID("_SSAOBlueNoiseParams");
        private static readonly int s_BlueNoiseTextureID = Shader.PropertyToID("_BlueNoiseTexture");
        private static readonly int s_CameraViewXExtentID = Shader.PropertyToID("_CameraViewXExtent");
        private static readonly int s_CameraViewYExtentID = Shader.PropertyToID("_CameraViewYExtent");
        private static readonly int s_CameraViewZExtentID = Shader.PropertyToID("_CameraViewZExtent");
        private static readonly int s_ProjectionParams2ID = Shader.PropertyToID("_ProjectionParams2");
        private static readonly int s_CameraViewProjectionsID = Shader.PropertyToID("_CameraViewProjections");
        private static readonly int s_CameraViewTopLeftCornerID = Shader.PropertyToID("_CameraViewTopLeftCorner");

        private static readonly int[] m_BilateralTexturesIndices            = { 0, 1, 2, 3 };
        private static readonly ShaderPasses[] m_BilateralPasses            = { ShaderPasses.BilateralBlurHorizontal, ShaderPasses.BilateralBlurVertical, ShaderPasses.BilateralBlurFinal };
        private static readonly ShaderPasses[] m_BilateralAfterOpaquePasses = { ShaderPasses.BilateralBlurHorizontal, ShaderPasses.BilateralBlurVertical, ShaderPasses.BilateralAfterOpaque };

        private static readonly int[] m_GaussianTexturesIndices             = { 0, 1, 3, 3 };
        private static readonly ShaderPasses[] m_GaussianPasses             = { ShaderPasses.GaussianBlurHorizontal, ShaderPasses.GaussianBlurVertical };
        private static readonly ShaderPasses[] m_GaussianAfterOpaquePasses  = { ShaderPasses.GaussianBlurHorizontal, ShaderPasses.GaussianAfterOpaque };

        private static readonly int[] m_KawaseTexturesIndices               = { 0, 3 };
        private static readonly ShaderPasses[] m_KawasePasses               = { ShaderPasses.KawaseBlur };
        private static readonly ShaderPasses[] m_KawaseAfterOpaquePasses    = { ShaderPasses.KawaseAfterOpaque };

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

        internal ScreenSpaceAmbientOcclusionPass()
        {
            m_CurrentSettings = new ScreenSpaceAmbientOcclusionSettings();
            m_PassData = new SetupPassData();
        }

        internal bool Setup(ref ScreenSpaceAmbientOcclusionSettings featureSettings, ref ScriptableRenderer renderer, ref Material material, ref Texture2D[] blueNoiseTextures)
        {
            m_BlueNoiseTextures = blueNoiseTextures;
            m_Material = material;
            m_Renderer = renderer;
            m_CurrentSettings = featureSettings;

            // RenderPass Event + Source Settings (Depth / Depth&Normals
            if (isRendererDeferred)
            {
                renderPassEvent = m_CurrentSettings.AfterOpaque ? RenderPassEvent.AfterRenderingOpaques : RenderPassEvent.AfterRenderingGbuffer;

                if (renderPassEvent == RenderPassEvent.AfterRenderingGbuffer)
                    breakGBufferAndDeferredRenderPass = true;

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
                    ConfigureInput(ScriptableRenderPassInput.Normal); // need depthNormal prepass for forward-only geometry
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

        private static bool IsAfterOpaquePass(ref ShaderPasses pass)
        {
            return pass == ShaderPasses.BilateralAfterOpaque
                   || pass == ShaderPasses.GaussianAfterOpaque
                   || pass == ShaderPasses.KawaseAfterOpaque;
        }

        private static void SetupKeywordsAndParameters(ref SetupPassData passData, UniversalCameraData cameraData)
        {
            int downsampleDivider = passData.settings.Downsample ? 2 : 1;

            #if ENABLE_VR && ENABLE_XR_MODULE
                int eyeCount = cameraData.xr.enabled && cameraData.xr.singlePassEnabled ? 2 : 1;
            #else
                int eyeCount = 1;
            #endif

            for (int eyeIndex = 0; eyeIndex < eyeCount; eyeIndex++)
            {
                Matrix4x4 view = cameraData.GetViewMatrix(eyeIndex);
                Matrix4x4 proj = cameraData.GetProjectionMatrix(eyeIndex);
                passData.cameraViewProjections[eyeIndex] = proj * view;

                // camera view space without translation, used by SSAO.hlsl ReconstructViewPos() to calculate view vector.
                Matrix4x4 cview = view;
                cview.SetColumn(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
                Matrix4x4 cviewProj = proj * cview;
                Matrix4x4 cviewProjInv = cviewProj.inverse;

                Vector4 topLeftCorner = cviewProjInv.MultiplyPoint(new Vector4(-1, 1, -1, 1));
                Vector4 topRightCorner = cviewProjInv.MultiplyPoint(new Vector4(1, 1, -1, 1));
                Vector4 bottomLeftCorner = cviewProjInv.MultiplyPoint(new Vector4(-1, -1, -1, 1));
                Vector4 farCentre = cviewProjInv.MultiplyPoint(new Vector4(0, 0, 1, 1));
                passData.cameraTopLeftCorner[eyeIndex] = topLeftCorner;
                passData.cameraXExtent[eyeIndex] = topRightCorner - topLeftCorner;
                passData.cameraYExtent[eyeIndex] = bottomLeftCorner - topLeftCorner;
                passData.cameraZExtent[eyeIndex] = farCentre;
            }

            passData.material.SetVector(s_ProjectionParams2ID, new Vector4(1.0f / cameraData.camera.nearClipPlane, 0.0f, 0.0f, 0.0f));
            passData.material.SetMatrixArray(s_CameraViewProjectionsID, passData.cameraViewProjections);
            passData.material.SetVectorArray(s_CameraViewTopLeftCornerID, passData.cameraTopLeftCorner);
            passData.material.SetVectorArray(s_CameraViewXExtentID, passData.cameraXExtent);
            passData.material.SetVectorArray(s_CameraViewYExtentID, passData.cameraYExtent);
            passData.material.SetVectorArray(s_CameraViewZExtentID, passData.cameraZExtent);

            // Update keywords
            CoreUtils.SetKeyword(passData.material, ScreenSpaceAmbientOcclusion.k_OrthographicCameraKeyword, cameraData.camera.orthographic);
            CoreUtils.SetKeyword(passData.material, ScreenSpaceAmbientOcclusion.k_AOBlueNoiseKeyword, false);
            CoreUtils.SetKeyword(passData.material, ScreenSpaceAmbientOcclusion.k_AOInterleavedGradientKeyword, false);
            switch (passData.settings.AOMethod)
            {
                case ScreenSpaceAmbientOcclusionSettings.AOMethodOptions.BlueNoise:
                    CoreUtils.SetKeyword(passData.material, ScreenSpaceAmbientOcclusion.k_AOBlueNoiseKeyword, true);
                    passData.blueNoiseTextureIndex = (passData.blueNoiseTextureIndex + 1) % passData.blueNoiseTextures.Length;
                    passData.blurRandomOffsetX = Random.value;
                    passData.blurRandomOffsetY = Random.value;

                    Texture2D noiseTexture = passData.blueNoiseTextures[passData.blueNoiseTextureIndex];
                    passData.material.SetTexture(s_BlueNoiseTextureID, noiseTexture);

                    passData.material.SetVector(s_SSAOParamsID, new Vector4(
                        passData.settings.Intensity, // Intensity
                        passData.settings.Radius * 1.5f, // Radius
                        1.0f / downsampleDivider, // Downsampling
                        passData.settings.Falloff // Falloff
                    ));

                    passData.material.SetVector(s_SSAOBlueNoiseParamsID, new Vector4(
                        cameraData.pixelWidth / (float)noiseTexture.width, // X Scale
                        cameraData.pixelHeight / (float)noiseTexture.height, // Y Scale
                        passData.blurRandomOffsetX, // X Offset
                        passData.blurRandomOffsetY // Y Offset
                    ));
                    break;
                case ScreenSpaceAmbientOcclusionSettings.AOMethodOptions.InterleavedGradient:
                    CoreUtils.SetKeyword(passData.material, ScreenSpaceAmbientOcclusion.k_AOInterleavedGradientKeyword, true);

                    // Update SSAO parameters in the material
                    passData.material.SetVector(s_SSAOParamsID, new Vector4(
                        passData.settings.Intensity, // Intensity
                        passData.settings.Radius, // Radius
                        1.0f / downsampleDivider, // Downsampling
                        passData.settings.Falloff // Falloff
                    ));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            CoreUtils.SetKeyword(passData.material, ScreenSpaceAmbientOcclusion.k_SampleCountLowKeyword, false);
            CoreUtils.SetKeyword(passData.material, ScreenSpaceAmbientOcclusion.k_SampleCountMediumKeyword, false);
            CoreUtils.SetKeyword(passData.material, ScreenSpaceAmbientOcclusion.k_SampleCountHighKeyword, false);
            switch (passData.settings.Samples)
            {
                case ScreenSpaceAmbientOcclusionSettings.AOSampleOption.High:
                    CoreUtils.SetKeyword(passData.material, ScreenSpaceAmbientOcclusion.k_SampleCountHighKeyword, true);
                    break;
                case ScreenSpaceAmbientOcclusionSettings.AOSampleOption.Medium:
                    CoreUtils.SetKeyword(passData.material, ScreenSpaceAmbientOcclusion.k_SampleCountMediumKeyword, true);
                    break;
                default:
                    CoreUtils.SetKeyword(passData.material, ScreenSpaceAmbientOcclusion.k_SampleCountLowKeyword, true);
                    break;
            }

            CoreUtils.SetKeyword(passData.material, ScreenSpaceAmbientOcclusion.k_OrthographicCameraKeyword, cameraData.camera.orthographic);

            // Set the source keywords...
            if (passData.settings.Source == ScreenSpaceAmbientOcclusionSettings.DepthSource.Depth)
            {
                CoreUtils.SetKeyword(passData.material, ScreenSpaceAmbientOcclusion.k_SourceDepthNormalsKeyword, false);
                switch (passData.settings.NormalSamples)
                {
                    case ScreenSpaceAmbientOcclusionSettings.NormalQuality.Low:
                        CoreUtils.SetKeyword(passData.material, ScreenSpaceAmbientOcclusion.k_SourceDepthLowKeyword, true);
                        CoreUtils.SetKeyword(passData.material, ScreenSpaceAmbientOcclusion.k_SourceDepthMediumKeyword, false);
                        CoreUtils.SetKeyword(passData.material, ScreenSpaceAmbientOcclusion.k_SourceDepthHighKeyword, false);
                        break;
                    case ScreenSpaceAmbientOcclusionSettings.NormalQuality.Medium:
                        CoreUtils.SetKeyword(passData.material, ScreenSpaceAmbientOcclusion.k_SourceDepthLowKeyword, false);
                        CoreUtils.SetKeyword(passData.material, ScreenSpaceAmbientOcclusion.k_SourceDepthMediumKeyword, true);
                        CoreUtils.SetKeyword(passData.material, ScreenSpaceAmbientOcclusion.k_SourceDepthHighKeyword, false);
                        break;
                    case ScreenSpaceAmbientOcclusionSettings.NormalQuality.High:
                        CoreUtils.SetKeyword(passData.material, ScreenSpaceAmbientOcclusion.k_SourceDepthLowKeyword, false);
                        CoreUtils.SetKeyword(passData.material, ScreenSpaceAmbientOcclusion.k_SourceDepthMediumKeyword, false);
                        CoreUtils.SetKeyword(passData.material, ScreenSpaceAmbientOcclusion.k_SourceDepthHighKeyword, true);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            else
            {
                CoreUtils.SetKeyword(passData.material, ScreenSpaceAmbientOcclusion.k_SourceDepthLowKeyword, false);
                CoreUtils.SetKeyword(passData.material, ScreenSpaceAmbientOcclusion.k_SourceDepthMediumKeyword, false);
                CoreUtils.SetKeyword(passData.material, ScreenSpaceAmbientOcclusion.k_SourceDepthHighKeyword, false);
                CoreUtils.SetKeyword(passData.material, ScreenSpaceAmbientOcclusion.k_SourceDepthNormalsKeyword, true);
            }
        }

        /*----------------------------------------------------------------------------------------------------------------------------------------
         ------------------------------------------------------------- RENDER-GRAPH --------------------------------------------------------------
         ----------------------------------------------------------------------------------------------------------------------------------------*/

        private class SetupPassData
        {
            internal ScreenSpaceAmbientOcclusionSettings settings;
            internal UniversalCameraData cameraData;
            internal TextureHandle cameraColor;
            internal RTHandle[] ssaoTextures;
            internal Texture2D[] blueNoiseTextures;
            internal Material material;
            internal Vector4 ambientOcclusionParams;
            internal Vector4[] cameraTopLeftCorner;
            internal Vector4[] cameraXExtent;
            internal Vector4[] cameraYExtent;
            internal Vector4[] cameraZExtent;
            internal Matrix4x4[] cameraViewProjections;
            internal int blueNoiseTextureIndex;
            internal float blurRandomOffsetX;
            internal float blurRandomOffsetY;
        }

        private class PassData
        {
            internal int shaderPassID;
            internal bool afterOpaque;
            internal Material material;
            internal TextureHandle source;
            internal TextureHandle destination;
            internal TextureHandle depthTexture;
            internal TextureHandle normalTexture;
        }

        private void InitSetupPassData(ref SetupPassData data)
        {
            // Fill in the Pass data...
            data.settings = m_CurrentSettings;
            data.ssaoTextures = m_SSAOTextures;
            data.blueNoiseTextures = m_BlueNoiseTextures;
            data.material = m_Material;
            data.cameraTopLeftCorner = m_CameraTopLeftCorner;
            data.cameraXExtent = m_CameraXExtent;
            data.cameraYExtent = m_CameraYExtent;
            data.cameraZExtent = m_CameraZExtent;
            data.cameraViewProjections = m_CameraViewProjections;
            data.blueNoiseTextureIndex = m_BlueNoiseTextureIndex;
            data.blurRandomOffsetX = m_BlurRandomOffsetX;
            data.blurRandomOffsetY = m_BlurRandomOffsetY;
        }

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
            // Setup up keywords and parameters...
            ExecuteSetupPass(renderGraph, cameraData, resourceData);

            // Ambient Occlusion Pass...
            UniversalRenderer renderer = cameraData.renderer as UniversalRenderer;
            ExecuteOcclusionPass(renderGraph, resourceData, in aoTexture, renderer.renderingModeActual);

            // Blur & Upsample Passes...
            switch (m_BlurType)
            {
                case BlurTypes.Bilateral:
                    ExecuteBilateralBlurPasses(renderGraph, in aoTexture, in blurTexture, in finalTexture);
                    break;
                case BlurTypes.Gaussian:
                    ExecuteGaussianBlurPasses(renderGraph, in aoTexture, in blurTexture, in finalTexture);
                    break;
                case BlurTypes.Kawase:
                    ExecuteKawaseBlurPasses(renderGraph, in aoTexture, in finalTexture);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void ExecuteSetupPass(RenderGraph renderGraph, UniversalCameraData cameraData, UniversalResourceData resourceData)
        {
            using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass<SetupPassData>("SSAO_Setup", out var passData, m_ProfilingSampler))
            {
                // Initialize the pass data
                InitSetupPassData(ref passData);
                passData.cameraColor = resourceData.cameraColor;
                passData.cameraData = cameraData;
                passData.ambientOcclusionParams = new Vector4(0f, 0f, 0f, m_CurrentSettings.DirectLightingStrength);

                // Shader keyword changes are considered as global state modifications
                builder.AllowGlobalStateModification(true);
                builder.AllowPassCulling(false);

                // Set up the builder
                builder.SetRenderFunc((SetupPassData data, RasterGraphContext rgContext) =>
                {
                    if (data.cameraColor.IsValid())
                        PostProcessUtils.SetSourceSize(rgContext.cmd, data.cameraColor);

                    SetupKeywordsAndParameters(ref data, data.cameraData);

                    // We only want URP shaders to sample SSAO if After Opaque is disabled...
                    if (!data.settings.AfterOpaque)
                    {
                        rgContext.cmd.SetKeyword(ShaderGlobalKeywords.ScreenSpaceOcclusion, true);
                        rgContext.cmd.SetGlobalVector(s_AmbientOcclusionParamID, data.ambientOcclusionParams);
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
            finalTextureDescriptor.depthBufferBits = 0;
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
            blurTexture = UniversalRenderer.CreateRenderGraphTexture(renderGraph, aoBlurDescriptor, "_SSAO_OcclusionTexture1", false, FilterMode.Bilinear);
            finalTexture = m_CurrentSettings.AfterOpaque ? resourceData.activeColorTexture : UniversalRenderer.CreateRenderGraphTexture(renderGraph, finalTextureDescriptor, k_SSAOTextureName, false, FilterMode.Bilinear);

            if (!m_CurrentSettings.AfterOpaque)
                resourceData.ssaoTexture = finalTexture;
        }

        private void ExecuteOcclusionPass(RenderGraph renderGraph, UniversalResourceData resourceData, in TextureHandle aoTexture, RenderingMode renderingMode)
        {
            using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass<PassData>("SSAO_Occlusion", out PassData passData, m_ProfilingSampler))
            {
                // Initialize the pass data
                passData.source = aoTexture;
                passData.material = m_Material;
                passData.shaderPassID = (int)ShaderPasses.AmbientOcclusion;

                // Set up the builder
                builder.SetRenderAttachment(aoTexture, 0, AccessFlags.Write);

                // Get the resources
                TextureHandle cameraDepthTexture = renderingMode == RenderingMode.Deferred ? resourceData.activeDepthTexture : resourceData.cameraDepthTexture;
                TextureHandle cameraNormalsTexture = resourceData.cameraNormalsTexture;

                if (cameraDepthTexture.IsValid())
                {
                    passData.depthTexture = cameraDepthTexture;
                    builder.UseTexture(cameraDepthTexture, AccessFlags.Read);
                }

                if (m_CurrentSettings.Source == ScreenSpaceAmbientOcclusionSettings.DepthSource.DepthNormals)
                {
                    if (cameraNormalsTexture.IsValid())
                    {
                        passData.normalTexture = cameraNormalsTexture;
                        builder.UseTexture(cameraNormalsTexture, AccessFlags.Read);
                    }
                }

                builder.SetRenderFunc<PassData>((data, context) => RenderGraphRenderFunc(data, context));
            }
        }

        private void ExecuteBilateralBlurPasses(RenderGraph renderGraph, in TextureHandle aoTexture, in TextureHandle blurTexture, in TextureHandle finalTexture)
        {
            using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass<PassData>("SSAO_Bilateral_HorizontalBlur", out var passData, m_ProfilingSampler))
            {
                // Initialize the pass data
                passData.source = aoTexture;
                builder.UseTexture(aoTexture, AccessFlags.Read);
                passData.destination = blurTexture;
                builder.SetRenderAttachment(blurTexture, 0, AccessFlags.Write);
                passData.material = m_Material;
                passData.shaderPassID = (int) ShaderPasses.BilateralBlurHorizontal;

                // Set up the builder
                builder.SetRenderFunc<PassData>((data, context) => RenderGraphRenderFunc(data, context));
            }

            using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass<PassData>("SSAO_Bilateral_VerticalBlur", out var passData, m_ProfilingSampler))
            {
                // Initialize the pass data
                passData.source = blurTexture;
                builder.UseTexture(blurTexture, AccessFlags.Read);
                passData.destination = aoTexture;
                builder.SetRenderAttachment(aoTexture, 0, AccessFlags.Write);
                passData.material = m_Material;
                passData.shaderPassID = (int) ShaderPasses.BilateralBlurVertical;

                // Set up the builder
                builder.SetRenderFunc<PassData>((data, context) => RenderGraphRenderFunc(data, context));
            }

            using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass<PassData>("SSAO_Bilateral_FinalBlur", out var passData, m_ProfilingSampler))
            {
                // Initialize the pass data
                passData.source = aoTexture;
                builder.UseTexture(aoTexture, AccessFlags.Read);
                passData.destination = finalTexture;
                builder.SetRenderAttachment(finalTexture, 0, AccessFlags.Write);
                passData.material = m_Material;
                passData.afterOpaque = m_CurrentSettings.AfterOpaque;
                passData.shaderPassID = (int) (passData.afterOpaque ? ShaderPasses.BilateralAfterOpaque : ShaderPasses.BilateralBlurFinal);

                // The global SSAO texture only needs to be set if After Opaque is disabled...
                if (!m_CurrentSettings.AfterOpaque && finalTexture.IsValid())
                    builder.SetGlobalTextureAfterPass(finalTexture, Shader.PropertyToID(k_SSAOTextureName));

                // Set up the builder
                builder.SetRenderFunc<PassData>((data, context) => RenderGraphRenderFunc(data, context));
            }
        }

        private void ExecuteGaussianBlurPasses(RenderGraph renderGraph, in TextureHandle aoTexture, in TextureHandle blurTexture, in TextureHandle finalTexture)
        {
            using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass<PassData>("SSAO_Gaussian_HorizontalBlur", out var passData, m_ProfilingSampler))
            {
                // Initialize the pass data
                passData.source = aoTexture;
                builder.UseTexture(aoTexture, AccessFlags.Read);
                passData.destination = blurTexture;
                builder.SetRenderAttachment(blurTexture, 0, AccessFlags.Write);
                passData.material = m_Material;
                passData.shaderPassID = (int) ShaderPasses.GaussianBlurHorizontal;

                // Set up the builder
                builder.SetRenderFunc<PassData>((data, context) => RenderGraphRenderFunc(data, context));
            }

            using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass<PassData>("SSAO_Gaussian_VerticalBlur", out var passData, m_ProfilingSampler))
            {
                // Initialize the pass data
                passData.source = blurTexture;
                builder.UseTexture(blurTexture, AccessFlags.Read);
                passData.destination = finalTexture;
                builder.SetRenderAttachment(finalTexture, 0, AccessFlags.Write);
                passData.material = m_Material;
                passData.afterOpaque = m_CurrentSettings.AfterOpaque;
                passData.shaderPassID = (int) (passData.afterOpaque ? ShaderPasses.GaussianAfterOpaque : ShaderPasses.GaussianBlurVertical);

                // The global SSAO texture only needs to be set if After Opaque is disabled...
                if (!m_CurrentSettings.AfterOpaque && finalTexture.IsValid())
                    builder.SetGlobalTextureAfterPass(finalTexture, Shader.PropertyToID(k_SSAOTextureName));

                // Set up the builder
                builder.SetRenderFunc<PassData>((data, context) => RenderGraphRenderFunc(data, context));
            }
        }

        private void ExecuteKawaseBlurPasses(RenderGraph renderGraph, in TextureHandle aoTexture, in TextureHandle finalTexture)
        {
            using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass<PassData>("SSAO_Kawase", out var passData, m_ProfilingSampler))
            {
                // Initialize the pass data
                passData.source = aoTexture;
                builder.UseTexture(aoTexture, AccessFlags.Read);
                passData.destination = finalTexture;
                builder.SetRenderAttachment(finalTexture, 0, AccessFlags.Write);
                passData.material = m_Material;
                passData.afterOpaque = m_CurrentSettings.AfterOpaque;
                passData.shaderPassID = (int) (passData.afterOpaque ? ShaderPasses.KawaseAfterOpaque : ShaderPasses.KawaseBlur);

                // The global SSAO texture only needs to be set if After Opaque is disabled...
                if (!m_CurrentSettings.AfterOpaque && finalTexture.IsValid())
                    builder.SetGlobalTextureAfterPass(finalTexture, Shader.PropertyToID(k_SSAOTextureName));

                // Set up the builder
                builder.SetRenderFunc<PassData>((data, context) => RenderGraphRenderFunc(data, context));
            }
        }

        private static void RenderGraphRenderFunc(PassData data, RasterGraphContext context)
        {
            if (data.depthTexture.IsValid())
                data.material.SetTexture(Shader.PropertyToID("_CameraDepthTexture"), data.depthTexture);
            if (data.normalTexture.IsValid())
                data.material.SetTexture(Shader.PropertyToID("_CameraNormalsTexture"), data.normalTexture);

            Blitter.BlitTexture(context.cmd, data.source, Vector2.one, data.material, data.shaderPassID);
        }

        /*----------------------------------------------------------------------------------------------------------------------------------------
         ------------------------------------------------------------- RENDER-GRAPH --------------------------------------------------------------
         ----------------------------------------------------------------------------------------------------------------------------------------*/

        /// <inheritdoc/>
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            ContextContainer frameData = renderingData.frameData;
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            // Fill in the Pass data...
            InitSetupPassData(ref m_PassData);

            SetupKeywordsAndParameters(ref m_PassData, cameraData);

            // Set up the descriptors
            int downsampleDivider = m_CurrentSettings.Downsample ? 2 : 1;
            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.msaaSamples = 1;
            descriptor.depthBufferBits = 0;

            // AO PAss
            m_AOPassDescriptor = descriptor;
            m_AOPassDescriptor.width /= downsampleDivider;
            m_AOPassDescriptor.height /= downsampleDivider;
            bool useRedComponentOnly = m_SupportsR8RenderTextureFormat && m_BlurType > BlurTypes.Bilateral;
            m_AOPassDescriptor.colorFormat = useRedComponentOnly ? RenderTextureFormat.R8 : RenderTextureFormat.ARGB32;

            // Allocate textures for the AO and blur
            RenderingUtils.ReAllocateIfNeeded(ref m_PassData.ssaoTextures[0], m_AOPassDescriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_SSAO_OcclusionTexture0");
            RenderingUtils.ReAllocateIfNeeded(ref m_PassData.ssaoTextures[1], m_AOPassDescriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_SSAO_OcclusionTexture1");
            RenderingUtils.ReAllocateIfNeeded(ref m_PassData.ssaoTextures[2], m_AOPassDescriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_SSAO_OcclusionTexture2");

            // Upsample setup
            m_AOPassDescriptor.width *= downsampleDivider;
            m_AOPassDescriptor.height *= downsampleDivider;
            m_AOPassDescriptor.colorFormat = m_SupportsR8RenderTextureFormat ? RenderTextureFormat.R8 : RenderTextureFormat.ARGB32;

            // Allocate texture for the final SSAO results
            RenderingUtils.ReAllocateIfNeeded(ref m_PassData.ssaoTextures[3], m_AOPassDescriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_SSAO_OcclusionTexture");
            PostProcessUtils.SetSourceSize(cmd, m_PassData.ssaoTextures[3]);

            // Configure targets and clear color
            ConfigureTarget(m_CurrentSettings.AfterOpaque ? m_Renderer.cameraColorTargetHandle : m_SSAOTextures[3]);
            ConfigureClear(ClearFlag.None, Color.white);
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_Material == null)
            {
                Debug.LogErrorFormat(
                    "{0}.Execute(): Missing material. ScreenSpaceAmbientOcclusion pass will not execute. Check for missing reference in the renderer resources.",
                    GetType().Name);
                return;
            }

            var cmd = renderingData.commandBuffer;
            using (new ProfilingScope(cmd, ProfilingSampler.Get(URPProfileId.SSAO)))
            {
                // We only want URP shaders to sample SSAO if After Opaque is off.
                if (!m_CurrentSettings.AfterOpaque)
                    cmd.SetKeyword(ShaderGlobalKeywords.ScreenSpaceOcclusion, true);

                cmd.SetGlobalTexture(k_SSAOTextureName, m_SSAOTextures[3]);

                #if ENABLE_VR && ENABLE_XR_MODULE
                    bool isFoveatedEnabled = false;
                    if (renderingData.cameraData.xr.supportsFoveatedRendering)
                    {
                        // If we are downsampling we can't use the VRS texture
                        // If it's a non uniform raster foveated rendering has to be turned off because it will keep applying non uniform for the other passes.
                        // When calculating normals from depth, this causes artifacts that are amplified from VRS when going to say 4x4. Thus we disable foveated because of that
                        if (m_CurrentSettings.Downsample || SystemInfo.foveatedRenderingCaps.HasFlag(FoveatedRenderingCaps.NonUniformRaster) ||
                            (SystemInfo.foveatedRenderingCaps.HasFlag(FoveatedRenderingCaps.FoveationImage) && m_CurrentSettings.Source == ScreenSpaceAmbientOcclusionSettings.DepthSource.Depth))
                        {
                            cmd.SetFoveatedRenderingMode(FoveatedRenderingMode.Disabled);
                        }
                        // If we aren't downsampling and it's a VRS texture we can apply foveation in this case
                        else if (SystemInfo.foveatedRenderingCaps.HasFlag(FoveatedRenderingCaps.FoveationImage))
                        {
                            cmd.SetFoveatedRenderingMode(FoveatedRenderingMode.Enabled);
                            isFoveatedEnabled = true;
                        }
                    }
                #endif

                GetPassOrder(m_BlurType, m_CurrentSettings.AfterOpaque, out int[] textureIndices, out ShaderPasses[] shaderPasses);

                // Execute the SSAO Occlusion pass
                RTHandle cameraDepthTargetHandle = renderingData.cameraData.renderer.cameraDepthTargetHandle;
                RenderAndSetBaseMap(ref cmd, ref renderingData, ref renderingData.cameraData.renderer, ref m_Material, ref cameraDepthTargetHandle, ref m_SSAOTextures[0], ShaderPasses.AmbientOcclusion);

                // Execute the Blur Passes
                for (int i = 0; i < shaderPasses.Length; i++)
                {
                    int baseMapIndex = textureIndices[i];
                    int targetIndex = textureIndices[i + 1];
                    RenderAndSetBaseMap(ref cmd, ref renderingData, ref renderingData.cameraData.renderer, ref m_Material, ref m_SSAOTextures[baseMapIndex], ref m_SSAOTextures[targetIndex], shaderPasses[i]);
                }

                // Set the global SSAO Params
                cmd.SetGlobalVector(s_AmbientOcclusionParamID, new Vector4(0f, 0f, 0f, m_CurrentSettings.DirectLightingStrength));
                #if ENABLE_VR && ENABLE_XR_MODULE
                    // Cleanup, making sure it doesn't stay enabled for a pass after that should not have it on
                    if (isFoveatedEnabled)
                        cmd.SetFoveatedRenderingMode(FoveatedRenderingMode.Disabled);
                #endif
            }
        }

        private static void RenderAndSetBaseMap(ref CommandBuffer cmd, ref RenderingData renderingData, ref ScriptableRenderer renderer, ref Material mat, ref RTHandle baseMap, ref RTHandle target, ShaderPasses pass)
        {
            if (IsAfterOpaquePass(ref pass))
                Blitter.BlitCameraTexture(cmd, baseMap, renderer.cameraColorTargetHandle, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store, mat, (int)pass);

            else if (baseMap.rt == null)
            {
                // Obsolete usage of RTHandle aliasing a RenderTargetIdentifier
                Vector2 viewportScale = baseMap.useScaling ? new Vector2(baseMap.rtHandleProperties.rtHandleScale.x, baseMap.rtHandleProperties.rtHandleScale.y) : Vector2.one;

                // Will set the correct camera viewport as well.
                CoreUtils.SetRenderTarget(cmd, target);
                Blitter.BlitTexture(cmd, baseMap.nameID, viewportScale, mat, (int)pass);
            }

            else
                Blitter.BlitCameraTexture(cmd, baseMap, target, mat, (int)pass);
        }

        private static void GetPassOrder(BlurTypes blurType, bool isAfterOpaque, out int[] textureIndices, out ShaderPasses[] shaderPasses)
        {
            switch (blurType)
            {
                case BlurTypes.Bilateral:
                    textureIndices = m_BilateralTexturesIndices;
                    shaderPasses = isAfterOpaque ? m_BilateralAfterOpaquePasses : m_BilateralPasses;
                    break;
                case BlurTypes.Gaussian:
                    textureIndices = m_GaussianTexturesIndices;
                    shaderPasses = isAfterOpaque ? m_GaussianAfterOpaquePasses : m_GaussianPasses;
                    break;
                case BlurTypes.Kawase:
                    textureIndices = m_KawaseTexturesIndices;
                    shaderPasses = isAfterOpaque ? m_KawaseAfterOpaquePasses : m_KawasePasses;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
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
            m_SSAOTextures[0]?.Release();
            m_SSAOTextures[1]?.Release();
            m_SSAOTextures[2]?.Release();
            m_SSAOTextures[3]?.Release();
        }
    }
}
