using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    static class UniversalRequiredFields
    {
        public static FieldDescriptor[] PBRForward = new FieldDescriptor[]
        {
            StructFields.Attributes.uv1,                            // needed for meta vertex position
            StructFields.Varyings.positionWS,
            StructFields.Varyings.normalWS,
            StructFields.Varyings.tangentWS,                        // needed for vertex lighting
            StructFields.Varyings.viewDirectionWS,
            UniversalStructFields.Varyings.lightmapUV,
            UniversalStructFields.Varyings.sh,
            UniversalStructFields.Varyings.fogFactorAndVertexLight, // fog and vertex lighting, vert input is dependency
            UniversalStructFields.Varyings.shadowCoord,             // shadow coord, vert input is dependency
        };

        public static FieldDescriptor[] PBRShadowCaster = new FieldDescriptor[]
        {
            StructFields.Attributes.normalOS,
        };

        public static FieldDescriptor[] PBRMeta = new FieldDescriptor[]
        {
            StructFields.Attributes.uv1,                            // needed for meta vertex position
            StructFields.Attributes.uv2,                            //needed for meta vertex position
        };

        public static FieldDescriptor[] SpriteLit = new FieldDescriptor[]
        {
            StructFields.Varyings.color,
            StructFields.Varyings.texCoord0,
            StructFields.Varyings.screenPosition,
        };

        public static FieldDescriptor[] SpriteNormal = new FieldDescriptor[]
        {
            StructFields.Varyings.normalWS,
            StructFields.Varyings.tangentWS,
        };

        public static FieldDescriptor[] SpriteForward = new FieldDescriptor[]
        {
            StructFields.Varyings.color,
            StructFields.Varyings.texCoord0,
        };

        public static FieldDescriptor[] SpriteUnlit = new FieldDescriptor[]
        {
            StructFields.Attributes.color,
            StructFields.Attributes.uv0,
            StructFields.Varyings.color,
            StructFields.Varyings.texCoord0,
        };
    }
}
