using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEditor;

namespace UnityEngine.ScriptableRenderLoop
{
    public class ShaderGeneratorMenu
    {
        [UnityEditor.MenuItem("Renderloop/Generate Shader Includes")]
        static void GenerateShaderIncludes()
        {
            CSharpToHLSL.GenerateAll();
        }
    }
}
