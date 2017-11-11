using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    // A Material can be authored from the shader graph or by hand. When written by hand we need to provide an inspector.
    // Such a Material will share some properties between it various variant (shader graph variant or hand authored variant).
    // This is the purpose of BaseLitGUI. It contain all properties that are common to all Material based on Lit template.
    // For the default hand written Lit material see LitUI.cs that contain specific properties for our default implementation.
    public abstract class BaseHairGUI : BaseUnlitGUI
    {
        protected static class StylesBaseLit
        {
            public static GUIContent doubleSidedMirrorEnableText = new GUIContent("Mirror normal", "This will mirror the normal with vertex normal plane if enabled, else flip the normal");
            public static GUIContent depthOffsetEnableText = new GUIContent("Enable Depth Offset", "EnableDepthOffset on this shader (Use with heightmap)");

            // Material ID
            public static GUIContent materialIDText = new GUIContent("Material type", "Subsurface Scattering: enable for translucent materials such as skin, vegetation, fruit, marble, wax and milk.");

            // Wind
            public static GUIContent windText = new GUIContent("Enable Wind");
            public static GUIContent windInitialBendText = new GUIContent("Initial Bend");
            public static GUIContent windStiffnessText = new GUIContent("Stiffness");
            public static GUIContent windDragText = new GUIContent("Drag");
            public static GUIContent windShiverDragText = new GUIContent("Shiver Drag");
            public static GUIContent windShiverDirectionalityText = new GUIContent("Shiver Directionality");

            public static string vertexAnimation = "Vertex Animation";
        }

        protected MaterialProperty doubleSidedMirrorEnable = null;
        protected const string kDoubleSidedMirrorEnable = "_DoubleSidedMirrorEnable";
        protected MaterialProperty depthOffsetEnable = null;
        protected const string kDepthOffsetEnable = "_DepthOffsetEnable";

        // Properties
        // Material ID
        protected MaterialProperty materialID = null;
        protected const string kMaterialID = "_MaterialID";

        // Wind
        protected MaterialProperty windEnable = null;
        protected const string kWindEnabled = "_EnableWind";
        protected MaterialProperty windInitialBend = null;
        protected const string kWindInitialBend = "_InitialBend";
        protected MaterialProperty windStiffness = null;
        protected const string kWindStiffness = "_Stiffness";
        protected MaterialProperty windDrag = null;
        protected const string kWindDrag = "_Drag";
        protected MaterialProperty windShiverDrag = null;
        protected const string kWindShiverDrag = "_ShiverDrag";
        protected MaterialProperty windShiverDirectionality = null;
        protected const string kWindShiverDirectionality = "_ShiverDirectionality";

        protected override void FindBaseMaterialProperties(MaterialProperty[] props)
        {
            base.FindBaseMaterialProperties(props);

            doubleSidedMirrorEnable = FindProperty(kDoubleSidedMirrorEnable, props);
            depthOffsetEnable = FindProperty(kDepthOffsetEnable, props);

            // MaterialID
            materialID = FindProperty(kMaterialID, props, false);

            // Wind
            windEnable = FindProperty(kWindEnabled, props);
            windInitialBend = FindProperty(kWindInitialBend, props);
            windStiffness = FindProperty(kWindStiffness, props);
            windDrag = FindProperty(kWindDrag, props);
            windShiverDrag = FindProperty(kWindShiverDrag, props);
            windShiverDirectionality = FindProperty(kWindShiverDirectionality, props);
        }

        protected override void BaseMaterialPropertiesGUI()
        {
            base.BaseMaterialPropertiesGUI();

            // This follow double sided option
            if (doubleSidedEnable.floatValue > 0.0f)
            {
                EditorGUI.indentLevel++;
                m_MaterialEditor.ShaderProperty(doubleSidedMirrorEnable, StylesBaseLit.doubleSidedMirrorEnableText);
                EditorGUI.indentLevel--;
            }

            if (materialID != null)
                m_MaterialEditor.ShaderProperty(materialID, StylesBaseLit.materialIDText);

            EditorGUI.indentLevel--;
        }

        protected override void VertexAnimationPropertiesGUI()
        {
            GUILayout.Label(StylesBaseLit.vertexAnimation, EditorStyles.boldLabel);

            EditorGUI.indentLevel++;

            m_MaterialEditor.ShaderProperty(windEnable, StylesBaseLit.windText);
            if (!windEnable.hasMixedValue && windEnable.floatValue > 0.0f)
            {
                EditorGUI.indentLevel++;
                m_MaterialEditor.ShaderProperty(windInitialBend, StylesBaseLit.windInitialBendText);
                m_MaterialEditor.ShaderProperty(windStiffness, StylesBaseLit.windStiffnessText);
                m_MaterialEditor.ShaderProperty(windDrag, StylesBaseLit.windDragText);
                m_MaterialEditor.ShaderProperty(windShiverDrag, StylesBaseLit.windShiverDragText);
                m_MaterialEditor.ShaderProperty(windShiverDirectionality, StylesBaseLit.windShiverDirectionalityText);
                EditorGUI.indentLevel--;
            }

            EditorGUI.indentLevel--;
        }

        // All Setup Keyword functions must be static. It allow to create script to automatically update the shaders with a script if ocde change
        static public void SetupBaseHairKeywords(Material material)
        {
            SetupBaseUnlitKeywords(material);

            bool doubleSidedEnable = material.GetFloat(kDoubleSidedEnable) > 0.0f;
            bool doubleSidedMirrorEnable = material.GetFloat(kDoubleSidedMirrorEnable) > 0.0f;

            if (doubleSidedEnable)
            {
                if (doubleSidedMirrorEnable)
                {
                    // Mirror mode (in tangent space)
                    material.SetVector("_DoubleSidedConstants", new Vector4(1.0f, 1.0f, -1.0f, 0.0f));
                }
                else
                {
                    // Flip mode (in tangent space)
                    material.SetVector("_DoubleSidedConstants", new Vector4(-1.0f, -1.0f, -1.0f, 0.0f));
                }
            }

            bool depthOffsetEnable = material.GetFloat(kDepthOffsetEnable) > 0.0f;
            SetKeyword(material, "_DEPTHOFFSET_ON", depthOffsetEnable);

            bool windEnabled = material.GetFloat(kWindEnabled) > 0.0f;
            SetKeyword(material, "_VERTEX_WIND", windEnabled);
        }
    }
} // namespace UnityEditor
