using UnityEditor.ShaderGraph;
using System.Reflection;
using UnityEngine;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    // Companion to Filter Input for filters that manipulate UV before sampling (multi-tap blur, swirl
    // distortion). Filter Input's UV is already positioned in this element's sub-rect of the shared
    // atlas (unity_uie_UVRect, see FilterUVRect.hlsl), so manipulating it directly risks bleeding into
    // a neighboring element's region once an atlas holds more than one element. This node exposes only
    // the "normalize to local 0-1 space" half; feed the distorted result into Filter Input's UV socket
    // with "UV Space" set to Local, which remaps back to atlas space internally (FilterInputNode.cs).
    [SubTargetFilter(typeof(UniversalFilterSubTarget))]
    [Title("Input", "Filter", "Normalize Filter UV")]
    sealed class NormalizeFilterUVNode : CodeFunctionNode
    {
        public NormalizeFilterUVNode()
        {
            name = "Normalize Filter UV";
            synonyms = new string[] { "atlas uv", "local uv", "unpack filter uv" };
            UpdateNodeAfterDeserialization();
        }

        public override bool hasPreview => false;

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_NormalizeFilterUV", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_NormalizeFilterUV(
            [Slot(0, Binding.MeshUV0)] Vector2 UV,
            [Slot(1, Binding.None, ShaderStageCapability.Fragment)] out Vector2 Out)
        {
            Out = Vector2.zero;
            return
@"
{
#if defined(SHADERGRAPH_PREVIEW) || defined(SHADERGRAPH_PREVIEW_MAIN)
    // No real atlas rect exists in preview -- treat the mesh UV as already local (rect = full 0-1),
    // same convention as Filter Input's own preview fallback.
    Out = UV;
#else
    float4 uvRect = unity_uie_UVRect[0];
    Out = (UV - uvRect.xy) / uvRect.zw;
#endif
}
";
        }
    }
}
