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
        public static readonly string ZWrite = "_ZWrite";
        public static readonly string CullMode = "_Cull";
        public static readonly string CastShadows = "_CastShadows";
        public static readonly string ReceiveShadows = "_ReceiveShadows";
        public static readonly string QueueOffset = "_QueueOffset";

        // for ShaderGraph shaders only
        public static readonly string ZTest = "_ZTest";
        public static readonly string ZWriteControl = "_ZWriteControl";

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
                overrideReferenceName = SpecularWorkflowMode,
            };
        }
    }

    internal static class Keyword
    {
        // for hand-written shaders (Lit.shader, Unlit.shader, maybe others)
        public static readonly string HW_ReceiveShadowsOff = ShaderKeywordStrings._RECEIVE_SHADOWS_OFF;
        public static readonly string HW_Emission = ShaderKeywordStrings._EMISSION;
        public static readonly string HW_AlphaTestOn = ShaderKeywordStrings._ALPHATEST_ON;
        public static readonly string HW_SurfaceTypeTransparent = ShaderKeywordStrings._SURFACE_TYPE_TRANSPARENT;
        public static readonly string HW_AlphaPremultiplyOn = ShaderKeywordStrings._ALPHAPREMULTIPLY_ON;
        public static readonly string HW_AlphaModulateOn = ShaderKeywordStrings._ALPHAMODULATE_ON;

        // custom keywords for Lit.shader
        public static readonly string HW_NormalMap = ShaderKeywordStrings._NORMALMAP;

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
