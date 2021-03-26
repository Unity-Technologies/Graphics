using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor
{
    // Used for ShaderGraph Lit shaders
    class URPUnlitGUI : BaseShaderGUI
    {
        public override void MaterialChanged(Material material)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            SetMaterialKeywords(material);
        }

        // material main surface inputs
        public override void DrawSurfaceInputs(Material material)
        {
            DrawShaderGraphProperties(material);
        }
    }
} // namespace UnityEditor
