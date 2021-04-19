using System;

namespace UnityEditor.ShaderGraph.Internal
{
    internal static class LightmappingShaderProperties
    {
        public class LightmapTextureArrayProperty : Texture2DArrayShaderProperty
        {
            internal override void ForeachHLSLProperty(Action<HLSLProperty> action)
            {
                // no declaration from ShaderGraph side -- declared by SRP internal include files
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
            overrideHLSLDeclaration = false,
            hlslDeclarationOverride = HLSLDeclaration.DoNotDeclare,
            hidden = true,
            modifiable = true,
            overrideReferenceName = "unity_Lightmaps",
            precision = Precision.Single
        };

        public static readonly LightmapTextureArrayProperty kLightmapsIndirectionArray = new LightmapTextureArrayProperty()
            {
                displayName = "unity_LightmapsInd",
                generatePropertyBlock = true,
                overrideHLSLDeclaration = false,
                hlslDeclarationOverride = HLSLDeclaration.DoNotDeclare,
                hidden = true,
                modifiable = true,
                overrideReferenceName = "unity_LightmapsInd",
                precision = Precision.Single
        };

        public static readonly LightmapTextureArrayProperty kShadowMasksArray = new LightmapTextureArrayProperty()
        {
            displayName = "unity_ShadowMasks",
            generatePropertyBlock = true,
            overrideHLSLDeclaration = false,
            hlslDeclarationOverride = HLSLDeclaration.DoNotDeclare,
            hidden = true,
            modifiable = true,
            overrideReferenceName = "unity_ShadowMasks",
            precision = Precision.Single
        };
    }
}
