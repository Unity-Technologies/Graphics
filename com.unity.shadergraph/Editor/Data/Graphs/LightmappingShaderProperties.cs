using System;

namespace UnityEditor.ShaderGraph.Internal
{
    internal static class LightmappingShaderProperties
    {
        public class LightmapTextureArrayProperty : Texture2DArrayShaderProperty
        {
            internal override string GetPropertyDeclarationString(string delimiter = ";")
            {
                return String.Empty;
            }

            internal override string GetPropertyAsArgumentString()
            {
                return String.Empty;
            }
        }

        public static readonly LightmapTextureArrayProperty kLightmapsArray = new LightmapTextureArrayProperty()
        {
            displayName = "unity_Lightmaps",
            generatePropertyBlock = true,
            gpuInstanced = false,
            hidden = true,
            modifiable = true,
            overrideReferenceName = "unity_Lightmaps",
            precision = Precision.Single
        };

        public static readonly LightmapTextureArrayProperty kLightmapsIndirectionArray = new LightmapTextureArrayProperty()
            {
                displayName = "unity_LightmapsInd",
                generatePropertyBlock = true,
                gpuInstanced = false,
                hidden = true,
                modifiable = true,
                overrideReferenceName = "unity_LightmapsInd",
                precision = Precision.Single
        };

        public static readonly LightmapTextureArrayProperty kShadowMasksArray = new LightmapTextureArrayProperty()
        {
            displayName = "unity_ShadowMasks",
            generatePropertyBlock = true,
            gpuInstanced = false,
            hidden = true,
            modifiable = true,
            overrideReferenceName = "unity_ShadowMasks",
            precision = Precision.Single
        };
    }
}
