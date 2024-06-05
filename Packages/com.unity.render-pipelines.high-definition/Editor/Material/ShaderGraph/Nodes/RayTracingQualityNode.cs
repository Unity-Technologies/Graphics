using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.Rendering.HighDefinition
{
    [FormerName("UnityEditor.Rendering.HighDefinition.RayTracingNode")]
    class RayTracingQualityNode
    {
        private const string k_KeywordDefault = "RAYTRACING_SHADER_GRAPH_DEFAULT";
        private const string k_KeywordRaytraced = "RAYTRACING_SHADER_GRAPH_RAYTRACED";
        private const string k_KeywordPathtracer = "RAYTRACING_SHADER_GRAPH_PATHTRACED";

        public enum RayTracingQualityVariant
        {
            Default,
            Raytraced,
            Pathtraced
        }

        public static string RaytracingVariantKeyword(RayTracingQualityVariant variant)
        {
            switch (variant)
            {
                case RayTracingQualityVariant.Default: return k_KeywordDefault;
                case RayTracingQualityVariant.Raytraced: return k_KeywordRaytraced;
                case RayTracingQualityVariant.Pathtraced: return k_KeywordPathtracer;
                default: throw new ArgumentOutOfRangeException(nameof(variant));
            }
        }

        [BuiltinKeyword]
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        public static KeywordDescriptor GetRayTracingQualityKeyword()
        {
            return new KeywordDescriptor()
            {
                displayName = "Raytracing Quality",
                referenceName = "RAYTRACING_SHADER_GRAPH",
                type = KeywordType.Enum,
                definition = KeywordDefinition.Predefined,
                scope = KeywordScope.Global,
                value = 0,
                entries = new KeywordEntry[]
                {
                    new KeywordEntry("Default", "DEFAULT"),
                    new KeywordEntry("Raytraced", "RAYTRACED"),
                    new KeywordEntry("Pathtraced", "PATHTRACED"),
                },
            };
        }
    }
}
