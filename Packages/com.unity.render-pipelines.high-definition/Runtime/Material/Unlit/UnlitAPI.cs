using UnityEditor.Rendering.HighDefinition;

using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

namespace UnityEngine.Rendering.HighDefinition
{
    internal static class UnlitAPI
    {
        // All Validate functions must be static. It allows to automatically update the shaders with a script if code changes
        internal static void ValidateMaterial(Material material)
        {
            material.SetupBaseUnlitKeywords();
            material.SetupBaseUnlitPass();

            if (material.HasProperty(kEmissiveColorMap))
                CoreUtils.SetKeyword(material, "_EMISSIVE_COLOR_MAP", material.GetTexture(kEmissiveColorMap));
            if (material.HasProperty(kUseEmissiveIntensity) && material.GetFloat(kUseEmissiveIntensity) != 0)
                material.UpdateEmissiveColorFromIntensityAndEmissiveColorLDR();

            // All the bits exclusively related to lit are ignored inside the BaseLitGUI function.
            bool receivesLighting = (material.HasProperty(kShadowMatteFilter) && material.GetFloat(kShadowMatteFilter) != 0);
            bool excludeFromTUAndAA = BaseLitAPI.CompatibleWithExcludeFromTUAndAA(material) && material.GetInt(kExcludeFromTUAndAA) != 0;
            BaseLitAPI.SetupStencil(material, receivesLighting: receivesLighting, receivesSSR: false, useSplitLighting: false, excludeFromTUAndAA: excludeFromTUAndAA);
        }
    }
}
