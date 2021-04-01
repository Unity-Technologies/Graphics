using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor
{
    // Used for ShaderGraph Unlit shaders
    class URPUnlitGUI : BaseShaderGUI
    {
        public static void UpdateMaterial(Material material)
        {
            BaseShaderGUI.SetMaterialKeywords(material);
        }

        public override void MaterialChanged(Material material)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            UpdateMaterial(material);
        }

        // material main surface inputs
        public override void DrawSurfaceInputs(Material material)
        {
            DrawShaderGraphProperties(material);
        }
    }
} // namespace UnityEditor
