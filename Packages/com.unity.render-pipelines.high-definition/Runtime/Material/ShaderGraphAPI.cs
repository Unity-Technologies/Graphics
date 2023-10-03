using UnityEditor.Rendering.HighDefinition;

// Include material common properties names
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Common GUI for Lit ShaderGraphs.
    /// </summary>
    internal static class ShaderGraphAPI
    {
        readonly static string[] floatPropertiesToSynchronize =
        {
            kUseSplitLighting,
        };

        /// <summary>
        /// Synchronize a set of properties that Unity requires for Shader Graph materials to work correctly. This function is for Shader Graph only.
        /// </summary>
        /// <param name="material">The target material.</param>
        public static void SynchronizeShaderGraphProperties(Material material)
        {
            var defaultProperties = new Material(material.shader);
            foreach (var floatToSync in floatPropertiesToSynchronize)
                if (material.HasProperty(floatToSync) && defaultProperties.HasProperty(floatToSync))
                    material.SetFloat(floatToSync, defaultProperties.GetFloat(floatToSync));

            CoreUtils.Destroy(defaultProperties);
            defaultProperties = null;
        }

        /// <summary>
        /// Sets up the keywords and passes for the Unlit Shader Graph material you pass in.
        /// </summary>
        /// <param name="material">The target material.</param>
        public static void ValidateUnlitMaterial(Material material)
        {
            SynchronizeShaderGraphProperties(material);
            UnlitAPI.ValidateMaterial(material);
        }

        /// <summary>
        /// Sets up the keywords and passes for a Lit Shader Graph material.
        /// </summary>
        /// <param name="material">The target material.</param>
        public static void ValidateLightingMaterial(Material material)
        {
            SynchronizeShaderGraphProperties(material);

            BaseLitAPI.SetupBaseLitKeywords(material);
            BaseLitAPI.SetupBaseLitMaterialPass(material);

            bool receiveSSR = false;
            if (material.HasProperty(kSurfaceType) && (SurfaceType)material.GetFloat(kSurfaceType) == SurfaceType.Transparent)
                receiveSSR = material.HasProperty(kReceivesSSRTransparent) ? material.GetFloat(kReceivesSSRTransparent) != 0 : false;
            else
                receiveSSR = material.HasProperty(kReceivesSSR) ? material.GetFloat(kReceivesSSR) != 0 : false;

            bool useSplitLighting = false;
            int index = material.shader.FindPropertyIndex(kUseSplitLighting);
            if (index != -1)
                useSplitLighting = material.shader.GetPropertyDefaultFloatValue(index) != 0;

            bool excludeFromTUAndAA = BaseLitAPI.CompatibleWithExcludeFromTUAndAA(material) && material.GetInt(kExcludeFromTUAndAA) != 0;
            BaseLitAPI.SetupStencil(material, receivesLighting: true, receiveSSR, useSplitLighting, excludeFromTUAndAA);
        }

        public static void ValidateDecalMaterial(Material material)
        {
            DecalAPI.SetupCommonDecalMaterialKeywordsAndPass(material);
        }

        public static void ValidateFogVolumeMaterial(Material material)
        {
            FogVolumeAPI.SetupFogVolumeKeywordsAndProperties(material);
        }
    }
}
