using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;
using System;

// Include material common properties names
using static UnityEngine.Experimental.Rendering.HDPipeline.HDMaterialProperties;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    /// <summary>
    /// GUI for HDRP Lit materials (and tesselation), does not include shader graph + function to setup material keywords for Lit
    /// </summary>
    class LitGUI : HDShaderGUI
    {
        // For lit GUI we don't display the heightmap nor layering options
        const LitSurfaceInputsUIBlock.Features litSurfaceFeatures = LitSurfaceInputsUIBlock.Features.All ^ LitSurfaceInputsUIBlock.Features.HeightMap ^ LitSurfaceInputsUIBlock.Features.LayerOptions;

        MaterialUIBlockList uiBlocks = new MaterialUIBlockList
        {
            new SurfaceOptionUIBlock(MaterialUIBlock.Expandable.Base, features: SurfaceOptionUIBlock.Features.Lit ^ SurfaceOptionUIBlock.Features.ShowAfterPostProcessPass),
            new TessellationOptionsUIBlock(MaterialUIBlock.Expandable.Tesselation),
            new LitSurfaceInputsUIBlock(MaterialUIBlock.Expandable.Input, features: litSurfaceFeatures),
            new DetailInputsUIBlock(MaterialUIBlock.Expandable.Detail),
            new TransparencyUIBlock(MaterialUIBlock.Expandable.Transparency),
            new EmissionUIBlock(MaterialUIBlock.Expandable.Emissive),
            new AdvancedOptionsUIBlock(MaterialUIBlock.Expandable.Advance),
        };

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            using (var changed = new EditorGUI.ChangeCheckScope())
            {
                uiBlocks.OnGUI(materialEditor, props);

                // Apply material keywords and pass:
                if (changed.changed)
                {
                    foreach (var material in uiBlocks.materials)
                        SetupMaterialKeywordsAndPassInternal(material);
                }
            }
        }

        // Properties for material keyword setup
        protected const string kUVBase = "_UVBase";

        protected const string kMaskMap = "_MaskMap";
        protected const string kNormalMap = "_NormalMap";
        protected const string kNormalMapOS = "_NormalMapOS";
        protected const string kBentNormalMap = "_BentNormalMap";
        protected const string kBentNormalMapOS = "_BentNormalMapOS";
        protected const string kNormalMapSpace = "_NormalMapSpace";

        protected const string kHeightMap = "_HeightMap";

        protected const string kSubsurfaceMaskMap = "_SubsurfaceMaskMap";
        protected const string kThicknessMap = "_ThicknessMap";

        protected const string kUVDetail = "_UVDetail";
        protected const string kDetailMap = "_DetailMap";

        protected const string kSpecularColorMap = "_SpecularColorMap";

        protected const string kTangentMap = "_TangentMap";
        protected const string kTangentMapOS = "_TangentMapOS";
        protected const string kAnisotropyMap = "_AnisotropyMap";

        protected const string kIridescenceThicknessMap = "_IridescenceThicknessMap";

        protected const string kCoatMask = "_CoatMask";
        protected const string kCoatMaskMap = "_CoatMaskMap";

        protected MaterialProperty emissiveColorMode = null;
        protected const string kEmissiveColorMode = "_EmissiveColorMode";
        protected MaterialProperty emissiveColor = null;
        protected const string kEmissiveColor = "_EmissiveColor";
        protected MaterialProperty emissiveColorMap = null;
        protected const string kEmissiveColorMap = "_EmissiveColorMap";
        protected MaterialProperty albedoAffectEmissive = null;
        protected const string kAlbedoAffectEmissive = "_AlbedoAffectEmissive";
        protected MaterialProperty UVEmissive = null;
        protected const string kUVEmissive = "_UVEmissive";
        protected MaterialProperty TexWorldScaleEmissive = null;
        protected const string kTexWorldScaleEmissive = "_TexWorldScaleEmissive";
        protected MaterialProperty UVMappingMaskEmissive = null;
        protected const string kUVMappingMaskEmissive = "_UVMappingMaskEmissive";
        protected MaterialProperty emissiveIntensity = null;
        protected const string kEmissiveIntensity = "_EmissiveIntensity";
        protected MaterialProperty emissiveColorLDR = null;
        protected const string kEmissiveColorLDR = "_EmissiveColorLDR";
        protected MaterialProperty emissiveIntensityUnit = null;
        protected const string kEmissiveIntensityUnit = "_EmissiveIntensityUnit";
        protected MaterialProperty emissiveExposureWeight = null;
        protected const string kemissiveExposureWeight = "_EmissiveExposureWeight";
        protected MaterialProperty useEmissiveIntensity = null;
        protected const string kUseEmissiveIntensity = "_UseEmissiveIntensity";

        protected const string kEnableSpecularOcclusion = "_EnableSpecularOcclusion";

        // transparency params
        protected const string kTransmittanceColorMap = "_TransmittanceColorMap";
        protected const string kRefractionModel = "_RefractionModel";

        protected override void SetupMaterialKeywordsAndPassInternal(Material material) => SetupMaterialKeywordsAndPass(material);

        // All Setup Keyword functions must be static. It allow to create script to automatically update the shaders with a script if code change
        static public void SetupMaterialKeywordsAndPass(Material material)
        {
            BaseLitGUI.SetupBaseLitKeywords(material);
            BaseLitGUI.SetupBaseLitMaterialPass(material);
            BaseLitGUI.SetupStencil(material, material.GetInt(kReceivesSSR) != 0, material.GetMaterialId() == MaterialId.LitSSS);

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
                    // With details map, we always use a normal map but in case of objects space there is no good default, so the result will be weird until users fix it
                    CoreUtils.SetKeyword(material, "_NORMALMAP", material.GetTexture(kNormalMapOS) || material.GetTexture(kDetailMap));
                    CoreUtils.SetKeyword(material, "_TANGENTMAP", material.GetTexture(kTangentMapOS));
                    CoreUtils.SetKeyword(material, "_BENTNORMALMAP", material.GetTexture(kBentNormalMapOS));
                }
            }

            if (material.HasProperty(kMaskMap))
                CoreUtils.SetKeyword(material, "_MASKMAP", material.GetTexture(kMaskMap));

            if (material.HasProperty(kUVEmissive) && material.HasProperty(kEmissiveColorMap))
            {
                CoreUtils.SetKeyword(material, "_EMISSIVE_MAPPING_PLANAR", ((UVBaseMapping)material.GetFloat(kUVEmissive)) == UVBaseMapping.Planar && material.GetTexture(kEmissiveColorMap));
                CoreUtils.SetKeyword(material, "_EMISSIVE_MAPPING_TRIPLANAR", ((UVBaseMapping)material.GetFloat(kUVEmissive)) == UVBaseMapping.Triplanar && material.GetTexture(kEmissiveColorMap));
                CoreUtils.SetKeyword(material, "_EMISSIVE_COLOR_MAP", material.GetTexture(kEmissiveColorMap));
            }

            if (material.HasProperty(kEnableSpecularOcclusion))
                CoreUtils.SetKeyword(material, "_ENABLESPECULAROCCLUSION", material.GetFloat(kEnableSpecularOcclusion) > 0.0f);
            if (material.HasProperty(kHeightMap))
                CoreUtils.SetKeyword(material, "_HEIGHTMAP", material.GetTexture(kHeightMap));
            if (material.HasProperty(kAnisotropyMap))
                CoreUtils.SetKeyword(material, "_ANISOTROPYMAP", material.GetTexture(kAnisotropyMap));
            if (material.HasProperty(kDetailMap))
                CoreUtils.SetKeyword(material, "_DETAIL_MAP", material.GetTexture(kDetailMap));
            if (material.HasProperty(kSubsurfaceMaskMap))
                CoreUtils.SetKeyword(material, "_SUBSURFACE_MASK_MAP", material.GetTexture(kSubsurfaceMaskMap));
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
                MaterialId materialId = material.GetMaterialId();
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
                var refractionModelValue = (ScreenSpaceRefraction.RefractionModel)material.GetFloat(kRefractionModel);
                // We can't have refraction in pre-refraction queue
                var canHaveRefraction = !HDRenderQueue.k_RenderQueue_PreRefraction.Contains(material.renderQueue);
                CoreUtils.SetKeyword(material, "_REFRACTION_PLANE", (refractionModelValue == ScreenSpaceRefraction.RefractionModel.Box) && canHaveRefraction);
                CoreUtils.SetKeyword(material, "_REFRACTION_SPHERE", (refractionModelValue == ScreenSpaceRefraction.RefractionModel.Sphere) && canHaveRefraction);
                CoreUtils.SetKeyword(material, "_TRANSMITTANCECOLORMAP", material.GetTexture(kTransmittanceColorMap) && canHaveRefraction);
            }
        }
    }
} // namespace UnityEditor
