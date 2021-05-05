using System;
using UnityEngine;              // Vector3,4
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEditor.Rendering.HighDefinition.ShaderGraph;
using UnityEditor.ShaderGraph.Legacy;

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

    internal static class HDSubShaderUtilities
    {
        // Utils property to add properties to the collector, all hidden because we use a custom UI to display them
        static void AddIntProperty(this PropertyCollector collector, string referenceName, int defaultValue, HLSLDeclaration declarationType = HLSLDeclaration.DoNotDeclare)
        {
            collector.AddShaderProperty(new Vector1ShaderProperty
            {
                floatType = FloatType.Integer,
                value = defaultValue,
                hidden = true,
                overrideHLSLDeclaration = true,
                hlslDeclarationOverride = declarationType,
                overrideReferenceName = referenceName,
            });
        }

        static void AddFloatProperty(this PropertyCollector collector, string referenceName, float defaultValue, HLSLDeclaration declarationType = HLSLDeclaration.DoNotDeclare)
        {
            collector.AddShaderProperty(new Vector1ShaderProperty
            {
                floatType = FloatType.Default,
                hidden = true,
                overrideHLSLDeclaration = true,
                hlslDeclarationOverride = declarationType,
                value = defaultValue,
                overrideReferenceName = referenceName,
            });
        }

        static void AddFloatProperty(this PropertyCollector collector, string referenceName, string displayName, float defaultValue, HLSLDeclaration declarationType = HLSLDeclaration.DoNotDeclare)
        {
            collector.AddShaderProperty(new Vector1ShaderProperty
            {
                floatType = FloatType.Default,
                value = defaultValue,
                overrideReferenceName = referenceName,
                hidden = true,
                overrideHLSLDeclaration = true,
                hlslDeclarationOverride = declarationType,
                displayName = displayName,
            });
        }

        public static void AddPrimitiveProperty<T>(this PropertyCollector collector, string referenceName, ExposableProperty<T> property, HLSLDeclaration declarationType = HLSLDeclaration.DoNotDeclare)
        {
            AbstractShaderProperty prop;

            switch (property.value)
            {
                case bool b: prop = new BooleanShaderProperty { value = b }; break;
                case int i: prop = new Vector1ShaderProperty { value = i, floatType = FloatType.Integer }; break;
                case float f: prop = new Vector1ShaderProperty { value = f, floatType = FloatType.Default }; break;
                default: throw new Exception($"Can't create Shader property for type {typeof(T)}. Consider using PropertyCollector.AddShaderProperty instead.");
            }

            collector.AddShaderProperty(referenceName, property, prop, declarationType);
        }

        static void AddShaderProperty<T>(this PropertyCollector collector, string referenceName, ExposableProperty<T> property, AbstractShaderProperty abstractProperty, HLSLDeclaration declarationType = HLSLDeclaration.DoNotDeclare)
        {
            abstractProperty.hidden = true;
            abstractProperty.exposed = property.IsExposed;
            abstractProperty.overrideHLSLDeclaration = true;
            abstractProperty.hlslDeclarationOverride = declarationType;
            abstractProperty.overrideReferenceName = referenceName;
            abstractProperty.displayName = " ";

            collector.AddShaderProperty(abstractProperty);
        }

        static void AddToggleProperty(this PropertyCollector collector, string referenceName, bool defaultValue, HLSLDeclaration declarationType = HLSLDeclaration.DoNotDeclare)
        {
            collector.AddShaderProperty(new BooleanShaderProperty
            {
                value = defaultValue,
                hidden = true,
                overrideHLSLDeclaration = true,
                hlslDeclarationOverride = declarationType,
                overrideReferenceName = referenceName,
            });
        }

        public static void AddStencilShaderProperties(PropertyCollector collector, SystemData systemData, LightingData lightingData, bool splitLighting)
        {
            bool ssrStencil = false;

            if (lightingData != null)
            {
                ssrStencil = systemData.surfaceType == SurfaceType.Opaque ? lightingData.receiveSSR : lightingData.receiveSSRTransparent;
                bool blendPreserveSpecular = lightingData.blendPreserveSpecular;

                // Don't add those property on Unlit
                collector.AddToggleProperty(kUseSplitLighting, splitLighting);
                collector.AddPrimitiveProperty(kReceivesSSR, lightingData.receiveSSRProp);
                collector.AddPrimitiveProperty(kReceivesSSRTransparent, lightingData.receiveSSRTransparentProp);
                collector.AddToggleProperty(kEnableBlendModePreserveSpecularLighting, blendPreserveSpecular, HLSLDeclaration.UnityPerMaterial);
                collector.AddPrimitiveProperty(kSupportDecals, lightingData.receiveDecalsProp);
            }
            else
            {
                // We still need to define it on unlit as it is needed to compile when Material.hlsl is used
                collector.AddToggleProperty(kEnableBlendModePreserveSpecularLighting, false, HLSLDeclaration.UnityPerMaterial);
            }

            // Configure render state
            BaseLitGUI.ComputeStencilProperties(ssrStencil, splitLighting, out int stencilRef, out int stencilWriteMask,
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
        }

        public static void AddBlendingStatesShaderProperties(
            PropertyCollector collector, ExposableProperty<SurfaceType> surface, ExposableProperty<BlendMode> blend, ExposableProperty<int> sortingPriority,
            ExposableProperty<bool> transparentZWrite, ExposableProperty<TransparentCullMode> transparentCullMode,
            ExposableProperty<OpaqueCullMode> opaqueCullMode, ExposableProperty<CompareFunction> zTest,
            ExposableProperty<bool> backThenFrontRendering, ExposableProperty<bool> fogOnTransparent)
        {
            collector.AddShaderProperty("_SurfaceType", surface, new Vector1ShaderProperty { value = (int)surface.value, floatType = FloatType.Default });
            collector.AddShaderProperty("_BlendMode", blend, new Vector1ShaderProperty { value = (int)blend.value, floatType = FloatType.Default }, HLSLDeclaration.UnityPerMaterial);

            // All these properties values will be patched with the material keyword update
            collector.AddFloatProperty("_SrcBlend", 1.0f);
            collector.AddFloatProperty("_DstBlend", 0.0f);
            collector.AddFloatProperty("_AlphaSrcBlend", 1.0f);
            collector.AddFloatProperty("_AlphaDstBlend", 0.0f);
            collector.AddToggleProperty(kZWrite, (surface == SurfaceType.Transparent) ? transparentZWrite : true);
            collector.AddPrimitiveProperty(kTransparentZWrite, transparentZWrite);
            collector.AddFloatProperty("_CullMode", (int)CullMode.Back);
            collector.AddPrimitiveProperty(kTransparentSortPriority, sortingPriority);
            collector.AddPrimitiveProperty(kEnableFogOnTransparent, fogOnTransparent);
            collector.AddFloatProperty("_CullModeForward", (int)CullMode.Back);
            collector.AddShaderProperty(kTransparentCullMode, transparentCullMode, new Vector1ShaderProperty
            {
                value = (int)transparentCullMode.value,
                floatType = FloatType.Enum,
                enumNames = {"Front", "Back"},
                enumValues = {(int)TransparentCullMode.Front, (int)TransparentCullMode.Back},
            });
            collector.AddShaderProperty(kOpaqueCullMode, opaqueCullMode, new Vector1ShaderProperty
            {
                value = (int)opaqueCullMode.value,
                floatType = FloatType.Enum,
                enumType = EnumType.CSharpEnum,
                cSharpEnumType = typeof(OpaqueCullMode),
            });

            // Add ZTest properties:
            collector.AddIntProperty("_ZTestDepthEqualForOpaque", (int)CompareFunction.LessEqual);
            collector.AddShaderProperty(kZTestTransparent, zTest, new Vector1ShaderProperty
            {
                value = (int)zTest.value,
                floatType = FloatType.Enum,
                enumType = EnumType.CSharpEnum,
                cSharpEnumType = typeof(CompareFunction),
            });

            collector.AddPrimitiveProperty(kTransparentBackfaceEnable, backThenFrontRendering);
        }

        public static void AddAlphaCutoffShaderProperties(PropertyCollector collector, ExposableProperty<bool> alphaCutoff, ExposableProperty<bool> shadowThreshold, ExposableProperty<bool> alphaToMask)
        {
            collector.AddPrimitiveProperty(kAlphaCutoffEnabled, alphaCutoff);
            collector.AddFloatProperty(kTransparentSortPriority, kTransparentSortPriority, 0);
            collector.AddPrimitiveProperty("_UseShadowThreshold", shadowThreshold, HLSLDeclaration.UnityPerMaterial);
            collector.AddToggleProperty(kAlphaToMask, alphaToMask.value);
            collector.AddPrimitiveProperty(kAlphaToMaskInspector, alphaToMask);
        }

        public static void AddDoubleSidedProperty(PropertyCollector collector, ExposableProperty<DoubleSidedMode> modeProp, DoubleSidedGIMode giMode = DoubleSidedGIMode.Auto)
        {
            DoubleSidedMode mode = modeProp.value;
            var normalMode = ConvertDoubleSidedModeToDoubleSidedNormalMode(mode);
            collector.AddShaderProperty(new BooleanShaderProperty
            {
                value = mode != DoubleSidedMode.Disabled,
                hidden = true,
                exposed = modeProp.IsExposed,
                overrideHLSLDeclaration = true,
                hlslDeclarationOverride = HLSLDeclaration.DoNotDeclare,
                overrideReferenceName = "_DoubleSidedEnable",
                displayName = " "
            });
            collector.AddShaderProperty(new Vector1ShaderProperty
            {
                enumNames = {"Flip", "Mirror", "None"}, // values will be 0, 1 and 2
                floatType = FloatType.Enum,
                overrideReferenceName = "_DoubleSidedNormalMode",
                hidden = true,
                exposed = modeProp.IsExposed,
                overrideHLSLDeclaration = true,
                hlslDeclarationOverride = HLSLDeclaration.DoNotDeclare,
                value = (int)normalMode,
                displayName = " "
            });
            collector.AddShaderProperty(new Vector4ShaderProperty
            {
                overrideReferenceName = "_DoubleSidedConstants",
                hidden = true,
                overrideHLSLDeclaration = true,
                hlslDeclarationOverride = HLSLDeclaration.UnityPerMaterial,
                value = new Vector4(1, 1, -1, 0)
            });
            collector.AddShaderProperty(new Vector1ShaderProperty
            {
                enumNames = { "Auto", "On", "Off" }, // values will be 0, 1 and 2
                floatType = FloatType.Enum,
                overrideReferenceName = "_DoubleSidedGIMode",
                hidden = true,
                overrideHLSLDeclaration = true,
                hlslDeclarationOverride = HLSLDeclaration.DoNotDeclare,
                value = (int)giMode,
                displayName = " "
            });
        }

        public static void AddRayTracingProperty(PropertyCollector collector, bool isRayTracing)
        {
            collector.AddToggleProperty("_RayTracing", isRayTracing, HLSLDeclaration.UnityPerMaterial);
        }

        public static void AddPrePostPassProperties(PropertyCollector collector, ExposableProperty<bool> prepass, ExposableProperty<bool> postpass)
        {
            collector.AddPrimitiveProperty(kTransparentDepthPrepassEnable, prepass);
            collector.AddPrimitiveProperty(kTransparentDepthPostpassEnable, postpass);
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

                default:
                    return "None";
            }
        }

        public static System.Collections.Generic.List<HDRenderQueue.RenderQueueType> GetRenderingPassList(bool opaque, bool needAfterPostProcess)
        {
            // We can't use RenderPipelineManager.currentPipeline here because this is called before HDRP is created by SG window
            var result = new System.Collections.Generic.List<HDRenderQueue.RenderQueueType>();
            if (opaque)
            {
                result.Add(HDRenderQueue.RenderQueueType.Opaque);
                if (needAfterPostProcess)
                    result.Add(HDRenderQueue.RenderQueueType.AfterPostProcessOpaque);
            }
            else
            {
                result.Add(HDRenderQueue.RenderQueueType.PreRefraction);
                result.Add(HDRenderQueue.RenderQueueType.Transparent);
                result.Add(HDRenderQueue.RenderQueueType.LowTransparent);
                if (needAfterPostProcess)
                    result.Add(HDRenderQueue.RenderQueueType.AfterPostprocessTransparent);
            }

            return result;
        }

        public static bool IsValidRenderingPassValue(HDRenderQueue.RenderQueueType value, bool needAfterPostProcess)
        {
            if (!needAfterPostProcess && (value == HDRenderQueue.RenderQueueType.AfterPostProcessOpaque || value == HDRenderQueue.RenderQueueType.AfterPostprocessTransparent))
                return false;

            return true;
        }

        public static bool UpgradeLegacyAlphaClip(IMasterNode1 masterNode)
        {
            var clipThresholdId = 8;
            var node = masterNode as AbstractMaterialNode;
            var clipThresholdSlot = node.FindSlot<Vector1MaterialSlot>(clipThresholdId);
            if (clipThresholdSlot == null)
                return false;

            clipThresholdSlot.owner = node;
            return (clipThresholdSlot.isConnected || clipThresholdSlot.value > 0.0f);
        }

        public static BlendMode UpgradeLegacyAlphaModeToBlendMode(int alphaMode)
        {
            switch (alphaMode)
            {
                case 0: //AlphaMode.Alpha:
                    return BlendMode.Alpha;
                case 1: //AlphaMode.Premultiply:
                    return BlendMode.Premultiply;
                case 2: //AlphaMode.Additive:
                    return BlendMode.Additive;
                case 3: //AlphaMode.Multiply: // In case of multiply we fall back to Premultiply
                    return BlendMode.Premultiply;
                default:
                    throw new System.Exception("Unknown AlphaMode at index: " + alphaMode + ": can't convert to BlendMode.");
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
