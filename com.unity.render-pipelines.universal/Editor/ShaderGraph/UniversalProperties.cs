using System;
using UnityEditor;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.Rendering.Universal.ShaderGraph;


namespace UnityEditor.Rendering.Universal
{
    internal static class Property
    {
        public static string SpecularWorkflowMode(bool isShaderGraph)          { return isShaderGraph ? SG_SpecularWorkflowMode : HW_SpecularWorkflowMode; }
        public static string Surface(bool isShaderGraph)                       { return isShaderGraph ? SG_Surface : HW_Surface; }
        public static string Blend(bool isShaderGraph)                         { return isShaderGraph ? SG_Blend : HW_Blend; }
        public static string AlphaClip(bool isShaderGraph)                     { return isShaderGraph ? SG_AlphaClip : HW_AlphaClip; }
        public static string SrcBlend(bool isShaderGraph)                      { return isShaderGraph ? SG_SrcBlend : HW_SrcBlend; }
        public static string DstBlend(bool isShaderGraph)                      { return isShaderGraph ? SG_DstBlend : HW_DstBlend; }
        public static string ZWrite(bool isShaderGraph)                        { return isShaderGraph ? SG_ZWrite : HW_ZWrite; }
        public static string ZTest(bool isShaderGraph)                         { return isShaderGraph ? SG_ZTest : null; }   // no HW equivalent
        public static string Cull(bool isShaderGraph)                          { return isShaderGraph ? SG_Cull : HW_Cull; }
        public static string CastShadows(bool isShaderGraph)                   { return isShaderGraph ? SG_CastShadows : HW_CastShadows; }
        public static string ReceiveShadows(bool isShaderGraph)                { return isShaderGraph ? SG_ReceiveShadows : HW_ReceiveShadows; }
        public static string QueueOffset(bool isShaderGraph)                   { return isShaderGraph ? SG_QueueOffset : HW_QueueOffset; }

        // for hand-written shaders (Lit.shader, Unlit.shader, maybe others)
        public static readonly string HW_SpecularWorkflowMode = "_WorkflowMode";
        public static readonly string HW_Surface = "_Surface";
        public static readonly string HW_Blend = "_Blend";
        public static readonly string HW_AlphaClip = "_AlphaClip";
        public static readonly string HW_SrcBlend = "_SrcBlend";
        public static readonly string HW_DstBlend = "_DstBlend";
        public static readonly string HW_ZWrite = "_ZWrite";
        public static readonly string HW_Cull = "_Cull";
        public static readonly string HW_CastShadows = "_CastShadows";
        public static readonly string HW_ReceiveShadows = "_ReceiveShadows";
        public static readonly string HW_QueueOffset = "_QueueOffset";

        // for ShaderGraph shaders (renamed more uniquely to avoid potential naming collisions with HDRP properties and user properties)
        public static readonly string SG_SpecularWorkflowMode = "_URP_WorkflowMode";
        public static readonly string SG_Surface = "_URP_Surface";
        public static readonly string SG_Blend = "_URP_Blend";
        public static readonly string SG_AlphaClip = "_URP_AlphaClip";
        public static readonly string SG_SrcBlend = "_URP_SrcBlend";
        public static readonly string SG_DstBlend = "_URP_DstBlend";
        public static readonly string SG_ZTest = "_URP_ZTest";
        public static readonly string SG_ZWrite = "_URP_ZWrite";
        public static readonly string SG_Cull = "_URP_CullMode";
        public static readonly string SG_CastShadows = "_URP_CastShadows";
        public static readonly string SG_ReceiveShadows = "_URP_ReceiveShadows";
        public static readonly string SG_QueueOffset = "_URP_QueueOffset";

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
        // for hand-written shaders (Lit.shader, Unlit.shader, maybe others)
        public static readonly string HW_ReceiveShadowsOff = "_RECEIVE_SHADOWS_OFF";
        public static readonly string HW_Emission = "_EMISSION";
        public static readonly string HW_AlphaTestOn = "_ALPHATEST_ON";
        public static readonly string HW_SurfaceTypeTransparent = "_SURFACE_TYPE_TRANSPARENT";
        public static readonly string HW_AlphaPremultiplyOn = "_ALPHAPREMULTIPLY_ON";
        public static readonly string HW_AlphaModulateOn = "_ALPHAMODULATE_ON";

        // custom keywords for Lit.shader
        public static readonly string HW_NormalMap = "_NORMALMAP";

        // for ShaderGraph shaders (renamed more uniquely to avoid potential naming collisions with HDRP and user keywords)
    }

    internal static class UniversalMaterialInspectorUtilities
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
