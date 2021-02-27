using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor
{
    // Used for ShaderGraph Lit shaders
    class URPLitGUI : BaseShaderGUI
    {
        public override void MaterialChanged(Material material)
        {
            Debug.Log("URPLitGUI Material Changed");
        }
    }
} // namespace UnityEditor
