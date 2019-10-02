using System.Collections.Generic;
using System.IO;
using System.Linq;
using Data.Util;
using UnityEditor.Graphing;
using UnityEngine;              // Vector3,4
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using ShaderPass = UnityEditor.ShaderGraph.Internal.ShaderPass;

// Include material common properties names
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

namespace UnityEditor.Rendering.HighDefinition
{
    internal enum HDRenderTypeTags
    {
        HDLitShader,    // For Lit, LayeredLit, LitTesselation, LayeredLitTesselation
        HDUnlitShader,  // Unlit
        Opaque,         // Used by Terrain
    }

    static class HDSubShaderUtilities
    {
        public static void AddTags(ShaderGenerator generator, string pipeline, HDRenderTypeTags renderType, int queue)
        {
            ShaderStringBuilder builder = new ShaderStringBuilder();
            builder.AppendLine("Tags");
            using (builder.BlockScope())
            {
                builder.AppendLine("\"RenderPipeline\"=\"{0}\"", pipeline);
                builder.AppendLine("\"RenderType\"=\"{0}\"", renderType);
                builder.AppendLine("\"Queue\" = \"{0}\"", HDRenderQueue.GetShaderTagValue(queue));
            }

            generator.AddShaderChunk(builder.ToString());
        }

        // Utils property to add properties to the collector, all hidden because we use a custom UI to display them
        static void AddIntProperty(this PropertyCollector collector, string referenceName, int defaultValue)
        {
            collector.AddShaderProperty(new Vector1ShaderProperty{
                floatType = FloatType.Integer,
                value = defaultValue,
                hidden = true,
                overrideReferenceName = referenceName,
            });
        }

        static void AddFloatProperty(this PropertyCollector collector, string referenceName, float defaultValue)
        {
            collector.AddShaderProperty(new Vector1ShaderProperty{
                floatType = FloatType.Default,
                hidden = true,
                value = defaultValue,
                overrideReferenceName = referenceName,
            });
        }

        static void AddFloatProperty(this PropertyCollector collector, string referenceName, string displayName, float defaultValue)
        {
            collector.AddShaderProperty(new Vector1ShaderProperty{
                floatType = FloatType.Default,
                value = defaultValue,
                overrideReferenceName = referenceName,
                hidden = true,
                displayName = displayName,
            });
        }

        static void AddToggleProperty(this PropertyCollector collector, string referenceName, bool defaultValue)
        {
            collector.AddShaderProperty(new BooleanShaderProperty{
                value = defaultValue,
                hidden = true,
                overrideReferenceName = referenceName,
            });
        }

        public static void AddStencilShaderProperties(PropertyCollector collector, bool splitLighting, bool receiveSSR)
        {
            // All these properties values will be patched with the material keyword update
            collector.AddIntProperty("_StencilRef", 0); // StencilLightingUsage.NoLighting
            collector.AddIntProperty("_StencilWriteMask", 3); // StencilMask.Lighting
            // Depth prepass
            collector.AddIntProperty("_StencilRefDepth", 0); // Nothing
            collector.AddIntProperty("_StencilWriteMaskDepth", 32); // DoesntReceiveSSR
            // Motion vector pass
            collector.AddIntProperty("_StencilRefMV", 128); // StencilBitMask.ObjectMotionVectors
            collector.AddIntProperty("_StencilWriteMaskMV", 128); // StencilBitMask.ObjectMotionVectors
            // Distortion vector pass
            collector.AddIntProperty("_StencilRefDistortionVec", 64); // StencilBitMask.DistortionVectors
            collector.AddIntProperty("_StencilWriteMaskDistortionVec", 64); // StencilBitMask.DistortionVectors
            // Gbuffer
            collector.AddIntProperty("_StencilWriteMaskGBuffer", 3); // StencilMask.Lighting
            collector.AddIntProperty("_StencilRefGBuffer", 2); // StencilLightingUsage.RegularLighting
            collector.AddIntProperty("_ZTestGBuffer", 4);

            collector.AddToggleProperty(kUseSplitLighting, splitLighting);
            collector.AddToggleProperty(kReceivesSSR, receiveSSR);

        }

        public static void AddBlendingStatesShaderProperties(
            PropertyCollector collector, SurfaceType surface, BlendMode blend, int sortingPriority,
            bool zWrite, TransparentCullMode transparentCullMode, CompareFunction zTest, bool backThenFrontRendering)
        {
            collector.AddFloatProperty("_SurfaceType", (int)surface);
            collector.AddFloatProperty("_BlendMode", (int)blend);

            // All these properties values will be patched with the material keyword update
            collector.AddFloatProperty("_SrcBlend", 1.0f);
            collector.AddFloatProperty("_DstBlend", 0.0f);
            collector.AddFloatProperty("_AlphaSrcBlend", 1.0f);
            collector.AddFloatProperty("_AlphaDstBlend", 0.0f);
            collector.AddToggleProperty("_ZWrite", zWrite);
            collector.AddFloatProperty("_CullMode", (int)CullMode.Back);
            collector.AddIntProperty("_TransparentSortPriority", sortingPriority);
            collector.AddFloatProperty("_CullModeForward", (int)CullMode.Back);
            collector.AddShaderProperty(new Vector1ShaderProperty{
                overrideReferenceName = kTransparentCullMode,
                floatType = FloatType.Enum,
                value = (int)transparentCullMode,
                enumNames = {"Front", "Back"},
                enumValues = {(int)TransparentCullMode.Front, (int)TransparentCullMode.Back},
                hidden = true,
            });

            // Add ZTest properties:
            collector.AddIntProperty("_ZTestDepthEqualForOpaque", (int)CompareFunction.LessEqual);
            collector.AddShaderProperty(new Vector1ShaderProperty{
                overrideReferenceName = kZTestTransparent,
                floatType = FloatType.Enum,
                value = (int)zTest,
                enumType = EnumType.CSharpEnum,
                cSharpEnumType = typeof(CompareFunction),
                hidden = true,
            });

            collector.AddToggleProperty(kTransparentBackfaceEnable, backThenFrontRendering);
        }

