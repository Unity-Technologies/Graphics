using System;
using UnityEditor.ShaderGraph.Drawing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    class PBRMasterGUI : ShaderGUI
    {
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {

            ShaderGraphPropertyDrawers.DrawShaderGraphGUI(materialEditor, props);
            // Change the GI emission flag and fix it up with emissive as black if necessary.
            materialEditor.LightmapEmissionFlagsProperty(MaterialEditor.kMiniTextureFieldLabelIndentLevel, true);
        }
    }
}
