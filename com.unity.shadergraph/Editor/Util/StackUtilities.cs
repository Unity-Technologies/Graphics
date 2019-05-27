using System.Linq;
using UnityEngine;
using UnityEditor.Graphing;
using System.Collections.Generic;
using System;
using UnityEditor.ShaderGraph.Drawing.Controls;

namespace UnityEditor.ShaderGraph
{
    //[Title("Input", "Texture", "Sample Stack")]
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
                    material.DisableKeyword("VT_ON");
                }
                else
                {
                    material.EnableKeyword("VT_ON");
                }
            }
        }
    }
}
