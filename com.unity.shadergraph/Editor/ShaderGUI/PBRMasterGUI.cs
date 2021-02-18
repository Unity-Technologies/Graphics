using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    class PBRMasterGUI : ShaderGUI
    {
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            materialEditor.PropertiesDefaultGUI(props);

            // Change the GI emission flag and fix it up with emissive as black if necessary.
            materialEditor.LightmapEmissionFlagsProperty(MaterialEditor.kMiniTextureFieldLabelIndentLevel, true);
        }
    }
}
