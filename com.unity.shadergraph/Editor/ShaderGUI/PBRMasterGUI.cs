using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    class PBRMasterGUI : ShaderGUI
    {
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
			materialEditor.PropertiesDefaultGUI(props);

            Material material = materialEditor.target as Material;

            if (materialEditor.EmissionEnabledProperty())
            {
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            } 
        }
    }
}
