using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.Rendering.HighDefinition
{
    class RayTracingNode
    {
        private const string k_KeywordHigh = "RAYTRACING_SHADER_GRAPH_HIGH";
        private const string k_KeywordLow = "RAYTRACING_SHADER_GRAPH_LOW";

        public enum RaytracingVariant
        {
            High,
            Low
        }

        public static string RaytracingVariantKeyword(RaytracingVariant variant)
        {
            switch (variant)
            {
                case RaytracingVariant.High: return k_KeywordHigh;
                case RaytracingVariant.Low: return k_KeywordLow;
                default: throw new ArgumentOutOfRangeException(nameof(variant));
            }
        }

        [BuiltinKeyword]
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        public static KeywordDescriptor GetRayTracingKeyword()
        {
            return new KeywordDescriptor()
            {
                displayName = "Raytracing",
                referenceName = "RAYTRACING_SHADER_GRAPH",
                type = KeywordType.Enum,
                definition = KeywordDefinition.Predefined,
                scope = KeywordScope.Global,
                value = 0,
                entries = new KeywordEntry[]
                {
                    new KeywordEntry("High", "HIGH"),
                    new KeywordEntry("Low", "LOW"),
                },
            };
        }
    }
}
