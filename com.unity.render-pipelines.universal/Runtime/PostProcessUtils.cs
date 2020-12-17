namespace UnityEngine.Rendering.Universal
{
    public static class PostProcessUtils
    {
        [System.Obsolete("This method is obsolete. Use ConfigureDithering override that takes camera pixel width and height instead.")]
        public static int ConfigureDithering(PostProcessData data, int index, Camera camera, Material material)
        {
            return ConfigureDithering(data, index, camera.pixelWidth, camera.pixelHeight, material);
        }

        // TODO: Add API docs
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

            float rndOffsetX = Random.value;
            float rndOffsetY = Random.value;
            #endif

            // Ideally we would be sending a texture array once and an index to the slice to use
            // on every frame but these aren't supported on all Universal targets
            var noiseTex = blueNoise[index];

            material.SetTexture(URPShaderIDs._BlueNoise_Texture, noiseTex);
            material.SetVector(URPShaderIDs._Dithering_Params, new Vector4(
                cameraPixelWidth / (float)noiseTex.width,
                cameraPixelHeight / (float)noiseTex.height,
                rndOffsetX,
                rndOffsetY
            ));

            return index;
        }

        [System.Obsolete("This method is obsolete. Use ConfigureFilmGrain override that takes camera pixel width and height instead.")]
        public static void ConfigureFilmGrain(PostProcessData data, FilmGrain settings, Camera camera, Material material)
        {
            ConfigureFilmGrain(data, settings, camera.pixelWidth, camera.pixelHeight, material);
        }

        // TODO: Add API docs
        public static void ConfigureFilmGrain(PostProcessData data, FilmGrain settings, int cameraPixelWidth, int cameraPixelHeight, Material material)
        {
            var texture = settings.texture.value;

            if (settings.type.value != FilmGrainLookup.Custom)
                texture = data.textures.filmGrainTex[(int)settings.type.value];

            #if LWRP_DEBUG_STATIC_POSTFX
            float offsetX = 0f;
            float offsetY = 0f;
            #else
            float offsetX = Random.value;
            float offsetY = Random.value;
            #endif

            var tilingParams = texture == null
                ? Vector4.zero
                : new Vector4(cameraPixelWidth / (float)texture.width, cameraPixelHeight / (float)texture.height, offsetX, offsetY);

            material.SetTexture(URPShaderIDs._Grain_Texture, texture);
            material.SetVector(URPShaderIDs._Grain_Params, new Vector2(settings.intensity.value * 4f, settings.response.value));
            material.SetVector(URPShaderIDs._Grain_TilingParams, tilingParams);
        }

        internal static void SetSourceSize(CommandBuffer cmd, RenderTextureDescriptor desc)
        {
            float width = desc.width;
            float height = desc.height;
            if (desc.useDynamicScale)
            {
                width *= ScalableBufferManager.widthScaleFactor;
                height *= ScalableBufferManager.heightScaleFactor;
            }
            cmd.SetGlobalVector(URPShaderIDs._SourceSize, new Vector4(width, height, 1.0f / width, 1.0f / height));
        }
    }
}
