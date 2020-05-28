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
        // TODO: remove ?
        public enum Features
        {
            None            = 0,
            All             = ~0,

            Unlit           = All,
            Lit             = All,
        }

        class Styles
        {
            public static GUIContent fragmentNormalSpace = new GUIContent("Fragment Normal Space", "TODO");
            public static GUIContent doubleSidedModeText = new GUIContent("Double Sided Mode", "TODO");
        }

        Features enabledFeatures;

        protected override string title => "Surface Option";
        protected override int foldoutIndex => 0;

        public SurfaceOptionPropertyBlock(Features features) => enabledFeatures = features;

        protected override void CreatePropertyGUI()
        {
            AddProperty(surfaceTypeText, () => systemData.surfaceType, (newValue) => {
                systemData.surfaceType = newValue;
                systemData.TryChangeRenderingPass(systemData.renderingPass);
            });

            var renderingPassList = HDSubShaderUtilities.GetRenderingPassList(systemData.surfaceType == SurfaceType.Opaque, false);
            var renderingPassValue = systemData.surfaceType == SurfaceType.Opaque ? HDRenderQueue.GetOpaqueEquivalent(systemData.renderingPass) : HDRenderQueue.GetTransparentEquivalent(systemData.renderingPass);
            var renderQueueType = systemData.surfaceType == SurfaceType.Opaque ? HDRenderQueue.RenderQueueType.Opaque : HDRenderQueue.RenderQueueType.Transparent;
            context.AddProperty(renderingPassText, new PopupField<HDRenderQueue.RenderQueueType>(renderingPassList, renderQueueType, HDSubShaderUtilities.RenderQueueName, HDSubShaderUtilities.RenderQueueName) { value = renderingPassValue }, (evt) =>
            {
                registerUndo(renderingPassText);
                if(systemData.TryChangeRenderingPass(evt.newValue))
                    onChange();
            });

            if (systemData.surfaceType == SurfaceType.Transparent)
            {
                context.globalIndentLevel++;
                AddProperty(blendModeText, () => systemData.blendMode, (newValue) => systemData.blendMode = newValue);
                AddProperty(enableTransparentFogText, () => builtinData.transparencyFog, (newValue) => builtinData.transparencyFog = newValue);
                AddProperty(transparentZTestText, () => systemData.zTest, (newValue) => systemData.zTest = newValue);
                AddProperty(zWriteEnableText, () => systemData.zWrite, (newValue) => systemData.zWrite = newValue);
                AddProperty(transparentCullModeText, () => systemData.transparentCullMode, (newValue) => systemData.transparentCullMode = newValue);
                AddProperty(transparentSortPriorityText, () => systemData.sortPriority, (newValue) => systemData.sortPriority = HDRenderQueue.ClampsTransparentRangePriority(newValue));
                AddProperty(transparentBackfaceEnableText, () => builtinData.backThenFrontRendering, (newValue) => builtinData.backThenFrontRendering = newValue);
                AddProperty(transparentDepthPrepassEnableText, () => systemData.alphaTestDepthPrepass, (newValue) => systemData.alphaTestDepthPrepass = newValue);
                AddProperty(transparentDepthPostpassEnableText, () => systemData.alphaTestDepthPostpass, (newValue) => systemData.alphaTestDepthPostpass = newValue);
                AddProperty(transparentWritingMotionVecText, () => builtinData.transparentWritesMotionVec, (newValue) => builtinData.transparentWritesMotionVec = newValue);

                if (lightingData != null)
                    AddProperty(enableBlendModePreserveSpecularLightingText, () => lightingData.blendPreserveSpecular, (newValue) => lightingData.blendPreserveSpecular = newValue);
                context.globalIndentLevel--;
            }

            // Alpha Test
            // TODO: AlphaTest is in SystemData but Alpha to Mask is in BuiltinData?
            AddProperty(alphaCutoffEnableText, () => systemData.alphaTest, (newValue) => systemData.alphaTest = newValue);
            AddProperty(useShadowThresholdText, () => builtinData.alphaTestShadow, (newValue) => builtinData.alphaTestShadow = newValue);
            AddProperty(alphaToMaskText, () => builtinData.alphaToMask, (newValue) => builtinData.alphaToMask = newValue);

            // Misc
            AddProperty(Styles.doubleSidedModeText, () => systemData.doubleSidedMode, (newValue) => systemData.doubleSidedMode = newValue);
            if (lightingData != null)
                AddProperty(Styles.fragmentNormalSpace, () => lightingData.normalDropOffSpace, (newValue) => lightingData.normalDropOffSpace = newValue);

            // Misc Cont.
            if (lightingData != null)
            {
                AddProperty(supportDecalsText, () => lightingData.receiveDecals, (newValue) => lightingData.receiveDecals = newValue);
                AddProperty(receivesSSRText, () => lightingData.receiveSSR, (newValue) => lightingData.receiveSSR = newValue);
                AddProperty(enableGeometricSpecularAAText, () => lightingData.specularAA, (newValue) => lightingData.specularAA = newValue);
            }
            AddProperty(depthOffsetEnableText, () => builtinData.depthOffset, (newValue) => builtinData.depthOffset = newValue);
        }
    }
}
