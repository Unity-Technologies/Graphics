using System;
using UnityEngine;
using UnityEngine.Rendering;

using System.Linq;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    internal class LayeredLitGUI : LitGUI
    {
        public enum LayerUVBaseMapping
        {
            UV0,
            UV1,
            UV2,
            UV3,
            Planar,
            Triplanar,
        }

        public enum VertexColorMode
        {
            None,
            Multiply,
            Add
        }

        private class StylesLayer
        {
            public readonly GUIContent[] layerLabels =
            {
                new GUIContent("Main layer"),
                new GUIContent("Layer 1"),
                new GUIContent("Layer 2"),
                new GUIContent("Layer 3"),
            };

            public readonly GUIStyle[] layerLabelColors =
            {
                new GUIStyle(EditorStyles.label),
                new GUIStyle(EditorStyles.label),
                new GUIStyle(EditorStyles.label),
                new GUIStyle(EditorStyles.label)
            };

            public readonly GUIContent materialLayerText = new GUIContent("Material");
            public readonly GUIContent syncButtonText = new GUIContent("Re-Synchronize Layers", "Re-synchronize all layers's properties with the referenced Material");
            public readonly GUIContent layersText = new GUIContent("Inputs");
            public readonly GUIContent emissiveText = new GUIContent("Emissive");
            public readonly GUIContent layerMapMaskText = new GUIContent("Layer Mask", "Layer mask");
            public readonly GUIContent vertexColorModeText = new GUIContent("Vertex Color Mode", "Mode multiply: vertex color is multiply with the mask. Mode additive: vertex color values are remapped between -1 and 1 and added to the mask (neutral at 0.5 vertex color).");
            public readonly GUIContent layerCountText = new GUIContent("Layer Count", "Number of layers.");
            public readonly GUIContent layerTilingBlendMaskText = new GUIContent("Tiling", "Tiling for the blend mask.");
            public readonly GUIContent objectScaleAffectTileText = new GUIContent("Tiling 0123 follow object Scale", "Tiling will be affected by the object scale.");
            public readonly GUIContent objectScaleAffectTileText2 = new GUIContent("Tiling 123 follow object Scale", "Tiling will be affected by the object scale.");

            public readonly GUIContent layerTilingText = new GUIContent("Tiling", "Tiling factor applied to UVSet");
            public readonly GUIContent layerTexWorldScaleText = new GUIContent("World Scale", "Tiling factor applied to Planar/Trilinear mapping");
            public readonly GUIContent UVBaseText = new GUIContent("Base UV Mapping", "Base UV Mapping mode of the layer.");
            public readonly GUIContent UVBlendMaskText = new GUIContent("BlendMask UV Mapping", "Base UV Mapping mode of the layer.");
            public readonly GUIContent UVDetailText = new GUIContent("Detail UV Mapping", "Detail UV Mapping mode of the layer.");
            public readonly GUIContent mainLayerInfluenceText = new GUIContent("Main layer influence", "Main layer influence.");
            public readonly GUIContent densityOpacityInfluenceText = new GUIContent("Density / Opacity", "Density / Opacity");
            public readonly GUIContent useHeightBasedBlendText = new GUIContent("Use Height Based Blend", "Layer will be blended with the underlying layer based on the height.");
            public readonly GUIContent useDensityModeModeText = new GUIContent("Use Density Mode", "Enable density mode");
            public readonly GUIContent useMainLayerInfluenceModeText = new GUIContent("Main Layer Influence", "Switch between regular layers mode and base/layers mode");
            public readonly GUIContent heightFactorText = new GUIContent("Height Multiplier", "Scale applied to the height of the layer.");
            public readonly GUIContent heightControlText = new GUIContent("Height control");

            public readonly GUIContent heightCenterOffsetText = new GUIContent("Height Center Offset", "Offset applied to the center of the height of the layer.");
            public readonly GUIContent blendUsingHeight = new GUIContent("Blend Using Height", "Blend Layers using height.");
            public readonly GUIContent inheritBaseColorThresholdText = new GUIContent("Threshold", "Inherit the base color from the base layer.");
            public readonly GUIContent minimumOpacityText = new GUIContent("Minimum Opacity", "Minimum Opacity.");
            public readonly GUIContent opacityAsDensityText = new GUIContent("Use Opacity as Density", "Use Opacity as Density.");
            public readonly GUIContent inheritBaseNormalText = new GUIContent("Normal influence", "Inherit the normal from the base layer.");
            public readonly GUIContent inheritBaseHeightText = new GUIContent("Heightmap influence", "Inherit the height from the base layer.");
            public readonly GUIContent inheritBaseColorText = new GUIContent("BaseColor influence", "Inherit the base color from the base layer.");
            public StylesLayer()
            {
                layerLabelColors[0].normal.textColor = Color.white;
                layerLabelColors[1].normal.textColor = Color.red;
                layerLabelColors[2].normal.textColor = Color.green;
                layerLabelColors[3].normal.textColor = Color.blue;
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

        const int kMaxLayerCount = 4;
        const int kSyncButtonWidth = 58;

        Material[] m_MaterialLayers = new Material[kMaxLayerCount];

        // Layer options
        MaterialProperty layerCount = null;
        const string kLayerCount = "_LayerCount";
        MaterialProperty layerMaskMap = null;
        const string kLayerMaskMap = "_LayerMaskMap";
        MaterialProperty vertexColorMode = null;
        const string kVertexColorMode = "_VertexColorMode";
        MaterialProperty objectScaleAffectTile = null;
        const string kObjectScaleAffectTile = "_ObjectScaleAffectTile";
        MaterialProperty UVBlendMask = null;
        const string kUVBlendMask = "_UVBlendMask";
        MaterialProperty layerTilingBlendMask = null;
        const string kLayerTilingBlendMask = "_LayerTilingBlendMask";
        MaterialProperty texWorldScaleBlendMask = null;
        const string kTexWorldScaleBlendMask = "_TexWorldScaleBlendMask";
        MaterialProperty useMainLayerInfluence = null;
        const string kkUseMainLayerInfluence = "_UseMainLayerInfluence";
        MaterialProperty useHeightBasedBlend = null;
        const string kUseHeightBasedBlend = "_UseHeightBasedBlend";


        // Properties for multiple layers inherit from referenced lit materials
        MaterialProperty[] layerTexWorldScale = new MaterialProperty[kMaxLayerCount];
        MaterialProperty[] layerUVBase = new MaterialProperty[kMaxLayerCount];
        MaterialProperty[] layerUVMappingMask = new MaterialProperty[kMaxLayerCount];
        MaterialProperty[] layerUVDetail = new MaterialProperty[kMaxLayerCount];
        MaterialProperty[] layerUVDetailsMappingMask = new MaterialProperty[kMaxLayerCount];
        // This one is specific to layer lit
        MaterialProperty[] layerTiling = new MaterialProperty[kMaxLayerCount];
        const string kLayerTiling = "_LayerTiling";

        // Density/opacity mode
        MaterialProperty useDensityMode = null;
        const string kUseDensityMode = "_UseDensityMode";
        MaterialProperty[] opacityAsDensity = new MaterialProperty[kMaxLayerCount];
        const string kOpacityAsDensity = "_OpacityAsDensity";
        MaterialProperty[] minimumOpacity = new MaterialProperty[kMaxLayerCount];
        const string kMinimumOpacity = "_MinimumOpacity";

        // HeightmapMode control
        MaterialProperty[] heightFactor = new MaterialProperty[kMaxLayerCount];
        const string kHeightFactor = "_HeightFactor";
        MaterialProperty[] heightCenterOffset = new MaterialProperty[kMaxLayerCount];
        const string kHeightCenterOffset = "_HeightCenterOffset";
        MaterialProperty[] layerHeightAmplitude = new MaterialProperty[kMaxLayerCount];
        const string kLayerHeightAmplitude = "_LayerHeightAmplitude";
        MaterialProperty[] layerCenterOffset = new MaterialProperty[kMaxLayerCount];
        const string kLayerCenterOffset = "_LayerCenterOffset";
        MaterialProperty[] blendUsingHeight = new MaterialProperty[kMaxLayerCount - 1]; // Only in case of influence mode
        const string kBlendUsingHeight = "_BlendUsingHeight";

        // Influence
        MaterialProperty[] inheritBaseNormal = new MaterialProperty[kMaxLayerCount - 1];
        const string kInheritBaseNormal = "_InheritBaseNormal";
        MaterialProperty[] inheritBaseHeight = new MaterialProperty[kMaxLayerCount - 1];
        const string kInheritBaseHeight = "_InheritBaseHeight";
        MaterialProperty[] inheritBaseColor = new MaterialProperty[kMaxLayerCount - 1];
        const string kInheritBaseColor = "_InheritBaseColor";
        MaterialProperty[] inheritBaseColorThreshold = new MaterialProperty[kMaxLayerCount - 1];
        const string kInheritBaseColorThreshold = "_InheritBaseColorThreshold";

        protected override void FindMaterialProperties(MaterialProperty[] props)
        {
            // Inherit from LitUI
            horizonFade = FindProperty(kHorizonFade, props);

            layerCount = FindProperty(kLayerCount, props);
            layerMaskMap = FindProperty(kLayerMaskMap, props);
            vertexColorMode = FindProperty(kVertexColorMode, props);
            objectScaleAffectTile = FindProperty(kObjectScaleAffectTile, props);
            UVBlendMask = FindProperty(kUVBlendMask, props);
            layerTilingBlendMask = FindProperty(kLayerTilingBlendMask, props);
            texWorldScaleBlendMask = FindProperty(kTexWorldScaleBlendMask, props);

            useMainLayerInfluence = FindProperty(kkUseMainLayerInfluence, props);
            useHeightBasedBlend = FindProperty(kUseHeightBasedBlend, props);

            useDensityMode = FindProperty(kUseDensityMode, props);

            for (int i = 0; i < kMaxLayerCount; ++i)
            {
                layerTexWorldScale[i] = FindProperty(string.Format("{0}{1}", kTexWorldScale, i), props);
                layerUVBase[i] = FindProperty(string.Format("{0}{1}", kUVBase, i), props);
                layerUVMappingMask[i] = FindProperty(string.Format("{0}{1}", kUVMappingMask, i), props);
                layerUVDetail[i] = FindProperty(string.Format("{0}{1}", kUVDetail, i), props);
                layerUVDetailsMappingMask[i] = FindProperty(string.Format("{0}{1}", kUVDetailsMappingMask, i), props);
                layerTiling[i] = FindProperty(string.Format("{0}{1}", kLayerTiling, i), props);

                // Density/opacity mode
                opacityAsDensity[i] = FindProperty(string.Format("{0}{1}", kOpacityAsDensity, i), props);
                minimumOpacity[i] = FindProperty(string.Format("{0}{1}", kMinimumOpacity, i), props);

                // HeightmapMode control
                heightFactor[i] = FindProperty(string.Format("{0}{1}", kHeightFactor, i), props);
                heightCenterOffset[i] = FindProperty(string.Format("{0}{1}", kHeightCenterOffset, i), props);
                layerHeightAmplitude[i] = FindProperty(string.Format("{0}{1}", kLayerHeightAmplitude, i), props);
                layerCenterOffset[i] = FindProperty(string.Format("{0}{1}", kLayerCenterOffset, i), props);

                if (i != 0)
                {
                    blendUsingHeight[i - 1] = FindProperty(string.Format("{0}{1}", kBlendUsingHeight, i), props);
                    // Influence
                    inheritBaseNormal[i - 1] = FindProperty(string.Format("{0}{1}", kInheritBaseNormal, i), props);
                    inheritBaseHeight[i - 1] = FindProperty(string.Format("{0}{1}", kInheritBaseHeight, i), props);
                    inheritBaseColor[i - 1] = FindProperty(string.Format("{0}{1}", kInheritBaseColor, i), props);
                    inheritBaseColorThreshold[i - 1] = FindProperty(string.Format("{0}{1}", kInheritBaseColorThreshold, i), props);
                }
            }

            // Reuse property from LitUI.cs
            emissiveColor = FindProperty(kEmissiveColor, props);
            emissiveColorMap = FindProperty(kEmissiveColorMap, props);
            emissiveIntensity = FindProperty(kEmissiveIntensity, props);
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
                    SynchronizeLayerProperties(material, layers, i);
                }
            }
        }

        void SynchronizeAllLayersProperties()
        {
            for (int i = 0; i < numLayer; ++i)
            {
                SynchronizeLayerProperties(m_MaterialEditor.target as Material, m_MaterialLayers, i);
            }
        }

        // This function will look for all referenced lit material, and assign value from Lit to layered lit layers.
        // This is based on the naming of the variables, i.E BaseColor will match BaseColor0, if a properties shouldn't be override
        // put the name in the exclusionList below
        static void SynchronizeLayerProperties(Material material, Material[] layers, int layerIndex)
        {
            string[] exclusionList = { kTexWorldScale, kUVBase, kUVMappingMask, kUVDetail, kUVDetailsMappingMask };

            Material layerMaterial = layers[layerIndex];

            if (layerMaterial != null)
            {
                Shader layerShader = layerMaterial.shader;
                int propertyCount = ShaderUtil.GetPropertyCount(layerShader);
                for (int i = 0; i < propertyCount; ++i)
                {
                    string propertyName = ShaderUtil.GetPropertyName(layerShader, i);
                    string layerPropertyName = propertyName + layerIndex;

                    if (!exclusionList.Contains(propertyName))
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
                                    material.SetTextureOffset(layerPropertyName, layerMaterial.GetTextureOffset(propertyName));
                                    material.SetTextureScale(layerPropertyName, layerMaterial.GetTextureScale(propertyName));
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

            Material material = m_MaterialEditor.target as Material;

            bool mainLayerInfluenceEnable = useMainLayerInfluence.floatValue > 0.0f;

            EditorGUILayout.LabelField(styles.layerLabels[layerIndex], styles.layerLabelColors[layerIndex]);

            EditorGUI.indentLevel++;

            EditorGUI.BeginChangeCheck();
            m_MaterialLayers[layerIndex] = EditorGUILayout.ObjectField(styles.materialLayerText, m_MaterialLayers[layerIndex], typeof(Material), true) as Material;
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(materialImporter, "Change layer material");
                SynchronizeLayerProperties(material, m_MaterialLayers, layerIndex);
                result = true;
            }

            EditorGUI.BeginChangeCheck();
            m_MaterialEditor.ShaderProperty(layerUVBase[layerIndex], styles.UVBaseText);
            if (EditorGUI.EndChangeCheck())
            {
                SynchronizeLayerProperties(material, m_MaterialLayers, layerIndex);
                result = true;
            }

            if (((LayerUVBaseMapping)layerUVBase[layerIndex].floatValue == LayerUVBaseMapping.Planar) ||
                ((LayerUVBaseMapping)layerUVBase[layerIndex].floatValue == LayerUVBaseMapping.Triplanar))
            {
                EditorGUI.indentLevel++;
                m_MaterialEditor.ShaderProperty(layerTexWorldScale[layerIndex], styles.layerTexWorldScaleText);
                EditorGUI.indentLevel--;

                if (((LayerUVBaseMapping)layerUVBase[layerIndex].floatValue == LayerUVBaseMapping.Planar))
                    GUILayout.Label("       " + styles.UVDetailText.text + ": Planar");
                else
                    GUILayout.Label("       " + styles.UVDetailText.text + ": Triplanar");
            }
            else
            {
                EditorGUI.indentLevel++;
                m_MaterialEditor.ShaderProperty(layerTiling[layerIndex], styles.layerTilingText);
                EditorGUI.indentLevel--;

                EditorGUI.BeginChangeCheck();
                m_MaterialEditor.ShaderProperty(layerUVDetail[layerIndex], styles.UVDetailText);
                if (EditorGUI.EndChangeCheck())
                {
                    SynchronizeLayerProperties(material, m_MaterialLayers, layerIndex);
                    result = true;
                }
            }

            // We setup the masking map based on the enum for each layer.
            // using mapping mask allow to reduce the number of generated combination for a very small increase in ALU
            LayerUVBaseMapping layerUVBaseMapping = (LayerUVBaseMapping)layerUVBase[layerIndex].floatValue;

            float X, Y, Z, W;
            X = (layerUVBaseMapping == LayerUVBaseMapping.UV0) ? 1.0f : 0.0f;
            Y = (layerUVBaseMapping == LayerUVBaseMapping.UV1) ? 1.0f : 0.0f;
            Z = (layerUVBaseMapping == LayerUVBaseMapping.UV2) ? 1.0f : 0.0f;
            W = (layerUVBaseMapping == LayerUVBaseMapping.UV3) ? 1.0f : 0.0f;
            layerUVMappingMask[layerIndex].colorValue = (layerIndex == 0) ? new Color(1.0f, 0.0f, 0.0f, 0.0f) : new Color(X, Y, Z, W); // Special case for Main Layer and Blend Mask, only UV0. As Layer0 is share by both here, need to force X to 1.0 in all case

            UVDetailMapping layerUVDetailMapping = (UVDetailMapping)layerUVDetail[layerIndex].floatValue;
            X = (layerUVDetailMapping == UVDetailMapping.UV0) ? 1.0f : 0.0f;
            Y = (layerUVDetailMapping == UVDetailMapping.UV1) ? 1.0f : 0.0f;
            Z = (layerUVDetailMapping == UVDetailMapping.UV2) ? 1.0f : 0.0f;
            W = (layerUVDetailMapping == UVDetailMapping.UV3) ? 1.0f : 0.0f;
            layerUVDetailsMappingMask[layerIndex].colorValue = new Color(X, Y, Z, W);

            bool useDensityModeEnable = useDensityMode.floatValue != 0.0f;
            if (useDensityModeEnable)
            {
                EditorGUILayout.LabelField(styles.densityOpacityInfluenceText, EditorStyles.boldLabel);

                EditorGUI.indentLevel++;
                m_MaterialEditor.ShaderProperty(opacityAsDensity[layerIndex], styles.opacityAsDensityText);
                m_MaterialEditor.ShaderProperty(minimumOpacity[layerIndex], styles.minimumOpacityText);
                EditorGUI.indentLevel--;
            }

            // Display height control if they have a meaning
            if ((tessellationMode != null && ((TessellationMode)tessellationMode.floatValue == TessellationMode.Displacement || (TessellationMode)tessellationMode.floatValue == TessellationMode.DisplacementPhong))
                || (enablePerPixelDisplacement.floatValue > 0.0f)
                || (useHeightBasedBlend.floatValue > 0.0f)
                )
            {
                EditorGUILayout.LabelField(styles.heightControlText, EditorStyles.boldLabel);

                EditorGUI.indentLevel++;
                m_MaterialEditor.ShaderProperty(heightFactor[layerIndex], styles.heightFactorText);
                layerHeightAmplitude[layerIndex].floatValue = material.GetFloat(kHeightAmplitude + layerIndex) * heightFactor[layerIndex].floatValue;
                m_MaterialEditor.ShaderProperty(heightCenterOffset[layerIndex], styles.heightCenterOffsetText);
                layerCenterOffset[layerIndex].floatValue = material.GetFloat(kHeightCenter + layerIndex) + heightCenterOffset[layerIndex].floatValue;
                EditorGUI.indentLevel--;
            }


            // influence
            if (layerIndex > 0)
            {
                int paramIndex = layerIndex - 1;

                bool heightBasedBlendEnable = useHeightBasedBlend.floatValue > 0.0f;
                if (heightBasedBlendEnable)
                {
                    EditorGUI.indentLevel++;
                    m_MaterialEditor.ShaderProperty(blendUsingHeight[paramIndex], styles.blendUsingHeight);
                    EditorGUI.indentLevel--;
                }

                if (mainLayerInfluenceEnable)
                {
                    EditorGUILayout.LabelField(styles.mainLayerInfluenceText, EditorStyles.boldLabel);

                    EditorGUI.indentLevel++;

                    m_MaterialEditor.ShaderProperty(inheritBaseColor[paramIndex], styles.inheritBaseColorText);
                    EditorGUI.indentLevel++;
                    m_MaterialEditor.ShaderProperty(inheritBaseColorThreshold[paramIndex], styles.inheritBaseColorThresholdText);
                    EditorGUI.indentLevel--;
                    m_MaterialEditor.ShaderProperty(inheritBaseNormal[paramIndex], styles.inheritBaseNormalText);
                    // Main height influence is only available if the shader use the heightmap for displacement (per vertex or per level)
                    // We always display it as it can be tricky to know when per pixel displacement is enabled or not
                    m_MaterialEditor.ShaderProperty(inheritBaseHeight[paramIndex], styles.inheritBaseHeightText);

                    EditorGUI.indentLevel--;
                }
            }

            EditorGUI.indentLevel--;

            if (layerIndex == 0)
                EditorGUILayout.Space();

            return result;
        }

        bool DoLayersGUI(AssetImporter materialImporter)
        {
            Material material = m_MaterialEditor.target as Material;

            bool layerChanged = false;

            GUI.changed = false;

            EditorGUI.indentLevel++;
            GUILayout.Label(styles.layersText, EditorStyles.boldLabel);

            EditorGUI.showMixedValue = layerCount.hasMixedValue;
            EditorGUI.BeginChangeCheck();
            int newLayerCount = EditorGUILayout.IntSlider(styles.layerCountText, (int)layerCount.floatValue, 2, 4);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(material, "Change layer count");
                layerCount.floatValue = (float)newLayerCount;
                SynchronizeAllLayersProperties();
                layerChanged = true;
            }

            m_MaterialEditor.TexturePropertySingleLine(styles.layerMapMaskText, layerMaskMap);

            EditorGUI.indentLevel++;
            m_MaterialEditor.ShaderProperty(UVBlendMask, styles.UVBlendMaskText);

            if (((LayerUVBaseMapping)UVBlendMask.floatValue == LayerUVBaseMapping.Planar) ||
                ((LayerUVBaseMapping)UVBlendMask.floatValue == LayerUVBaseMapping.Triplanar))
            {
                m_MaterialEditor.ShaderProperty(texWorldScaleBlendMask, styles.layerTexWorldScaleText);
            }
            else
            {
                m_MaterialEditor.ShaderProperty(layerTilingBlendMask, styles.layerTilingBlendMaskText);
            }
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
            EditorGUI.showMixedValue = useDensityMode.hasMixedValue;
            bool useDensityModeEnable = EditorGUILayout.Toggle(styles.useDensityModeModeText, useDensityMode.floatValue > 0.0f);
            if (EditorGUI.EndChangeCheck())
            {
                useDensityMode.floatValue = useDensityModeEnable ? 1.0f : 0.0f;
            }

            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = useHeightBasedBlend.hasMixedValue;
            bool enabled = EditorGUILayout.Toggle(styles.useHeightBasedBlendText, useHeightBasedBlend.floatValue > 0.0f);
            if (EditorGUI.EndChangeCheck())
            {
                useHeightBasedBlend.floatValue = enabled ? 1.0f : 0.0f;
            }

            m_MaterialEditor.ShaderProperty(objectScaleAffectTile, mainLayerModeInfluenceEnable ? styles.objectScaleAffectTileText2 : styles.objectScaleAffectTileText);

            m_MaterialEditor.ShaderProperty(horizonFade, Styles.horizonFadeText);

            EditorGUILayout.Space();

            for (int i = 0; i < numLayer; i++)
            {
                layerChanged |= DoLayerGUI(materialImporter, i);
            }

            EditorGUILayout.Space();
            GUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(styles.syncButtonText))
                {
                    SynchronizeAllLayersProperties();
                    layerChanged = true;
                }
            }
            GUILayout.EndHorizontal();

            EditorGUI.indentLevel--;

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
            SetKeyword(material, "_LAYER_TILING_COUPLED_WITH_UNIFORM_OBJECT_SCALE", material.GetFloat(kObjectScaleAffectTile) > 0.0f);

            // Blend mask
            LayerUVBaseMapping UVBlendMaskMapping = (LayerUVBaseMapping)material.GetFloat(kUVBlendMask);
            SetKeyword(material, "_LAYER_MAPPING_PLANAR_BLENDMASK", UVBlendMaskMapping == LayerUVBaseMapping.Planar);
            SetKeyword(material, "_LAYER_MAPPING_TRIPLANAR_BLENDMASK",  UVBlendMaskMapping == LayerUVBaseMapping.Triplanar);

            int numLayer = (int)material.GetFloat(kLayerCount);

            // Layer
            if (numLayer == 4)
            {
                SetKeyword(material, "_LAYEREDLIT_4_LAYERS", true);
                SetKeyword(material, "_LAYEREDLIT_3_LAYERS", false);
            }
            else if (numLayer == 3)
            {
                SetKeyword(material, "_LAYEREDLIT_4_LAYERS", false);
                SetKeyword(material, "_LAYEREDLIT_3_LAYERS", true);
            }
            else
            {
                SetKeyword(material, "_LAYEREDLIT_4_LAYERS", false);
                SetKeyword(material, "_LAYEREDLIT_3_LAYERS", false);
            }

            const string kLayerMappingPlanar = "_LAYER_MAPPING_PLANAR";
            const string kLayerMappingTriplanar = "_LAYER_MAPPING_TRIPLANAR";

            // We have to check for each layer if the UV2 or UV3 is needed.
            bool needUV3 = false;
            bool needUV2 = false;

            for (int i = 0; i < numLayer; ++i)
            {
                string layerUVBaseParam = string.Format("{0}{1}", kUVBase, i);
                LayerUVBaseMapping layerUVBaseMapping = (LayerUVBaseMapping)material.GetFloat(layerUVBaseParam);
                string currentLayerMappingPlanar = string.Format("{0}{1}", kLayerMappingPlanar, i);
                SetKeyword(material, currentLayerMappingPlanar, layerUVBaseMapping == LayerUVBaseMapping.Planar);
                string currentLayerMappingTriplanar = string.Format("{0}{1}", kLayerMappingTriplanar, i);
                SetKeyword(material, currentLayerMappingTriplanar, layerUVBaseMapping == LayerUVBaseMapping.Triplanar);

                string uvBase = string.Format("{0}{1}", kUVBase, i);
                string uvDetail = string.Format("{0}{1}", kUVDetail, i);

                if (((UVDetailMapping)material.GetFloat(uvDetail) == UVDetailMapping.UV2) ||
                    ((LayerUVBaseMapping)material.GetFloat(uvBase) == LayerUVBaseMapping.UV2))
                {
                    needUV2 = true;
                }

                if (((UVDetailMapping)material.GetFloat(uvDetail) == UVDetailMapping.UV3) ||
                    ((LayerUVBaseMapping)material.GetFloat(uvBase) == LayerUVBaseMapping.UV3))
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

                SetKeyword(material, "_NORMALMAP_TANGENT_SPACE" + i, normalMapSpace == NormalMapSpace.TangentSpace);

                if (normalMapSpace == NormalMapSpace.TangentSpace)
                {
                    SetKeyword(material, "_NORMALMAP" + i, material.GetTexture(kNormalMap + i) || material.GetTexture(kDetailMap + i));
                }
                else
                {
                    SetKeyword(material, "_NORMALMAP" + i, material.GetTexture(kNormalMapOS + i) || material.GetTexture(kDetailMap + i));
                }

                SetKeyword(material, "_MASKMAP" + i, material.GetTexture(kMaskMap + i));

                SetKeyword(material, "_SPECULAROCCLUSIONMAP" + i, material.GetTexture(kSpecularOcclusionMap + i));

                SetKeyword(material, "_DETAIL_MAP" + i, material.GetTexture(kDetailMap + i));

                SetKeyword(material, "_HEIGHTMAP" + i, material.GetTexture(kHeightMap + i));
            }

            SetKeyword(material, "_EMISSIVE_COLOR_MAP", material.GetTexture(kEmissiveColorMap));

            SetKeyword(material, "_MAIN_LAYER_INFLUENCE_MODE", material.GetFloat(kkUseMainLayerInfluence) != 0.0f);

            VertexColorMode VCMode = (VertexColorMode)material.GetFloat(kVertexColorMode);
            if (VCMode == VertexColorMode.Multiply)
            {
                SetKeyword(material, "_LAYER_MASK_VERTEX_COLOR_MUL", true);
                SetKeyword(material, "_LAYER_MASK_VERTEX_COLOR_ADD", false);
            }
            else if (VCMode == VertexColorMode.Add)
            {
                SetKeyword(material, "_LAYER_MASK_VERTEX_COLOR_MUL", false);
                SetKeyword(material, "_LAYER_MASK_VERTEX_COLOR_ADD", true);
            }
            else
            {
                SetKeyword(material, "_LAYER_MASK_VERTEX_COLOR_MUL", false);
                SetKeyword(material, "_LAYER_MASK_VERTEX_COLOR_ADD", false);
            }

            bool useHeightBasedBlend = material.GetFloat(kUseHeightBasedBlend) != 0.0f;
            SetKeyword(material, "_HEIGHT_BASED_BLEND", useHeightBasedBlend);

            bool useDensityModeEnable = material.GetFloat(kUseDensityMode) != 0.0f;
            SetKeyword(material, "_DENSITY_MODE", useDensityModeEnable);
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

            bool layerChanged = DoLayersGUI(materialImporter);

            EditorGUILayout.Space();
            GUILayout.Label(Styles.lightingText, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            m_MaterialEditor.TexturePropertySingleLine(Styles.emissiveText, emissiveColorMap, emissiveColor);
            m_MaterialEditor.ShaderProperty(emissiveIntensity, Styles.emissiveIntensityText);
            DoEmissionArea(material);
            EditorGUI.indentLevel--;
            m_MaterialEditor.EnableInstancingField();

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
