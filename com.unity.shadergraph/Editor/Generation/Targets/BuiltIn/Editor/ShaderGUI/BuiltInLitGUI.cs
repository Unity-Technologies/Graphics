using System;
using UnityEngine;
using UnityEngine.Rendering;
using RenderQueue = UnityEngine.Rendering.RenderQueue;

namespace UnityEditor.Rendering.BuiltIn.ShaderGraph
{
    public class BuiltInLitGUI : BuiltInBaseShaderGUI
    {
        public static void UpdateMaterial(Material material)
        {
            SetupSurface(material);
        }
    }
}
