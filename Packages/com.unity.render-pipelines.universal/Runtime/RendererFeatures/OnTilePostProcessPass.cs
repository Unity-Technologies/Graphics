using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Experimental.Rendering;

/// <summary>
/// Renders the on-tile post-processing stack.
/// </summary>
public class OnTilePostProcessPass : ScriptableRenderPass
{
    /// <summary>
    /// The override shader to use.
    /// </summary>
    internal readonly bool k_SupportsMultisampleShaderResolve = false;
    internal bool m_UseTextureReadFallback = false;
    
    RTHandle m_UserLut;
    Material m_OnTileUberMaterial;
    static readonly int s_BlitScaleBias = Shader.PropertyToID("_BlitScaleBias");
    static readonly int s_BlitTexture = Shader.PropertyToID("_BlitTexture");
    int m_DitheringTextureIndex;
    PostProcessData m_PostProcessData;

    // Cache vignette center from peripheral (outer) pass for quad view
    static Vector4 s_CachedPeripheralVignetteCenter = Vector4.zero;

    const string m_PassName = "On Tile Post Processing";
    const string m_FallbackPassName = "On Tile Post Processing (sampling fallback) ";

    int m_PassOnTile, m_PassOnTileMsaa, m_PassTextureSample, m_PassOnTileVis, m_PassOnTileMsaaVis, m_PassTexureSampleVis;

    internal OnTilePostProcessPass(PostProcessData postProcessData)
    {
        m_PostProcessData = postProcessData;

#if ENABLE_VR && ENABLE_XR_MODULE        
        k_SupportsMultisampleShaderResolve = SystemInfo.supportsMultisampledShaderResolve;
#endif
    }

    internal void Setup(ref Material onTileUberMaterial)
    {
        Debug.Assert(onTileUberMaterial != null, "The material set in OnTilePostProcessPass can't be null.");

        if (m_OnTileUberMaterial == null)
        {
            m_OnTileUberMaterial = onTileUberMaterial;

            // We just do this once, assuming the shader never changes. 
            m_PassOnTile = onTileUberMaterial.FindPass("OnTileUberPost");
            m_PassOnTileMsaa = onTileUberMaterial.FindPass("OnTileUberPostMSSoftware");
            m_PassTextureSample = onTileUberMaterial.FindPass("OnTileUberPostTextureSample");
            m_PassOnTileVis = onTileUberMaterial.FindPass("OnTileUberPostVisMesh");
            m_PassOnTileMsaaVis = onTileUberMaterial.FindPass("OnTileUberPostMSSoftwareVisMesh");
            m_PassTexureSampleVis = onTileUberMaterial.FindPass("OnTileUberPostTextureSampleVisMesh");
        }

        m_OnTileUberMaterial = onTileUberMaterial;                
    }

    /// <summary>
    /// Disposes used resources.
    /// </summary>
    public void Dispose()
    {
        m_UserLut?.Release();
        CoreUtils.Destroy(m_OnTileUberMaterial);
    }

    /// <inheritdoc cref="IRenderGraphRecorder.RecordRenderGraph"/>
    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        Debug.Assert(m_OnTileUberMaterial != null, "The material set in OnTilePostProcessPass can't be null.");     

        var resourceData = frameData.Get<UniversalResourceData>();
        var renderingData = frameData.Get<UniversalRenderingData>();
        var cameraData = frameData.Get<UniversalCameraData>();
        var postProcessingData = frameData.Get<UniversalPostProcessingData>();

        if (SystemInfo.graphicsShaderLevel < 30)
        {
            Debug.LogError("DrawProcedural is required for the On-Tile post processing feature but it is not supported by the platform. Pass will not execute.");
            return;
        }

        int lutSize = postProcessingData.lutSize;

