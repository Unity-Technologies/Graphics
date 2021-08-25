using System.Runtime.CompilerServices;
using UnityEngine;

[assembly: InternalsVisibleTo("Unity.RenderPipelines.Universal.Editor")]

namespace UnityEditor.Rendering.Universal
{
    internal static class MaterialAccess
    {
        internal static int ReadMaterialRawRenderQueue(Material mat)
        {
            return mat.rawRenderQueue;
        }
    }
}
