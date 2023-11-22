using UnityEditor.Rendering.HighDefinition;

using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

namespace UnityEngine.Rendering.HighDefinition
{
    internal static class LayeredLitAPI
    {
        // Material property name for Layered Lit keyword setup
        const string kLayerInfluenceMaskMap = "_LayerInfluenceMaskMap";
        const string kVertexColorMode = "_VertexColorMode";
        const string kUVBlendMask = "_UVBlendMask";
        const string kkUseMainLayerInfluence = "_UseMainLayerInfluence";
        const string kUseHeightBasedBlend = "_UseHeightBasedBlend";
        const string kObjectScaleAffectTile = "_ObjectScaleAffectTile";

        // Density/opacity mode
        const string kOpacityAsDensity = "_OpacityAsDensity";

        static public void SetupLayersMappingKeywords(Material material)
        {
            // object scale affect tile
            CoreUtils.SetKeyword(material, "_LAYER_TILING_COUPLED_WITH_UNIFORM_OBJECT_SCALE", material.GetFloat(kObjectScaleAffectTile) > 0.0f);

            // Blend mask
            UVBaseMapping UVBlendMaskMapping = (UVBaseMapping)material.GetFloat(kUVBlendMask);
            CoreUtils.SetKeyword(material, "_LAYER_MAPPING_PLANAR_BLENDMASK", UVBlendMaskMapping == UVBaseMapping.Planar);
            CoreUtils.SetKeyword(material, "_LAYER_MAPPING_TRIPLANAR_BLENDMASK", UVBlendMaskMapping == UVBaseMapping.Triplanar);

            int numLayer = (int)material.GetFloat(kLayerCount);

            // Layer
            if (numLayer == 4)
            {
                CoreUtils.SetKeyword(material, "_LAYEREDLIT_4_LAYERS", true);
                CoreUtils.SetKeyword(material, "_LAYEREDLIT_3_LAYERS", false);
            }
            else if (numLayer == 3)
            {
                CoreUtils.SetKeyword(material, "_LAYEREDLIT_4_LAYERS", false);
                CoreUtils.SetKeyword(material, "_LAYEREDLIT_3_LAYERS", true);
            }
            else
            {
                CoreUtils.SetKeyword(material, "_LAYEREDLIT_4_LAYERS", false);
                CoreUtils.SetKeyword(material, "_LAYEREDLIT_3_LAYERS", false);
            }

            const string kLayerMappingPlanar = "_LAYER_MAPPING_PLANAR";
            const string kLayerMappingTriplanar = "_LAYER_MAPPING_TRIPLANAR";

            // We have to check for each layer if the UV2 or UV3 is needed.
            bool needUV3 = false;
            bool needUV2 = false;

            for (int i = 0; i < numLayer; ++i)
            {
                string layerUVBaseParam = string.Format("{0}{1}", kUVBase, i);
                UVBaseMapping layerUVBaseMapping = (UVBaseMapping)material.GetFloat(layerUVBaseParam);
                string currentLayerMappingPlanar = string.Format("{0}{1}", kLayerMappingPlanar, i);
                CoreUtils.SetKeyword(material, currentLayerMappingPlanar, layerUVBaseMapping == UVBaseMapping.Planar);
                string currentLayerMappingTriplanar = string.Format("{0}{1}", kLayerMappingTriplanar, i);
                CoreUtils.SetKeyword(material, currentLayerMappingTriplanar, layerUVBaseMapping == UVBaseMapping.Triplanar);

                string uvBase = string.Format("{0}{1}", kUVBase, i);
                string uvDetail = string.Format("{0}{1}", kUVDetail, i);

                if (((UVDetailMapping)material.GetFloat(uvDetail) == UVDetailMapping.UV2) || ((UVBaseMapping)material.GetFloat(uvBase) == UVBaseMapping.UV2))
                {
                    needUV2 = true;
                }

                if (((UVDetailMapping)material.GetFloat(uvDetail) == UVDetailMapping.UV3) || ((UVBaseMapping)material.GetFloat(uvBase) == UVBaseMapping.UV3))
                {
                    needUV3 = true;
                    break; // If we find it UV3 let's early out
                }
            }

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

        // All Validate functions must be static. It allows to automatically update the shaders with a script if code changes
        internal static void ValidateMaterial(Material material)
        {
            MaterialId materialId = material.GetMaterialType();
            if (material.HasProperty(kMaterialID))
            {
                if (materialId != MaterialId.LitStandard && materialId != MaterialId.LitSSS && materialId != MaterialId.LitTranslucent)
                {
                    materialId = MaterialId.LitStandard;
                    material.SetFloat(kMaterialID, (float)materialId);
                }
            }

            BaseLitAPI.SetupBaseLitKeywords(material);
            BaseLitAPI.SetupBaseLitMaterialPass(material);
            SetupLayersMappingKeywords(material);
            bool receiveSSR = material.GetSurfaceType() == SurfaceType.Opaque ? (material.HasProperty(kReceivesSSR) ? material.GetInt(kReceivesSSR) != 0 : false)
                : (material.HasProperty(kReceivesSSRTransparent) ? material.GetInt(kReceivesSSRTransparent) != 0 : false);

            bool excludeFromTUAndAA = BaseLitAPI.CompatibleWithExcludeFromTUAndAA(material) && material.GetInt(kExcludeFromTUAndAA) != 0;
            BaseLitAPI.SetupStencil(material, receivesLighting: true, receiveSSR, materialId == MaterialId.LitSSS, excludeFromTUAndAA: excludeFromTUAndAA);

            for (int i = 0; i < kMaxLayerCount; ++i)
            {
                NormalMapSpace normalMapSpace = ((NormalMapSpace)material.GetFloat(kNormalMapSpace + i));

                CoreUtils.SetKeyword(material, "_NORMALMAP_TANGENT_SPACE" + i, normalMapSpace == NormalMapSpace.TangentSpace);

                if (normalMapSpace == NormalMapSpace.TangentSpace)
                {
                    CoreUtils.SetKeyword(material, "_NORMALMAP" + i, material.GetTexture(kNormalMap + i) || material.GetTexture(kDetailMap + i));
                    CoreUtils.SetKeyword(material, "_BENTNORMALMAP" + i, material.GetTexture(kBentNormalMap + i));
                }
                else
                {
                    CoreUtils.SetKeyword(material, "_NORMALMAP" + i, material.GetTexture(kNormalMapOS + i) || material.GetTexture(kDetailMap + i));
                    CoreUtils.SetKeyword(material, "_BENTNORMALMAP" + i, material.GetTexture(kBentNormalMapOS + i));
                }

                CoreUtils.SetKeyword(material, "_MASKMAP" + i, material.GetTexture(kMaskMap + i));

                CoreUtils.SetKeyword(material, "_DETAIL_MAP" + i, material.GetTexture(kDetailMap + i));

                CoreUtils.SetKeyword(material, "_HEIGHTMAP" + i, material.GetTexture(kHeightMap + i));

                CoreUtils.SetKeyword(material, "_SUBSURFACE_MASK_MAP" + i, material.GetTexture(kSubsurfaceMaskMap + i));
                CoreUtils.SetKeyword(material, "_TRANSMISSION_MASK_MAP" + i, material.GetTexture(kTransmissionMaskMap + i));
                CoreUtils.SetKeyword(material, "_THICKNESSMAP" + i, material.GetTexture(kThicknessMap + i));
            }

            CoreUtils.SetKeyword(material, "_INFLUENCEMASK_MAP", material.GetTexture(kLayerInfluenceMaskMap) && material.GetFloat(kkUseMainLayerInfluence) != 0.0f);

            CoreUtils.SetKeyword(material, "_EMISSIVE_MAPPING_PLANAR", ((UVEmissiveMapping)material.GetFloat(kUVEmissive)) == UVEmissiveMapping.Planar && material.GetTexture(kEmissiveColorMap));
            CoreUtils.SetKeyword(material, "_EMISSIVE_MAPPING_TRIPLANAR", ((UVEmissiveMapping)material.GetFloat(kUVEmissive)) == UVEmissiveMapping.Triplanar && material.GetTexture(kEmissiveColorMap));
            CoreUtils.SetKeyword(material, "_EMISSIVE_MAPPING_BASE", ((UVEmissiveMapping)material.GetFloat(kUVEmissive)) == UVEmissiveMapping.SameAsBase && material.GetTexture(kEmissiveColorMap));
            CoreUtils.SetKeyword(material, "_EMISSIVE_COLOR_MAP", material.GetTexture(kEmissiveColorMap));

            if (material.HasProperty(kUseEmissiveIntensity) && material.GetFloat(kUseEmissiveIntensity) != 0)
                material.UpdateEmissiveColorFromIntensityAndEmissiveColorLDR();

            // For migration of specular occlusion to specular mode we remove previous keyword
            // _ENABLESPECULAROCCLUSION is deprecated
            CoreUtils.SetKeyword(material, "_ENABLESPECULAROCCLUSION", false);

            int specOcclusionMode = material.GetInt(kSpecularOcclusionMode);
            CoreUtils.SetKeyword(material, "_SPECULAR_OCCLUSION_NONE", specOcclusionMode == 0);
            CoreUtils.SetKeyword(material, "_SPECULAR_OCCLUSION_FROM_BENT_NORMAL_MAP", specOcclusionMode == 2);
            CoreUtils.SetKeyword(material, "_MAIN_LAYER_INFLUENCE_MODE", material.GetFloat(kkUseMainLayerInfluence) != 0.0f);

            VertexColorMode VCMode = (VertexColorMode)material.GetFloat(kVertexColorMode);
            if (VCMode == VertexColorMode.Multiply)
            {
                CoreUtils.SetKeyword(material, "_LAYER_MASK_VERTEX_COLOR_MUL", true);
                CoreUtils.SetKeyword(material, "_LAYER_MASK_VERTEX_COLOR_ADD", false);
            }
            else if (VCMode == VertexColorMode.Add)
            {
                CoreUtils.SetKeyword(material, "_LAYER_MASK_VERTEX_COLOR_MUL", false);
                CoreUtils.SetKeyword(material, "_LAYER_MASK_VERTEX_COLOR_ADD", true);
            }
            else
            {
                CoreUtils.SetKeyword(material, "_LAYER_MASK_VERTEX_COLOR_MUL", false);
                CoreUtils.SetKeyword(material, "_LAYER_MASK_VERTEX_COLOR_ADD", false);
            }

            bool useHeightBasedBlend = material.GetFloat(kUseHeightBasedBlend) != 0.0f;
            CoreUtils.SetKeyword(material, "_HEIGHT_BASED_BLEND", useHeightBasedBlend);

            bool useDensityModeEnable = false;
            for (int i = 0; i < material.GetInt(kLayerCount); ++i)
            {
                useDensityModeEnable |= material.GetFloat(kOpacityAsDensity + i) != 0.0f;
            }
            CoreUtils.SetKeyword(material, "_DENSITY_MODE", useDensityModeEnable);

            CoreUtils.SetKeyword(material, "_MATERIAL_FEATURE_SUBSURFACE_SCATTERING", materialId == MaterialId.LitSSS);
            CoreUtils.SetKeyword(material, "_MATERIAL_FEATURE_TRANSMISSION", materialId == MaterialId.LitTranslucent || (materialId == MaterialId.LitSSS && material.GetFloat(kTransmissionEnable) > 0.0f));

            BaseLitAPI.SetupDisplacement(material, material.GetInt(kLayerCount));
        }
    }
}