        var stack = VolumeManager.instance.stack;
        var vignette = stack.GetComponent<Vignette>();
        var colorLookup = stack.GetComponent<ColorLookup>();
        var colorAdjustments = stack.GetComponent<ColorAdjustments>();
        var tonemapping = stack.GetComponent<Tonemapping>();
        var filmgrain = stack.GetComponent<FilmGrain>();

#if ENABLE_VR && ENABLE_XR_MODULE
        bool useVisibilityMesh = cameraData.xr.enabled && cameraData.xr.hasValidVisibleMesh;
#else
        const bool useVisibilityMesh = false;
#endif

        TextureHandle source = resourceData.activeColorTexture;
        TextureDesc srcDesc = renderGraph.GetTextureDesc(source);


        TextureHandle destination = resourceData.backBufferColor;
        var destInfo = renderGraph.GetRenderTargetInfo(destination);

        // This signals to URP (or the next pass) that rendering has switched to the backbuffer. URP will therefore not add the final blit pass.
        // The code below can then also use resourceData.isActiveTargetBackBuffer correctly for robustness.
        resourceData.SwitchActiveTexturesToBackbuffer();

        SetupVignette(m_OnTileUberMaterial, cameraData.xr, srcDesc.width, srcDesc.height, vignette);
        SetupLut(m_OnTileUberMaterial, colorLookup, colorAdjustments, lutSize);
        SetupTonemapping(m_OnTileUberMaterial, tonemapping, isHdrGrading: postProcessingData.gradingMode == ColorGradingMode.HighDynamicRange);
        SetupGrain(m_OnTileUberMaterial, cameraData, filmgrain, m_PostProcessData);
        SetupDithering(m_OnTileUberMaterial, cameraData, m_PostProcessData);

        CoreUtils.SetKeyword(m_OnTileUberMaterial, ShaderKeywordStrings.LinearToSRGBConversion, cameraData.requireSrgbConversion);
        CoreUtils.SetKeyword(m_OnTileUberMaterial, ShaderKeywordStrings.UseFastSRGBLinearConversion, postProcessingData.useFastSRGBLinearConversion);
        CoreUtils.SetKeyword(m_OnTileUberMaterial, ShaderKeywordStrings._ENABLE_ALPHA_OUTPUT, cameraData.isAlphaOutputEnabled);

#if ENABLE_VR && ENABLE_XR_MODULE
        // Setup XR UV remapping for Quad View (used by all screen-space effects)
        if (cameraData.xr != null && cameraData.xr.enabled && cameraData.xr.singlePassEnabled)
        {
            PostProcessUtils.SetupXRUVRemapping(m_OnTileUberMaterial, cameraData.xr);
        }
#endif

        int shaderPass;

        if (m_UseTextureReadFallback)
        {
            shaderPass = useVisibilityMesh ? m_PassTexureSampleVis : m_PassTextureSample;
        }
        else 
        {
            Debug.Assert(srcDesc.width == destInfo.width && srcDesc.height == destInfo.height && srcDesc.slices == destInfo.volumeDepth
                , "On Tile Post Processing expects the source and destination to have the same dimensions.");

            switch (srcDesc.msaaSamples)
            {
                case MSAASamples.None:
                    shaderPass = useVisibilityMesh ? m_PassOnTileVis : m_PassOnTile;
                    break;
                case MSAASamples.MSAA8x:
                    Debug.LogError("MSAA8x is enabled in Universal Render Pipeline Asset but it is not supported by the on-tile post-processing feature yet. Please use MSAA4x or MSAA2x instead.");
                    return;
                default:
                    shaderPass = useVisibilityMesh ? m_PassOnTileMsaaVis: m_PassOnTileMsaa;
                    break;
            }
        }

