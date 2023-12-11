using UnityEditor.Rendering.HighDefinition;

using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

namespace UnityEngine.Rendering.HighDefinition
{
    internal static class LitAPI
    {
        // All Validate functions must be static. It allows to automatically update the shaders with a script if code changes
        internal static void ValidateMaterial(Material material)
        {
            BaseLitAPI.SetupBaseLitKeywords(material);
            BaseLitAPI.SetupBaseLitMaterialPass(material);
            bool receiveSSR = material.GetSurfaceType() == SurfaceType.Opaque ? (material.HasProperty(kReceivesSSR) ? material.GetInt(kReceivesSSR) != 0 : false)
                : (material.HasProperty(kReceivesSSRTransparent) ? material.GetInt(kReceivesSSRTransparent) != 0 : false);
            bool excludeFromTUAndAA = BaseLitAPI.CompatibleWithExcludeFromTUAndAA(material) && material.GetInt(kExcludeFromTUAndAA) != 0;
            BaseLitAPI.SetupStencil(material, receivesLighting: true, receiveSSR, material.GetMaterialType() == MaterialId.LitSSS, excludeFromTUAndAA: excludeFromTUAndAA);
            BaseLitAPI.SetupDisplacement(material);

            if (material.HasProperty(kNormalMapSpace))
            {
                NormalMapSpace normalMapSpace = (NormalMapSpace)material.GetFloat(kNormalMapSpace);

                // Note: keywords must be based on Material value not on MaterialProperty due to multi-edit & material animation
                // (MaterialProperty value might come from renderer material property block)
                CoreUtils.SetKeyword(material, "_MAPPING_PLANAR", ((UVBaseMapping)material.GetFloat(kUVBase)) == UVBaseMapping.Planar);
                CoreUtils.SetKeyword(material, "_MAPPING_TRIPLANAR", ((UVBaseMapping)material.GetFloat(kUVBase)) == UVBaseMapping.Triplanar);

                CoreUtils.SetKeyword(material, "_NORMALMAP_TANGENT_SPACE", (normalMapSpace == NormalMapSpace.TangentSpace));

                if (normalMapSpace == NormalMapSpace.TangentSpace)
                {
                    // With details map, we always use a normal map and Unity provide a default (0, 0, 1) normal map for it
                    CoreUtils.SetKeyword(material, "_NORMALMAP", material.GetTexture(kNormalMap) || material.GetTexture(kDetailMap));
                    CoreUtils.SetKeyword(material, "_TANGENTMAP", material.GetTexture(kTangentMap));
                    CoreUtils.SetKeyword(material, "_BENTNORMALMAP", material.GetTexture(kBentNormalMap));
                }
                else // Object space
                {
                    CoreUtils.SetKeyword(material, "_NORMALMAP", material.GetTexture(kNormalMapOS));
                    CoreUtils.SetKeyword(material, "_TANGENTMAP", material.GetTexture(kTangentMapOS));
                    CoreUtils.SetKeyword(material, "_BENTNORMALMAP", material.GetTexture(kBentNormalMapOS));
                }
            }

            if (material.HasProperty(kMaskMap))
                CoreUtils.SetKeyword(material, "_MASKMAP", material.GetTexture(kMaskMap));

            if (material.HasProperty(kUVEmissive) && material.HasProperty(kEmissiveColorMap))
            {
                CoreUtils.SetKeyword(material, "_EMISSIVE_MAPPING_PLANAR", ((UVEmissiveMapping)material.GetFloat(kUVEmissive)) == UVEmissiveMapping.Planar && material.GetTexture(kEmissiveColorMap));
                CoreUtils.SetKeyword(material, "_EMISSIVE_MAPPING_TRIPLANAR", ((UVEmissiveMapping)material.GetFloat(kUVEmissive)) == UVEmissiveMapping.Triplanar && material.GetTexture(kEmissiveColorMap));
                CoreUtils.SetKeyword(material, "_EMISSIVE_MAPPING_BASE", ((UVEmissiveMapping)material.GetFloat(kUVEmissive)) == UVEmissiveMapping.SameAsBase && material.GetTexture(kEmissiveColorMap));
                CoreUtils.SetKeyword(material, "_EMISSIVE_COLOR_MAP", material.GetTexture(kEmissiveColorMap));
            }
            if (material.HasProperty(kUseEmissiveIntensity) && material.GetFloat(kUseEmissiveIntensity) != 0)
                material.UpdateEmissiveColorFromIntensityAndEmissiveColorLDR();

            if (material.HasProperty(kSpecularOcclusionMode))
            {
                // For migration of specular occlusion to specular mode we remove previous keyword
                // _ENABLESPECULAROCCLUSION is deprecated
                CoreUtils.SetKeyword(material, "_ENABLESPECULAROCCLUSION", false);

                int specOcclusionMode = material.GetInt(kSpecularOcclusionMode);
                CoreUtils.SetKeyword(material, "_SPECULAR_OCCLUSION_NONE", specOcclusionMode == 0);
                CoreUtils.SetKeyword(material, "_SPECULAR_OCCLUSION_FROM_BENT_NORMAL_MAP", specOcclusionMode == 2);
            }
            if (material.HasProperty(kHeightMap))
                CoreUtils.SetKeyword(material, "_HEIGHTMAP", material.GetTexture(kHeightMap));
            if (material.HasProperty(kAnisotropyMap))
                CoreUtils.SetKeyword(material, "_ANISOTROPYMAP", material.GetTexture(kAnisotropyMap));
            if (material.HasProperty(kDetailMap))
                CoreUtils.SetKeyword(material, "_DETAIL_MAP", material.GetTexture(kDetailMap));
            if (material.HasProperty(kSubsurfaceMaskMap))
                CoreUtils.SetKeyword(material, "_SUBSURFACE_MASK_MAP", material.GetTexture(kSubsurfaceMaskMap));
            if (material.HasProperty(kTransmissionMaskMap))
                CoreUtils.SetKeyword(material, "_TRANSMISSION_MASK_MAP", material.GetTexture(kTransmissionMaskMap));
            if (material.HasProperty(kThicknessMap))
                CoreUtils.SetKeyword(material, "_THICKNESSMAP", material.GetTexture(kThicknessMap));
            if (material.HasProperty(kIridescenceThicknessMap))
                CoreUtils.SetKeyword(material, "_IRIDESCENCE_THICKNESSMAP", material.GetTexture(kIridescenceThicknessMap));
            if (material.HasProperty(kSpecularColorMap))
                CoreUtils.SetKeyword(material, "_SPECULARCOLORMAP", material.GetTexture(kSpecularColorMap));

            if (material.HasProperty(kUVDetail) || material.HasProperty(kUVBase))
            {
                bool needUV2 = (UVDetailMapping)material.GetFloat(kUVDetail) == UVDetailMapping.UV2 || (UVBaseMapping)material.GetFloat(kUVBase) == UVBaseMapping.UV2;
                bool needUV3 = (UVDetailMapping)material.GetFloat(kUVDetail) == UVDetailMapping.UV3 || (UVBaseMapping)material.GetFloat(kUVBase) == UVBaseMapping.UV3;

                if (needUV3)
                {
                    material.DisableKeyword("_REQUIRE_UV2");
                    material.EnableKeyword("_REQUIRE_UV3");
                }
                else if (needUV2)
                {
                    material.EnableKeyword("_REQUIRE_UV2");
                    material.DisableKeyword("_REQUIRE_UV3");
                }
                else
                {
                    material.DisableKeyword("_REQUIRE_UV2");
                    material.DisableKeyword("_REQUIRE_UV3");
                }
            }

            if (material.HasProperty(kMaterialID))
            {
                MaterialId materialId = material.GetMaterialType();
                CoreUtils.SetKeyword(material, "_MATERIAL_FEATURE_SUBSURFACE_SCATTERING", materialId == MaterialId.LitSSS);
                CoreUtils.SetKeyword(material, "_MATERIAL_FEATURE_TRANSMISSION", materialId == MaterialId.LitTranslucent || (materialId == MaterialId.LitSSS && material.GetFloat(kTransmissionEnable) > 0.0f));

                CoreUtils.SetKeyword(material, "_MATERIAL_FEATURE_ANISOTROPY", materialId == MaterialId.LitAniso);
                // No material Id for clear coat, just test the attribute
                CoreUtils.SetKeyword(material, "_MATERIAL_FEATURE_CLEAR_COAT", material.GetFloat(kCoatMask) > 0.0 || material.GetTexture(kCoatMaskMap));
                CoreUtils.SetKeyword(material, "_MATERIAL_FEATURE_IRIDESCENCE", materialId == MaterialId.LitIridescence);
                CoreUtils.SetKeyword(material, "_MATERIAL_FEATURE_SPECULAR_COLOR", materialId == MaterialId.LitSpecular);
            }

            if (material.HasProperty(kRefractionModel))
            {
                var canHaveRefraction = material.GetSurfaceType() == SurfaceType.Transparent && !HDRenderQueue.k_RenderQueue_PreRefraction.Contains(material.renderQueue);
                CoreUtils.SetKeyword(material, "_TRANSMITTANCECOLORMAP", material.GetTexture(kTransmittanceColorMap) && canHaveRefraction);
            }
        }
    }
}
