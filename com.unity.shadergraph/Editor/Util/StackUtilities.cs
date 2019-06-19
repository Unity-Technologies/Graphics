using System.Linq;
using UnityEngine;
using UnityEditor.Graphing;
using System.Collections.Generic;
using System;
using UnityEditor.ShaderGraph.Drawing.Controls;

namespace UnityEditor.ShaderGraph
{
    class StackUtilities
    {
        public static void SetMaterialKeywords(IEnumerable<object> materials)
        {
            foreach( var mat in materials)
            {
                if ( mat is Material)
                {
                    SetMaterialKeywords(mat as Material);
                }
            }
        }

        public static void SetMaterialKeywords(Material material)
        {
            if (material.HasProperty("_VirtualTexturing"))
            {
                if (material.GetFloat("_VirtualTexturing") == 0.0f || !StackStatus.AllStacksValid(material))
                {
                    material.DisableKeyword("VIRTUAL_TEXTURES_BUILT");
                }
                else
                {
                    material.EnableKeyword("VIRTUAL_TEXTURES_BUILT");
                }
            }
        }
    }
}
