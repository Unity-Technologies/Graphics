using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.Universal.Internal;

/// <summary>
/// Renders the on-tile post-processing stack.
/// </summary>
public class OnTilePostProcessPass : ScriptableRenderPass
{
    /// <summary>
    /// The override shader to use.
    /// </summary>
    internal bool m_UseMultisampleShaderResolve = false;
    internal bool m_UseTextureReadFallback = false;
    
    RTHandle m_UserLut;
    Material m_OnTileUberMaterial;
    static readonly int s_BlitScaleBias = Shader.PropertyToID("_BlitScaleBias");
    static readonly int s_BlitTexture = Shader.PropertyToID("_BlitTexture");
    int m_DitheringTextureIndex;
    PostProcessData m_PostProcessData;

    const string m_PassName = "On Tile Post Processing";
    const string m_FallbackPassName = "On Tile Post Processing (sampling fallback) ";

    internal OnTilePostProcessPass(PostProcessData postProcessData)
    {
        m_PostProcessData = postProcessData;
#if ENABLE_VR && ENABLE_XR_MODULE
        m_UseMultisampleShaderResolve = SystemInfo.supportsMultisampledShaderResolve;
#endif
    }

    internal void Setup(ref Material onTileUberMaterial)
    {
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
        if (m_OnTileUberMaterial == null) return;

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
        TextureDesc srcDesc = renderGraph.GetTextureDesc(source); ;
        
        TextureHandle destination = resourceData.backBufferColor;

        SetupVignette(m_OnTileUberMaterial, cameraData.xr, srcDesc.width, srcDesc.height, vignette);
        SetupLut(m_OnTileUberMaterial, colorLookup, colorAdjustments, lutSize);
        SetupTonemapping(m_OnTileUberMaterial, tonemapping);
        SetupGrain(m_OnTileUberMaterial, cameraData, filmgrain, m_PostProcessData);
        SetupDithering(m_OnTileUberMaterial, cameraData, m_PostProcessData);

        CoreUtils.SetKeyword(m_OnTileUberMaterial, ShaderKeywordStrings._ENABLE_ALPHA_OUTPUT, cameraData.isAlphaOutputEnabled);

        UberShaderPasses shaderPass = useVisibilityMesh ? UberShaderPasses.NormalVisMesh : UberShaderPasses.Normal;
#if ENABLE_VR && ENABLE_XR_MODULE
        bool setMultisamplesShaderResolveFeatureFlag = false;
#endif
        if (srcDesc.msaaSamples != MSAASamples.None)
        {
            if (srcDesc.msaaSamples == MSAASamples.MSAA8x)
            {
                Debug.LogError("MSAA8x is enabled in Universal Render Pipeline Asset but it is not supported by the on-tile post-processing feature yet. Please use MSAA4x or MSAA2x instead.");
                return;
            }

            var destInfo = renderGraph.GetRenderTargetInfo(destination);

#if ENABLE_VR && ENABLE_XR_MODULE
            if (m_UseMultisampleShaderResolve)
            {
                // If we have support for msaa shader resolve we can do MSAA -> non MSAA in our render pass.
                shaderPass = useVisibilityMesh ? UberShaderPasses.MSAASoftwareResolveVisMesh : UberShaderPasses.MSAASoftwareResolve;

                // When rendering into the backbuffer, we could enable the shader resolve extension to resolve into the msaa1x surface directly on platforms that support auto resolve.
                // For platforms that don't support auto resolve, the backbuffer is a multisampled surface and we don't need to enable the extension. This is to maximize the pass merging because shader resolve enabled pass has to be the last subpass.
                if (SystemInfo.supportsMultisampleAutoResolve)
                {

                    setMultisamplesShaderResolveFeatureFlag = true;
                }
            }
            else
#endif
            {
                if (destInfo.msaaSamples == (int)srcDesc.msaaSamples)
                {
                    // If we have MSAA -> MSAA we can still resolve in the shader running at fragmnet rate
                    // and the hardware resolve will sometimes be optimized as a result.
                    shaderPass = useVisibilityMesh ? UberShaderPasses.MSAASoftwareResolveVisMesh : UberShaderPasses.MSAASoftwareResolve;
                }
                else
                {
                    // We are going MSAA -> Non MSAA without shader resolve support which is not a valid render pass
                    // configuration. So we need to force a resolve before running our shader which will cause us not
                    // to be on tile anymore.
                    shaderPass = useVisibilityMesh ? UberShaderPasses.TextureReadVisMesh : UberShaderPasses.TextureRead;
                }
            }
        }

        // Fallback to texture read mode when requested.
        if (m_UseTextureReadFallback)
        {
            shaderPass = useVisibilityMesh ? UberShaderPasses.TextureReadVisMesh : UberShaderPasses.TextureRead;
#if ENABLE_VR && ENABLE_XR_MODULE
            setMultisamplesShaderResolveFeatureFlag = false;
#endif
        }

        // Fallback logic to handle the case where Frame Buffer Fetch is not compatible with the pass setup.
        TextureDesc destDesc;
        var info = renderGraph.GetRenderTargetInfo(destination);
        destDesc = new TextureDesc(info.width, info.height);
        destDesc.format = info.format;
        destDesc.msaaSamples = (MSAASamples)info.msaaSamples;
        destDesc.bindTextureMS = info.bindMS;
        destDesc.slices = info.volumeDepth;
        destDesc.dimension = info.volumeDepth > 1 ? TextureDimension.Tex2DArray : TextureDimension.Tex2D;

        // Falls back to texture read mode if texture dimension does not match between source and destination (invalid frame buffer fetch setup). 
        if (srcDesc.width != destDesc.width || srcDesc.height != destDesc.height || srcDesc.slices != destDesc.slices)
        {
            shaderPass = useVisibilityMesh ? UberShaderPasses.TextureReadVisMesh : UberShaderPasses.TextureRead;
#if ENABLE_VR && ENABLE_XR_MODULE
            setMultisamplesShaderResolveFeatureFlag = false;
#endif
        }

        var lutTexture = resourceData.internalColorLut;
        var passName = m_UseTextureReadFallback ? m_FallbackPassName : m_PassName;
        using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData))
        {
            passData.source = source;
            passData.destination = destination;
            passData.material = m_OnTileUberMaterial;
            passData.shaderPass = shaderPass;

            if (shaderPass == UberShaderPasses.TextureRead || shaderPass == UberShaderPasses.TextureReadVisMesh)
            {
                builder.UseTexture(source, AccessFlags.Read);
            }
            else
            {
                builder.SetInputAttachment(source, 0, AccessFlags.Read);
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

            builder.SetRenderAttachment(destination, 0, AccessFlags.WriteAll);
            builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecuteFBFetchPass(data, context));

            passData.useXRVisibilityMesh = false;
            passData.msaaSamples = (int)srcDesc.msaaSamples;

#if ENABLE_VR && ENABLE_XR_MODULE
            if (cameraData.xr.enabled)
            {
                ExtendedFeatureFlags xrFeatureFlag = ExtendedFeatureFlags.MultiviewRenderRegionsCompatible;
                if (setMultisamplesShaderResolveFeatureFlag)
                {
                    xrFeatureFlag |= ExtendedFeatureFlags.MultisampledShaderResolve;
                }
                builder.SetExtendedFeatureFlags(xrFeatureFlag);

                // We want our foveation logic to match other geometry passes(eg. Opaque, Transparent, Skybox) because we want to merge with previous passes.
                bool passSupportsFoveation = cameraData.xrUniversal.canFoveateIntermediatePasses || resourceData.isActiveTargetBackBuffer;
                builder.EnableFoveatedRasterization(
                    cameraData.xr.supportsFoveatedRendering && passSupportsFoveation);

                passData.useXRVisibilityMesh = useVisibilityMesh;
                passData.xr = cameraData.xr; // Need to pass this down for the method call RenderVisibleMeshCustomMaterial()
            }
#endif
        }

        //This will prevent the final blit pass from being added/needed (still internal API in trunk)
        resourceData.activeColorID = UniversalResourceData.ActiveID.BackBuffer;
        resourceData.activeDepthID = UniversalResourceData.ActiveID.BackBuffer;
    }

    enum UberShaderPasses
    {
        Normal,
        MSAASoftwareResolve,
        TextureRead,
        NormalVisMesh,
        MSAASoftwareResolveVisMesh,
        TextureReadVisMesh,
    };

    // This static method is used to execute the pass and passed as the RenderFunc delegate to the RenderGraph render pass
    static void ExecuteFBFetchPass(PassData data, RasterGraphContext context)
    {
        var cmd = context.cmd;

        data.material.SetTexture(ShaderConstants._InternalLut, data.lutTexture);
        if (data.userLutTexture.IsValid())
            data.material.SetTexture(ShaderConstants._UserLut, data.userLutTexture);

        bool IsHandleYFlipped = RenderingUtils.IsHandleYFlipped(context, in data.destination); 
        // We need to set the "_BlitScaleBias" uniform for user materials with shaders relying on core Blit.hlsl to work
        data.material.SetVector(s_BlitScaleBias, !IsHandleYFlipped ? new Vector4(1, -1, 0, 1) : new Vector4(1, 1, 0, 0));

        if (data.shaderPass == UberShaderPasses.TextureRead || data.shaderPass == UberShaderPasses.TextureReadVisMesh)
        {
            data.material.SetTexture(s_BlitTexture, data.source);
        }
        else if (data.shaderPass == UberShaderPasses.MSAASoftwareResolve || data.shaderPass == UberShaderPasses.MSAASoftwareResolveVisMesh)
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
        internal UberShaderPasses shaderPass;
        internal Vector4 scaleBias;
        internal bool useXRVisibilityMesh;
        internal XRPass xr;
        internal int msaaSamples;
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
                material.SetVector(ShaderConstants._Vignette_ParamsXR, xrPass.ApplyXRViewCenterOffset(center));
            else
                // In multi-pass mode we need to modify the eye center with the values from .xy of the corrected
                // center since the version of the shader that is not single-pass will use the value in _Vignette_Params2
                center = xrPass.ApplyXRViewCenterOffset(center);
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

    private void SetupTonemapping(Material onTileUberMaterial, Tonemapping tonemapping)
    {
        CoreUtils.SetKeyword(m_OnTileUberMaterial, ShaderKeywordStrings.TonemapNeutral,
            tonemapping.mode.value == TonemappingMode.Neutral);
        CoreUtils.SetKeyword(m_OnTileUberMaterial, ShaderKeywordStrings.TonemapACES,
            tonemapping.mode.value == TonemappingMode.ACES);
    }

    void SetupGrain(Material onTileUberMaterial, UniversalCameraData cameraData, FilmGrain filmgrain, PostProcessData data)
    {
        if (filmgrain.IsActive())
        {
            onTileUberMaterial.EnableKeyword(ShaderKeywordStrings.FilmGrain);
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
        if (cameraData.isDitheringEnabled)
        {
            onTileUberMaterial.EnableKeyword(ShaderKeywordStrings.Dithering);
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
