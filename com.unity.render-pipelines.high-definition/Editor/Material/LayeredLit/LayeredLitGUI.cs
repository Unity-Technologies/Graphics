using System;
using UnityEngine;
using System.Linq;
using UnityEngine.Rendering;

// Include material common properties names
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

namespace UnityEditor.Rendering.HighDefinition
{
    // Needed for json serialization to work
    [Serializable]
    internal struct SerializeableGUIDs
    {
        public string[] GUIDArray;
    }

    /// <summary>
    /// GUI for HDRP Layered Lit materials (and tesselation), does not include shader graph + function to setup material keywords for Lit
    /// </summary>
    class LayeredLitGUI : HDShaderGUI
    {
        const LitSurfaceInputsUIBlock.Features commonLitSurfaceInputsFeatures = LitSurfaceInputsUIBlock.Features.LayerOptions;
        // Layered lit shaders don't support emission (realtime or baked)
        const EmissionUIBlock.Features emissionFeatures = EmissionUIBlock.Features.All ^ EmissionUIBlock.Features.EnableEmissionForGI;

        MaterialUIBlockList uiBlocks = new MaterialUIBlockList
        {
            new SurfaceOptionUIBlock(MaterialUIBlock.Expandable.Base, 4, SurfaceOptionUIBlock.Features.Lit),
            new TessellationOptionsUIBlock(MaterialUIBlock.Expandable.Tesselation),
            new LitSurfaceInputsUIBlock(MaterialUIBlock.Expandable.Input, kMaxLayerCount, features: commonLitSurfaceInputsFeatures),
            new LayerListUIBlock(MaterialUIBlock.Expandable.MaterialReferences),
            new LayersUIBlock(),
            new EmissionUIBlock(MaterialUIBlock.Expandable.Emissive, features: emissionFeatures),
            new AdvancedOptionsUIBlock(MaterialUIBlock.Expandable.Advance),
        };

        protected override void OnMaterialGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            using (var changed = new EditorGUI.ChangeCheckScope())
            {
                uiBlocks.OnGUI(materialEditor, props);
                ApplyKeywordsAndPassesIfNeeded(changed.changed, uiBlocks.materials);
            }
        }

        // Material property name for Layered Lit keyword setup
        const string kLayerCount = "_LayerCount";
        const string kLayerInfluenceMaskMap = "_LayerInfluenceMaskMap";
        const string kVertexColorMode = "_VertexColorMode";
        const string kUVBlendMask = "_UVBlendMask";
        const string kkUseMainLayerInfluence = "_UseMainLayerInfluence";
        const string kUseHeightBasedBlend = "_UseHeightBasedBlend";
        const string kObjectScaleAffectTile = "_ObjectScaleAffectTile";

        // Density/opacity mode
        const string kOpacityAsDensity = "_OpacityAsDensity";

        const string kMaskMap = "_MaskMap";
        const string kNormalMap = "_NormalMap";
        const string kNormalMapOS = "_NormalMapOS";
        const string kBentNormalMap = "_BentNormalMap";
        const string kBentNormalMapOS = "_BentNormalMapOS";
        const string kNormalMapSpace = "_NormalMapSpace";

        const string kHeightMap = "_HeightMap";

        const string kSubsurfaceMaskMap = "_SubsurfaceMaskMap";
        const string kThicknessMap = "_ThicknessMap";

        const string kUVDetail = "_UVDetail";
        const string kDetailMap = "_DetailMap";

        const string kEmissiveColorMap = "_EmissiveColorMap";
        const string kUVEmissive = "_UVEmissive";

        const string kSpecularOcclusionMode = "_SpecularOcclusionMode";

        protected override void SetupMaterialKeywordsAndPassInternal(Material material) => SetupMaterialKeywordsAndPass(material);

