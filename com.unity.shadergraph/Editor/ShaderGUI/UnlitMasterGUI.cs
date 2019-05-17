using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    public class UnlitMasterGUI : ShaderGUI
    {
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
			materialEditor.PropertiesDefaultGUI(props);
            // TODO: Add something here
        }
    }
}
