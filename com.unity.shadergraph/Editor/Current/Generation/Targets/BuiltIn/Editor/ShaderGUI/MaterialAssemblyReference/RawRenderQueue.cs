using System.Runtime.CompilerServices;
using UnityEngine;

[assembly: InternalsVisibleTo("Unity.ShaderGraph.Editor")]

namespace UnityEditor.Rendering.BuiltIn.ShaderGraph
{
    internal static class MaterialAccess
    {
        internal static int ReadMaterialRawRenderQueue(Material mat)
        {
            return mat.rawRenderQueue;
        }
    }
}
