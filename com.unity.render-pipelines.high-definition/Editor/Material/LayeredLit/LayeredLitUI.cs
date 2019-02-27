using System;
using UnityEngine;
using System.Linq;
using UnityEditor.Rendering;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    class LayeredLitGUI : LitGUI
    {
        //Be sure to start after last BaseUnlitGUI.Expandable
        [Flags]
        protected enum LayerExpandable : uint
        {
            MainLayer = 1 << 11,
            Layer1 = 1 << 12,
            Layer2 = 1 << 13,
            Layer3 = 1 << 14,
            LayeringOptionMain = 1 << 15,
            ShowLayer1 = 1 << 16,
            ShowLayer2 = 1 << 17,
            ShowLayer3 = 1 << 18,
            MaterialReferences = 1 << 19,
            MainInput = 1 << 20,
            Layer1Input = 1 << 21,
            Layer2Input = 1 << 22,
            Layer3Input = 1 << 23,
            MainDetail = 1 << 24,
            Layer1Detail = 1 << 25,
            Layer2Detail = 1 << 26,
            Layer3Detail = 1 << 27,
            LayeringOption1 = 1 << 28,
            LayeringOption2 = 1 << 29,
            LayeringOption3 = 1 << 30
        }

        protected override uint defaultExpandedState { get { return (uint)(Expandable.Base | Expandable.Input | Expandable.VertexAnimation | Expandable.Detail | Expandable.Emissive | Expandable.Transparency | Expandable.Other | Expandable.Tesselation) + (uint)(LayerExpandable.MaterialReferences | LayerExpandable.MainInput | LayerExpandable.MainDetail | LayerExpandable.Layer1 | LayerExpandable.Layer2 | LayerExpandable.Layer3); } }
        
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
            
            public readonly GUIContent layerNameHeader = EditorGUIUtility.TrTextContent("Layer name");
            public readonly GUIContent materialToCopyHeader = EditorGUIUtility.TrTextContent("Material to copy");

            public readonly GUIContent uvHeader = EditorGUIUtility.TrTextContent("UV", "Also copy UV.");
            public readonly GUIContent copyButtonIcon = EditorGUIUtility.IconContent("d_UnityEditor.ConsoleWindow", "Copy Material parameters to layer. If UV is disabled, this will not copy UV.");
            public readonly GUIContent layersText = EditorGUIUtility.TrTextContent("Surface Inputs");
            public readonly GUIContent emissiveText = EditorGUIUtility.TrTextContent("Emissive");
            public readonly GUIContent layerMapMaskText = EditorGUIUtility.TrTextContent("Layer Mask", "Specifies the Layer Mask for this Material");
            public readonly GUIContent layerInfluenceMapMaskText = EditorGUIUtility.TrTextContent("Layer Influence Mask", "Specifies the Layer Influence Mask for this Material.");
            public readonly GUIContent vertexColorModeText = EditorGUIUtility.TrTextContent("Vertex Color Mode", "Specifies the method HDRP uses to color vertices.\nMultiply: Multiplies vertex color with the mask.\nAdditive: Remaps vertex color values between [-1, 1] and adds them to the mask (neutral value is 0.5 vertex color).");
            public readonly GUIContent layerCountText = EditorGUIUtility.TrTextContent("Layer Count", "Controls the number of layers for this Material.");
            public readonly GUIContent objectScaleAffectTileText = EditorGUIUtility.TrTextContent("Lock layers 0123 tiling with object Scale", "When enabled, tiling of each layer is affected by the Transform's Scale.");
            public readonly GUIContent objectScaleAffectTileText2 = EditorGUIUtility.TrTextContent("Lock layers  123 tiling with object Scale", "When enabled, tiling of each influenced layer (except the main layer) is affected by the Transform's Scale.");

            public readonly GUIContent layerTexWorldScaleText = EditorGUIUtility.TrTextContent("World Scale", "Sets the tiling factor of the Planar/Trilinear mapping.");
            public readonly GUIContent UVBlendMaskText = EditorGUIUtility.TrTextContent("BlendMask UV Mapping", "Specifies the UV Mapping mode of the layer.");


            public readonly GUIContent layeringOptionText = EditorGUIUtility.TrTextContent("Layering Options");

            public readonly GUIContent useHeightBasedBlendText = EditorGUIUtility.TrTextContent("Use Height Based Blend", "When enabled, HDRP blends the layer with the underlying layer based on the height.");
            public readonly GUIContent useMainLayerInfluenceModeText = EditorGUIUtility.TrTextContent("Main Layer Influence", "Switches between regular layers mode and base/layers mode.");

            public readonly GUIContent opacityAsDensityText = EditorGUIUtility.TrTextContent("Use Opacity map as Density map", "When enabled, HDRP uses the opacity map (alpha channel of Base Color) as the Density map.");
            public readonly GUIContent inheritBaseNormalText = EditorGUIUtility.TrTextContent("Normal influence", "Controls the strength of the normals inherited from the base layer.");
            public readonly GUIContent inheritBaseHeightText = EditorGUIUtility.TrTextContent("Heightmap influence", "Controls the strength of the height map inherited from the base layer.");
            public readonly GUIContent inheritBaseColorText = EditorGUIUtility.TrTextContent("BaseColor influence", "Controls the strength of the Base Color inherited from the base layer.");
            public readonly GUIContent heightTransition = EditorGUIUtility.TrTextContent("Height Transition", "Sets the size, in world units, of the smooth transition between layers.");

            public readonly GUIContent perPixelDisplacementLayersWarning = EditorGUIUtility.TrTextContent("For pixel displacement to work correctly, all layers with a heightmap must use the same UV mapping.");


            public readonly GUIContent materialReferencesText = EditorGUIUtility.TrTextContent("Material To Copy");

            public readonly string materialImporterNotAvailable = "Can't display material layer options because the material is not an asset";

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

        bool[] m_WithUV;

        public LayeredLitGUI()
        {
            m_LayerCount = 4;
            m_PropertySuffixes[0] = "0";
            m_PropertySuffixes[1] = "1";
            m_PropertySuffixes[2] = "2";
            m_PropertySuffixes[3] = "3";

            m_WithUV = new bool[]{ true, true, true, true };
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
        MaterialProperty heightTransition = null;
        const string kHeightTransition = "_HeightTransition";

        bool m_UseHeightBasedBlend;

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

            for (int i = 0; i < kMaxLayerCount; ++i)
            {
                // Density/opacity mode
                opacityAsDensity[i] = FindProperty(string.Format("{0}{1}", kOpacityAsDensity, i), props);

                if (i != 0)
                {
                    // Influence
                    inheritBaseNormal[i - 1] = FindProperty(string.Format("{0}{1}", kInheritBaseNormal, i), props);
                    inheritBaseHeight[i - 1] = FindProperty(string.Format("{0}{1}", kInheritBaseHeight, i), props);
                    inheritBaseColor[i - 1] = FindProperty(string.Format("{0}{1}", kInheritBaseColor, i), props);
                }
            }
            
            UpdateEditorExpended((int)layerCount.floatValue);
        }

        void UpdateEditorExpended(int layerNumber)
        {
            if (layerNumber == 4)
            {
                SetExpandedAreas((uint)LayerExpandable.ShowLayer3, true);
            }
            if (layerNumber >= 3)
            {
                SetExpandedAreas((uint)LayerExpandable.ShowLayer2, true);
            }
            SetExpandedAreas((uint)LayerExpandable.ShowLayer1, true);
        }

        int numLayer
        {
            get { return (int)layerCount.floatValue; }
            set
            {
                layerCount.floatValue = (float)value;
                UpdateEditorExpended(value);
            }
        }

        // This function is call by a script to help artists to ahve up to date material
        // that why it is static
        public static void SynchronizeAllLayers(Material material)
        {
            int layerCount = (int)material.GetFloat(kLayerCount);
            AssetImporter materialImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(material.GetInstanceID()));

            Material[] layers = null;

            // Material importer can be null when the selected material doesn't exists as asset (Material saved inside the scene)
            if (materialImporter != null)
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

        void DrawLayeringOptions(bool mainLayerInfluenceEnable, uint expended, int layerIndex)
        {
            // do layering option (if main layer (0) check if there is any content before drawing the foldout)
            if (layerIndex > 0 || layerIndex == 0 && !useMainLayerInfluence.hasMixedValue && useMainLayerInfluence.floatValue != 0.0f)
            {
                using (var header = new HeaderScope(styles.layeringOptionText.text, expended, this, colorDot: s_Styles.layerColors[layerIndex], subHeader: true))
                {
                    if (header.expanded)
                    {
                        // Main layer does not have any options but height base blend.
                        if (layerIndex > 0)
                        {
                            m_MaterialEditor.ShaderProperty(opacityAsDensity[layerIndex], styles.opacityAsDensityText);

                            if (mainLayerInfluenceEnable)
                            {
                                m_MaterialEditor.ShaderProperty(inheritBaseColor[layerIndex - 1], styles.inheritBaseColorText);
                                m_MaterialEditor.ShaderProperty(inheritBaseNormal[layerIndex - 1], styles.inheritBaseNormalText);
                                // Main height influence is only available if the shader use the heightmap for displacement (per vertex or per level)
                                // We always display it as it can be tricky to know when per pixel displacement is enabled or not
                                m_MaterialEditor.ShaderProperty(inheritBaseHeight[layerIndex - 1], styles.inheritBaseHeightText);
                            }
                        }
                        else
                        {
                            m_MaterialEditor.TexturePropertySingleLine(styles.layerInfluenceMapMaskText, layerInfluenceMaskMap);
                        }
                    }
                }
            }
        }

        bool DoLayerGUI(AssetImporter materialImporter, int layerIndex)
        {
            bool result = false;

            Array values = Enum.GetValues(typeof(LayerExpandable));
            if (layerIndex > 1) //main layer (0) and layer 1 always here
            {
                int startShowVal = Array.IndexOf(values, LayerExpandable.ShowLayer1);
                if (!GetExpandedAreas((uint)values.GetValue(startShowVal + layerIndex)))
                {
                    return false;
                }
            }
                        
            Material material = m_MaterialEditor.target as Material;

            bool mainLayerInfluenceEnable = useMainLayerInfluence.floatValue > 0.0f;

            int startLayer = Array.IndexOf(values, LayerExpandable.MainLayer);
            using (var layerHeader = new HeaderScope(s_Styles.layerLabels[layerIndex].text, (uint)values.GetValue(startLayer + layerIndex), this, false, s_Styles.layerColors[layerIndex]))
            {
                if (layerHeader.expanded)
                {
                    //Note LayeringOptionMain do not preced LayeringOption1
                    int startLayeringOptionValue = Array.IndexOf(values, LayerExpandable.LayeringOption1);
                    var layeringOptionValue = layerIndex == 0 ? LayerExpandable.LayeringOptionMain : (LayerExpandable)values.GetValue(startLayeringOptionValue + layerIndex - 1);
                    DrawLayeringOptions(mainLayerInfluenceEnable, (uint)layeringOptionValue, layerIndex);
                    
                    int startInputValue = Array.IndexOf(values, LayerExpandable.MainInput);
                    var inputValue = (LayerExpandable)values.GetValue(startInputValue + layerIndex);
                    int startDetailValue = Array.IndexOf(values, LayerExpandable.MainDetail);
                    var detailValue = (LayerExpandable)values.GetValue(startDetailValue + layerIndex);
                    DoLayerGUI(material, layerIndex, true, m_UseHeightBasedBlend, (uint)inputValue, (uint)detailValue, colorDot: s_Styles.layerColors[layerIndex], subHeader: true);

                    if (!GetExpandedAreas((uint)detailValue))
                        EditorGUILayout.Space();
                }
            }
            return result;
        }

        void DoLayeringInputGUI()
        {
            using (var header = new HeaderScope(styles.layersText.text, (uint)Expandable.Input, this))
            {
                if (header.expanded)
                {
                    EditorGUI.showMixedValue = layerCount.hasMixedValue;
                    EditorGUI.BeginChangeCheck();
                    int newLayerCount = EditorGUILayout.IntSlider(styles.layerCountText, (int)layerCount.floatValue, 2, 4);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Material material = m_MaterialEditor.target as Material;
                        Undo.RecordObject(material, "Change layer count");
                        numLayer = newLayerCount;
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
                    m_UseHeightBasedBlend = EditorGUILayout.Toggle(styles.useHeightBasedBlendText, useHeightBasedBlend.floatValue > 0.0f);
                    if (EditorGUI.EndChangeCheck())
                    {
                        useHeightBasedBlend.floatValue = m_UseHeightBasedBlend ? 1.0f : 0.0f;
                    }

                    if (m_UseHeightBasedBlend)
                    {
                        EditorGUI.indentLevel++;
                        m_MaterialEditor.ShaderProperty(heightTransition, styles.heightTransition);
                        EditorGUI.indentLevel--;
                    }

                    m_MaterialEditor.ShaderProperty(objectScaleAffectTile, mainLayerModeInfluenceEnable ? styles.objectScaleAffectTileText2 : styles.objectScaleAffectTileText);
                }
            }
        }

        bool DoMaterialReferencesGUI(AssetImporter materialImporter)
        {
            bool layersChanged = false;

            using (var header = new HeaderScope(styles.materialReferencesText.text, (uint)LayerExpandable.MaterialReferences, this))
            {
                if (header.expanded)
                {
                    var width = EditorGUIUtility.labelWidth;
                    EditorGUIUtility.labelWidth = 90;

                    Material material = m_MaterialEditor.target as Material;

                    Color originalContentColor = GUI.contentColor;

                    float indentOffset = EditorGUI.indentLevel * 15f;
                    float colorWidth = 14;
                    float UVWidth = 30;
                    float copyButtonWidth = EditorGUIUtility.singleLineHeight;
                    float endOffset = 5f;

                    Rect headerLineRect = GUILayoutUtility.GetRect(1, EditorGUIUtility.singleLineHeight);
                    Rect headerLabelRect = new Rect(headerLineRect.x, headerLineRect.y, EditorGUIUtility.labelWidth - indentOffset, headerLineRect.height);
                    Rect headerUVRect = new Rect(headerLineRect.x + headerLineRect.width - 48 - endOffset, headerLineRect.y, UVWidth + 5, headerLineRect.height);
                    Rect headerMaterialDropRect = new Rect(headerLineRect.x + headerLabelRect.width, headerLineRect.y, headerLineRect.width - headerLabelRect.width - headerUVRect.width, headerLineRect.height);

                    EditorGUI.LabelField(headerLabelRect, styles.layerNameHeader, EditorStyles.centeredGreyMiniLabel);
                    EditorGUI.LabelField(headerMaterialDropRect, styles.materialToCopyHeader, EditorStyles.centeredGreyMiniLabel);
                    EditorGUI.LabelField(headerUVRect, styles.uvHeader, EditorStyles.centeredGreyMiniLabel);

                    for (int layerIndex = 0; layerIndex < numLayer; ++layerIndex)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUI.BeginChangeCheck();

                            Rect lineRect = GUILayoutUtility.GetRect(1, EditorGUIUtility.singleLineHeight);
                            Rect colorRect = new Rect(lineRect.x, lineRect.y, colorWidth, lineRect.height);
                            Rect materialRect = new Rect(lineRect.x + colorRect.width, lineRect.y, lineRect.width - UVWidth - colorWidth - copyButtonWidth + endOffset, lineRect.height);
                            Rect uvRect = new Rect(lineRect.x + lineRect.width - copyButtonWidth - UVWidth - endOffset, lineRect.y, UVWidth, lineRect.height);
                            Rect copyRect = new Rect(lineRect.x + lineRect.width - copyButtonWidth - endOffset, lineRect.y, copyButtonWidth, lineRect.height);

                            m_MaterialLayers[layerIndex] = EditorGUI.ObjectField(materialRect, styles.layerLabels[layerIndex], m_MaterialLayers[layerIndex], typeof(Material), true) as Material;
                            if (EditorGUI.EndChangeCheck())
                            {
                                Undo.RecordObject(materialImporter, "Change layer material");
                                SynchronizeLayerProperties(material, m_MaterialLayers, layerIndex, true);
                                layersChanged = true;
                            }

                            
                            colorRect.width = 30f;
                            GUI.contentColor = styles.layerColors[layerIndex];
                            EditorGUI.LabelField(colorRect, "■");
                            GUI.contentColor = originalContentColor;
                            
                            m_WithUV[layerIndex] = EditorGUI.Toggle(uvRect, m_WithUV[layerIndex]);
                            
                            if (GUI.Button(copyRect, GUIContent.none))
                            {
                                SynchronizeLayerProperties(material, m_MaterialLayers, layerIndex, !m_WithUV[layerIndex]);
                                layersChanged = true;
                            }

                            //fake the icon with two Console icon
                            //Rect copyRect = GUILayoutUtility.GetLastRect();
                            copyRect.x -= 16;
                            copyRect.width = 40;
                            EditorGUI.LabelField(copyRect, styles.copyButtonIcon);
                            copyRect.x -= 3;
                            copyRect.y += 3;
                            EditorGUI.LabelField(copyRect, styles.copyButtonIcon);
                        }
                    }

                    EditorGUIUtility.labelWidth = width;
                }
            }
            
            return layersChanged;
        }

        bool DoLayersGUI(AssetImporter materialImporter)
        {
            if (materialImporter == null)
            {
                EditorGUILayout.HelpBox(styles.materialImporterNotAvailable, MessageType.Warning);
                return false;
            }
            
            bool layerChanged = false;

            GUI.changed = false;

            DoLayeringInputGUI();

            layerChanged |= DoMaterialReferencesGUI(materialImporter);

            for (int i = 0; i < numLayer; i++)
            {
                layerChanged |= DoLayerGUI(materialImporter, i);
            }

            layerChanged |= GUI.changed;
            GUI.changed = false;

            return layerChanged;
        }

        protected override bool ShouldEmissionBeEnabled(Material material)
        {
            return (material.GetColor(kEmissiveColor) != Color.black) || material.GetTexture(kEmissiveColorMap);
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
            for (int i = 0; i < material.GetInt(kLayerCount); ++i)
            {
                useDensityModeEnable |= material.GetFloat(kOpacityAsDensity + i) != 0.0f;
            }
            CoreUtils.SetKeyword(material, "_DENSITY_MODE", useDensityModeEnable);

            MaterialId materialId = (MaterialId)material.GetFloat(kMaterialID);
            CoreUtils.SetKeyword(material, "_MATERIAL_FEATURE_SUBSURFACE_SCATTERING", materialId == MaterialId.LitSSS);
            CoreUtils.SetKeyword(material, "_MATERIAL_FEATURE_TRANSMISSION", materialId == MaterialId.LitTranslucent || (materialId == MaterialId.LitSSS && material.GetFloat(kTransmissionEnable) > 0.0f));
        }

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            FindBaseMaterialProperties(props);
            FindMaterialProperties(props);

            m_MaterialEditor = materialEditor;

            // We should always register the key used to keep collapsable state
            InitExpandableState(materialEditor);

            // We should always do this call at the beginning
            m_MaterialEditor.serializedObject.Update();

            Material material = m_MaterialEditor.target as Material;
            AssetImporter materialImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(material.GetInstanceID()));

            // Material importer can be null when the selected material doesn't exists as asset (Material saved inside the scene)
            if (materialImporter != null)
                InitializeMaterialLayers(materialImporter, ref m_MaterialLayers);

            bool optionsChanged = false;
            EditorGUI.BeginChangeCheck();
            {
                using (var header = new HeaderScope(StylesBaseUnlit.optionText, (uint)Expandable.Base, this))
                {
                    if (header.expanded)
                        BaseMaterialPropertiesGUI();
                }
                MaterialTesselationPropertiesGUI();
                VertexAnimationPropertiesGUI();
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

            using (var header = new HeaderScope(StylesBaseUnlit.advancedText, (uint)Expandable.Advance, this))
            {
                if (header.expanded)
                {
                    // NB RenderQueue editor is not shown on purpose: we want to override it based on blend mode
                    m_MaterialEditor.EnableInstancingField();
                    m_MaterialEditor.ShaderProperty(enableSpecularOcclusion, Styles.enableSpecularOcclusionText);
                }
            }

            if (layerChanged || optionsChanged)
            {
                foreach (var obj in m_MaterialEditor.targets)
                {
                    SetupMaterialKeywordsAndPassInternal((Material)obj);
                }

                // SaveAssetsProcessor the referenced material in the users data
                if (materialImporter != null)
                    SaveMaterialLayers(materialImporter);
            }

            // We should always do this call at the end
            m_MaterialEditor.serializedObject.ApplyModifiedProperties();
        }
    }
} // namespace UnityEditor
