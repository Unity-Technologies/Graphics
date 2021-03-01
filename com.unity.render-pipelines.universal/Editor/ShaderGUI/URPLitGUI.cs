using System;
using UnityEditor.Rendering.Universal.ShaderGUI;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor
{
    // Used for ShaderGraph Lit shaders
    class URPLitGUI : BaseShaderGUI
    {
        private LitGUI.LitProperties litProperties;

        // collect properties from the material properties
        public override void FindProperties(MaterialProperty[] properties)
        {
            base.FindProperties(properties);
            litProperties = new LitGUI.LitProperties(properties);
        }

        public override void MaterialChanged(Material material)
        {
            Debug.Log("URPLitGUI Material Changed");

            if (material == null)
                throw new ArgumentNullException("material");

            SetMaterialKeywords(material, LitGUI.SetMaterialKeywords); //, LitDetailGUI.SetMaterialKeywords);
        }

        public override void DrawSurfaceOptions(Material material)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            // Use default labelWidth
            EditorGUIUtility.labelWidth = 0f;

            // Detect any changes to the material
            EditorGUI.BeginChangeCheck();
            if (litProperties.workflowMode != null)
            {
                DoPopup(LitGUI.Styles.workflowModeText, litProperties.workflowMode, Enum.GetNames(typeof(LitGUI.WorkflowMode)));
            }
            if (EditorGUI.EndChangeCheck())
            {
                foreach (var obj in blendModeProp.targets)
                    MaterialChanged((Material)obj);
            }
            base.DrawSurfaceOptions(material);
        }

        // material main surface inputs
        public override void DrawSurfaceInputs(Material material)
        {
            DrawShaderGraphProperties(material);

            /*
                        base.DrawSurfaceInputs(material);
                        LitGUI.Inputs(litProperties, materialEditor, material);
                        DrawEmissionProperties(material, true);
                        DrawTileOffset(materialEditor, baseMapProp);
            */
        }
    }
} // namespace UnityEditor
