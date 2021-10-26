using UnityEditor.Rendering.BuiltIn.ShaderGraph;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.Rendering.BuiltIn
{
    internal static class Property
    {
        public static string SpecularWorkflowMode() { return SG_SpecularWorkflowMode; }
        public static string Surface() { return SG_Surface; }
        public static string Blend() { return SG_Blend; }
        public static string AlphaClip() { return SG_AlphaClip; }
        public static string SrcBlend() { return SG_SrcBlend; }
        public static string DstBlend() { return SG_DstBlend; }
        public static string ZWrite() { return SG_ZWrite; }
        public static string ZWriteControl() { return SG_ZWriteControl; }
        public static string ZTest() { return SG_ZTest; }   // no HW equivalent
        public static string Cull() { return SG_Cull; }
        public static string CastShadows() { return SG_CastShadows; }
        public static string ReceiveShadows() { return SG_ReceiveShadows; }
        public static string QueueOffset() { return SG_QueueOffset; }
        public static string QueueControl() { return SG_QueueControl; }

        // for shadergraph shaders (renamed more uniquely to avoid potential naming collisions with HDRP properties and user properties)
        public static readonly string SG_SpecularWorkflowMode = "_BUILTIN_WorkflowMode";
        public static readonly string SG_Surface = "_BUILTIN_Surface";
        public static readonly string SG_Blend = "_BUILTIN_Blend";
        public static readonly string SG_AlphaClip = "_BUILTIN_AlphaClip";
        public static readonly string SG_SrcBlend = "_BUILTIN_SrcBlend";
        public static readonly string SG_DstBlend = "_BUILTIN_DstBlend";
        public static readonly string SG_ZWrite = "_BUILTIN_ZWrite";
        public static readonly string SG_ZWriteControl = "_BUILTIN_ZWriteControl";
        public static readonly string SG_ZTest = "_BUILTIN_ZTest";
        public static readonly string SG_Cull = "_BUILTIN_CullMode";
        public static readonly string SG_CastShadows = "_BUILTIN_CastShadows";
        public static readonly string SG_ReceiveShadows = "_BUILTIN_ReceiveShadows";
        public static readonly string SG_QueueOffset = "_BUILTIN_QueueOffset";
        public static readonly string SG_QueueControl = "_BUILTIN_QueueControl";

        // Global Illumination requires some properties to be named specifically:
        public static readonly string EmissionMap = "_EmissionMap";
        public static readonly string EmissionColor = "_EmissionColor";

        public static Vector1ShaderProperty WorkflowModeProperty(WorkflowMode workflowModeDefault)
        {
            return new Vector1ShaderProperty()
            {
                floatType = FloatType.Default,
                hidden = true,
                overrideHLSLDeclaration = true,
                hlslDeclarationOverride = HLSLDeclaration.DoNotDeclare,
                value = (float)workflowModeDefault,
                displayName = "Workflow Mode",
                overrideReferenceName = SG_SpecularWorkflowMode,
            };
        }
    }

    internal static class Keyword
    {
        // for ShaderGraph shaders (renamed more uniquely to avoid potential naming collisions with HDRP and user keywords).
        // These should be used to control the above (currently in the template)
        public static readonly string SG_ReceiveShadowsOff = "_BUILTIN_RECEIVE_SHADOWS_OFF";
        public static readonly string SG_Emission = "_BUILTIN_EMISSION";
        public static readonly string SG_AlphaTestOn = "_BUILTIN_ALPHATEST_ON";
        public static readonly string SG_AlphaClip = "_BUILTIN_AlphaClip";
        public static readonly string SG_SurfaceTypeTransparent = "_BUILTIN_SURFACE_TYPE_TRANSPARENT";
        public static readonly string SG_AlphaPremultiplyOn = "_BUILTIN_ALPHAPREMULTIPLY_ON";
        public static readonly string SG_AlphaModulateOn = "_BUILTIN_ALPHAMODULATE_ON";
    }

    internal static class BuiltInMaterialInspectorUtilities
    {
        internal static void AddFloatProperty(this PropertyCollector collector, string referenceName, float defaultValue, HLSLDeclaration declarationType = HLSLDeclaration.DoNotDeclare)
        {
            collector.AddShaderProperty(new Vector1ShaderProperty
            {
                floatType = FloatType.Default,
                hidden = true,
                overrideHLSLDeclaration = true,
                hlslDeclarationOverride = declarationType,
                value = defaultValue,
                overrideReferenceName = referenceName,
            });
        }
    }
}
