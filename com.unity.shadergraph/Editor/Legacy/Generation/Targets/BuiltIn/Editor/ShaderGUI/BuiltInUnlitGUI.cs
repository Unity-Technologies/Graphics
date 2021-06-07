using UnityEngine;

namespace UnityEditor.Rendering.BuiltIn.ShaderGraph
{
    // Currently this is just the base shader gui, but was put in place in case they're separate later
    public class BuiltInUnlitGUI : BuiltInBaseShaderGUI
    {
        public static void UpdateMaterial(Material material)
        {
            SetupSurface(material);
        }
    }
}
