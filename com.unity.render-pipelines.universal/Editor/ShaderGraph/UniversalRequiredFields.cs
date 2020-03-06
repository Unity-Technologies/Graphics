using UnityEditor.ShaderGraph;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    static class UniversalRequiredFields
    {
        public static FieldCollection PBRForward = new FieldCollection()
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

        public static FieldCollection PBRShadowCaster = new FieldCollection()
        {
            StructFields.Attributes.normalOS,
        };

        public static FieldCollection PBRMeta = new FieldCollection()
        {
            StructFields.Attributes.uv1,                            // needed for meta vertex position
            StructFields.Attributes.uv2,                            //needed for meta vertex position
        };

        public static FieldCollection SpriteLit = new FieldCollection()
        {
            StructFields.Varyings.color,
            StructFields.Varyings.texCoord0,
            StructFields.Varyings.screenPosition,
        };

        public static FieldCollection SpriteNormal = new FieldCollection()
        {
            StructFields.Varyings.normalWS,
            StructFields.Varyings.tangentWS,
        };

        public static FieldCollection SpriteForward = new FieldCollection()
        {
            StructFields.Varyings.color,
            StructFields.Varyings.texCoord0,
        };

        public static FieldCollection SpriteUnlit = new FieldCollection()
        {
            StructFields.Attributes.color,
            StructFields.Attributes.uv0,
            StructFields.Varyings.color,
            StructFields.Varyings.texCoord0,
        };
    }
}
