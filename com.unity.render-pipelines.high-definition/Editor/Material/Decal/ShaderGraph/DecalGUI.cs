using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    class DecalGUI : ExpandableAreaMaterial
    {
        protected MaterialEditor m_MaterialEditor;

        protected MaterialProperty decalMeshDepthBias = new MaterialProperty();
        protected const string kDecalMeshDepthBias = "_DecalMeshDepthBias";

        protected MaterialProperty drawOrder = new MaterialProperty();
        protected const string kDrawOrder = "_DrawOrder";

        [Flags]
        enum Expandable : uint
        {
            Input = 1 << 0,
            Sorting = 1 << 1
        }
        protected override uint defaultExpandedState { get { return (uint)Expandable.Input; } }

        protected static class Styles
        {
            public static string InputsText = "Surface Inputs";
            public static string SortingText = "Sorting Inputs";

            public static GUIContent meshDecalDepthBiasText = new GUIContent("Mesh decal depth bias", "Adjust this to prevents z-fighting with the decal mesh.");
            public static GUIContent drawOrderText = new GUIContent("Draw order", "Controls the draw order of Decal Projectors.");
        }

        void FindMaterialProperties(MaterialProperty[] props)
        {
            decalMeshDepthBias = FindProperty(kDecalMeshDepthBias, props, true);
            drawOrder = FindProperty(kDrawOrder, props, true);

            // always instanced
            SerializedProperty instancing = m_MaterialEditor.serializedObject.FindProperty("m_EnableInstancingVariants");
            instancing.boolValue = true;
        }

        public void ShaderPropertiesGUI()
        {
            using (var header = new HeaderScope(Styles.SortingText, (uint)Expandable.Sorting, this))
            {
                if (header.expanded)
                {
                    m_MaterialEditor.ShaderProperty(drawOrder, Styles.drawOrderText);
                    m_MaterialEditor.ShaderProperty(decalMeshDepthBias, Styles.meshDecalDepthBiasText);
                }
            }
        }

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            m_MaterialEditor = materialEditor;

            FindMaterialProperties(props);

            // Copy/Paste of public void PropertiesDefaultGUI(MaterialProperty[] props) in MaterialEditor.cs to allow to customize inspector for
            // decal. We don't want to display GI option or instancing option and want to add tooltips to our parameters.
            m_MaterialEditor.SetDefaultGUIWidths();

            using (var header = new HeaderScope(Styles.InputsText, (uint)Expandable.Input, this))
            {
                if (header.expanded)
                {
                    // We do "- 2" as we always have draw order and decal mesh bias. Update this number if we change the number of mandatory parameters
                    for (var i = 0; i < props.Length - 2; i++)
                    {
                        if ((props[i].flags & (MaterialProperty.PropFlags.HideInInspector | MaterialProperty.PropFlags.PerRendererData)) != 0)
                            continue;

                        float h = m_MaterialEditor.GetPropertyHeight(props[i], props[i].displayName);
                        Rect r = EditorGUILayout.GetControlRect(true, h, EditorStyles.layerMaskField);

                        m_MaterialEditor.ShaderProperty(r, props[i], props[i].displayName);
                    }
                }
            }         

            ShaderPropertiesGUI();

            // We should always do this call at the end
            m_MaterialEditor.serializedObject.ApplyModifiedProperties();
        }
    }
}
