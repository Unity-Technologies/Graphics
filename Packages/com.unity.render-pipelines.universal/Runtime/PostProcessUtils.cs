using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using System.Runtime.CompilerServices;  // AggressiveInlining

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Utility class for post processing effects.
    /// </summary>
    public static class PostProcessUtils
    {
        /// <summary>
        /// Creates a post-processing pass material from a shader. Shader is error checked for device support.
        /// </summary>
        /// <param name="shader">Shader used for the material</param>
        /// <param name="passName">Pass name for error messages.</param>
        /// <returns>A new Material instance using the provided shader. Null if the shader is not supported.</returns>
        internal static Material LoadShader(Shader shader, string passName = "")
        {
            if (shader == null)
            {
                Debug.LogError($"Missing shader (in '{passName}'). PostProcessing render passes will not execute. Check for missing reference in the Renderer and/or PostProcessData resources.");
                return null;
            }
            else if (!shader.isSupported)
            {
                Debug.LogWarning($"Shader '{shader.name}' is not supported (in '{passName}'). PostProcessing render passes will not execute.");
                return null;
            }

            return CoreUtils.CreateEngineMaterial(shader);
        }

        /// <summary>
        /// Creates a texture compatible with post-processing effects.
        /// </summary>
        /// <param name="renderGraph">RenderGraph that creates the texture.</param>
        /// <param name="source">Source texture for the texture descriptor.</param>
        /// <param name="name">Texture name.</param>
        /// <param name="clear">Texture needs to be cleared on first use.</param>
        /// <param name="filterMode">Texture filtering mode.</param>
        /// <returns>Texture compatible with post-processing effects.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static TextureHandle CreateCompatibleTexture(RenderGraph renderGraph, in TextureHandle source, string name, bool clear, FilterMode filterMode)
        {
            var desc = source.GetDescriptor(renderGraph);
            MakeCompatible(ref desc);
            desc.name = name;
            desc.clearBuffer = clear;
            desc.filterMode = filterMode;
            return renderGraph.CreateTexture(desc);
        }

        /// <summary>
        /// Creates a texture compatible with post-processing effects.
        /// </summary>
        /// <param name="renderGraph">RenderGraph that creates the texture.</param>
        /// <param name="desc">Texture descriptor.</param>
        /// <param name="name">Texture name.</param>
        /// <param name="clear">Texture needs to be cleared on first use.</param>
        /// <param name="filterMode">Texture filtering mode.</param>
        /// <returns>Texture compatible with post-processing effects.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static TextureHandle CreateCompatibleTexture(RenderGraph renderGraph, in TextureDesc desc, string name, bool clear, FilterMode filterMode)
        {
            var descCompatible = GetCompatibleDescriptor(desc);
            descCompatible.name = name;
            descCompatible.clearBuffer = clear;
            descCompatible.filterMode = filterMode;
            return renderGraph.CreateTexture(descCompatible);
        }

        /// <summary>
        /// Converts existing descriptor into a post-processing compatible descriptor.
        /// </summary>
        /// <param name="desc">Source texture descriptor.</param>
        /// <param name="width">Texture width.</param>
        /// <param name="height">Texture height.</param>
        /// <param name="format">Texture format.</param>
        /// <returns>Texture descriptor compatible with post-processing.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static TextureDesc GetCompatibleDescriptor(TextureDesc desc, int width, int height, GraphicsFormat format)
        {
            desc.width = width;
            desc.height = height;
            desc.format = format;

            MakeCompatible(ref desc);

            return desc;
        }

        /// <summary>
        /// Converts existing descriptor into a post-processing compatible descriptor.
        /// Disables MSAA, mipmaps etc.
        /// </summary>
        /// <param name="desc">Source texture descriptor.</param>
        /// <returns>Texture descriptor compatible with post-processing.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static TextureDesc GetCompatibleDescriptor(TextureDesc desc)
        {
            MakeCompatible(ref desc);

            return desc;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void MakeCompatible(ref TextureDesc desc)
        {
            desc.msaaSamples = MSAASamples.None;
            desc.useMipMap = false;
            desc.autoGenerateMips = false;
            desc.anisoLevel = 0;
            desc.discardBuffer = false;
        }

        /// <summary>
        /// Configures the blue noise dithering used.
        /// </summary>
        /// <param name="data">The <c>PostProcessData</c> resources to use.</param>
        /// <param name="index">The current array index to the Blue noise textures.</param>
        /// <param name="camera">The camera using the dithering effect.</param>
        /// <param name="material">The material used with the dithering effect.</param>
        /// <returns>The new array index to the Blue noise textures.</returns>
        [System.Obsolete("This method is obsolete. Use ConfigureDithering override that takes camera pixel width and height instead. #from(2021.1)")]
        public static int ConfigureDithering(PostProcessData data, int index, Camera camera, Material material)
        {
            return ConfigureDithering(data, index, camera.pixelWidth, camera.pixelHeight, material);
        }

        /// <summary>
        /// Configures the blue noise dithering used.
        /// </summary>
        /// <param name="data">The <c>PostProcessData</c> resources to use.</param>
        /// <param name="index">The current array index to the Blue noise textures.</param>
        /// <param name="cameraPixelWidth">The camera pixel width.</param>
        /// <param name="cameraPixelHeight">The camera pixel height.</param>
        /// <param name="material">The material used with the dithering effect.</param>
        /// <returns>The new array index to the Blue noise textures.</returns>
        public static int ConfigureDithering(PostProcessData data, int index, int cameraPixelWidth, int cameraPixelHeight, Material material)
        {
            var blueNoise = data.textures.blueNoise16LTex;

            if (blueNoise == null || blueNoise.Length == 0)
                return 0; // Safe guard

#if LWRP_DEBUG_STATIC_POSTFX // Used by QA for automated testing
            index = 0;
#else
            if (++index >= blueNoise.Length)
                index = 0;
#endif
            // Ideally we would be sending a texture array once and an index to the slice to use
            // on every frame but these aren't supported on all Universal targets
            var noiseTex = blueNoise[index];

            var tilingParams = CalcNoiseTextureTilingParams(noiseTex, cameraPixelWidth, cameraPixelHeight, GetRandomOffset2D());
            ConfigureDitheringMaterial(material, noiseTex, tilingParams);

            return index;
        }

        /// <summary>
        /// Configures the Film grain shader parameters.
        /// </summary>
        /// <param name="data">The <c>PostProcessData</c> resources to use.</param>
        /// <param name="settings">The Film Grain settings. </param>
        /// <param name="camera">The camera using the dithering effect.</param>
        /// <param name="material">The material used with the dithering effect.</param>
        [System.Obsolete("This method is obsolete. Use ConfigureFilmGrain override that takes camera pixel width and height instead. #from(2021.1)")]
        public static void ConfigureFilmGrain(PostProcessData data, FilmGrain settings, Camera camera, Material material)
        {
            ConfigureFilmGrain(data, settings, camera.pixelWidth, camera.pixelHeight, material);
        }

        /// <summary>
        /// Configures the Film grain shader parameters.
        /// </summary>
        /// <param name="data">The <c>PostProcessData</c> resources to use.</param>
        /// <param name="settings">The Film Grain settings. </param>
        /// <param name="cameraPixelWidth">The camera pixel width.</param>
        /// <param name="cameraPixelHeight">The camera pixel height.</param>
        /// <param name="material">The material used with the dithering effect.</param>
        public static void ConfigureFilmGrain(PostProcessData data, FilmGrain settings, int cameraPixelWidth, int cameraPixelHeight, Material material)
        {
            var texture = settings.texture.value;

            if (settings.type.value != FilmGrainLookup.Custom)
                texture = data.textures.filmGrainTex[(int)settings.type.value];

            var tilingParams = CalcNoiseTextureTilingParams(texture, cameraPixelWidth, cameraPixelHeight, GetRandomOffset2D());
            ConfigureFilmGrainMaterial(material, texture, new Vector2(settings.intensity.value * 4f, settings.response.value), tilingParams);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector2 GetRandomOffset2D()
        {
#if LWRP_DEBUG_STATIC_POSTFX
            return Vector2.zero;
#else
            var oldState = Random.state;
            Random.InitState(Time.frameCount);
            float rndOffsetX = Random.value;
            float rndOffsetY = Random.value;
            Random.state = oldState;
            return new Vector2(rndOffsetX, rndOffsetY);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector4 CalcNoiseTextureTilingParams(Texture noiseTexture, int cameraPixelWidth, int cameraPixelHeight, Vector2 offset)
        {
            if (noiseTexture == null)
                return Vector4.zero;

            return new Vector4(cameraPixelWidth / (float)noiseTexture.width, cameraPixelHeight / (float)noiseTexture.height, offset.x, offset.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ConfigureDitheringMaterial(Material material, Texture noiseTexture, Vector4 tilingParams)
        {
            material.SetTexture(ShaderConstants._BlueNoise_Texture, noiseTexture);
            material.SetVector(ShaderConstants._Dithering_Params, tilingParams);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ConfigureFilmGrainMaterial(Material material, Texture grainTexture, Vector2 grainParams, Vector4 tilingParams)
        {
            material.SetTexture(ShaderConstants._Grain_Texture, grainTexture);
            material.SetVector(ShaderConstants._Grain_Params, grainParams);
            material.SetVector(ShaderConstants._Grain_TilingParams, tilingParams);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsFxaaEnabled(UniversalCameraData cameraData)
        {
            return (cameraData.antialiasing == AntialiasingMode.FastApproximateAntialiasing);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsFsrEnabled(UniversalCameraData cameraData)
        {
            return ((cameraData.imageScalingMode == ImageScalingMode.Upscaling) &&
#if ENABLE_UPSCALER_FRAMEWORK
                (cameraData.resolvedUpscalerHash == UniversalRenderPipeline.k_UpscalerHash_FSR1)
#else
                (cameraData.upscalingFilter == ImageUpscalingFilter.FSR)
#endif
            );
        }

#region HDR Output

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool RequireHDROutput(UniversalCameraData cameraData)
        {
            // If capturing, don't convert to HDR.
            // If not last in the stack, don't convert to HDR.
            return cameraData.isHDROutputActive && cameraData.captureActions == null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SetupHDROutput(Material material, HDROutputUtils.HDRDisplayInformation hdrDisplayInformation, ColorGamut hdrDisplayColorGamut, Tonemapping tonemapping, HDROutputUtils.Operation hdrOperations, bool rendersOverlayUI)
        {
            Vector4 hdrOutputLuminanceParams;
            UniversalRenderPipeline.GetHDROutputLuminanceParameters(hdrDisplayInformation, hdrDisplayColorGamut, tonemapping, out hdrOutputLuminanceParams);
            material.SetVector(ShaderPropertyId.hdrOutputLuminanceParams, hdrOutputLuminanceParams);

            HDROutputUtils.ConfigureHDROutput(material, hdrDisplayColorGamut, hdrOperations);
            CoreUtils.SetKeyword(material, ShaderKeywordStrings.HDROverlay, rendersOverlayUI);
        }
#endregion

#region Blit

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector4 CalcShaderSourceSize(float width, float height, RenderTexture rt)
        {
            if (rt != null && rt.useDynamicScale)
            {
                width *= ScalableBufferManager.widthScaleFactor;
                height *= ScalableBufferManager.heightScaleFactor;
            }
            return new Vector4(width, height, 1.0f / width, 1.0f / height);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector4 CalcShaderSourceSize(RTHandle source)
        {
            return CalcShaderSourceSize(source.rt.width, source.rt.height, source.rt);
        }

        internal static void SetGlobalShaderSourceSize(RasterCommandBuffer cmd, float width, float height, RenderTexture rt)
        {
            cmd.SetGlobalVector(ShaderConstants._SourceSize, CalcShaderSourceSize(width, height, rt));
        }

        internal static void SetGlobalShaderSourceSize(CommandBuffer cmd, float width, float height, RenderTexture rt)
        {
            SetGlobalShaderSourceSize(CommandBufferHelpers.GetRasterCommandBuffer(cmd), width, height, rt);
        }

        internal static void SetGlobalShaderSourceSize(RasterCommandBuffer cmd, RTHandle source)
        {
            SetGlobalShaderSourceSize(cmd, source.rt.width, source.rt.height, source.rt);
        }

        internal static void SetGlobalShaderSourceSize(CommandBuffer cmd, RTHandle source)
        {
            SetGlobalShaderSourceSize(CommandBufferHelpers.GetRasterCommandBuffer(cmd), source);
        }

        internal static void ScaleViewport(RasterCommandBuffer cmd, RTHandle dest, UniversalCameraData cameraData, bool isFinalPass)
        {
            RenderTargetIdentifier cameraTarget = BuiltinRenderTextureType.CameraTarget;
#if ENABLE_VR && ENABLE_XR_MODULE
            if (cameraData.xr.enabled)
                cameraTarget = cameraData.xr.renderTarget;
#endif
            if (dest.nameID == cameraTarget || cameraData.targetTexture != null)
            {
                if (!isFinalPass || !cameraData.resolveFinalTarget)
                {
                    // Inside the camera stack the target is the shared intermediate target, which can be scaled with render scale.
                    // camera.pixelRect is the viewport of the final target in pixels, so it cannot be used for the intermediate target.
                    // On intermediate target allocation the viewport size is baked into the target size.
                    // Which means the intermediate target does not have a viewport rect. Its offset is always 0 and its size matches viewport size.
                    // The overlay cameras inherit the base viewport, so they cannot have a different viewport,
                    // a necessary limitation since the target covers only the base viewport area.
                    // The offsetting is finally done by the final output viewport-rect to the final target.
                    // Note: effectively this is setting a fullscreen viewport for the intermediate target.
                    var targetWidth = cameraData.cameraTargetDescriptor.width;
                    var targetHeight = cameraData.cameraTargetDescriptor.height;
                    var targetViewportInPixels = new Rect(
                        0,
                        0,
                        targetWidth,
                        targetHeight);
                    cmd.SetViewport(targetViewportInPixels);
                }
                else
                    cmd.SetViewport(cameraData.pixelRect);
            }
        }

        internal static void ScaleViewportAndBlit(RasterGraphContext context, in TextureHandle sourceTexture, in TextureHandle destTexture, UniversalCameraData cameraData, Material material, bool isFinalPass)
        {
            Vector4 scaleBias = RenderingUtils.GetFinalBlitScaleBias(context, sourceTexture, destTexture);
            ScaleViewport(context.cmd, destTexture, cameraData, isFinalPass);

            Blitter.BlitTexture(context.cmd, sourceTexture, scaleBias, material, 0);
        }

        internal static void ScaleViewportAndDrawVisibilityMesh(RasterGraphContext context, in TextureHandle sourceTexture, in TextureHandle destTexture, UniversalCameraData cameraData, Material material, bool isFinalPass)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            Vector4 scaleBias = RenderingUtils.GetFinalBlitScaleBias(context, sourceTexture, destTexture);
            ScaleViewport(context.cmd, destTexture, cameraData, isFinalPass);

            // Set property block for blit shader
            MaterialPropertyBlock xrPropertyBlock = XRSystemUniversal.GetMaterialPropertyBlock();
            xrPropertyBlock.SetVector(Shader.PropertyToID("_BlitScaleBias"), scaleBias);
            xrPropertyBlock.SetTexture(Shader.PropertyToID("_BlitTexture"), sourceTexture);
            cameraData.xr.RenderVisibleMeshCustomMaterial(context.cmd, cameraData.xr.occlusionMeshScale, material, xrPropertyBlock, 1, context.GetTextureUVOrigin(in sourceTexture) == context.GetTextureUVOrigin(in destTexture));
#endif
        }
#endregion

        // Precomputed shader ids to same some CPU cycles (mostly affects mobile)
        static class ShaderConstants
        {
            public static readonly int _Grain_Texture = Shader.PropertyToID("_Grain_Texture");
            public static readonly int _Grain_Params = Shader.PropertyToID("_Grain_Params");
            public static readonly int _Grain_TilingParams = Shader.PropertyToID("_Grain_TilingParams");

            public static readonly int _BlueNoise_Texture = Shader.PropertyToID("_BlueNoise_Texture");
            public static readonly int _Dithering_Params = Shader.PropertyToID("_Dithering_Params");

            public static readonly int _SourceSize = Shader.PropertyToID("_SourceSize");
        }
    }
}
