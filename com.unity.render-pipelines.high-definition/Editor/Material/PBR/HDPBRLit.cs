using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    class HDPBRLitGUI : ShaderGUI
    {
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            materialEditor.PropertiesDefaultGUI(props);

            EmissionUIBlock.BakedEmissionEnabledProperty(materialEditor);

            // Make sure all selected materials are initialized.
            string materialTag = "MotionVector";
            foreach (var obj in materialEditor.targets)
            {
                var material = (Material)obj;
                string tag = material.GetTag(materialTag, false, "Nothing");
                if (tag == "Nothing")
                {
                    material.SetShaderPassEnabled(HDShaderPassNames.s_MotionVectorsStr, false);
                    material.SetOverrideTag(materialTag, "User");
                }
            }

            {
                // If using multi-select, apply toggled material to all materials.
                bool enabled = ((Material)materialEditor.target).GetShaderPassEnabled(HDShaderPassNames.s_MotionVectorsStr);
                EditorGUI.BeginChangeCheck();
                enabled = EditorGUILayout.Toggle("Motion Vector For Vertex Animation", enabled);
                if (EditorGUI.EndChangeCheck())
                {
                    foreach (var obj in materialEditor.targets)
                    {
                        var material = (Material)obj;
                        material.SetShaderPassEnabled(HDShaderPassNames.s_MotionVectorsStr, enabled);
                    }
                }
            }

            if (DiffusionProfileMaterialUI.IsSupported(materialEditor))
                DiffusionProfileMaterialUI.OnGUI(materialEditor, FindProperty("_DiffusionProfileAsset", props), FindProperty("_DiffusionProfileHash", props), 0);
        }
    }
}
