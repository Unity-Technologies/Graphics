using System;
using UnityEditor;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.Rendering.Universal.ShaderGraph;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    internal static class Property
    {
        public static readonly string SpecularWorkflowMode = "_WorkflowMode";
        public static readonly string SurfaceType = "_Surface";
        public static readonly string BlendMode = "_Blend";
        public static readonly string AlphaClip = "_AlphaClip";
        public static readonly string SrcBlend = "_SrcBlend";
        public static readonly string DstBlend = "_DstBlend";
        public static readonly string SrcBlendAlpha = "_SrcBlendAlpha";
        public static readonly string DstBlendAlpha = "_DstBlendAlpha";
        public static readonly string BlendModePreserveSpecular = "_BlendModePreserveSpecular";
        public static readonly string ZWrite = "_ZWrite";
        public static readonly string CullMode = "_Cull";
        public static readonly string CastShadows = "_CastShadows";
        public static readonly string ReceiveShadows = "_ReceiveShadows";
        public static readonly string QueueOffset = "_QueueOffset";

        // for ShaderGraph shaders only
        public static readonly string ZTest = "_ZTest";
        public static readonly string ZWriteControl = "_ZWriteControl";
        public static readonly string QueueControl = "_QueueControl";

        // Global Illumination requires some properties to be named specifically:
        public static readonly string EmissionMap = "_EmissionMap";
        public static readonly string EmissionColor = "_EmissionColor";
    }
}
