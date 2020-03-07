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
using ShaderPass = UnityEditor.ShaderGraph.PassDescriptor;

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
            BaseLitGUI.ComputeStencilProperties(receiveSSR, splitLighting, out int stencilRef, out int stencilWriteMask,
                out int stencilRefDepth, out int stencilWriteMaskDepth, out int stencilRefGBuffer, out int stencilWriteMaskGBuffer,
                out int stencilRefMV, out int stencilWriteMaskMV
            );

            // All these properties values will be patched with the material keyword update
            collector.AddIntProperty("_StencilRef", stencilRef); 
            collector.AddIntProperty("_StencilWriteMask", stencilWriteMask); 
            // Depth prepass
            collector.AddIntProperty("_StencilRefDepth", stencilRefDepth); // Nothing
            collector.AddIntProperty("_StencilWriteMaskDepth", stencilWriteMaskDepth); // StencilUsage.TraceReflectionRay
            // Motion vector pass
            collector.AddIntProperty("_StencilRefMV", stencilRefMV); // StencilUsage.ObjectMotionVector
            collector.AddIntProperty("_StencilWriteMaskMV", stencilWriteMaskMV); // StencilUsage.ObjectMotionVector
            // Distortion vector pass
            collector.AddIntProperty("_StencilRefDistortionVec", (int)StencilUsage.DistortionVectors);
            collector.AddIntProperty("_StencilWriteMaskDistortionVec", (int)StencilUsage.DistortionVectors);
            // Gbuffer
            collector.AddIntProperty("_StencilWriteMaskGBuffer", stencilWriteMaskGBuffer); 
            collector.AddIntProperty("_StencilRefGBuffer", stencilRefGBuffer); 
            collector.AddIntProperty("_ZTestGBuffer", 4);

            collector.AddToggleProperty(kUseSplitLighting, splitLighting);
            collector.AddToggleProperty(kReceivesSSR, receiveSSR);

        }

        public static void AddBlendingStatesShaderProperties(
            PropertyCollector collector, SurfaceType surface, BlendMode blend, int sortingPriority,
            bool zWrite, TransparentCullMode transparentCullMode, CompareFunction zTest,
            bool backThenFrontRendering, bool fogOnTransparent)
        {
            collector.AddFloatProperty("_SurfaceType", (int)surface);
            collector.AddFloatProperty("_BlendMode", (int)blend);

            // All these properties values will be patched with the material keyword update
            collector.AddFloatProperty("_SrcBlend", 1.0f);
            collector.AddFloatProperty("_DstBlend", 0.0f);
            collector.AddFloatProperty("_AlphaSrcBlend", 1.0f);
            collector.AddFloatProperty("_AlphaDstBlend", 0.0f);
            collector.AddToggleProperty(kZWrite, (surface == SurfaceType.Transparent) ? zWrite : true);
            collector.AddToggleProperty(kTransparentZWrite, zWrite);
            collector.AddFloatProperty("_CullMode", (int)CullMode.Back);
            collector.AddIntProperty(kTransparentSortPriority, sortingPriority);
            collector.AddToggleProperty(kEnableFogOnTransparent, fogOnTransparent);
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
            collector.AddFloatProperty(kTransparentSortPriority, kTransparentSortPriority, 0);
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

                case HDRenderQueue.RenderQueueType.RaytracingOpaque:
                {
                    if ((RenderPipelineManager.currentPipeline as HDRenderPipeline).rayTracingSupported)
                        return "RayTracing";
                    return "None";
                }
                case HDRenderQueue.RenderQueueType.RaytracingTransparent:
                {
                    if ((RenderPipelineManager.currentPipeline as HDRenderPipeline).rayTracingSupported)
                        return "RayTracing";
                    return "None";
                }
                default:
                    return "None";
            }
        }

        public static System.Collections.Generic.List<HDRenderQueue.RenderQueueType> GetRenderingPassList(bool opaque, bool needAfterPostProcess)
        {
            // We can't use RenderPipelineManager.currentPipeline here because this is called before HDRP is created by SG window
            bool supportsRayTracing = HDRenderPipeline.GatherRayTracingSupport(HDRenderPipeline.currentAsset.currentPlatformRenderPipelineSettings);
            var result = new System.Collections.Generic.List<HDRenderQueue.RenderQueueType>();
            if (opaque)
            {
                result.Add(HDRenderQueue.RenderQueueType.Opaque);
                if (needAfterPostProcess)
                    result.Add(HDRenderQueue.RenderQueueType.AfterPostProcessOpaque);
                if (supportsRayTracing)
                    result.Add(HDRenderQueue.RenderQueueType.RaytracingOpaque);
            }
            else
            {
                result.Add(HDRenderQueue.RenderQueueType.PreRefraction);
                result.Add(HDRenderQueue.RenderQueueType.Transparent);
                result.Add(HDRenderQueue.RenderQueueType.LowTransparent);
                if (needAfterPostProcess)
                    result.Add(HDRenderQueue.RenderQueueType.AfterPostprocessTransparent);
                if (supportsRayTracing)
                    result.Add(HDRenderQueue.RenderQueueType.RaytracingTransparent);
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
