namespace UnityEngine.Rendering
{
    static class VrsShaders
    {
        internal const string k_TileSizePrefix = "VRS_TILE_SIZE_";

        internal const string k_DisableTexture2dXArray = "DISABLE_TEXTURE2D_X_ARRAY";
        internal const string k_YFlip = "APPLY_Y_FLIP";

        internal static readonly int s_ScaleBias = Shader.PropertyToID("_VrsScaleBias");

        internal static readonly int s_MainTex = Shader.PropertyToID("_VrsMainTex");
        internal static readonly int s_MainTexLut = Shader.PropertyToID("_VrsMainTexLut");

        internal static readonly int s_ShadingRateNativeValues = Shader.PropertyToID("_ShadingRateNativeValues");
        internal static readonly int s_ShadingRateImage = Shader.PropertyToID("_ShadingRateImage");

        internal const string k_KernelTextureCopy = "TextureCopy";
        internal const string k_KernelTextureReduce = "TextureReduce";

        internal static readonly int s_VisualizationLut = Shader.PropertyToID("_VisualizationLut");
        internal static readonly int s_VisualizationParams = Shader.PropertyToID("_VisualizationParams");
    }
}
