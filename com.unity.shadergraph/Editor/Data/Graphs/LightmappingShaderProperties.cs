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

            internal override string GetPropertyAsArgumentString(string precisionString)
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

        public static readonly Matrix4ShaderProperty kPRevMatrix = new Matrix4ShaderProperty()
        {
            displayName = "unity_MatrixPreviousM",
            generatePropertyBlock = true,
            overrideHLSLDeclaration = false,
            hlslDeclarationOverride = HLSLDeclaration.UnityPerMaterial,
            hidden = true,
            overrideReferenceName = "unity_MatrixPreviousM",
            precision = Precision.Single
        };
    }
}