        static public void SetupLayersMappingKeywords(Material material)
        {
            // object scale affect tile
            CoreUtils.SetKeyword(material, "_LAYER_TILING_COUPLED_WITH_UNIFORM_OBJECT_SCALE", material.GetFloat(kObjectScaleAffectTile) > 0.0f);

            // Blend mask
            UVBaseMapping UVBlendMaskMapping = (UVBaseMapping)material.GetFloat(kUVBlendMask);
            CoreUtils.SetKeyword(material, "_LAYER_MAPPING_PLANAR_BLENDMASK", UVBlendMaskMapping == UVBaseMapping.Planar);
            CoreUtils.SetKeyword(material, "_LAYER_MAPPING_TRIPLANAR_BLENDMASK",  UVBlendMaskMapping == UVBaseMapping.Triplanar);

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

        // All Setup Keyword functions must be static. It allow to create script to automatically update the shaders with a script if code change
        static public void SetupMaterialKeywordsAndPass(Material material)
        {
            BaseLitGUI.SetupBaseLitKeywords(material);
            BaseLitGUI.SetupBaseLitMaterialPass(material);
            SetupLayersMappingKeywords(material);
            bool receiveSSR = material.GetSurfaceType() == SurfaceType.Opaque ? (material.HasProperty(kReceivesSSR) ? material.GetInt(kReceivesSSR) != 0 : false)
                                    : (material.HasProperty(kReceivesSSRTransparent) ? material.GetInt(kReceivesSSRTransparent) != 0 : false);
            BaseLitGUI.SetupStencil(material, receiveSSR, material.GetMaterialId() == MaterialId.LitSSS);

            if (material.HasProperty(kAddPrecomputedVelocity))
            {
                CoreUtils.SetKeyword(material, "_ADD_PRECOMPUTED_VELOCITY", material.GetInt(kAddPrecomputedVelocity) != 0);
            }


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
                CoreUtils.SetKeyword(material, "_THICKNESSMAP" + i, material.GetTexture(kThicknessMap + i));
            }

            CoreUtils.SetKeyword(material, "_INFLUENCEMASK_MAP", material.GetTexture(kLayerInfluenceMaskMap) && material.GetFloat(kkUseMainLayerInfluence) != 0.0f);

            CoreUtils.SetKeyword(material, "_EMISSIVE_MAPPING_PLANAR", ((UVEmissiveMapping)material.GetFloat(kUVEmissive)) == UVEmissiveMapping.Planar && material.GetTexture(kEmissiveColorMap));
            CoreUtils.SetKeyword(material, "_EMISSIVE_MAPPING_TRIPLANAR", ((UVEmissiveMapping)material.GetFloat(kUVEmissive)) == UVEmissiveMapping.Triplanar && material.GetTexture(kEmissiveColorMap));
            CoreUtils.SetKeyword(material, "_EMISSIVE_MAPPING_BASE", ((UVEmissiveMapping)material.GetFloat(kUVEmissive)) == UVEmissiveMapping.SameAsBase && material.GetTexture(kEmissiveColorMap));
            CoreUtils.SetKeyword(material, "_EMISSIVE_COLOR_MAP", material.GetTexture(kEmissiveColorMap));

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

            MaterialId materialId = material.GetMaterialId();
            CoreUtils.SetKeyword(material, "_MATERIAL_FEATURE_SUBSURFACE_SCATTERING", materialId == MaterialId.LitSSS);
            CoreUtils.SetKeyword(material, "_MATERIAL_FEATURE_TRANSMISSION", materialId == MaterialId.LitTranslucent || (materialId == MaterialId.LitSSS && material.GetFloat(kTransmissionEnable) > 0.0f));
        }

        // This function is call by a script to help artists to have up to date material
        // that why it is static
        public static void SynchronizeAllLayers(Material material)
        {
            int layerCount = (int)material.GetFloat(kLayerCount);
            AssetImporter materialImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(material.GetInstanceID()));

            Material[] layers = null;

            // Material importer can be null when the selected material doesn't exists as asset (Material saved inside the scene)
            if (materialImporter != null)
                InitializeMaterialLayers(material, ref layers);

