using UnityEditor.Rendering.HighDefinition;
using System;

// Include material common properties names
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Common GUI for Lit ShaderGraphs.
    /// </summary>
    internal static class ShaderGraphAPI
    {
        /// <summary>
        /// Sets up the keywords and passes for the Unlit Shader Graph material you pass in.
        /// </summary>
        /// <param name="material">The target material.</param>
        public static void ValidateUnlitMaterial(Material material)
        {
            UnlitAPI.ValidateMaterial(material);
        }

        /// <summary>
        /// Sets up the keywords and passes for a Lit Shader Graph material.
        /// </summary>
        /// <param name="material">The target material.</param>
        public static void ValidateLightingMaterial(Material material)
        {
            BaseLitAPI.SetupBaseLitKeywords(material);
            BaseLitAPI.SetupBaseLitMaterialPass(material);

            bool receiveSSR = false;
            if (material.HasProperty(kSurfaceType) && (SurfaceType)material.GetFloat(kSurfaceType) == SurfaceType.Transparent)
                receiveSSR = material.HasProperty(kReceivesSSRTransparent) ? material.GetFloat(kReceivesSSRTransparent) != 0 : false;
            else
                receiveSSR = material.HasProperty(kReceivesSSR) ? material.GetFloat(kReceivesSSR) != 0 : false;

            bool useSplitLighting = false;
            if (material.HasProperty(kMaterialID))
            {
                var materialId = material.GetMaterialType();

                // Check that the value of material type is in range with the allowed values from the shader:
                int materialTypeMaskIndex = material.shader.FindPropertyIndex(kMaterialTypeMask);
                if (materialTypeMaskIndex != -1)
                {
                    int materialTypeMask = (int)material.shader.GetPropertyDefaultFloatValue(materialTypeMaskIndex);
                    if ((materialTypeMask & (1 << (int)materialId)) == 0)
                    {
                        // In case the material type in the shader is no longer supported by the shader, we reset it
                        // to the first available material type in the mask.
                        foreach (MaterialId id in Enum.GetValues(typeof(MaterialId)))
                        {
                            if ((materialTypeMask & (1 << (int)id)) != 0)
                            {
                                material.SetFloat(kMaterialID, (int)id);
                                materialId = id;
                                break;
                            }
                        }
                    }
                }

                useSplitLighting = materialId == MaterialId.LitSSS;
                CoreUtils.SetKeyword(material, "_MATERIAL_FEATURE_SUBSURFACE_SCATTERING", materialId == MaterialId.LitSSS);
                CoreUtils.SetKeyword(material, "_MATERIAL_FEATURE_TRANSMISSION", materialId == MaterialId.LitTranslucent || (materialId == MaterialId.LitSSS && material.GetFloat(kTransmissionEnable) > 0.0f));
                CoreUtils.SetKeyword(material, "_MATERIAL_FEATURE_COLORED_TRANSMISSION", materialId == MaterialId.LitColoredTranslucent);
                CoreUtils.SetKeyword(material, "_MATERIAL_FEATURE_ANISOTROPY", materialId == MaterialId.LitAniso);
                CoreUtils.SetKeyword(material, "_MATERIAL_FEATURE_IRIDESCENCE", materialId == MaterialId.LitIridescence);
                CoreUtils.SetKeyword(material, "_MATERIAL_FEATURE_SPECULAR_COLOR", materialId == MaterialId.LitSpecular);
            }
            else
            {
                int index = material.shader.FindPropertyIndex(kUseSplitLighting);
                if (index != -1)
                    useSplitLighting = material.shader.GetPropertyDefaultFloatValue(index) != 0;

            }

            if (material.HasProperty(kClearCoatEnabled))
                CoreUtils.SetKeyword(material, "_MATERIAL_FEATURE_CLEAR_COAT", material.GetFloat(kClearCoatEnabled) > 0.0);

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

        public static void ValidateSixWayMaterial(Material material)
        {
            ValidateLightingMaterial(material);
            SixWayAPI.ValidateMaterial(material);
        }

        public static void ValidateWaterDecalMaterial(Material material)
        {
            WaterDecalAPI.SetupWaterDecalKeywordsAndProperties(material);
        }
    }
}
