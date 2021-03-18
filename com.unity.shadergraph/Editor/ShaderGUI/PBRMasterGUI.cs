using System;
using UnityEditor.ShaderGraph.Drawing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    class PBRMasterGUI : ShaderGUI
    {
        static readonly int k_EmissionColor = Shader.PropertyToID("_EmissionColor");

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            ShaderGraphPropertyDrawers.DrawShaderGraphGUI(materialEditor, props);

            Material material = materialEditor.target as Material;

            if (materialEditor.EmissionEnabledProperty())
            {
                var property = material?.GetColor(k_EmissionColor);
                // Change the GI emission flag and fix it up with emissive as black if necessary.
                if(property != null)
                    materialEditor.LightmapEmissionFlagsProperty(1, true);
            }
        }
    }
}
