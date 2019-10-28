using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    static class UniversalRequiredFields
    {
        public static IField[] PBRForward = new IField[]
        {
            StructFields.Attributes.uv1,                            // needed for meta vertex position
            StructFields.Varyings.positionWS,
            StructFields.Varyings.normalWS,
            StructFields.Varyings.tangentWS,                        // needed for vertex lighting
            StructFields.Varyings.bitangentWS,
            StructFields.Varyings.viewDirectionWS,
            UniversalStructFields.Varyings.lightmapUV,
            UniversalStructFields.Varyings.sh,
            UniversalStructFields.Varyings.fogFactorAndVertexLight, // fog and vertex lighting, vert input is dependency
            UniversalStructFields.Varyings.shadowCoord,             // shadow coord, vert input is dependency
        };

        public static IField[] PBRShadowCaster = new IField[]
        {
            StructFields.Attributes.normalOS,
        };

        public static IField[] PBRMeta = new IField[]
        {
            StructFields.Attributes.uv1,                            // needed for meta vertex position
            StructFields.Attributes.uv2,                            //needed for meta vertex position
        };

        public static IField[] SpriteLit = new IField[]
        {
            StructFields.Varyings.color,
            StructFields.Varyings.texCoord0,
            StructFields.Varyings.screenPosition,
        };

        public static IField[] SpriteNormal = new IField[]
        {
            StructFields.Varyings.normalWS,
            StructFields.Varyings.tangentWS,
            StructFields.Varyings.bitangentWS,
        };

        public static IField[] SpriteForward = new IField[]
        {
            StructFields.Varyings.color,
            StructFields.Varyings.texCoord0,
        };

        public static IField[] SpriteUnlit = new IField[]
        {
            StructFields.Attributes.color,
            StructFields.Attributes.uv0,
            StructFields.Varyings.color,
            StructFields.Varyings.texCoord0,
        };
    }
}
