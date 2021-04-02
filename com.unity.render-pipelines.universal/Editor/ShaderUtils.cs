using System;
using UnityEngine;
using UnityEditor;

namespace Unity.Rendering.Universal // Unity.RenderPipelines.Universal.Editor
{
    public static class ShaderUtils
    {
        public static void ResetMaterialKeywords(Material material)
        {
            var sgTargetId = material.GetTag("ShaderGraphTargetId", false, null);
            if (sgTargetId == "UniversalLitSubTarget")
            {
                URPLitGUI.UpdateMaterial(material);
            }
            else if (sgTargetId == "UniversalUnlitSubTarget")
            {
                URPUnlitGUI.UpdateMaterial(material);
            }
            else
            {
                // TODO: handle non shadergraph shaders here, if they need keyword resets
                Debug.LogWarning("Unhandled material in ResetMaterialKeywords: " + material.name);
            }
        }
    }
}
