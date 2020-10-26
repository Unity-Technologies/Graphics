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
            AddProperty(surfaceTypeText, () => systemData.surfaceType, (newValue) => {
                systemData.surfaceType = newValue;
                systemData.TryChangeRenderingPass(systemData.renderQueueType);
            });

            context.globalIndentLevel++;
            var renderingPassList = HDSubShaderUtilities.GetRenderingPassList(systemData.surfaceType == SurfaceType.Opaque, enabledFeatures == Features.Unlit); // Show after post process for unlit shaders
            var renderingPassValue = systemData.surfaceType == SurfaceType.Opaque ? HDRenderQueue.GetOpaqueEquivalent(systemData.renderQueueType) : HDRenderQueue.GetTransparentEquivalent(systemData.renderQueueType);
            var renderQueueType = systemData.surfaceType == SurfaceType.Opaque ? HDRenderQueue.RenderQueueType.Opaque : HDRenderQueue.RenderQueueType.Transparent;

            context.AddProperty(renderingPassText, new PopupField<HDRenderQueue.RenderQueueType>(renderingPassList, renderQueueType, HDSubShaderUtilities.RenderQueueName, HDSubShaderUtilities.RenderQueueName) { value = renderingPassValue }, (evt) =>
            {
                registerUndo(renderingPassText);
                if(systemData.TryChangeRenderingPass(evt.newValue))
                    onChange();
            });

            if (systemData.surfaceType == SurfaceType.Transparent)
            {
                AddProperty(blendModeText, () => systemData.blendMode, (newValue) => systemData.blendMode = newValue);
                AddProperty(enableTransparentFogText, () => builtinData.transparencyFog, (newValue) => builtinData.transparencyFog = newValue);
                AddProperty(transparentZTestText, () => systemData.zTest, (newValue) => systemData.zTest = newValue);
                AddProperty(zWriteEnableText, () => systemData.transparentZWrite, (newValue) => systemData.transparentZWrite = newValue);
                AddProperty(transparentCullModeText, () => systemData.transparentCullMode, (newValue) => systemData.transparentCullMode = newValue);
                AddProperty(transparentSortPriorityText, () => systemData.sortPriority, (newValue) => systemData.sortPriority = HDRenderQueue.ClampsTransparentRangePriority(newValue));
                AddProperty(transparentBackfaceEnableText, () => builtinData.backThenFrontRendering, (newValue) => builtinData.backThenFrontRendering = newValue);
                AddProperty(transparentDepthPrepassEnableText, () => builtinData.transparentDepthPrepass, (newValue) => builtinData.transparentDepthPrepass = newValue);
                AddProperty(transparentDepthPostpassEnableText, () => builtinData.transparentDepthPostpass, (newValue) => builtinData.transparentDepthPostpass = newValue);
                AddProperty(transparentWritingMotionVecText, () => builtinData.transparentWritesMotionVec, (newValue) => builtinData.transparentWritesMotionVec = newValue);

                if (lightingData != null)
                    AddProperty(enableBlendModePreserveSpecularLightingText, () => lightingData.blendPreserveSpecular, (newValue) => lightingData.blendPreserveSpecular = newValue);
            }
            else
            {
                AddProperty(opaqueCullModeText, () => systemData.opaqueCullMode, (newValue) => systemData.opaqueCullMode = newValue);
            }
            context.globalIndentLevel--;

            // Alpha Test
            // TODO: AlphaTest is in SystemData but Alpha to Mask is in BuiltinData?
            AddProperty(alphaCutoffEnableText, () => systemData.alphaTest, (newValue) => systemData.alphaTest = newValue);
            if (systemData.alphaTest)
            {
                context.globalIndentLevel++;
                AddProperty(useShadowThresholdText, () => builtinData.alphaTestShadow, (newValue) => builtinData.alphaTestShadow = newValue);
                AddProperty(alphaToMaskText, () => builtinData.alphaToMask, (newValue) => builtinData.alphaToMask = newValue);
                context.globalIndentLevel--;
            }

            // Misc
            if ((enabledFeatures & Features.ShowDoubleSidedNormal) != 0)
                AddProperty(Styles.doubleSidedModeText, () => systemData.doubleSidedMode, (newValue) => systemData.doubleSidedMode = newValue);
            else
                AddProperty(doubleSidedEnableText, () => systemData.doubleSidedMode != DoubleSidedMode.Disabled, (newValue) => systemData.doubleSidedMode = newValue ? DoubleSidedMode.Enabled : DoubleSidedMode.Disabled);
            if (lightingData != null)
                AddProperty(Styles.fragmentNormalSpace, () => lightingData.normalDropOffSpace, (newValue) => lightingData.normalDropOffSpace = newValue);

            // Misc Cont.
            if (lightingData != null)
            {
                AddProperty(supportDecalsText, () => lightingData.receiveDecals, (newValue) => lightingData.receiveDecals = newValue);

                if (systemData.surfaceType == SurfaceType.Transparent)
                    AddProperty(receivesSSRTransparentText, () => lightingData.receiveSSRTransparent, (newValue) => lightingData.receiveSSRTransparent = newValue);
                else
                    AddProperty(receivesSSRText, () => lightingData.receiveSSR, (newValue) => lightingData.receiveSSR = newValue);
                
                AddProperty(enableGeometricSpecularAAText, () => lightingData.specularAA, (newValue) => lightingData.specularAA = newValue);
            }
            AddProperty(depthOffsetEnableText, () => builtinData.depthOffset, (newValue) => builtinData.depthOffset = newValue);
        }
    }
}