            // We could have no userData in the assets, so test if we have load something
            if (layers != null)
            {
                for (int i = 0; i < layerCount; ++i)
                {
                    SynchronizeLayerProperties(material, layers, i, true);
                }
            }
        }

        public static void SynchronizeAllLayersProperties(Material material, Material[] materialLayers, bool excludeUVMappingProperties)
        {
            int numLayer = material.GetLayerCount();

            for (int i = 0; i < numLayer; ++i)
            {
                SynchronizeLayerProperties(material, materialLayers, i, excludeUVMappingProperties);
            }
        }

        // This function will look for all referenced lit material, and assign value from Lit to layered lit layers.
        // This is based on the naming of the variables, i.E BaseColor will match BaseColor0, if a properties shouldn't be override
        // put the name in the exclusionList below
        public static void SynchronizeLayerProperties(Material material, Material[] layers, int layerIndex, bool excludeUVMappingProperties)
        {
            Material layerMaterial = layers[layerIndex];
            string[] exclusionList = { kTexWorldScale, kUVBase, kUVMappingMask, kUVDetail, kUVDetailsMappingMask };

            if (layerMaterial != null)
            {
                Shader layerShader = layerMaterial.shader;
                int propertyCount = ShaderUtil.GetPropertyCount(layerShader);
                for (int i = 0; i < propertyCount; ++i)
                {
                    string propertyName = ShaderUtil.GetPropertyName(layerShader, i);
                    string layerPropertyName = propertyName + layerIndex;

                    if (!exclusionList.Contains(propertyName) || !excludeUVMappingProperties)
                    {
                        if (material.HasProperty(layerPropertyName))
                        {
                            ShaderUtil.ShaderPropertyType type = ShaderUtil.GetPropertyType(layerShader, i);
                            switch (type)
                            {
                                case ShaderUtil.ShaderPropertyType.Color:
                                {
                                    material.SetColor(layerPropertyName, layerMaterial.GetColor(propertyName));
                                    break;
                                }
                                case ShaderUtil.ShaderPropertyType.Float:
                                case ShaderUtil.ShaderPropertyType.Range:
                                {
                                    material.SetFloat(layerPropertyName, layerMaterial.GetFloat(propertyName));
                                    break;
                                }
                                case ShaderUtil.ShaderPropertyType.Vector:
                                {
                                    material.SetVector(layerPropertyName, layerMaterial.GetVector(propertyName));
                                    break;
                                }
                                case ShaderUtil.ShaderPropertyType.TexEnv:
                                {
                                    material.SetTexture(layerPropertyName, layerMaterial.GetTexture(propertyName));
                                    if (!excludeUVMappingProperties)
                                    {
                                        material.SetTextureOffset(layerPropertyName, layerMaterial.GetTextureOffset(propertyName));
                                        material.SetTextureScale(layerPropertyName, layerMaterial.GetTextureScale(propertyName));
                                    }
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        // We use the user data to save a string that represent the referenced lit material
        // so we can keep reference during serialization
        public static void InitializeMaterialLayers(Material material, ref Material[] layers)
        {
            AssetImporter materialImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(material.GetInstanceID()));

            if (materialImporter.userData != string.Empty)
            {
                SerializeableGUIDs layersGUID = JsonUtility.FromJson<SerializeableGUIDs>(materialImporter.userData);
                if (layersGUID.GUIDArray.Length > 0)
                {
                    layers = new Material[layersGUID.GUIDArray.Length];
                    for (int i = 0; i < layersGUID.GUIDArray.Length; ++i)
                    {
                        layers[i] = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(layersGUID.GUIDArray[i]), typeof(Material)) as Material;
                    }
                }
            }
        }

        public static void SaveMaterialLayers(Material material, Material[] materialLayers)
        {
            AssetImporter materialImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(material.GetInstanceID()));

            SerializeableGUIDs layersGUID;
            layersGUID.GUIDArray = new string[materialLayers.Length];
            for (int i = 0; i < materialLayers.Length; ++i)
            {
                if (materialLayers[i] != null)
                    layersGUID.GUIDArray[i] = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(materialLayers[i].GetInstanceID()));
            }

            materialImporter.userData = JsonUtility.ToJson(layersGUID);
        }

    }
} // namespace UnityEditor
