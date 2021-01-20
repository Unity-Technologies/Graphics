using System;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine;

// We share the name of the properties in the UI to avoid duplication
using static UnityEditor.Rendering.HighDefinition.SurfaceOptionUIBlock.Styles;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    class SurfaceOptionPropertyBlock : SubTargetPropertyBlock
    {
        [Flags]
        public enum Features
        {
            None                    = 0,
            ShowDoubleSidedNormal   = 1 << 0,
            All                     = ~0,

            Unlit                   = Lit ^ ShowDoubleSidedNormal, // hide double sided normal for unlit
            Lit                     = All,
        }

        class Styles
        {
            public static GUIContent fragmentNormalSpace = new GUIContent("Fragment Normal Space", "Select the space use for normal map in Fragment shader in this shader graph.");
            public static GUIContent doubleSidedModeText = new GUIContent("Double Sided Mode", "Select the double sided mode to use with this Material.");
        }

        Features enabledFeatures;

        protected override string title => "Surface Option";
        protected override int foldoutIndex => 0;

        public SurfaceOptionPropertyBlock(Features features) => enabledFeatures = features;

        protected override void CreatePropertyGUI()
        {
            AddProperty(surfaceTypeText, "SystemData.surfaceType", () => systemData.surfaceType, (newValue) => {
                systemData.surfaceType = newValue;
                systemData.TryChangeRenderingPass(systemData.renderQueueType);
            });

            context.globalIndentLevel++;
            var renderingPassList = HDSubShaderUtilities.GetRenderingPassList(systemData.surfaceType == SurfaceType.Opaque, enabledFeatures == Features.Unlit); // Show after post process for unlit shaders
            var renderingPassValue = systemData.surfaceType == SurfaceType.Opaque ? HDRenderQueue.GetOpaqueEquivalent(systemData.renderQueueType) : HDRenderQueue.GetTransparentEquivalent(systemData.renderQueueType);
            var renderQueueType = systemData.surfaceType == SurfaceType.Opaque ? HDRenderQueue.RenderQueueType.Opaque : HDRenderQueue.RenderQueueType.Transparent;

            var renderingPassLine = new LockableBaseField<BaseField<HDRenderQueue.RenderQueueType>, HDRenderQueue.RenderQueueType>(
                new PopupField<HDRenderQueue.RenderQueueType>(renderingPassList, renderQueueType, HDSubShaderUtilities.RenderQueueName, HDSubShaderUtilities.RenderQueueName) { value = renderingPassValue },
                lockedProperties.Contains("SystemData.renderingPass"),
                CreateLockerFor("SystemData.renderingPass"));
            context.AddProperty(renderingPassText, renderingPassLine, (evt) =>
            {
                registerUndo(renderingPassText);
                if (systemData.TryChangeRenderingPass(evt.newValue))
                    onChange();
            });
            (renderingPassLine as ILockable).InitLockPosition();

            if (systemData.surfaceType == SurfaceType.Transparent)
            {
                AddProperty(blendModeText, "SystemData.blendMode", () => systemData.blendMode, (newValue) => systemData.blendMode = newValue);
                AddProperty(enableTransparentFogText, "BuiltinData.transparencyFog", () => builtinData.transparencyFog, (newValue) => builtinData.transparencyFog = newValue);
                AddProperty(transparentZTestText, "SystemData.zTest", () => systemData.zTest, (newValue) => systemData.zTest = newValue);
                AddProperty(zWriteEnableText, "SystemData.transparentZWrite", () => systemData.transparentZWrite, (newValue) => systemData.transparentZWrite = newValue);
                AddProperty(transparentCullModeText, "SystemData.transparentCullMode", () => systemData.transparentCullMode, (newValue) => systemData.transparentCullMode = newValue);
                AddProperty(transparentSortPriorityText, "SystemData.sortPriority", () => systemData.sortPriority, (newValue) => systemData.sortPriority = HDRenderQueue.ClampsTransparentRangePriority(newValue));
                AddProperty(transparentBackfaceEnableText, "BuiltinData.backThenFrontRendering", () => builtinData.backThenFrontRendering, (newValue) => builtinData.backThenFrontRendering = newValue);
                AddProperty(transparentDepthPrepassEnableText, "BuiltinData.transparentDepthPrepass", () => builtinData.transparentDepthPrepass, (newValue) => builtinData.transparentDepthPrepass = newValue);
                AddProperty(transparentDepthPostpassEnableText, "BuiltinData.transparentDepthPostpass", () => builtinData.transparentDepthPostpass, (newValue) => builtinData.transparentDepthPostpass = newValue);
                AddProperty(transparentWritingMotionVecText, "BuiltinData.transparentWritesMotionVec", () => builtinData.transparentWritesMotionVec, (newValue) => builtinData.transparentWritesMotionVec = newValue);

                if (lightingData != null)
                    AddProperty(enableBlendModePreserveSpecularLightingText, "LightingData.blendPreserveSpecular", () => lightingData.blendPreserveSpecular, (newValue) => lightingData.blendPreserveSpecular = newValue);
            }
            else
            {
                AddProperty(opaqueCullModeText, "SystemData.opaqueCullMode", () => systemData.opaqueCullMode, (newValue) => systemData.opaqueCullMode = newValue);
            }
            context.globalIndentLevel--;

            // Alpha Test
            // TODO: AlphaTest is in SystemData but Alpha to Mask is in BuiltinData?
            AddProperty(alphaCutoffEnableText, "SystemData.alphaTest", () => systemData.alphaTest, (newValue) => systemData.alphaTest = newValue);
            if (systemData.alphaTest)
            {
                context.globalIndentLevel++;
                AddProperty(useShadowThresholdText, "BuiltinData.alphaTestShadow", () => builtinData.alphaTestShadow, (newValue) => builtinData.alphaTestShadow = newValue);
                AddProperty(alphaToMaskText, "BuiltinData.alphaToMask", () => builtinData.alphaToMask, (newValue) => builtinData.alphaToMask = newValue);
                context.globalIndentLevel--;
            }

            // Misc
            if ((enabledFeatures & Features.ShowDoubleSidedNormal) != 0)
                AddProperty(Styles.doubleSidedModeText, "SystemData.doubleSidedMode", () => systemData.doubleSidedMode, (newValue) => systemData.doubleSidedMode = newValue);
            else
                AddProperty(doubleSidedEnableText, "SystemData.doubleSidedModeEnabled", () => systemData.doubleSidedMode != DoubleSidedMode.Disabled, (newValue) => systemData.doubleSidedMode = newValue ? DoubleSidedMode.Enabled : DoubleSidedMode.Disabled);
            if (lightingData != null)
                AddProperty(Styles.fragmentNormalSpace, "LightingData.normalDropOffSpace", () => lightingData.normalDropOffSpace, (newValue) => lightingData.normalDropOffSpace = newValue);

            // Misc Cont.
            if (lightingData != null)
            {
                AddProperty(supportDecalsText, "LightingData.receiveDecals", () => lightingData.receiveDecals, (newValue) => lightingData.receiveDecals = newValue);

                if (systemData.surfaceType == SurfaceType.Transparent)
                    AddProperty(receivesSSRTransparentText, "LightingData.receiveSSRTransparent", () => lightingData.receiveSSRTransparent, (newValue) => lightingData.receiveSSRTransparent = newValue);
                else
                    AddProperty(receivesSSRText, "LightingData.receiveSSR", () => lightingData.receiveSSR, (newValue) => lightingData.receiveSSR = newValue);

                AddProperty(enableGeometricSpecularAAText, "LightingData.specularAA", () => lightingData.specularAA, (newValue) => lightingData.specularAA = newValue);
            }
            AddProperty(depthOffsetEnableText, "BuiltinData.depthOffset", () => builtinData.depthOffset, (newValue) => builtinData.depthOffset = newValue);
        }
    }
}
