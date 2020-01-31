using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    static class HDKeywords
    {
        public static class Descriptors
        {
            public static KeywordDescriptor WriteNormalBuffer = new KeywordDescriptor()
            {
                displayName = "Write Normal Buffer",
                referenceName = "WRITE_NORMAL_BUFFER",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Global,
            };

            public static KeywordDescriptor WriteMsaaDepth = new KeywordDescriptor()
            {
                displayName = "Write MSAA Depth",
                referenceName = "WRITE_MSAA_DEPTH",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Global,
            };

            public static KeywordDescriptor DebugDisplay = new KeywordDescriptor()
            {
                displayName = "Debug Display",
                referenceName = "DEBUG_DISPLAY",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Global,
            };

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

            public static KeywordDescriptor DynamicLightmap = new KeywordDescriptor()
            {
                displayName = "Dynamic Lightmap",
                referenceName = "DYNAMICLIGHTMAP_ON",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Global,
            };

            public static KeywordDescriptor ShadowsShadowmask = new KeywordDescriptor()
            {
                displayName = "Shadows Shadowmask",
                referenceName = "SHADOWS_SHADOWMASK",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Global,
            };

            public static KeywordDescriptor DiffuseLightingOnly = new KeywordDescriptor()
            {
                displayName = "Diffuse Lighting Only",
                referenceName = "DIFFUSE_LIGHTING_ONLY",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Global,
            };

            public static KeywordDescriptor LightLayers = new KeywordDescriptor()
            {
                displayName = "Light Layers",
                referenceName = "LIGHT_LAYERS",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Global,
            };

            public static KeywordDescriptor Decals = new KeywordDescriptor()
            {
                displayName = "Decals",
                referenceName = "DECALS",
                type = KeywordType.Enum,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Global,
                entries = new KeywordEntry[]
                {
                    new KeywordEntry() { displayName = "Off", referenceName = "OFF" },
                    new KeywordEntry() { displayName = "3RT", referenceName = "3RT" },
                    new KeywordEntry() { displayName = "4RT", referenceName = "4RT" },
                }
            };

            public static KeywordDescriptor LodFadeCrossfade = new KeywordDescriptor()
            {
                displayName = "LOD Fade Crossfade",
                referenceName = "LOD_FADE_CROSSFADE",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Global,
            };

            public static KeywordDescriptor HasLightloop = new KeywordDescriptor()
            {
                displayName = "Has Lightloop",
                referenceName = "HAS_LIGHTLOOP",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Global,
            };

            public static KeywordDescriptor LightList = new KeywordDescriptor()
            {
                displayName = "Light List",
                referenceName = "USE",
                type = KeywordType.Enum,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Global,
                entries = new KeywordEntry[]
                {
                    new KeywordEntry() { displayName = "FPTL", referenceName = "FPTL_LIGHTLIST" },
                    new KeywordEntry() { displayName = "Clustered", referenceName = "CLUSTERED_LIGHTLIST" },
                }
            };

            public static KeywordDescriptor Shadow = new KeywordDescriptor()
            {
                displayName = "Shadow",
                referenceName = "SHADOW",
                type = KeywordType.Enum,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Global,
                entries = new KeywordEntry[]
                {
                    new KeywordEntry() { displayName = "Low", referenceName = "LOW" },
                    new KeywordEntry() { displayName = "Medium", referenceName = "MEDIUM" },
                    new KeywordEntry() { displayName = "High", referenceName = "HIGH" },
                }
            };

            public static KeywordDescriptor SurfaceTypeTransparent = new KeywordDescriptor()
            {
                displayName = "Surface Type Transparent",
                referenceName = "_SURFACE_TYPE_TRANSPARENT",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.ShaderFeature,
                scope = KeywordScope.Global,
            };

            public static KeywordDescriptor DoubleSided = new KeywordDescriptor()
            {
                displayName = "Double Sided",
                referenceName = "_DOUBLESIDED_ON",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.ShaderFeature,
                scope = KeywordScope.Local,
            };

            public static KeywordDescriptor BlendMode = new KeywordDescriptor()
            {
                displayName = "Blend Mode",
                referenceName = "_BLENDMODE",
                type = KeywordType.Enum,
                definition = KeywordDefinition.ShaderFeature,
                scope = KeywordScope.Local,
                entries = new KeywordEntry[]
                {
                    new KeywordEntry() { displayName = "Off", referenceName = "OFF" },
                    new KeywordEntry() { displayName = "Alpha", referenceName = "ALPHA" },
                    new KeywordEntry() { displayName = "Add", referenceName = "ADD" },
                    new KeywordEntry() { displayName = "Multiply", referenceName = "MULTIPLY" },
                }
            };

            public static KeywordDescriptor FogOnTransparent = new KeywordDescriptor()
            {
                displayName = "Enable Fog On Transparent",
                referenceName = "_ENABLE_FOG_ON_TRANSPARENT",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.ShaderFeature,
                scope = KeywordScope.Local,
            };

            public static KeywordDescriptor SceneSelectionPass = new KeywordDescriptor()
            {
                displayName = "Scene Selection Pass",
                referenceName = "SCENESELECTIONPASS",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.ShaderFeature,
                scope = KeywordScope.Local,
            };

            public static KeywordDescriptor TransparentDepthPrepass = new KeywordDescriptor()
            {
                displayName = "Transparent Depth Prepass",
                referenceName = "CUTOFF_TRANSPARENT_DEPTH_PREPASS",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.ShaderFeature,
                scope = KeywordScope.Local,
            };

            public static KeywordDescriptor TransparentDepthPostpass = new KeywordDescriptor()
            {
                displayName = "Transparent Depth Postpass",
                referenceName = "CUTOFF_TRANSPARENT_DEPTH_POSTPASS",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.ShaderFeature,
                scope = KeywordScope.Local,
            };

            public static KeywordDescriptor Decals3RT = new KeywordDescriptor()
            {
                displayName = "Decals 3RT",
                referenceName = "DECALS_3RT",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.ShaderFeature,
                scope = KeywordScope.Global,
            };

            public static KeywordDescriptor Decals4RT = new KeywordDescriptor()
            {
                displayName = "Decals 4RT",
                referenceName = "DECALS_4RT",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.ShaderFeature,
                scope = KeywordScope.Global,
            };

            public static KeywordDescriptor SkipRasterizedShadows = new KeywordDescriptor()
            {
                displayName = "Skip Rasterized Shadows",
                referenceName = "SKIP_RASTERIZED_SHADOWS",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Global,
            };
        }

        public static KeywordCollection WriteMsaaDepth = new KeywordCollection
        {
            { Descriptors.WriteMsaaDepth },
        };

        public static KeywordCollection DebugDisplay = new KeywordCollection
        {
            { Descriptors.DebugDisplay },
        };

        public static KeywordCollection LodFadeCrossfade = new KeywordCollection
        {
            { Descriptors.LodFadeCrossfade },
        };

        public static KeywordCollection GBuffer = new KeywordCollection
        {
            { Descriptors.LodFadeCrossfade },
            { Descriptors.DebugDisplay },
            { Descriptors.Lightmap },
            { Descriptors.DirectionalLightmapCombined },
            { Descriptors.DynamicLightmap },
            { Descriptors.ShadowsShadowmask },
            { Descriptors.LightLayers },
            { Descriptors.Decals },
        };

        public static KeywordCollection DepthMotionVectors = new KeywordCollection
        {
            { Descriptors.WriteMsaaDepth },
            { Descriptors.WriteNormalBuffer },
            { Descriptors.LodFadeCrossfade },
        };

        public static KeywordCollection Forward = new KeywordCollection
        {
            { Descriptors.LodFadeCrossfade },
            { Descriptors.DebugDisplay },
            { Descriptors.Lightmap },
            { Descriptors.DirectionalLightmapCombined },
            { Descriptors.DynamicLightmap },
            { Descriptors.ShadowsShadowmask },
            { Descriptors.Decals },
            { Descriptors.Shadow },
            { Descriptors.LightList, new FieldCondition(Fields.SurfaceOpaque, true) },
        };

        public static KeywordCollection TransparentBlend = new KeywordCollection
        {
            { Descriptors.SurfaceTypeTransparent },
            { Descriptors.BlendMode },
            { Descriptors.FogOnTransparent },
        };

        public static KeywordCollection HDDepthMotionVectors = new KeywordCollection
        {
            { Descriptors.WriteMsaaDepth },
            { Descriptors.SurfaceTypeTransparent },
            { Descriptors.BlendMode },
            { Descriptors.FogOnTransparent },
        };

        public static KeywordCollection HDUnlitForward = new KeywordCollection
        {
            { Descriptors.DebugDisplay },
            { Descriptors.SurfaceTypeTransparent },
            { Descriptors.BlendMode },
            { Descriptors.FogOnTransparent },
        };

        public static KeywordCollection HDGBuffer = new KeywordCollection
        {
            { Descriptors.LodFadeCrossfade, new FieldCondition(Fields.LodCrossFade, true) },
            { Descriptors.SurfaceTypeTransparent },
            { Descriptors.DoubleSided },
            { Descriptors.BlendMode },
            { Descriptors.DebugDisplay },
            { Descriptors.Lightmap },
            { Descriptors.DirectionalLightmapCombined },
            { Descriptors.DynamicLightmap },
            { Descriptors.ShadowsShadowmask },
            { Descriptors.LightLayers },
            { Descriptors.Decals },
            { Descriptors.FogOnTransparent },
        };

        public static KeywordCollection HDBase = new KeywordCollection
        {
            { Descriptors.LodFadeCrossfade, new FieldCondition(Fields.LodCrossFade, true) },
            { Descriptors.SurfaceTypeTransparent },
            { Descriptors.BlendMode },
            { Descriptors.DoubleSided },
            { Descriptors.FogOnTransparent },
        };

        public static KeywordCollection HDLitDepthMotionVectors = new KeywordCollection
        {
            { Descriptors.WriteMsaaDepth },
            { Descriptors.WriteNormalBuffer },
            { Descriptors.LodFadeCrossfade, new FieldCondition(Fields.LodCrossFade, true) },
            { Descriptors.SurfaceTypeTransparent },
            { Descriptors.BlendMode },
            { Descriptors.DoubleSided },
            { Descriptors.FogOnTransparent },
        };

        public static KeywordCollection HDForward = new KeywordCollection
        {
            { Descriptors.LodFadeCrossfade, new FieldCondition(Fields.LodCrossFade, true) },
            { Descriptors.SurfaceTypeTransparent },
            { Descriptors.DoubleSided },
            { Descriptors.BlendMode },
            { Descriptors.DebugDisplay },
            { Descriptors.Lightmap },
            { Descriptors.DirectionalLightmapCombined },
            { Descriptors.DynamicLightmap },
            { Descriptors.ShadowsShadowmask },
            { Descriptors.Shadow },
            { Descriptors.Decals },
            { Descriptors.LightList, new FieldCondition(Fields.SurfaceOpaque, true) },
            { Descriptors.FogOnTransparent },
        };

        public static KeywordCollection HDDepthMotionVectorsNoNormal = new KeywordCollection
        {
            { Descriptors.WriteMsaaDepth },
            { Descriptors.LodFadeCrossfade, new FieldCondition(Fields.LodCrossFade, true) },
            { Descriptors.SurfaceTypeTransparent },
            { Descriptors.BlendMode },
            { Descriptors.DoubleSided },
            { Descriptors.FogOnTransparent },
        };

        public static KeywordCollection RaytracingBasic = new KeywordCollection
        {
            { Descriptors.SurfaceTypeTransparent },
            { Descriptors.DoubleSided },
            { Descriptors.BlendMode },
        };

        public static KeywordCollection RaytracingIndirect = new KeywordCollection
        {
            { Descriptors.SurfaceTypeTransparent },
            { Descriptors.DoubleSided },
            { Descriptors.BlendMode },
            { Descriptors.DiffuseLightingOnly },
            { Descriptors.Lightmap },
            { Descriptors.DirectionalLightmapCombined },
            { Descriptors.DynamicLightmap },
        };

        public static KeywordCollection RaytracingGBufferForward = new KeywordCollection
        {
            { Descriptors.SurfaceTypeTransparent },
            { Descriptors.DoubleSided },
            { Descriptors.BlendMode },
            { Descriptors.Lightmap },
            { Descriptors.DirectionalLightmapCombined },
            { Descriptors.DynamicLightmap },
        };
    }
}