        var lutTexture = resourceData.internalColorLut;
        var passName = m_UseTextureReadFallback ? m_FallbackPassName : m_PassName;
        using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData))
        {            
            passData.material = m_OnTileUberMaterial;
            passData.shaderPass = shaderPass;
            passData.useTextureReadFallback = m_UseTextureReadFallback;

            if (m_UseTextureReadFallback)
            {
                builder.UseTexture(passData.source = source, AccessFlags.Read);
            }
            else
            {
                builder.SetInputAttachment(source, 0);
                // MSAA shader resolve keywords require global state modification
                builder.AllowGlobalStateModification(true);
            }

            builder.UseTexture(lutTexture, AccessFlags.Read);
            passData.lutTexture = lutTexture;

            var userLutTexture = TryGetCachedUserLutTextureHandle(colorLookup, renderGraph);
            passData.userLutTexture = userLutTexture;
            if (userLutTexture.IsValid())
            {
                builder.UseTexture(userLutTexture, AccessFlags.Read);
            }

            builder.SetRenderAttachment(passData.destination = destination, 0, AccessFlags.WriteAll);
            builder.SetRenderFunc(static (PassData data, RasterGraphContext context) => ExecuteFBFetchPass(data, context));

            passData.useXRVisibilityMesh = useVisibilityMesh;
            passData.msaaSamples = (int)srcDesc.msaaSamples;

            // When rendering into the backbuffer, we could enable the shader resolve extension to resolve into the msaa1x surface directly on platforms that support auto resolve.
            // For platforms that don't support auto resolve, the backbuffer is a multisampled surface and we don't need to enable the extension. This is to maximize the pass merging because shader resolve enabled pass has to be the last subpass.
            bool useMultisampledShaderResolve = (int)srcDesc.msaaSamples > destInfo.msaaSamples && k_SupportsMultisampleShaderResolve;

            ExtendedFeatureFlags featureFlags = ExtendedFeatureFlags.None;

            if (useMultisampledShaderResolve)
            {
                featureFlags |= ExtendedFeatureFlags.MultisampledShaderResolve;                
            }            

#if ENABLE_VR && ENABLE_XR_MODULE
            if (cameraData.xr.enabled)
            {
                featureFlags |= ExtendedFeatureFlags.MultiviewRenderRegionsCompatible;

                // We want our foveation logic to match other geometry passes(eg. Opaque, Transparent, Skybox) because we want to merge with previous passes.
                bool passSupportsFoveation = cameraData.xrUniversal.canFoveateIntermediatePasses || resourceData.isActiveTargetBackBuffer;
                builder.EnableFoveatedRasterization(
                    cameraData.xr.supportsFoveatedRendering && passSupportsFoveation);

                passData.xr = cameraData.xr; // Need to pass this down for the method call RenderVisibleMeshCustomMaterial()
            }
#endif
            builder.SetExtendedFeatureFlags(featureFlags);

        }
    }

    // This static method is used to execute the pass and passed as the RenderFunc delegate to the RenderGraph render pass
    static void ExecuteFBFetchPass(PassData data, RasterGraphContext context)
    {
        var cmd = context.cmd;

        data.material.SetTexture(ShaderConstants._InternalLut, data.lutTexture);
        if (data.userLutTexture.IsValid())
            data.material.SetTexture(ShaderConstants._UserLut, data.userLutTexture);

        bool flip = context.GetTextureUVOrigin(data.source) != context.GetTextureUVOrigin(data.destination);

        // We need to set the "_BlitScaleBias" uniform for user materials with shaders relying on core Blit.hlsl to work
        data.material.SetVector(s_BlitScaleBias, flip ? new Vector4(1, -1, 0, 1) : new Vector4(1, 1, 0, 0));

        if (data.useTextureReadFallback)
        {
            data.material.SetTexture(s_BlitTexture, data.source);
        }
        else 
        {
            // Setup MSAA samples
            switch (data.msaaSamples)
            {
                case 4:
                    CoreUtils.SetKeyword(data.material, ShaderKeywordStrings.Msaa2, false);
                    CoreUtils.SetKeyword(data.material, ShaderKeywordStrings.Msaa4, true);
                    break;

                case 2:
                    CoreUtils.SetKeyword(data.material, ShaderKeywordStrings.Msaa2, true);
                    CoreUtils.SetKeyword(data.material, ShaderKeywordStrings.Msaa4, false);
                    break;

                // MSAA disabled, auto resolve supported, resolve texture requested, or ms textures not supported
                default:
                    CoreUtils.SetKeyword(data.material, ShaderKeywordStrings.Msaa2, false);
                    CoreUtils.SetKeyword(data.material, ShaderKeywordStrings.Msaa4, false);
                    break;
            }
        }

#if ENABLE_VR && ENABLE_XR_MODULE
        if (data.useXRVisibilityMesh)
        {
            MaterialPropertyBlock xrPropertyBlock = XRSystemUniversal.GetMaterialPropertyBlock();
            data.xr.RenderVisibleMeshCustomMaterial(cmd, data.xr.occlusionMeshScale, data.material, xrPropertyBlock, (int)(data.shaderPass), false);
        }
        else
#endif
        {
            cmd.DrawProcedural(Matrix4x4.identity, data.material, (int)(data.shaderPass),
                MeshTopology.Triangles, 3, 1);
        }
    }

    private class PassData
    {
        internal TextureHandle source;
        internal TextureHandle destination;
        internal TextureHandle lutTexture;
        internal TextureHandle userLutTexture;
        internal Material material;
        internal int shaderPass;
        internal Vector4 scaleBias;
        internal bool useXRVisibilityMesh;
        internal XRPass xr;
        internal int msaaSamples;
        internal bool useTextureReadFallback;
    }

    TextureHandle TryGetCachedUserLutTextureHandle(ColorLookup colorLookup, RenderGraph renderGraph)
    {
        if (colorLookup.texture.value == null)
        {
            if (m_UserLut != null)
            {
                m_UserLut.Release();
                m_UserLut = null;
            } 
        }
        else
        {
            if (m_UserLut == null || m_UserLut.externalTexture != colorLookup.texture.value)
            {
                m_UserLut?.Release();
                m_UserLut = RTHandles.Alloc(colorLookup.texture.value);
            }
        }
        return m_UserLut != null ? renderGraph.ImportTexture(m_UserLut) : TextureHandle.nullHandle;
    }

    void SetupLut(Material material, ColorLookup colorLookup, ColorAdjustments colorAdjustments, int lutSize)
    {
        int lutHeight = lutSize;
        int lutWidth = lutHeight * lutHeight;

        float postExposureLinear = Mathf.Pow(2f, colorAdjustments.postExposure.value);
        Vector4 lutParams = new Vector4(1f / lutWidth, 1f / lutHeight, lutHeight - 1f, postExposureLinear);

        Vector4 userLutParams = !colorLookup.IsActive()
            ? Vector4.zero
            : new Vector4(1f / colorLookup.texture.value.width,
                1f / colorLookup.texture.value.height,
                colorLookup.texture.value.height - 1f,
                colorLookup.contribution.value);

        material.SetVector(ShaderConstants._Lut_Params, lutParams);
        material.SetVector(ShaderConstants._UserLut_Params, userLutParams);
    }

