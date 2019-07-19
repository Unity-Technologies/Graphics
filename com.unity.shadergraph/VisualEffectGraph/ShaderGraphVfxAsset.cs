using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.ShaderGraph;

namespace UnityEditor.ShaderGraph.VisualEffectGraph
{

    class GraphFunction
    {
        public string code { get; set; }
    }

    class ShaderGraphVfxAsset : ScriptableObject
    {
        internal GraphCompilationResult compilationResult;
        public PortMetadata[] ports
        {
            get => compilationResult.ports;
        }

        public GraphFunction GenerateGraphFunction(PortMetadata[] includedPorts)
        {
            return default;
        }
    }
}
