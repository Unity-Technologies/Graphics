using System;
using UnityEditor;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.Rendering.Universal.ShaderGraph;


namespace UnityEditor.Rendering.Universal
{
    internal static class Property
    {
        public static readonly string SpecularWorkflowMode = "_WorkflowMode";
        public static readonly string Surface = "_Surface";
        public static readonly string Blend = "_Blend";
        public static readonly string AlphaClip = "_AlphaClip";
        public static readonly string SrcBlend = "_SrcBlend";
        public static readonly string DstBlend = "_DstBlend";
        public static readonly string ZWrite = "_ZWrite";
        public static readonly string Cull = "_Cull";
        public static readonly string ReceiveShadows = "_ReceiveShadows";
        public static readonly string QueueOffset = "_QueueOffset";

        public static Vector1ShaderProperty WorkflowModeProperty(WorkflowMode workflowModeDefault)
        {
            return new Vector1ShaderProperty()
            {
                floatType = FloatType.Default,
                hidden = true,
                overrideHLSLDeclaration = true,
                hlslDeclarationOverride = HLSLDeclaration.DoNotDeclare,
                value = (float)((workflowModeDefault == WorkflowMode.MaterialChoice) ? WorkflowMode.Metallic : workflowModeDefault),
                displayName = "Workflow Mode",
                overrideReferenceName = SpecularWorkflowMode,
            };
        }

        //public static readonly string AlphaCutoff = "_Cutoff";
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