#region Vignette


    //these methods should be publicly available for user features
    void SetupVignette(Material material, XRPass xrPass, int width, int height, Vignette vignette)
    {
        var color = vignette.color.value;
        var center = vignette.center.value;
        var aspectRatio = width / (float)height;

#if ENABLE_VR
        if (xrPass != null && xrPass.enabled)
        {
            if (xrPass.singlePassEnabled)
            {
                Vector4 vignetteXRCenter;
                var xrLayout = XRSystem.currentLayout;

                if (xrLayout != null && xrPass.viewCount > 1 && xrPass.multipassId == 1 && xrPass.isLastCameraPass)
                {
                    // Second pass (inner views): Reuse the cached peripheral vignette center
                    // This ensures vignette is calculated in the outer UV space after remapping
                    vignetteXRCenter = xrLayout.quadView.cachedPeripheralVignetteCenter;
                    // In quad view we need to also apply the aspect ratio correction to the vignette as the UV remapping will cause it to be stretched/squashed if not corrected
                    aspectRatio *= xrPass.uvScales.y / xrPass.uvScales.x;
                }
                else
                {
                    // First pass (peripheral/outer views): Calculate and cache the vignette center
                    vignetteXRCenter = xrPass.ApplyXRViewCenterOffset(center);
                    if (xrLayout != null)
                        xrLayout.quadView.cachedPeripheralVignetteCenter = vignetteXRCenter;
                }
                material.SetVector(ShaderConstants._Vignette_ParamsXR, vignetteXRCenter);
            }
            else
            {
                // In multi-pass mode we need to modify the eye center with the values from .xy of the corrected
                // center since the version of the shader that is not single-pass will use the value in _Vignette_Params2
                center = xrPass.ApplyXRViewCenterOffset(center);
            }
        }
#endif

        var v1 = new Vector4(
            color.r, color.g, color.b,
            vignette.rounded.value ? aspectRatio : 1f
        );
        var v2 = new Vector4(
            center.x, center.y,
            vignette.intensity.value * 3f,
            vignette.smoothness.value * 5f
        );

        material.SetVector(ShaderConstants._Vignette_Params1, v1);
        material.SetVector(ShaderConstants._Vignette_Params2, v2);
    }

