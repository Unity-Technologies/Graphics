using System;
using UnityEditor.ShaderGraph;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;

namespace UnityEditor.Rendering.Universal
{
    /// <summary>
    /// Scope that indicates start of <see cref="DecalProjector"/> GUI.
    /// </summary>
    internal class DecalProjectorScope : GUI.Scope
    {
        public DecalProjectorScope()
        {
            DecalShaderGraphGUI.isDecalProjectorGUI = true;
        }

        protected override void CloseScope()
        {
            DecalShaderGraphGUI.isDecalProjectorGUI = false;
        }
    }

    /// <summary>
    /// Represents the GUI for Decal Shader Graph materials.
    /// </summary>
    internal class DecalShaderGraphGUI : UnityEditor.ShaderGUI
    {
        internal class Styles
        {
            public static GUIContent inputs = new GUIContent("Inputs");
            public static GUIContent advancedOptions = new GUIContent("Advanced Options");

            public static GUIContent meshDecalBiasType = new GUIContent("Mesh Bias Type", "Set the type of bias that is applied to the mesh decal. Depth Bias applies a bias to the final depth value, while View bias applies a world space bias (in meters) alongside the view vector.");
            public static GUIContent meshDecalDepthBiasText = new GUIContent("Depth Bias", "Sets a depth bias to stop the decal's Mesh from overlapping with other Meshes.");
            public static GUIContent meshDecalViewBiasText = new GUIContent("View Bias", "Sets a world-space bias alongside the view vector to stop the decal's Mesh from overlapping with other Meshes. The unit is meters.");
            public static GUIContent drawOrderText = new GUIContent("Priority", "Controls the draw order of Decal Projectors. URP draws decals with lower values first.");
        }

        protected enum Expandable
        {
            Inputs = 1 << 0,
            Advanced = 1 << 1,
        }

        public static bool isDecalProjectorGUI { get; set; }

        const string kDecalMeshBiasType = "_DecalMeshBiasType";
        const string kDecalMeshDepthBias = "_DecalMeshDepthBias";
        const string kDecalViewDepthBias = "_DecalMeshViewBias";
        const string kDrawOrder = "_DrawOrder";

        readonly MaterialHeaderScopeList m_MaterialScopeList = new MaterialHeaderScopeList(uint.MaxValue & ~((uint)Expandable.Advanced));

        MaterialEditor m_MaterialEditor;
        MaterialProperty[] m_Properties;

        MaterialProperty decalMeshBiasType;
        MaterialProperty decalMeshDepthBias;
        MaterialProperty decalMeshViewBias;
        MaterialProperty drawOrder;

        public DecalShaderGraphGUI()
        {
            m_MaterialScopeList.RegisterHeaderScope(Styles.inputs, Expandable.Inputs, DrawExposedProperties);
            m_MaterialScopeList.RegisterHeaderScope(Styles.advancedOptions, Expandable.Advanced, DrawSortingProperties);
        }

        /// <summary>
        /// Override this function to implement your custom GUI. To display a user interface similar to HDRP shaders, use a MaterialUIBlockList.
        /// </summary>
        /// <param name="materialEditor">The current material editor.</param>
        /// <param name="props">The list of properties the material has.</param>
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            DecalMeshWarning();

            m_MaterialEditor = materialEditor;
            FindProperties(props);

            Material material = materialEditor.target as Material;

            using (var changed = new EditorGUI.ChangeCheckScope())
            {
                m_MaterialScopeList.DrawHeaders(materialEditor, material);
            }

            // We should always do this call at the end
            materialEditor.serializedObject.ApplyModifiedProperties();
        }

        private void FindProperties(MaterialProperty[] properties)
        {
            decalMeshBiasType = FindProperty(kDecalMeshBiasType, properties);
            decalMeshViewBias = FindProperty(kDecalViewDepthBias, properties);
            decalMeshDepthBias = FindProperty(kDecalMeshDepthBias, properties);
            drawOrder = FindProperty(kDrawOrder, properties);

            m_Properties = properties;
        }

        private void DrawExposedProperties(Material material)
        {
            MaterialProperty[] properties = m_Properties;
            MaterialEditor materialEditor = m_MaterialEditor;

            // TODO: scope
            var fieldWidth = EditorGUIUtility.fieldWidth;
            var labelWidth = EditorGUIUtility.labelWidth;

            // Copy of MaterialEditor.PropertiesDefaultGUI that excludes properties of PerRendererData
            materialEditor.SetDefaultGUIWidths();
            for (var i = 0; i < properties.Length; i++)
            {
                if ((properties[i].propertyFlags & (ShaderPropertyFlags.HideInInspector | ShaderPropertyFlags.PerRendererData)) != 0)
                    continue;

                float h = materialEditor.GetPropertyHeight(properties[i], properties[i].displayName);
                Rect r = EditorGUILayout.GetControlRect(true, h, EditorStyles.layerMaskField);

                materialEditor.ShaderProperty(r, properties[i], properties[i].displayName);
            }

            EditorGUIUtility.fieldWidth = fieldWidth;
            EditorGUIUtility.labelWidth = labelWidth;
        }

        private void DrawSortingProperties(Material material)
        {
            MaterialEditor materialEditor = m_MaterialEditor;

            materialEditor.EnableInstancingField();
            DrawOrder();
            materialEditor.ShaderProperty(decalMeshBiasType, Styles.meshDecalBiasType);

            DecalMeshDepthBiasType decalBias = (DecalMeshDepthBiasType)decalMeshBiasType.floatValue;
            EditorGUI.indentLevel++;
            switch (decalBias)
            {
                case DecalMeshDepthBiasType.DepthBias:
                    materialEditor.ShaderProperty(decalMeshDepthBias, Styles.meshDecalDepthBiasText);
                    break;
                case DecalMeshDepthBiasType.ViewBias:
                    materialEditor.ShaderProperty(decalMeshViewBias, Styles.meshDecalViewBiasText);
                    break;
            }
            EditorGUI.indentLevel--;
        }

        private void DrawOrder()
        {
            MaterialEditor materialEditor = m_MaterialEditor;

            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = drawOrder.hasMixedValue;
            var queue = EditorGUILayout.IntSlider(Styles.drawOrderText, (int)drawOrder.floatValue, -50, 50);
            if (EditorGUI.EndChangeCheck())
            {
                foreach (var target in materialEditor.targets)
                {
                    var material = target as Material;
                    material.renderQueue = 2000 + queue;
                }
                drawOrder.floatValue = queue;
            }
            EditorGUI.showMixedValue = false;
        }

        private void DecalMeshWarning()
        {
            if (isDecalProjectorGUI)
                return;

            var urp = UniversalRenderPipeline.asset;
            if (urp == null)
                return;

            bool hasDecalScreenSpace = false;
            var renderers = urp.m_RendererDataList;
            foreach (var renderer in renderers)
            {
                if (renderer.TryGetRendererFeature(out DecalRendererFeature decalRendererFeature))
                {
                    var technique = decalRendererFeature.GetTechnique(renderer);
                    if (technique == DecalTechnique.ScreenSpace || technique == DecalTechnique.GBuffer)
                    {
                        hasDecalScreenSpace = true;
                        break;
                    }
                }
            }

            if (hasDecalScreenSpace)
                EditorGUILayout.HelpBox("Decals with Screen Space technique only support rendering with DecalProjector component.", MessageType.Warning);
        }
    }
}
