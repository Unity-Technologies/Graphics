namespace UnityEditor.ShaderGraph.Internal
{
    internal static class LightmappingShaderProperties
    {
        public static readonly Texture2DArrayShaderProperty kLightmapsArray = new Texture2DArrayShaderProperty()
        {
            displayName = "unity_Lightmaps",
            generatePropertyBlock = true,
            gpuInstanced = false,
            hidden = true,
            modifiable = true,
            overrideReferenceName = "unity_Lightmaps",
            precision = Precision.Float
        };

        public static readonly Texture2DArrayShaderProperty kLightmapsIndirectionArray =
            new Texture2DArrayShaderProperty()
            {
                displayName = "unity_LightmapsInd",
                generatePropertyBlock = true,
                gpuInstanced = false,
                hidden = true,
                modifiable = true,
                overrideReferenceName = "unity_LightmapsInd",
                precision = Precision.Float
            };

        public static readonly Texture2DArrayShaderProperty kShadowMasksArray = new Texture2DArrayShaderProperty()
        {
            displayName = "unity_ShadowMasks",
            generatePropertyBlock = true,
            gpuInstanced = false,
            hidden = true,
            modifiable = true,
            overrideReferenceName = "unity_ShadowMasks",
            precision = Precision.Float
        };
    }
}
