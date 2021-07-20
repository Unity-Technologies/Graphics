//#if HAS_VFX_GRAPH
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.VFX;
using UnityEditor.Rendering.Universal.ShaderGraph;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    //TODOPAUL : This ~is~was a copy/past from VFXHDRPSubTarget
    [InitializeOnLoad]
    static class VFXUniversalSubTarget
    {
        internal const string Inspector = "Rendering.TODOPAUL.VFXShaderGraphGUI";
    }
}
//#endif