#endregion

    private void SetupTonemapping(Material onTileUberMaterial, Tonemapping tonemapping, bool isHdrGrading)
    {
        CoreUtils.SetKeyword(m_OnTileUberMaterial, ShaderKeywordStrings.HDRGrading, isHdrGrading);

        if (!isHdrGrading)
        {
            CoreUtils.SetKeyword(m_OnTileUberMaterial, ShaderKeywordStrings.TonemapNeutral,
                tonemapping.mode.value == TonemappingMode.Neutral);
            CoreUtils.SetKeyword(m_OnTileUberMaterial, ShaderKeywordStrings.TonemapACES,
                tonemapping.mode.value == TonemappingMode.ACES);
        }
        else
        {
            CoreUtils.SetKeyword(m_OnTileUberMaterial, ShaderKeywordStrings.TonemapNeutral, false);
            CoreUtils.SetKeyword(m_OnTileUberMaterial, ShaderKeywordStrings.TonemapACES, false);
        }
    }

    void SetupGrain(Material onTileUberMaterial, UniversalCameraData cameraData, FilmGrain filmgrain, PostProcessData data)
    {
        bool isActive = filmgrain.IsActive();
        CoreUtils.SetKeyword(onTileUberMaterial, ShaderKeywordStrings.FilmGrain, isActive);

        if (isActive)
        {
            PostProcessUtils.ConfigureFilmGrain(
                data,
                filmgrain,
                cameraData.pixelWidth, cameraData.pixelHeight,
                onTileUberMaterial
            );
        }
    }

    void SetupDithering(Material onTileUberMaterial, UniversalCameraData cameraData, PostProcessData data)
    {
        CoreUtils.SetKeyword(onTileUberMaterial, ShaderKeywordStrings.Dithering, cameraData.isDitheringEnabled);

        if (cameraData.isDitheringEnabled)
        {
            m_DitheringTextureIndex = PostProcessUtils.ConfigureDithering(
                data,
                m_DitheringTextureIndex,
                cameraData.pixelWidth, cameraData.pixelHeight,
                onTileUberMaterial
            );
        }
    }

    static class ShaderConstants
    {
        public static readonly int _Vignette_Params1 = Shader.PropertyToID("_Vignette_Params1");
        public static readonly int _Vignette_Params2 = Shader.PropertyToID("_Vignette_Params2");
        public static readonly int _Vignette_ParamsXR = Shader.PropertyToID("_Vignette_ParamsXR");
        public static readonly int _Lut_Params = Shader.PropertyToID("_Lut_Params");
        public static readonly int _UserLut_Params = Shader.PropertyToID("_UserLut_Params");
        public static readonly int _InternalLut = Shader.PropertyToID("_InternalLut");
        public static readonly int _UserLut = Shader.PropertyToID("_UserLut");
    }
}
