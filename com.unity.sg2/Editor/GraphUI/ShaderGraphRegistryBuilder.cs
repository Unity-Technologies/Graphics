using System;
using System.Linq;
using UnityEditor.ShaderGraph.Defs;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;

namespace UnityEditor.ShaderGraph.GraphUI
{
    /// <summary>
    /// Creates and sets up a shader graph Registry.
    /// </summary>
    internal static class ShaderGraphRegistryBuilder
    {
        public static NodeUIInfo CreateNodeUIInfo()
        {
            return ShaderGraphRegistry.Instance.NodeUIInfo;
        }

        public static Registry CreateDefaultRegistry()
        {
            return ShaderGraphRegistry.Instance.Registry;
        }
    }
}
