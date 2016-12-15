using System;
using UnityEngine;
using UnityEngine.Rendering;

using System.Linq;

namespace UnityEditor.Experimental.ScriptableRenderLoop
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

        private class StylesLayer
        {
            public readonly GUIContent[] layerLabels =
            {
                new GUIContent("Layer 0"),
                new GUIContent("Layer 1"),
                new GUIContent("Layer 2"),
                new GUIContent("Layer 3"),
            };

            public readonly GUIContent materialLayerText = new GUIContent("Material");
            public readonly GUIContent syncButtonText = new GUIContent("Re-Synchronize Layers", "Re-synchronize all layers's properties with the referenced Material");
            public readonly GUIContent layersText = new GUIContent("Layers");
            public readonly GUIContent emissiveText = new GUIContent("Emissive");
            public readonly GUIContent layerMapMaskText = new GUIContent("Layer Mask", "Layer mask (multiplied by vertex color if enabled)");
            public readonly GUIContent layerMapVertexColorText = new GUIContent("Use Vertex Color", "Layer mask (multiplied by layer mask if enabled)");
            public readonly GUIContent layerCountText = new GUIContent("Layer Count", "Number of layers.");
            public readonly GUIContent layerTexWorldScaleText = new GUIContent("Tiling", "Tiling factor applied to Planar/Trilinear mapping");
            public readonly GUIContent UVBaseText = new GUIContent("Base UV Mapping", "Base UV Mapping mode of the layer.");
            public readonly GUIContent UVDetailText = new GUIContent("Detail UV Mapping", "Detail UV Mapping mode of the layer.");
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

        MaterialProperty layerMaskMap = null;
        const string kLayerMaskMap = "_LayerMaskMap";
        MaterialProperty layerMaskVertexColor = null;
        const string kLayerMaskVertexColor = "_LayerMaskVertexColor";
        MaterialProperty layerCount = null;
        const string kLayerCount = "_LayerCount";
        MaterialProperty[] layerTexWorldScale = new MaterialProperty[kMaxLayerCount];
        MaterialProperty[] layerUVBase = new MaterialProperty[kMaxLayerCount];
        MaterialProperty[] layerUVMappingMask = new MaterialProperty[kMaxLayerCount];
        MaterialProperty[] layerUVDetail = new MaterialProperty[kMaxLayerCount];
        MaterialProperty[] layerUVDetailsMappingMask = new MaterialProperty[kMaxLayerCount];

        MaterialProperty layerEmissiveColor = null;
        MaterialProperty layerEmissiveColorMap = null;
        MaterialProperty layerEmissiveIntensity = null;

        override protected void FindMaterialProperties(MaterialProperty[] props)
        {
			FindMaterialOptionProperties(props);

            layerMaskMap = FindProperty(kLayerMaskMap, props);
            layerMaskVertexColor = FindProperty(kLayerMaskVertexColor, props);
            layerCount = FindProperty(kLayerCount, props);
            for (int i = 0; i < numLayer; ++i)
            {
                layerTexWorldScale[i] = FindProperty(string.Format("{0}{1}", kTexWorldScale, i), props);
                layerUVBase[i] = FindProperty(string.Format("{0}{1}", kUVBase, i), props);
                layerUVMappingMask[i] = FindProperty(string.Format("{0}{1}", kUVMappingMask, i), props);
                layerUVDetail[i] = FindProperty(string.Format("{0}{1}", kUVDetail, i), props);
                layerUVDetailsMappingMask[i] = FindProperty(string.Format("{0}{1}", kUVDetailsMappingMask, i), props);
            }

            layerEmissiveColor = FindProperty(kEmissiveColor, props);
            layerEmissiveColorMap = FindProperty(kEmissiveColorMap, props);
            layerEmissiveIntensity = FindProperty(kEmissiveIntensity, props);
        }

        int numLayer
        {
            set { layerCount.floatValue = (float)value; }
            get { return (int)layerCount.floatValue; }
        }

        void SynchronizeAllLayersProperties()
        {
            for (int i = 0; i < numLayer; ++i)
            {
                SynchronizeLayerProperties(i);
            }
        }

        void SynchronizeLayerProperties(int layerIndex)
        {
			string[] exclusionList = { kTexWorldScale, kUVBase, kUVMappingMask, kUVDetail, kUVDetailsMappingMask };

            Material material = m_MaterialEditor.target as Material;
            Material layerMaterial = m_MaterialLayers[layerIndex];

            if (layerMaterial != null)
            {
                Shader layerShader = layerMaterial.shader;
                int propertyCount = ShaderUtil.GetPropertyCount(layerShader);
                for (int i = 0; i < propertyCount; ++i)
                {
                    string propertyName = ShaderUtil.GetPropertyName(layerShader, i);
                    string layerPropertyName = propertyName + layerIndex;

					if(!exclusionList.Contains(propertyName))
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

        void InitializeMaterialLayers(AssetImporter materialImporter)
        {
            if (materialImporter.userData != string.Empty)
            {
                SerializeableGUIDs layersGUID = JsonUtility.FromJson<SerializeableGUIDs>(materialImporter.userData);
                if (layersGUID.GUIDArray.Length > 0)
                {
                    m_MaterialLayers = new Material[layersGUID.GUIDArray.Length];
                    for (int i = 0; i < layersGUID.GUIDArray.Length; ++i)
                    {
                        m_MaterialLayers[i] = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(layersGUID.GUIDArray[i]), typeof(Material)) as Material;
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

        bool CheckInputOptionConsistency(string optionName, string[] shortNames, ref string outValueNames)
        {
            bool result = true;
            outValueNames = "";
            for (int i = 0; i < numLayer; ++i)
            {
                Material layer = m_MaterialLayers[i];
                if (layer != null)
                {
                    int currentValue = (int)layer.GetFloat(optionName); // All options are in fact enums
                    Debug.Assert(currentValue < shortNames.Length);
                    outValueNames += shortNames[currentValue] + "    ";

                    for (int j = i + 1; j < numLayer; ++j)
                    {
                        Material otherLayer = m_MaterialLayers[j];
                        if (otherLayer != null)
                        {
                            if (currentValue != (int)otherLayer.GetFloat(optionName))
                            {
                                result = false;
                            }
                        }
                    }
                }
                else
                {
                    outValueNames += "X    ";
                }
            }

            return result;
        }

        bool CheckInputMapConsistency(string mapName, ref string outValueNames)
        {
            bool result = true;
            outValueNames = "";
            for (int i = 0; i < numLayer; ++i)
            {
                Material layer = m_MaterialLayers[i];
                if (layer != null)
                {
                    bool currentValue = layer.GetTexture(mapName) != null;
                    outValueNames += (currentValue ? "Y" : "N") + "    ";

                    for (int j = i + 1; j < numLayer; ++j)
                    {
                        Material otherLayer = m_MaterialLayers[j];
                        if (otherLayer != null)
                        {
                            bool otherValue = otherLayer.GetTexture(mapName) != null;
                            if (currentValue != otherValue)
                            {
                                result = false;
                            }
                        }
                    }
                }
                else
                {
                    outValueNames += "N    ";
                }
            }

            return result;
        }

        void CheckLayerConsistency()
        {
            string optionValueNames = "";
            // We need to check consistency between all layers.
            // Each input options and each input maps can result in different #defines in the shader so all of them need to be consistent
            // otherwise the result will be undetermined

            // Input options consistency
            string[] smoothnessSourceShortNames = { "Mask", "Albedo" };
            string[] normalMapShortNames = { "Tan", "Obj" };
            string[] heightMapShortNames = { "Parallax", "Disp" };
            string[] detailModeShortNames = { "DNormal", "DAOHeight" };

            string warningInputOptions = "";
            if (!CheckInputOptionConsistency(kSmoothnessTextureChannel, smoothnessSourceShortNames, ref optionValueNames))
            {
                warningInputOptions += "Smoothness Source:    " + optionValueNames + "\n";
            }
            if (!CheckInputOptionConsistency(kNormalMapSpace, normalMapShortNames, ref optionValueNames))
            {
                warningInputOptions += "Normal Map Space:    " + optionValueNames + "\n";
            }
            if (!CheckInputOptionConsistency(kHeightMapMode, heightMapShortNames, ref optionValueNames))
            {
                warningInputOptions += "Height Map Mode:    " + optionValueNames + "\n";
            }
            if (!CheckInputOptionConsistency(kDetailMapMode, detailModeShortNames, ref optionValueNames))
            {
                warningInputOptions += "Detail Map Mode:    " + optionValueNames + "\n";
            }

            if (warningInputOptions != string.Empty)
            {
                warningInputOptions = "Input Option Consistency Error:\n" + warningInputOptions;
            }

            // Check input maps consistency
            string warningInputMaps = "";

            if (!CheckInputMapConsistency(kNormalMap, ref optionValueNames))
            {
                warningInputMaps += "Normal Map:    " + optionValueNames + "\n";
            }
            if (!CheckInputMapConsistency(kDetailMap, ref optionValueNames))
            {
                warningInputMaps += "Detail Map:    " + optionValueNames + "\n";
            }
            if (!CheckInputMapConsistency(kMaskMap, ref optionValueNames))
            {
                warningInputMaps += "Mask Map:    " + optionValueNames + "\n";
            }
            if (!CheckInputMapConsistency(kSpecularOcclusionMap, ref optionValueNames))
            {
                warningInputMaps += "Specular Occlusion Map:    " + optionValueNames + "\n";
            }
            if (!CheckInputMapConsistency(kHeightMap, ref optionValueNames))
            {
                warningInputMaps += "Height Map:    " + optionValueNames + "\n";
            }

            if (warningInputMaps != string.Empty)
            {
                warningInputMaps = "Input Maps Consistency Error:\n" + warningInputMaps;
                if (warningInputOptions != string.Empty)
                    warningInputMaps = "\n" + warningInputMaps;
            }

            string warning = warningInputOptions + warningInputMaps;
            if (warning != string.Empty)
            {
                EditorGUILayout.HelpBox(warning, MessageType.Error);
            }
        }

        void SynchronizeInputOptions()
        {
            Material material = m_MaterialEditor.target as Material;

            // We synchronize input options with the firsts non null Layer (all layers should have consistent options)
            Material firstLayer = null;
            int i = 0;
            while (i < numLayer && !(firstLayer = m_MaterialLayers[i])) ++i;

            if (firstLayer != null)
            {
                material.SetFloat(kSmoothnessTextureChannel, firstLayer.GetFloat(kSmoothnessTextureChannel));
                material.SetFloat(kNormalMapSpace, firstLayer.GetFloat(kNormalMapSpace));
                material.SetFloat(kHeightMapMode, firstLayer.GetFloat(kHeightMapMode));
                // Force emissive to be emissive color
                material.SetFloat(kEmissiveColorMode, (float)EmissiveColorMode.UseEmissiveColor);
            }
        }

        bool DoLayerGUI(AssetImporter materialImporter, int layerIndex)
        {
            bool result = false;

            EditorGUILayout.LabelField(styles.layerLabels[layerIndex]);

            EditorGUI.indentLevel++;

            EditorGUI.BeginChangeCheck();
            m_MaterialLayers[layerIndex] = EditorGUILayout.ObjectField(styles.materialLayerText, m_MaterialLayers[layerIndex], typeof(Material), true) as Material;
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(materialImporter, "Change layer material");
                SynchronizeLayerProperties(layerIndex);
                result = true;
            }

            EditorGUI.BeginChangeCheck();
            m_MaterialEditor.ShaderProperty(layerUVBase[layerIndex], styles.UVBaseText);
            if (EditorGUI.EndChangeCheck())
            {
				SynchronizeLayerProperties(layerIndex);
                result = true;
            }
            if (((LayerUVBaseMapping)layerUVBase[layerIndex].floatValue == LayerUVBaseMapping.Planar) ||
                ((LayerUVBaseMapping)layerUVBase[layerIndex].floatValue == LayerUVBaseMapping.Triplanar))
            {
                EditorGUI.indentLevel++;
                m_MaterialEditor.ShaderProperty(layerTexWorldScale[layerIndex], styles.layerTexWorldScaleText);
                EditorGUI.indentLevel--;
            }
            else
            {
                EditorGUI.BeginChangeCheck();
                m_MaterialEditor.ShaderProperty(layerUVDetail[layerIndex], styles.UVDetailText);
                if (EditorGUI.EndChangeCheck())
                {
					SynchronizeLayerProperties(layerIndex);
                    result = true;
                }
            }

            EditorGUI.indentLevel--;

            return result;
        }

        bool DoLayersGUI(AssetImporter materialImporter)
        {
            Material material = m_MaterialEditor.target as Material;

            bool layerChanged = false;

            GUI.changed = false;

            EditorGUI.indentLevel++;
            GUILayout.Label(styles.layersText, EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            int newLayerCount = EditorGUILayout.IntSlider(styles.layerCountText, (int)layerCount.floatValue, 2, 4);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(material, "Change layer count");
                layerCount.floatValue = (float)newLayerCount;
                SynchronizeAllLayersProperties();
                layerChanged = true;
            }

            m_MaterialEditor.ShaderProperty(layerMaskVertexColor, styles.layerMapVertexColorText);
            m_MaterialEditor.TexturePropertySingleLine(styles.layerMapMaskText, layerMaskMap);

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

		protected override void SetupMaterialKeywords(Material material)
        {
			SetupCommonOptionsKeywords(material);
			SetupLayersKeywords(material);

            // Find first non null layer
            int i = 0;
            while (i < numLayer && (m_MaterialLayers[i] == null)) ++i;

            if (i < numLayer)
            {
                SetKeyword(material, "_NORMALMAP", material.GetTexture(kNormalMap + i));
                SetKeyword(material, "_MASKMAP", material.GetTexture(kMaskMap + i));
                SetKeyword(material, "_SPECULAROCCLUSIONMAP", material.GetTexture(kSpecularOcclusionMap + i));
                SetKeyword(material, "_HEIGHTMAP", material.GetTexture(kHeightMap + i));
				SetKeyword(material, "_DETAIL_MAP", material.GetTexture(kDetailMap + i));
            }

            SetKeyword(material, "_EMISSIVE_COLOR_MAP", material.GetTexture(kEmissiveColorMap));
            SetKeyword(material, "_LAYER_MASK_VERTEX_COLOR", material.GetFloat(kLayerMaskVertexColor) != 0.0f);

			// We have to check for each layer if the UV3 is needed.
            SetKeyword(material, "_REQUIRE_UV2", false);
			SetKeyword(material, "_REQUIRE_UV3", false);
			for (int layer = 0; layer < numLayer; ++layer)
			{
				string uvBase = string.Format("{0}{1}", kUVBase, layer);
				string uvDetail = string.Format("{0}{1}", kUVDetail, layer);

				if (
					 ((UVDetailMapping)material.GetFloat(uvDetail) == UVDetailMapping.UV2) ||
					 ((LayerUVBaseMapping)material.GetFloat(uvBase) == LayerUVBaseMapping.UV2)
					)
				{
					SetKeyword(material, "_REQUIRE_UV2", true);
					break;
				}

				if (
					 ((UVDetailMapping)material.GetFloat(uvDetail) == UVDetailMapping.UV3) ||
					 ((LayerUVBaseMapping)material.GetFloat(uvBase) == LayerUVBaseMapping.UV3)
					)
				{
					SetKeyword(material, "_REQUIRE_UV3", true);
					break;
				}
			}
        }

		void SetupLayersKeywords(Material material)
        {
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

            const string kLayerMappingPlanar = "_LAYER_MAPPING_PLANAR_";
            const string kLayerMappingTriplanar = "_LAYER_MAPPING_TRIPLANAR_";

            for (int i = 0 ; i < numLayer; ++i)
            {
                // We setup the masking map based on the enum for each layer.
                // using mapping mask allow to reduce the number of generated combination for a very small increase in ALU
                string layerUVBaseParam = string.Format("{0}{1}", kUVBase, i);
                LayerUVBaseMapping layerUVBaseMapping = (LayerUVBaseMapping)material.GetFloat(layerUVBaseParam);
                string layerUVDetailParam = string.Format("{0}{1}", kUVDetail, i);
                UVDetailMapping layerUVDetailMapping = (UVDetailMapping)material.GetFloat(layerUVDetailParam);
                string currentLayerMappingPlanar = string.Format("{0}{1}", kLayerMappingPlanar, i);
                string currentLayerMappingTriplanar = string.Format("{0}{1}", kLayerMappingTriplanar, i);                

                float X, Y, Z, W;
                X = (layerUVBaseMapping == LayerUVBaseMapping.UV0) ? 1.0f : 0.0f;
                Y = (layerUVBaseMapping == LayerUVBaseMapping.UV1) ? 1.0f : 0.0f;
                Z = (layerUVBaseMapping == LayerUVBaseMapping.UV2) ? 1.0f : 0.0f;
                W = (layerUVBaseMapping == LayerUVBaseMapping.UV3) ? 1.0f : 0.0f;
                layerUVMappingMask[i].colorValue = new Color(X, Y, Z, W);

                SetKeyword(material, currentLayerMappingPlanar, layerUVBaseMapping == LayerUVBaseMapping.Planar);
                SetKeyword(material, currentLayerMappingTriplanar, layerUVBaseMapping == LayerUVBaseMapping.Triplanar);

                X = (layerUVDetailMapping == UVDetailMapping.UV0) ? 1.0f : 0.0f;
                Y = (layerUVDetailMapping == UVDetailMapping.UV1) ? 1.0f : 0.0f;
                Z = (layerUVDetailMapping == UVDetailMapping.UV2) ? 1.0f : 0.0f;
                W = (layerUVDetailMapping == UVDetailMapping.UV3) ? 1.0f : 0.0f;
                layerUVDetailsMappingMask[i].colorValue = new Color(X, Y, Z, W);
            }
        }

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            FindCommonOptionProperties(props);
            FindMaterialProperties(props);

            m_MaterialEditor = materialEditor;

            m_MaterialEditor.serializedObject.Update();

            Material material = m_MaterialEditor.target as Material;
            AssetImporter materialImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(material.GetInstanceID()));

            InitializeMaterialLayers(materialImporter);

            bool optionsChanged = false;
            EditorGUI.BeginChangeCheck();
            {
                ShaderOptionsGUI();
                EditorGUILayout.Space();
            }
            if (EditorGUI.EndChangeCheck())
            {
                optionsChanged = true;
            }

            bool layerChanged = DoLayersGUI(materialImporter);

            EditorGUILayout.Space();
            GUILayout.Label(Styles.emissiveText, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            m_MaterialEditor.TexturePropertySingleLine(Styles.emissiveText, layerEmissiveColorMap, layerEmissiveColor);
            m_MaterialEditor.ShaderProperty(layerEmissiveIntensity, Styles.emissiveIntensityText);
            m_MaterialEditor.LightmapEmissionProperty(1);
            EditorGUI.indentLevel--;

            CheckLayerConsistency();

            if (layerChanged || optionsChanged)
            {
                SynchronizeInputOptions();

                foreach (var obj in m_MaterialEditor.targets)
                {
					SetupMaterialKeywords((Material)obj);
                }

                SaveMaterialLayers(materialImporter);
            }

            m_MaterialEditor.serializedObject.ApplyModifiedProperties();

            if (layerChanged)
            {
                materialImporter.SaveAndReimport();
            }
        }
    }
} // namespace UnityEditor
