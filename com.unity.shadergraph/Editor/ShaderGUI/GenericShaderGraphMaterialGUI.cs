using System;
using UnityEditor.ShaderGraph.Drawing;
using UnityEngine;
using UnityEditor;

namespace UnityEditor.ShaderGraph
{
    // Used by the Material Inspector to draw UI for shader graph based materials, when no custom Editor GUI has been specified
    class GenericShaderGraphMaterialGUI : ShaderGUI
    {
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            ShaderGraphPropertyDrawers.DrawShaderGraphGUI(materialEditor, props);
        }
    }
}
