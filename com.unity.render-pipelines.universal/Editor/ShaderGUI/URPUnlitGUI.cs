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
            Debug.Log("URPUnlitGUI Material Changed");
            if (material == null)
                throw new ArgumentNullException("material");

            SetMaterialKeywords(material);
        }
    }
} // namespace UnityEditor
