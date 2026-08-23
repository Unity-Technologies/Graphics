using UnityEditor.ShaderGraph.Internal;
using UnityEditor.ShaderGraph;
namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    // Fullscreen-quad vertex/varying contract for a filter pass: no per-element mesh fields (clip
    // rect, tint, normal/tangent, packed IDs) like UISubTarget, and no rectIndex like
    // UnityUIEFilter.cginc's FilterVertexInput -- the atlas rect is always read via
    // unity_uie_UVRect[0] (FilterUVRect.hlsl), never per-vertex.
    internal static class FilterStructs
    {
        public static StructDescriptor Attributes = new StructDescriptor()
        {
            name = "Attributes",
            packFields = false,
            fields = new FieldDescriptor[]
            {
                StructFields.Attributes.positionOS,
                StructFields.Attributes.uv0,
                StructFields.Attributes.instanceID,
                StructFields.Attributes.vertexID,
            }
        };

        public static StructDescriptor Varyings = new StructDescriptor()
        {
            name = "Varyings",
            packFields = true,
            populateWithCustomInterpolators = false,
            fields = new[]
            {
                StructFields.Varyings.positionCS,
                StructFields.Varyings.texCoord0,
                StructFields.Varyings.instanceID,
                StructFields.Varyings.vertexID,
                StructFields.Varyings.stereoTargetEyeIndexAsBlendIdx0,
                StructFields.Varyings.stereoTargetEyeIndexAsRTArrayIdx,
            }
        };
    }
}
