using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Utility class for post processing effects.
    /// </summary>
    public static class PostProcessUtils
    {
        /// <summary>
        /// Configures the blue noise dithering used.
        /// </summary>
        /// <param name="data">The <c>PostProcessData</c> resources to use.</param>
        /// <param name="index">The current array index to the Blue noise textures.</param>
        /// <param name="camera">The camera using the dithering effect.</param>
        /// <param name="material">The material used with the dithering effect.</param>
        /// <returns>The new array index to the Blue noise textures.</returns>
        [System.Obsolete("This method is obsolete. Use ConfigureDithering override that takes camera pixel width and height instead.")]
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
            float rndOffsetX = 0f;
            float rndOffsetY = 0f;
#else
            if (++index >= blueNoise.Length)
                index = 0;

            Random.InitState(Time.frameCount);
            float rndOffsetX = Random.value;
            float rndOffsetY = Random.value;
#endif

            // Ideally we would be sending a texture array once and an index to the slice to use
            // on every frame but these aren't supported on all Universal targets
            var noiseTex = blueNoise[index];

            material.SetTexture(ShaderConstants._BlueNoise_Texture, noiseTex);
            material.SetVector(ShaderConstants._Dithering_Params, new Vector4(
                cameraPixelWidth / (float)noiseTex.width,
                cameraPixelHeight / (float)noiseTex.height,
                rndOffsetX,
                rndOffsetY
            ));

            return index;
        }

        /// <summary>
        /// Configures the Film grain shader parameters.
        /// </summary>
        /// <param name="data">The <c>PostProcessData</c> resources to use.</param>
        /// <param name="settings">The Film Grain settings. </param>
        /// <param name="camera">The camera using the dithering effect.</param>
        /// <param name="material">The material used with the dithering effect.</param>
        [System.Obsolete("This method is obsolete. Use ConfigureFilmGrain override that takes camera pixel width and height instead.")]
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

#if LWRP_DEBUG_STATIC_POSTFX
            float offsetX = 0f;
            float offsetY = 0f;
#else
            Random.InitState(Time.frameCount);
            float offsetX = Random.value;
            float offsetY = Random.value;
#endif

            var tilingParams = texture == null
                ? Vector4.zero
                : new Vector4(cameraPixelWidth / (float)texture.width, cameraPixelHeight / (float)texture.height, offsetX, offsetY);

            material.SetTexture(ShaderConstants._Grain_Texture, texture);
            material.SetVector(ShaderConstants._Grain_Params, new Vector2(settings.intensity.value * 4f, settings.response.value));
            material.SetVector(ShaderConstants._Grain_TilingParams, tilingParams);
        }

        internal static void SetSourceSize(RasterCommandBuffer cmd, RTHandle source)
        {
            float width = source.rt.width;
            float height = source.rt.height;
            if (source.rt.useDynamicScale)
            {
                width *= ScalableBufferManager.widthScaleFactor;
                height *= ScalableBufferManager.heightScaleFactor;
            }
            cmd.SetGlobalVector(ShaderConstants._SourceSize, new Vector4(width, height, 1.0f / width, 1.0f / height));
        }

        internal static void SetSourceSize(CommandBuffer cmd, RTHandle source)
        {
            SetSourceSize(CommandBufferHelpers.GetRasterCommandBuffer(cmd), source);
        }

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
