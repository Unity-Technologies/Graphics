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
            public readonly GUIContent layerBlendMode = new GUIContent("Layer Blend Mode", "Controls how terrain layers are blended.");
            public readonly GUIContent heightTransition = new GUIContent("Height Transition", "Size in world units of the smooth transition between layers.");
            public readonly GUIContent enableInstancedPerPixelNormal = new GUIContent("Enable Per-pixel Normal", "Enable per-pixel normal when the terrain uses instanced rendering.");
        }

        static StylesLayer s_Styles = null;
        private static StylesLayer styles { get { if (s_Styles == null) s_Styles = new StylesLayer(); return s_Styles; } }

        public TerrainLitGUI()
        {
        }

        MaterialProperty layerBlendMode;
        const string kLayerBlendMode = "_LayerBlendMode";

        // Height blend
        MaterialProperty heightTransition = null;
        const string kHeightTransition = "_HeightTransition";

        MaterialProperty enableInstancedPerPixelNormal = null;
        const string kEnableInstancedPerPixelNormal = "_EnableInstancedPerPixelNormal";

        protected override void FindMaterialProperties(MaterialProperty[] props)
        {
            layerBlendMode = FindProperty(kLayerBlendMode, props, false);
            heightTransition = FindProperty(kHeightTransition, props, false);
            enableInstancedPerPixelNormal = FindProperty(kEnableInstancedPerPixelNormal, props, false);
        }

        // We use the user data to save a string that represent the referenced lit material
        // so we can keep reference during serialization
        void DoLayeringInputGUI()
        {
            EditorGUI.indentLevel++;
            GUILayout.Label(styles.layersText, EditorStyles.boldLabel);

            if (layerBlendMode != null)
            {
                m_MaterialEditor.ShaderProperty(layerBlendMode, styles.layerBlendMode);
                if (layerBlendMode.floatValue == 2)
                {
                    EditorGUI.indentLevel++;
                    m_MaterialEditor.ShaderProperty(heightTransition, styles.heightTransition);
                    EditorGUI.indentLevel--;
                }
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

            int layerBlendMode = (int)material.GetFloat(kLayerBlendMode);
            CoreUtils.SetKeyword(material, "_TERRAIN_BLEND_DENSITY", layerBlendMode == 1);
            CoreUtils.SetKeyword(material, "_TERRAIN_BLEND_HEIGHT", layerBlendMode == 2);

            bool enableInstancedPerPixelNormal = material.GetFloat(kEnableInstancedPerPixelNormal) > 0.0f;
            CoreUtils.SetKeyword(material, "_TERRAIN_INSTANCED_PERPIXEL_NORMAL", enableInstancedPerPixelNormal);
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
            bool enablePerPixelNormalChanged = false;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(StylesBaseUnlit.advancedText, EditorStyles.boldLabel);
            // NB RenderQueue editor is not shown on purpose: we want to override it based on blend mode
            EditorGUI.indentLevel++;
            m_MaterialEditor.EnableInstancingField();
            if (m_MaterialEditor.IsInstancingEnabled())
            {
                EditorGUI.indentLevel++;
                EditorGUI.BeginChangeCheck();
                m_MaterialEditor.ShaderProperty(enableInstancedPerPixelNormal, styles.enableInstancedPerPixelNormal);
                enablePerPixelNormalChanged = EditorGUI.EndChangeCheck();
                EditorGUI.indentLevel--;
            }
            EditorGUI.indentLevel--;

            if (layerChanged || optionsChanged || enablePerPixelNormalChanged)
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
