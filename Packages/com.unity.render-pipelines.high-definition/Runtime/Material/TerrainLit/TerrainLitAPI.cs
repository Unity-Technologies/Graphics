using UnityEditor.Rendering.HighDefinition;

// Include material common properties names
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

namespace UnityEngine.Rendering.HighDefinition
{
    internal static class TerrainLitAPI
    {
        // All Validate functions must be static. It allows to automatically update the shaders with a script if code changes
        public static void ValidateMaterial(Material material)
        {
            BaseLitAPI.SetupBaseLitKeywords(material);
            BaseLitAPI.SetupBaseLitMaterialPass(material);

            bool receiveSSR = false;
            if (material.HasProperty(kSurfaceType) && (SurfaceType)material.GetFloat(kSurfaceType) == SurfaceType.Transparent)
                receiveSSR = material.HasProperty(kReceivesSSRTransparent) ? material.GetFloat(kReceivesSSRTransparent) != 0 : false;
            else
                receiveSSR = material.HasProperty(kReceivesSSR) ? material.GetFloat(kReceivesSSR) != 0 : false;
            BaseLitAPI.SetupStencil(material, receivesLighting: true, receiveSSR, material.GetMaterialId() == MaterialId.LitSSS);

            // TODO: planar/triplannar support
            //SetupLayersMappingKeywords(material);

            bool enableHeightBlend = material.HasProperty(kEnableHeightBlend) && material.GetFloat(kEnableHeightBlend) > 0;
            CoreUtils.SetKeyword(material, "_TERRAIN_BLEND_HEIGHT", enableHeightBlend);

            bool enableInstancedPerPixelNormal = material.HasProperty(kEnableInstancedPerPixelNormal) && material.GetFloat(kEnableInstancedPerPixelNormal) > 0.0f;
            CoreUtils.SetKeyword(material, "_TERRAIN_INSTANCED_PERPIXEL_NORMAL", enableInstancedPerPixelNormal);

            int specOcclusionMode = material.GetInt(kSpecularOcclusionMode);
            CoreUtils.SetKeyword(material, "_SPECULAR_OCCLUSION_NONE", specOcclusionMode == 0);
        }
    }
}
