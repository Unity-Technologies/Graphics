using System;
using UnityEngine;
using UnityEngine.Rendering;
using RenderQueue = UnityEngine.Rendering.RenderQueue;

namespace UnityEditor.Rendering.BuiltIn.ShaderGraph
{
    public class BuiltInUnlitGUI : BuiltInBaseShaderGUI
    {
        public static void UpdateMaterial(Material material)
        {
            SetupSurface(material);
        }
    }
}
