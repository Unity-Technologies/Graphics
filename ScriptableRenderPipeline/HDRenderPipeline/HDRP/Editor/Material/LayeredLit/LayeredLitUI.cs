using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

using System.Linq;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    internal class LayeredLitGUI : LitGUI
    {
        public enum VertexColorMode
        {
            None,
            Multiply,
            Add
        }

        private class StylesLayer
        {
            public readonly Color[] layerColors =
            {
                Color.white,
                Color.red,
                Color.green,
                Color.blue
            };

            public readonly GUIContent[] layerLabels =
            {
                new GUIContent("Main layer"),
                new GUIContent("Layer 1"),
                new GUIContent("Layer 2"),
                new GUIContent("Layer 3"),
            };

            public readonly GUIStyle[] layerLabelColors =
            {
                new GUIStyle(EditorStyles.foldout),
                new GUIStyle(EditorStyles.foldout),
                new GUIStyle(EditorStyles.foldout),
                new GUIStyle(EditorStyles.foldout)
            };

            public readonly GUIContent syncAllButtonText = new GUIContent("Re-Synchronize", "Re-synchronize all layers material properties with the referenced Materials");
            public readonly GUIContent syncAllButUVButtonText = new GUIContent("Re-Synchronize Without UV Mapping", "Re-synchronize all but UV Mapping properties with the referenced Materials");
            public readonly GUIContent layersText = new GUIContent("Inputs");
            public readonly GUIContent emissiveText = new GUIContent("Emissive");
            public readonly GUIContent layerMapMaskText = new GUIContent("Layer Mask", "Layer mask");
            public readonly GUIContent layerInfluenceMapMaskText = new GUIContent("Layer Influence Mask", "Layer mask");
            public readonly GUIContent vertexColorModeText = new GUIContent("Vertex Color Mode", "Mode multiply: vertex color is multiply with the mask. Mode additive: vertex color values are remapped between -1 and 1 and added to the mask (neutral at 0.5 vertex color).");
            public readonly GUIContent layerCountText = new GUIContent("Layer Count", "Number of layers.");
            public readonly GUIContent objectScaleAffectTileText = new GUIContent("Lock layers 0123 tiling with object Scale", "Tiling of each layers will be affected by the object scale.");
            public readonly GUIContent objectScaleAffectTileText2 = new GUIContent("Lock layers  123 tiling with object Scale", "Tiling of each influenced layers (all except main layer) will be affected by the object scale.");

            public readonly GUIContent layerTexWorldScaleText = new GUIContent("World Scale", "Tiling factor applied to Planar/Trilinear mapping");
            public readonly GUIContent UVBlendMaskText = new GUIContent("BlendMask UV Mapping", "Base UV Mapping mode of the layer.");

            public readonly GUIContent layeringOptionText = new GUIContent("Layering Options");

            public readonly GUIContent useHeightBasedBlendText = new GUIContent("Use Height Based Blend", "Layer will be blended with the underlying layer based on the height.");
            public readonly GUIContent useMainLayerInfluenceModeText = new GUIContent("Main Layer Influence", "Switch between regular layers mode and base/layers mode");

            public readonly GUIContent opacityAsDensityText = new GUIContent("Use Opacity map as Density map", "Use opacity map as (alpha channel of base color) as Density map.");
            public readonly GUIContent inheritBaseNormalText = new GUIContent("Normal influence", "Inherit the normal from the base layer.");
            public readonly GUIContent inheritBaseHeightText = new GUIContent("Heightmap influence", "Inherit the height from the base layer.");
            public readonly GUIContent inheritBaseColorText = new GUIContent("BaseColor influence", "Inherit the base color from the base layer.");
            public readonly GUIContent heightOffset = new GUIContent("Height Offset", "Offset applied to the height before layering.");
            public readonly GUIContent heightTransition = new GUIContent("Height Transition", "Size in world units of the smooth transition between layers.");

            public readonly GUIContent perPixelDisplacementLayersWarning = new GUIContent("For pixel displacement to work correctly, all layers with a heightmap must use the same UV mapping");

            public readonly GUIContent materialReferencesText = new GUIContent("Material References");

            public StylesLayer()
            {
                layerLabelColors[0].normal.textColor = layerColors[0];
                layerLabelColors[1].normal.textColor = layerColors[1];
                layerLabelColors[2].normal.textColor = layerColors[2];
                layerLabelColors[3].normal.textColor = layerColors[3];
            }
        }

        static StylesLayer s_Styles = null;
        private static StylesLayer styles { get { if (s_Styles == null) s_Styles = new StylesLayer(); return s_Styles; } }

        // Needed for json serialization to work
        [Serializable]
        internal struct SerializeableGUIDs
        {
            public string[] GUIDArray;
        }

        const int kSyncButtonWidth = 58;

        public LayeredLitGUI()
        {
            m_LayerCount = 4;
            m_PropertySuffixes[0] = "0";
            m_PropertySuffixes[1] = "1";
            m_PropertySuffixes[2] = "2";
            m_PropertySuffixes[3] = "3";
        }

        Material[] m_MaterialLayers = new Material[kMaxLayerCount];

        // Layer options
        MaterialProperty layerCount = null;
        const string kLayerCount = "_LayerCount";
        MaterialProperty layerMaskMap = null;
        const string kLayerMaskMap = "_LayerMaskMap";
        MaterialProperty layerInfluenceMaskMap = null;
        const string kLayerInfluenceMaskMap = "_LayerInfluenceMaskMap";
        MaterialProperty vertexColorMode = null;
        const string kVertexColorMode = "_VertexColorMode";
        MaterialProperty objectScaleAffectTile = null;
        const string kObjectScaleAffectTile = "_ObjectScaleAffectTile";
        MaterialProperty UVBlendMask = null;
        const string kUVBlendMask = "_UVBlendMask";
        MaterialProperty UVMappingMaskBlendMask = null;
        const string kUVMappingMaskBlendMask = "_UVMappingMaskBlendMask";
        MaterialProperty texWorldScaleBlendMask = null;
        const string kTexWorldScaleBlendMask = "_TexWorldScaleBlendMask";
        MaterialProperty useMainLayerInfluence = null;
        const string kkUseMainLayerInfluence = "_UseMainLayerInfluence";
        MaterialProperty useHeightBasedBlend = null;
        const string kUseHeightBasedBlend = "_UseHeightBasedBlend";

        // Density/opacity mode
        MaterialProperty[] opacityAsDensity = new MaterialProperty[kMaxLayerCount];
        const string kOpacityAsDensity = "_OpacityAsDensity";

        // Influence
        MaterialProperty[] inheritBaseNormal = new MaterialProperty[kMaxLayerCount - 1];
        const string kInheritBaseNormal = "_InheritBaseNormal";
        MaterialProperty[] inheritBaseHeight = new MaterialProperty[kMaxLayerCount - 1];
        const string kInheritBaseHeight = "_InheritBaseHeight";
        MaterialProperty[] inheritBaseColor = new MaterialProperty[kMaxLayerCount - 1];
        const string kInheritBaseColor = "_InheritBaseColor";

        // Height blend
        MaterialProperty[] heightBlendOffset = new MaterialProperty[kMaxLayerCount];
        const string kHeightBlendOffset = "_HeightOffset";
        MaterialProperty heightTransition = null;
        const string kHeightTransition = "_HeightTransition";

        // UI
        MaterialProperty showMaterialReferences = null;
        const string kShowMaterialReferences = "_ShowMaterialReferences";
        MaterialProperty[] showLayer = new MaterialProperty[kMaxLayerCount];
        const string kShowLayer = "_ShowLayer";

        protected override void FindMaterialProperties(MaterialProperty[] props)
        {
            base.FindMaterialLayerProperties(props);
            base.FindMaterialEmissiveProperties(props);

            layerCount = FindProperty(kLayerCount, props);
            layerMaskMap = FindProperty(kLayerMaskMap, props);
            layerInfluenceMaskMap = FindProperty(kLayerInfluenceMaskMap, props);
            vertexColorMode = FindProperty(kVertexColorMode, props);
            objectScaleAffectTile = FindProperty(kObjectScaleAffectTile, props);
            UVBlendMask = FindProperty(kUVBlendMask, props);
            UVMappingMaskBlendMask = FindProperty(kUVMappingMaskBlendMask, props);
            texWorldScaleBlendMask = FindProperty(kTexWorldScaleBlendMask, props);

            useMainLayerInfluence = FindProperty(kkUseMainLayerInfluence, props);
            useHeightBasedBlend = FindProperty(kUseHeightBasedBlend, props);
            heightTransition = FindProperty(kHeightTransition, props);

            showMaterialReferences = FindProperty(kShowMaterialReferences, props);

            for (int i = 0; i < kMaxLayerCount; ++i)
            {
                // Density/opacity mode
                opacityAsDensity[i] = FindProperty(string.Format("{0}{1}", kOpacityAsDensity, i), props);
                heightBlendOffset[i] = FindProperty(string.Format("{0}{1}", kHeightBlendOffset, i), props);
                showLayer[i] = FindProperty(string.Format("{0}{1}", kShowLayer, i), props);

                if (i != 0)
                {
                    // Influence
                    inheritBaseNormal[i - 1] = FindProperty(string.Format("{0}{1}", kInheritBaseNormal, i), props);
                    inheritBaseHeight[i - 1] = FindProperty(string.Format("{0}{1}", kInheritBaseHeight, i), props);
                    inheritBaseColor[i - 1] = FindProperty(string.Format("{0}{1}", kInheritBaseColor, i), props);
                }
            }
        }

        int numLayer
        {
            set { layerCount.floatValue = (float)value; }
            get { return (int)layerCount.floatValue; }
        }

        // This function is call by a script to help artists to ahve up to date material
        // that why it is static
        public static void SynchronizeAllLayers(Material material)
        {
            int layerCount = (int)material.GetFloat(kLayerCount);
            AssetImporter materialImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(material.GetInstanceID()));

            Material[] layers = null;
            InitializeMaterialLayers(materialImporter, ref layers);

            // We could have no userData in the assets, so test if we have load something
            if (layers != null)
            {
                for (int i = 0; i < layerCount; ++i)
                {
                    SynchronizeLayerProperties(material, layers, i, true);
                }
            }
        }

        void SynchronizeAllLayersProperties(bool excludeUVMappingProperties)
        {
            for (int i = 0; i < numLayer; ++i)
            {
                SynchronizeLayerProperties(m_MaterialEditor.target as Material, m_MaterialLayers, i, excludeUVMappingProperties);
            }
        }

        // This function will look for all referenced lit material, and assign value from Lit to layered lit layers.
        // This is based on the naming of the variables, i.E BaseColor will match BaseColor0, if a properties shouldn't be override
        // put the name in the exclusionList below
        static void SynchronizeLayerProperties(Material material, Material[] layers, int layerIndex, bool excludeUVMappingProperties)
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
                                    if(!excludeUVMappingProperties)
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
        static void InitializeMaterialLayers(AssetImporter materialImporter, ref Material[] layers)
        {
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

        void SaveMaterialLayers(AssetImporter materialImporter)
        {
            SerializeableGUIDs layersGUID;
            layersGUID.GUIDArray = new string[m_MaterialLayers.Length];
            for (int i = 0; i < m_MaterialLayers.Length; ++i)
            {
                if (m_MaterialLayers[i] != null)
                    layersGUID.GUIDArray[i] = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(m_MaterialLayers[i].GetInstanceID()));
            }

            materialImporter.userData = JsonUtility.ToJson(layersGUID);
        }

        bool DoLayerGUI(AssetImporter materialImporter, int layerIndex)
        {
            bool result = false;

            showLayer[layerIndex].floatValue = EditorGUILayout.Foldout(showLayer[layerIndex].floatValue != 0.0f, styles.layerLabels[layerIndex], styles.layerLabelColors[layerIndex]) ? 1.0f : 0.0f;
            if (showLayer[layerIndex].floatValue == 0.0f)
                return false;
            ;

            Material material = m_MaterialEditor.target as Material;

            bool mainLayerInfluenceEnable = useMainLayerInfluence.floatValue > 0.0f;
            bool heightBasedBlend = useHeightBasedBlend.floatValue > 0.0f;

            EditorGUILayout.LabelField(styles.layeringOptionText, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            // Main layer does not have any options but height base blend.
            if (layerIndex > 0)
            {
                int paramIndex = layerIndex - 1;

                m_MaterialEditor.ShaderProperty(opacityAsDensity[layerIndex], styles.opacityAsDensityText);

                if (mainLayerInfluenceEnable)
                {
                    m_MaterialEditor.ShaderProperty(inheritBaseColor[paramIndex], styles.inheritBaseColorText);
                    m_MaterialEditor.ShaderProperty(inheritBaseNormal[paramIndex], styles.inheritBaseNormalText);
                    // Main height influence is only available if the shader use the heightmap for displacement (per vertex or per level)
                    // We always display it as it can be tricky to know when per pixel displacement is enabled or not
                    m_MaterialEditor.ShaderProperty(inheritBaseHeight[paramIndex], styles.inheritBaseHeightText);
                }
            }
            else
            {
                if (!useMainLayerInfluence.hasMixedValue && useMainLayerInfluence.floatValue != 0.0f)
                {
                    m_MaterialEditor.TexturePropertySingleLine(styles.layerInfluenceMapMaskText, layerInfluenceMaskMap);
                }
            }

            if (heightBasedBlend)
            {
                m_MaterialEditor.ShaderProperty(heightBlendOffset[layerIndex], styles.heightOffset);
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();

            DoLayerGUI(material, layerIndex);

            if (layerIndex == 0)
                EditorGUILayout.Space();

            return result;
        }


        void DoLayeringInputGUI()
        {
            EditorGUI.indentLevel++;
            GUILayout.Label(styles.layersText, EditorStyles.boldLabel);

            EditorGUI.showMixedValue = layerCount.hasMixedValue;
            EditorGUI.BeginChangeCheck();
            int newLayerCount = EditorGUILayout.IntSlider(styles.layerCountText, (int)layerCount.floatValue, 2, 4);
            if (EditorGUI.EndChangeCheck())
            {
                Material material = m_MaterialEditor.target as Material;
                Undo.RecordObject(material, "Change layer count");
                layerCount.floatValue = (float)newLayerCount;
            }

            m_MaterialEditor.TexturePropertySingleLine(styles.layerMapMaskText, layerMaskMap);

            EditorGUI.indentLevel++;
            m_MaterialEditor.ShaderProperty(UVBlendMask, styles.UVBlendMaskText);
            UVBaseMapping uvBlendMask = (UVBaseMapping)UVBlendMask.floatValue;

            float X, Y, Z, W;
            X = (uvBlendMask == UVBaseMapping.UV0) ? 1.0f : 0.0f;
            Y = (uvBlendMask == UVBaseMapping.UV1) ? 1.0f : 0.0f;
            Z = (uvBlendMask == UVBaseMapping.UV2) ? 1.0f : 0.0f;
            W = (uvBlendMask == UVBaseMapping.UV3) ? 1.0f : 0.0f;

            UVMappingMaskBlendMask.colorValue = new Color(X, Y, Z, W);

            if (((UVBaseMapping)UVBlendMask.floatValue == UVBaseMapping.Planar) ||
                ((UVBaseMapping)UVBlendMask.floatValue == UVBaseMapping.Triplanar))
            {
                m_MaterialEditor.ShaderProperty(texWorldScaleBlendMask, styles.layerTexWorldScaleText);
            }
            m_MaterialEditor.TextureScaleOffsetProperty(layerMaskMap);
            EditorGUI.indentLevel--;

            m_MaterialEditor.ShaderProperty(vertexColorMode, styles.vertexColorModeText);

            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = useMainLayerInfluence.hasMixedValue;
            bool mainLayerModeInfluenceEnable = EditorGUILayout.Toggle(styles.useMainLayerInfluenceModeText, useMainLayerInfluence.floatValue > 0.0f);
            if (EditorGUI.EndChangeCheck())
            {
                useMainLayerInfluence.floatValue = mainLayerModeInfluenceEnable ? 1.0f : 0.0f;
            }

            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = useHeightBasedBlend.hasMixedValue;
            bool enabled = EditorGUILayout.Toggle(styles.useHeightBasedBlendText, useHeightBasedBlend.floatValue > 0.0f);
            if (EditorGUI.EndChangeCheck())
            {
                useHeightBasedBlend.floatValue = enabled ? 1.0f : 0.0f;
            }

            if (enabled)
            {
                EditorGUI.indentLevel++;
                m_MaterialEditor.ShaderProperty(heightTransition, styles.heightTransition);
                EditorGUI.indentLevel--;
            }

            m_MaterialEditor.ShaderProperty(objectScaleAffectTile, mainLayerModeInfluenceEnable ? styles.objectScaleAffectTileText2 : styles.objectScaleAffectTileText);
            EditorGUI.indentLevel--;
        }

        bool DoMaterialReferencesGUI(AssetImporter materialImporter)
        {
            showMaterialReferences.floatValue = EditorGUILayout.Foldout(showMaterialReferences.floatValue != 0.0f, styles.materialReferencesText, styles.layerLabelColors[0]) ? 1.0f : 0.0f;
            if (showMaterialReferences.floatValue == 0.0f)
                return false;

            bool layersChanged = false;
            Material material = m_MaterialEditor.target as Material;

            Color originalContentColor = GUI.contentColor;

            for (int layerIndex = 0; layerIndex < numLayer; ++layerIndex)
            {
                EditorGUI.BeginChangeCheck();
                GUI.contentColor = styles.layerColors[layerIndex];

                m_MaterialLayers[layerIndex] = EditorGUILayout.ObjectField(styles.layerLabels[layerIndex], m_MaterialLayers[layerIndex], typeof(Material), true) as Material;
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(materialImporter, "Change layer material");
                    SynchronizeLayerProperties(material, m_MaterialLayers, layerIndex, true);
                    layersChanged = true;
                }

                GUI.contentColor = originalContentColor;

                GUILayout.BeginHorizontal();
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(styles.syncAllButUVButtonText))
                    {
                        SynchronizeLayerProperties(material, m_MaterialLayers, layerIndex, true);
                        layersChanged = true;
                    }
                    if (GUILayout.Button(styles.syncAllButtonText))
                    {
                        SynchronizeLayerProperties(material, m_MaterialLayers, layerIndex, false);
                        layersChanged = true;
                    }
                }
                GUILayout.EndHorizontal();
            }

            return layersChanged;
        }

        bool DoLayersGUI(AssetImporter materialImporter)
        {
            bool layerChanged = false;

            GUI.changed = false;

            DoLayeringInputGUI();

            EditorGUILayout.Space();

            layerChanged |= DoMaterialReferencesGUI(materialImporter);

            EditorGUILayout.Space();

            for (int i = 0; i < numLayer; i++)
            {
                layerChanged |= DoLayerGUI(materialImporter, i);
            }

            EditorGUILayout.Space();

            layerChanged |= GUI.changed;
            GUI.changed = false;

            return layerChanged;
        }

        protected override bool ShouldEmissionBeEnabled(Material mat)
        {
            return mat.GetFloat(kEmissiveIntensity) > 0.0f;
        }

        protected override void SetupMaterialKeywordsAndPassInternal(Material material)
        {
            SetupMaterialKeywordsAndPass(material);
        }

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
        static new public void SetupMaterialKeywordsAndPass(Material material)
        {
            SetupBaseLitKeywords(material);
            SetupBaseLitMaterialPass(material);
            SetupLayersMappingKeywords(material);

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

            CoreUtils.SetKeyword(material, "_EMISSIVE_MAPPING_PLANAR", ((UVBaseMapping)material.GetFloat(kUVEmissive)) == UVBaseMapping.Planar && material.GetTexture(kEmissiveColorMap));
            CoreUtils.SetKeyword(material, "_EMISSIVE_MAPPING_TRIPLANAR", ((UVBaseMapping)material.GetFloat(kUVEmissive)) == UVBaseMapping.Triplanar && material.GetTexture(kEmissiveColorMap));
            CoreUtils.SetKeyword(material, "_EMISSIVE_COLOR_MAP", material.GetTexture(kEmissiveColorMap));
            CoreUtils.SetKeyword(material, "_ENABLESPECULAROCCLUSION", material.GetFloat(kEnableSpecularOcclusion) > 0.0f);

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
            for (int i = 0; i < material.GetInt(kLayerCount); ++i )
            {
                useDensityModeEnable |= material.GetFloat(kOpacityAsDensity + i) != 0.0f;
            }
            CoreUtils.SetKeyword(material, "_DENSITY_MODE", useDensityModeEnable);

            BaseLitGUI.MaterialId materialId = (BaseLitGUI.MaterialId)material.GetFloat(kMaterialID);

            CoreUtils.SetKeyword(material, "_MATERIAL_FEATURE_SUBSURFACE_SCATTERING", materialId == BaseLitGUI.MaterialId.LitSSS);
            CoreUtils.SetKeyword(material, "_MATERIAL_FEATURE_TRANSMISSION", materialId == BaseLitGUI.MaterialId.LitSSS);
        }

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            FindBaseMaterialProperties(props);
            FindMaterialProperties(props);

            m_MaterialEditor = materialEditor;
            // We should always do this call at the beginning
            m_MaterialEditor.serializedObject.Update();

            Material material = m_MaterialEditor.target as Material;
            AssetImporter materialImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(material.GetInstanceID()));

            InitializeMaterialLayers(materialImporter, ref m_MaterialLayers);

            bool optionsChanged = false;
            EditorGUI.BeginChangeCheck();
            {
                BaseMaterialPropertiesGUI();
                EditorGUILayout.Space();

                VertexAnimationPropertiesGUI();
                EditorGUILayout.Space();
            }
            if (EditorGUI.EndChangeCheck())
            {
                optionsChanged = true;
            }

            // In case of pixel displacement and layered shader, all layers must used the same texture mapping for layer that have a heightmap
            // (Else the algorithm will not work correctly)
            if ((DisplacementMode)displacementMode.floatValue == DisplacementMode.Pixel)
            {
                float compareValue = -1.0f;
                bool match = true;

                if (material.GetTexture(kHeightMap + 0))
                {
                    compareValue = UVBase[0].floatValue;
                }
                if (material.GetTexture(kHeightMap + 1))
                {
                    if (compareValue == -1.0f)
                        compareValue = UVBase[1].floatValue;
                    else if (compareValue != UVBase[1].floatValue)
                        match = false;
                }
                if (material.GetTexture(kHeightMap + 2))
                {
                    if (compareValue == -1.0f)
                        compareValue = UVBase[2].floatValue;
                    else if (compareValue != UVBase[2].floatValue)
                        match = false;
                }
                if (material.GetTexture(kHeightMap + 3))
                {
                    if (compareValue == -1.0f)
                        compareValue = UVBase[3].floatValue;
                    else if (compareValue != UVBase[3].floatValue)
                        match = false;
                }

                if (!match)
                {
                    EditorGUILayout.HelpBox(styles.perPixelDisplacementLayersWarning.text, MessageType.Warning);
                }
            }


            bool layerChanged = DoLayersGUI(materialImporter);
            EditorGUI.BeginChangeCheck();
            {
                DoEmissiveGUI(material);
            }
            if (EditorGUI.EndChangeCheck())
            {
                optionsChanged = true;
            }

            DoEmissionArea(material);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(StylesBaseUnlit.advancedText, EditorStyles.boldLabel);
            // NB RenderQueue editor is not shown on purpose: we want to override it based on blend mode
            EditorGUI.indentLevel++;
            m_MaterialEditor.EnableInstancingField();
            m_MaterialEditor.ShaderProperty(enableSpecularOcclusion, Styles.enableSpecularOcclusionText);
            EditorGUI.indentLevel--;

            if (layerChanged || optionsChanged)
            {
                foreach (var obj in m_MaterialEditor.targets)
                {
                    SetupMaterialKeywordsAndPassInternal((Material)obj);
                }

                // SaveAssetsProcessor the referenced material in the users data
                SaveMaterialLayers(materialImporter);
            }

            // We should always do this call at the end
            m_MaterialEditor.serializedObject.ApplyModifiedProperties();
        }
    }
} // namespace UnityEditor
