using UnityEditor.ShaderGraph;
using System.Reflection;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    // Atlas samples directly; Local remaps 0-1 UVs into the atlas rect (unity_uie_UVRect) first.
    enum FilterUVSpace
    {
        Atlas,
        Local,
    }

    // Samples _MainTex at an arbitrary UV -- how a filter graph reads the pixels it filters.
    [SubTargetFilter(typeof(UniversalFilterSubTarget))]
    [Title("Input", "Filter", "Filter Input")]
    sealed class FilterInputNode : CodeFunctionNode
    {
        [SerializeField]
        FilterUVSpace m_UVSpace = FilterUVSpace.Atlas;

        [EnumControl("UV Space")]
        public FilterUVSpace uvSpace
        {
            get => m_UVSpace;
            set
            {
                if (m_UVSpace == value)
                    return;

                m_UVSpace = value;
                Dirty(ModificationScope.Graph);
            }
        }

        public FilterInputNode()
        {
            name = "Filter Input";
            synonyms = new string[] { "sample filter", "main tex", "previous render", "source" };
            UpdateNodeAfterDeserialization();
        }

        public override bool hasPreview => false;

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod(
                m_UVSpace == FilterUVSpace.Local ? "Unity_FilterInput_Local" : "Unity_FilterInput_Atlas",
                BindingFlags.Static | BindingFlags.NonPublic);
        }

        // Preview has no bound _MainTex; a checkerboard still shows distortion where a flat color would not.
        const string kPreviewCheckerboard =
@"    float2 checkerUV = floor(UV * 8.0);
    float checker = fmod(checkerUV.x + checkerUV.y, 2.0);
    Out = float4(checker, checker, checker, 1.0);";

        static string Unity_FilterInput_Atlas(
            [Slot(0, Binding.MeshUV0)] Vector2 UV,
            [Slot(1, Binding.None, ShaderStageCapability.Fragment)] out Vector4 Out)
        {
            Out = Vector4.one;
            return
@"
{
    // _MainTex only exists in the real filter pass; both preview macros must be guarded.
#if defined(SHADERGRAPH_PREVIEW) || defined(SHADERGRAPH_PREVIEW_MAIN)
" + kPreviewCheckerboard + @"
#else
    Out = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, UV);
#endif
}
";
        }

        // Local space: remap the 0-1 UV into the atlas sub-rect before sampling.
        static string Unity_FilterInput_Local(
            [Slot(0, Binding.MeshUV0)] Vector2 UV,
            [Slot(1, Binding.None, ShaderStageCapability.Fragment)] out Vector4 Out)
        {
            Out = Vector4.one;
            return
@"
{
#if defined(SHADERGRAPH_PREVIEW) || defined(SHADERGRAPH_PREVIEW_MAIN)
" + kPreviewCheckerboard + @"
#else
    float4 uvRect = unity_uie_UVRect[0];
    // Distortion pushing UV outside 0-1 would read a neighboring element's atlas region, whose
    // contents vary per platform; the half-texel inset keeps the bilinear taps inside the rect too.
    float2 halfTexel = 0.5 * _MainTex_TexelSize.xy;
    float2 atlasUV = clamp(UV * uvRect.zw + uvRect.xy,
                           uvRect.xy + halfTexel,
                           uvRect.xy + uvRect.zw - halfTexel);
    Out = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, atlasUV);
#endif
}
";
        }
    }

    // Surfaces _MainTex_TexelSize for aspect-correct local-space math.
    [SubTargetFilter(typeof(UniversalFilterSubTarget))]
    [Title("Input", "Filter", "Filter Texel Size")]
    sealed class FilterTexelSizeNode : CodeFunctionNode
    {
        public FilterTexelSizeNode()
        {
            name = "Filter Texel Size";
            synonyms = new string[] { "main tex texel size", "atlas texel size" };
            UpdateNodeAfterDeserialization();
        }

        public override bool hasPreview => false;

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_FilterTexelSize", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_FilterTexelSize(
            [Slot(0, Binding.None, ShaderStageCapability.Fragment)] out Vector4 Out)
        {
            Out = Vector4.one;
            return
@"
{
#if defined(SHADERGRAPH_PREVIEW) || defined(SHADERGRAPH_PREVIEW_MAIN)
    // 1x1 is a neutral preview stand-in.
    Out = float4(1, 1, 1, 1);
#else
    Out = _MainTex_TexelSize;
#endif
}
";
        }
    }
}
