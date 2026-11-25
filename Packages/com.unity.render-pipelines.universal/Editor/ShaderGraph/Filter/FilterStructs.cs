using UnityEditor.ShaderGraph.Internal;
using UnityEditor.ShaderGraph;
namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    // Fullscreen-quad vertex/varying contract for a filter pass: no per-element mesh fields (clip
    // rect, tint, normal/tangent, packed IDs) like UISubTarget, and no rectIndex like
    // UnityUIEFilter.cginc's FilterVertexInput -- the atlas rect is always read via
    // GetFilterUVRect(0) (UnityUIEFilter.cginc, through FilterShim.hlsl), never per-vertex.
    internal static class FilterStructs
    {
        // The instanced draw has no vertex buffer: UIEGetFilterVertex builds each quad from SV_VertexID
        const string kWithoutInstancing = "!defined(UNITY_INSTANCING_ENABLED)";
        public static FieldDescriptor positionOS = new FieldDescriptor(StructFields.Attributes.name, "positionOS", "", ShaderValueType.Float3, "POSITION", kWithoutInstancing);
        public static FieldDescriptor uv0 = new FieldDescriptor(StructFields.Attributes.name, "uv0", "", ShaderValueType.Float4, "TEXCOORD0", kWithoutInstancing);

        public static StructDescriptor Attributes = new StructDescriptor()
        {
            name = "Attributes",
            packFields = false,
            fields = new FieldDescriptor[]
            {
                positionOS,
                uv0,
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
