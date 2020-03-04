using UnityEditor.ShaderGraph;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    static class UniversalKeywords
    {
        static class Descriptors
        {
            public static KeywordDescriptor Lightmap = new KeywordDescriptor()
            {
                displayName = "Lightmap",
                referenceName = "LIGHTMAP_ON",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Global,
            };

            public static KeywordDescriptor DirectionalLightmapCombined = new KeywordDescriptor()
            {
                displayName = "Directional Lightmap Combined",
                referenceName = "DIRLIGHTMAP_COMBINED",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Global,
            };

            public static KeywordDescriptor SampleGI = new KeywordDescriptor()
            {
                displayName = "Sample GI",
                referenceName = "_SAMPLE_GI",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.ShaderFeature,
                scope = KeywordScope.Global,
            };

            public static KeywordDescriptor MainLightShadows = new KeywordDescriptor()
            {
                displayName = "Main Light Shadows",
                referenceName = "_MAIN_LIGHT_SHADOWS",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Global,
            };

            public static KeywordDescriptor MainLightShadowsCascade = new KeywordDescriptor()
            {
                displayName = "Main Light Shadows Cascade",
                referenceName = "_MAIN_LIGHT_SHADOWS_CASCADE",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Global,
            };

            public static KeywordDescriptor AdditionalLights = new KeywordDescriptor()
            {
                displayName = "Additional Lights",
                referenceName = "_ADDITIONAL",
                type = KeywordType.Enum,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Global,
                entries = new KeywordEntry[]
                {
                    new KeywordEntry() { displayName = "Vertex", referenceName = "LIGHTS_VERTEX" },
                    new KeywordEntry() { displayName = "Fragment", referenceName = "LIGHTS" },
                    new KeywordEntry() { displayName = "Off", referenceName = "OFF" },
                }
            };

            public static KeywordDescriptor AdditionalLightShadows = new KeywordDescriptor()
            {
                displayName = "Additional Light Shadows",
                referenceName = "_ADDITIONAL_LIGHT_SHADOWS",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Global,
            };

            public static KeywordDescriptor ShadowsSoft = new KeywordDescriptor()
            {
                displayName = "Shadows Soft",
                referenceName = "_SHADOWS_SOFT",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Global,
            };

            public static KeywordDescriptor MixedLightingSubtractive = new KeywordDescriptor()
            {
                displayName = "Mixed Lighting Subtractive",
                referenceName = "_MIXED_LIGHTING_SUBTRACTIVE",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Global,
            };

            public static KeywordDescriptor SmoothnessChannel = new KeywordDescriptor()
            {
                displayName = "Smoothness Channel",
                referenceName = "_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.ShaderFeature,
                scope = KeywordScope.Global,
            };

            public static KeywordDescriptor ETCExternalAlpha = new KeywordDescriptor()
            {
                displayName = "ETC External Alpha",
                referenceName = "ETC1_EXTERNAL_ALPHA",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Global,
            };

            public static KeywordDescriptor ShapeLightType0 = new KeywordDescriptor()
            {
                displayName = "Shape Light Type 0",
                referenceName = "USE_SHAPE_LIGHT_TYPE_0",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Global,
            };

            public static KeywordDescriptor ShapeLightType1 = new KeywordDescriptor()
            {
                displayName = "Shape Light Type 1",
                referenceName = "USE_SHAPE_LIGHT_TYPE_1",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Global,
            };

            public static KeywordDescriptor ShapeLightType2 = new KeywordDescriptor()
            {
                displayName = "Shape Light Type 2",
                referenceName = "USE_SHAPE_LIGHT_TYPE_2",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Global,
            };

            public static KeywordDescriptor ShapeLightType3 = new KeywordDescriptor()
            {
                displayName = "Shape Light Type 3",
                referenceName = "USE_SHAPE_LIGHT_TYPE_3",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Global,
            };
        }

        public static KeywordCollection PBRForward = new KeywordCollection
        {
            { Descriptors.Lightmap },
            { Descriptors.DirectionalLightmapCombined },
            { Descriptors.MainLightShadows },
            { Descriptors.MainLightShadowsCascade },
            { Descriptors.AdditionalLights },
            { Descriptors.AdditionalLightShadows },
            { Descriptors.ShadowsSoft },
            { Descriptors.MixedLightingSubtractive },
        };

        public static KeywordCollection PBRMeta = new KeywordCollection
        {
            { Descriptors.SmoothnessChannel },
        };

        public static KeywordCollection Unlit = new KeywordCollection
        {
            { Descriptors.Lightmap },
            { Descriptors.DirectionalLightmapCombined },
            { Descriptors.SampleGI },
        };

        public static KeywordCollection SpriteLit = new KeywordCollection
        {
            { Descriptors.ETCExternalAlpha },
            { Descriptors.ShapeLightType0 },
            { Descriptors.ShapeLightType1 },
            { Descriptors.ShapeLightType2 },
            { Descriptors.ShapeLightType3 },
        };

        public static KeywordCollection ETCExternalAlpha = new KeywordCollection
        {
            { Descriptors.ETCExternalAlpha },
        };
    }
}
