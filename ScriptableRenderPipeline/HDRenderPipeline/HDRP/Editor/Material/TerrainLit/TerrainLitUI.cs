using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

using System.Linq;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public class TerrainLitGUI : LitGUI
    {
        private class StylesLayer
        {
            public readonly GUIContent layersText = new GUIContent("Inputs");
            public readonly GUIContent layerMapMaskText = new GUIContent("Layer Mask", "Layer mask");
            public readonly GUIContent useHeightBasedBlendText = new GUIContent("Use Height Based Blend", "Layer will be blended with the underlying layer based on the height.");
            public readonly GUIContent heightTransition = new GUIContent("Height Transition", "Size in world units of the smooth transition between layers.");
        }

        static StylesLayer s_Styles = null;
        private static StylesLayer styles { get { if (s_Styles == null) s_Styles = new StylesLayer(); return s_Styles; } }

        public TerrainLitGUI()
        {
            m_PropertySuffixes[0] = "0";
            m_PropertySuffixes[1] = "1";
            m_PropertySuffixes[2] = "2";
            m_PropertySuffixes[3] = "3";
        }

        // Layer options
        MaterialProperty useHeightBasedBlend = null;
        const string kUseHeightBasedBlend = "_UseHeightBasedBlend";

        // Density/opacity mode
        MaterialProperty[] opacityAsDensity = new MaterialProperty[kMaxLayerCount];
        const string kOpacityAsDensity = "_OpacityAsDensity";

        // Height blend
        MaterialProperty heightTransition = null;
        const string kHeightTransition = "_HeightTransition";

        bool m_UseHeightBasedBlend;

        protected override void FindMaterialProperties(MaterialProperty[] props)
        {
            useHeightBasedBlend = FindProperty(kUseHeightBasedBlend, props);
            heightTransition = FindProperty(kHeightTransition, props);

            for (int i = 0; i < kMaxLayerCount; ++i)
            {
                // Density/opacity mode
                opacityAsDensity[i] = FindProperty(string.Format("{0}{1}", kOpacityAsDensity, i), props);
            }
        }

        // We use the user data to save a string that represent the referenced lit material
        // so we can keep reference during serialization
        void DoLayeringInputGUI()
        {
            EditorGUI.indentLevel++;
            GUILayout.Label(styles.layersText, EditorStyles.boldLabel);

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
            EditorGUI.indentLevel--;
        }

        bool DoLayersGUI(AssetImporter materialImporter)
        {
            bool layerChanged = false;

            GUI.changed = false;

            DoLayeringInputGUI();

            EditorGUILayout.Space();

            layerChanged |= GUI.changed;
            GUI.changed = false;

            return layerChanged;
        }

        protected override bool ShouldEmissionBeEnabled(Material mat)
        {
            return false;
        }

        protected override void SetupMaterialKeywordsAndPassInternal(Material material)
        {
            SetupMaterialKeywordsAndPass(material);
        }

        static public void SetupLayersMappingKeywords(Material material)
        {
            const string kLayerMappingPlanar = "_LAYER_MAPPING_PLANAR";
            const string kLayerMappingTriplanar = "_LAYER_MAPPING_TRIPLANAR";

            for (int i = 0; i < kMaxLayerCount; ++i)
            {
                string layerUVBaseParam = string.Format("{0}{1}", kUVBase, i);
                UVBaseMapping layerUVBaseMapping = (UVBaseMapping)material.GetFloat(layerUVBaseParam);
                string currentLayerMappingPlanar = string.Format("{0}{1}", kLayerMappingPlanar, i);
                CoreUtils.SetKeyword(material, currentLayerMappingPlanar, layerUVBaseMapping == UVBaseMapping.Planar);
                string currentLayerMappingTriplanar = string.Format("{0}{1}", kLayerMappingTriplanar, i);
                CoreUtils.SetKeyword(material, currentLayerMappingTriplanar, layerUVBaseMapping == UVBaseMapping.Triplanar);
            }
        }

        // All Setup Keyword functions must be static. It allow to create script to automatically update the shaders with a script if code change
        static new public void SetupMaterialKeywordsAndPass(Material material)
        {
            SetupBaseLitKeywords(material);
            SetupBaseLitMaterialPass(material);

            // TODO: planar/triplannar supprt
            //SetupLayersMappingKeywords(material);

            for (int i = 0; i < kMaxLayerCount; ++i)
            {
                CoreUtils.SetKeyword(material, "_NORMALMAP" + i, material.GetTexture(kNormalMap + i) || material.GetTexture(kDetailMap + i));

                CoreUtils.SetKeyword(material, "_MASKMAP" + i, material.GetTexture(kMaskMap + i));

                CoreUtils.SetKeyword(material, "_HEIGHTMAP" + i, material.GetTexture(kHeightMap + i));

                CoreUtils.SetKeyword(material, "_THICKNESSMAP" + i, material.GetTexture(kThicknessMap + i));
            }

            bool useHeightBasedBlend = material.GetFloat(kUseHeightBasedBlend) != 0.0f;
            CoreUtils.SetKeyword(material, "_HEIGHT_BASED_BLEND", useHeightBasedBlend);
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

            bool optionsChanged = false;
            EditorGUI.BeginChangeCheck();
            {
                BaseMaterialPropertiesGUI();
                EditorGUILayout.Space();
            }
            if (EditorGUI.EndChangeCheck())
            {
                optionsChanged = true;
            }

            bool layerChanged = DoLayersGUI(materialImporter);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(StylesBaseUnlit.advancedText, EditorStyles.boldLabel);
            // NB RenderQueue editor is not shown on purpose: we want to override it based on blend mode
            EditorGUI.indentLevel++;
            m_MaterialEditor.EnableInstancingField();
            EditorGUI.indentLevel--;

            if (layerChanged || optionsChanged)
            {
                foreach (var obj in m_MaterialEditor.targets)
                {
                    SetupMaterialKeywordsAndPassInternal((Material)obj);
                }
            }

            // We should always do this call at the end
            m_MaterialEditor.serializedObject.ApplyModifiedProperties();
        }
    }
} // namespace UnityEditor
