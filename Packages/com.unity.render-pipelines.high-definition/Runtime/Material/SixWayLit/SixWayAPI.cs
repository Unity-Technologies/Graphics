using UnityEditor.Rendering.HighDefinition;

using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

namespace UnityEngine.Rendering.HighDefinition
{
    internal static class SixWayAPI
    {
        // All Validate functions must be static. It allows to automatically update the shaders with a script if code changes
        internal static void ValidateMaterial(Material material)
        {
            BaseLitAPI.SetupBaseLitKeywords(material);
            BaseLitAPI.SetupBaseLitMaterialPass(material);
            BaseLitAPI.SetupStencil(material, receivesLighting: true, false, false);

            if (material.HasProperty(kReceiveShadows))
            {
                bool receiveShadows = material.GetFloat(kReceiveShadows) > 0.0f;
                CoreUtils.SetKeyword(material, "_RECEIVE_SHADOWS_OFF", !receiveShadows);
            }

            if (material.HasProperty(kUseColorAbsorption))
            {
                bool useColorAbsorption = material.GetFloat(kUseColorAbsorption) > 0.0f;
                CoreUtils.SetKeyword(material, "_SIX_WAY_COLOR_ABSORPTION", useColorAbsorption);
            }
        }
    }
}
