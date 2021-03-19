using System;
using UnityEditor.ShaderGraph;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;

namespace UnityEditor.Rendering.Universal
{
    /// <summary>
    /// Represents the GUI for HDRP Shader Graph materials.
    /// </summary>
    internal class DecalShaderGraphGUI : PBRMasterGUI
    {
        internal class Styles
        {
            public static GUIContent sortingInputs = new GUIContent("Sorting Inputs");
            public static GUIContent exposedInputs = new GUIContent("Exposed Inputs");

            public static GUIContent meshDecalBiasType = new GUIContent("Mesh Decal Bias Type", "Set the type of bias that is applied to the mesh decal. Depth Bias applies a bias to the final depth value, while View bias applies a world space bias (in meters) alongside the view vector.");
            public static GUIContent meshDecalDepthBiasText = new GUIContent("Mesh Decal Depth Bias", "Sets a depth bias to stop the decal's Mesh from overlapping with other Meshes.");
            public static GUIContent meshDecalViewBiasText = new GUIContent("Mesh Decal View Bias", "Sets a world-space bias alongside the view vector to stop the decal's Mesh from overlapping with other Meshes. The unit is meters.");
            public static GUIContent drawOrderText = new GUIContent("Draw Order", "Controls the draw order of Decal Projectors. HDRP draws decals with lower values first.");
        }

        protected enum Expandable
        {
            ExposedInputs = 1 << 0,
            SortingInputs = 1 << 1,
        }

        const string kDecalMeshBiasType = "_DecalMeshBiasType";
        const string kDecalMeshDepthBias = "_DecalMeshDepthBias";
        const string kDecalViewDepthBias = "_DecalMeshViewBias";
        const string kDrawOrder = "_DrawOrder";

        readonly MaterialHeaderScopeList m_MaterialScopeList = new MaterialHeaderScopeList(uint.MaxValue);

        MaterialEditor m_MaterialEditor;
        MaterialProperty[] m_Properties;

        MaterialProperty decalMeshBiasType;
        MaterialProperty decalMeshDepthBias;
        MaterialProperty decalMeshViewBias;
        MaterialProperty drawOrder;

        public DecalShaderGraphGUI()
        {
            m_MaterialScopeList.RegisterHeaderScope(Styles.exposedInputs, (uint)Expandable.ExposedInputs, DrawExposedProperties);
            m_MaterialScopeList.RegisterHeaderScope(Styles.sortingInputs, (uint)Expandable.SortingInputs, DrawSortingProperties);
        }

        /// <summary>
        /// Override this function to implement your custom GUI. To display a user interface similar to HDRP shaders, use a MaterialUIBlockList.
        /// </summary>
        /// <param name="materialEditor">The current material editor.</param>
        /// <param name="props">The list of properties the material has.</param>
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            // always instanced
            SerializedProperty instancing = materialEditor.serializedObject.FindProperty("m_EnableInstancingVariants");
            instancing.boolValue = true;

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

            //materialEditor.PropertiesDefaultGUI(properties);
            //return;

            // TODO: scope
            var fieldWidth = EditorGUIUtility.fieldWidth;
            var labelWidth = EditorGUIUtility.labelWidth;

            // Copy of MaterialEditor.PropertiesDefaultGUI that excludes properties of PerRendererData
            materialEditor.SetDefaultGUIWidths();
            for (var i = 0; i < properties.Length; i++)
            {
                if ((properties[i].flags & (MaterialProperty.PropFlags.HideInInspector | MaterialProperty.PropFlags.PerRendererData)) != 0)
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

            materialEditor.ShaderProperty(drawOrder, Styles.drawOrderText);
            materialEditor.ShaderProperty(decalMeshBiasType, Styles.meshDecalBiasType);

            DecalMeshDepthBiasType decalBias = (DecalMeshDepthBiasType)decalMeshBiasType.intValue;
            switch (decalBias)
            {
                case DecalMeshDepthBiasType.DepthBias:
                    materialEditor.ShaderProperty(decalMeshDepthBias, Styles.meshDecalDepthBiasText);
                    break;
                case DecalMeshDepthBiasType.ViewBias:
                    materialEditor.ShaderProperty(decalMeshViewBias, Styles.meshDecalViewBiasText);
                    break;
            }
        }
    }
}