        public static void AddAlphaCutoffShaderProperties(PropertyCollector collector, bool alphaCutoff, bool shadowThreshold)
        {
            collector.AddToggleProperty("_AlphaCutoffEnable", alphaCutoff);
            collector.AddShaderProperty(new Vector1ShaderProperty{
                overrideReferenceName = "_AlphaCutoff",
                displayName = "Alpha Cutoff",
                floatType = FloatType.Slider,
                rangeValues = new Vector2(0, 1),
                hidden = true,
                value = 0.5f
            });
            collector.AddFloatProperty("_TransparentSortPriority", "_TransparentSortPriority", 0);
            collector.AddToggleProperty("_UseShadowThreshold", shadowThreshold);
        }

        public static void AddDoubleSidedProperty(PropertyCollector collector, DoubleSidedMode mode = DoubleSidedMode.Enabled)
        {
            var normalMode = ConvertDoubleSidedModeToDoubleSidedNormalMode(mode);
            collector.AddToggleProperty("_DoubleSidedEnable", mode != DoubleSidedMode.Disabled);
            collector.AddShaderProperty(new Vector1ShaderProperty{
                enumNames = {"Flip", "Mirror", "None"}, // values will be 0, 1 and 2
                floatType = FloatType.Enum,
                overrideReferenceName = "_DoubleSidedNormalMode",
                hidden = true,
                value = (int)normalMode
            });
            collector.AddShaderProperty(new Vector4ShaderProperty{
                overrideReferenceName = "_DoubleSidedConstants",
                hidden = true,
                value = new Vector4(1, 1, -1, 0)
            });
        }

        public static string RenderQueueName(HDRenderQueue.RenderQueueType value)
        {
            switch (value)
            {
                case HDRenderQueue.RenderQueueType.Opaque:
                    return "Default";
                case HDRenderQueue.RenderQueueType.AfterPostProcessOpaque:
                    return "After Post-process";
                case HDRenderQueue.RenderQueueType.PreRefraction:
                    return "Before Refraction";
                case HDRenderQueue.RenderQueueType.Transparent:
                    return "Default";
                case HDRenderQueue.RenderQueueType.LowTransparent:
                    return "Low Resolution";
                case HDRenderQueue.RenderQueueType.AfterPostprocessTransparent:
                    return "After Post-process";

#if ENABLE_RAYTRACING
                case HDRenderQueue.RenderQueueType.RaytracingOpaque: return "Raytracing";
                case HDRenderQueue.RenderQueueType.RaytracingTransparent: return "Raytracing";
#endif
                default:
                    return "None";
            }
        }

        public static System.Collections.Generic.List<HDRenderQueue.RenderQueueType> GetRenderingPassList(bool opaque, bool needAfterPostProcess)
        {
            var result = new System.Collections.Generic.List<HDRenderQueue.RenderQueueType>();
            if (opaque)
            {
                result.Add(HDRenderQueue.RenderQueueType.Opaque);
                if (needAfterPostProcess)
                    result.Add(HDRenderQueue.RenderQueueType.AfterPostProcessOpaque);
#if ENABLE_RAYTRACING
                result.Add(HDRenderQueue.RenderQueueType.RaytracingOpaque);
#endif
            }
            else
            {
                result.Add(HDRenderQueue.RenderQueueType.PreRefraction);
                result.Add(HDRenderQueue.RenderQueueType.Transparent);
                result.Add(HDRenderQueue.RenderQueueType.LowTransparent);
                if (needAfterPostProcess)
                    result.Add(HDRenderQueue.RenderQueueType.AfterPostprocessTransparent);
#if ENABLE_RAYTRACING
                result.Add(HDRenderQueue.RenderQueueType.RaytracingTransparent);
#endif
            }

            return result;
        }

        public static BlendMode ConvertAlphaModeToBlendMode(AlphaMode alphaMode)
        {
            switch (alphaMode)
            {
                case AlphaMode.Additive:
                    return BlendMode.Additive;
                case AlphaMode.Alpha:
                    return BlendMode.Alpha;
                case AlphaMode.Premultiply:
                    return BlendMode.Premultiply;
                case AlphaMode.Multiply: // In case of multiply we fall back to alpha
                    return BlendMode.Alpha;
                default:
                    throw new System.Exception("Unknown AlphaMode: " + alphaMode + ": can't convert to BlendMode.");
            }
        }

        public static DoubleSidedNormalMode ConvertDoubleSidedModeToDoubleSidedNormalMode(DoubleSidedMode shaderGraphMode)
        {
            switch (shaderGraphMode)
            {
                case DoubleSidedMode.FlippedNormals:
                    return DoubleSidedNormalMode.Flip;
                case DoubleSidedMode.MirroredNormals:
                    return DoubleSidedNormalMode.Mirror;
                case DoubleSidedMode.Enabled:
                case DoubleSidedMode.Disabled:
                default:
                    return DoubleSidedNormalMode.None;
            }
        }
    }
}
