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
            AddLockableProperty(surfaceTypeText, HDMaterialProperties.kSurfaceType, () => systemData.surfaceType, (newValue) => {
                systemData.surfaceType = newValue;
                systemData.TryChangeRenderingPass(systemData.renderQueueType);
            });

            context.globalIndentLevel++;
            var renderingPassList = HDSubShaderUtilities.GetRenderingPassList(systemData.surfaceType == SurfaceType.Opaque, enabledFeatures == Features.Unlit); // Show after post process for unlit shaders
            var renderingPassValue = systemData.surfaceType == SurfaceType.Opaque ? HDRenderQueue.GetOpaqueEquivalent(systemData.renderQueueType) : HDRenderQueue.GetTransparentEquivalent(systemData.renderQueueType);
            var renderQueueType = systemData.surfaceType == SurfaceType.Opaque ? HDRenderQueue.RenderQueueType.Opaque : HDRenderQueue.RenderQueueType.Transparent;

            var renderingPassLine = new LockableBaseField<BaseField<HDRenderQueue.RenderQueueType>, HDRenderQueue.RenderQueueType>(
                new PopupField<HDRenderQueue.RenderQueueType>(renderingPassList, renderQueueType, HDSubShaderUtilities.RenderQueueName, HDSubShaderUtilities.RenderQueueName) { value = renderingPassValue },
                lockedProperties.Contains("_RenderQueueType"),
                CreateLockerFor("_RenderQueueType"));
            context.AddProperty(renderingPassText, renderingPassLine, (evt) =>
            {
                registerUndo(renderingPassText);
                if (systemData.TryChangeRenderingPass(evt.newValue))
                    onChange();
            });
            (renderingPassLine as ILockable).InitLockPosition();

            if (systemData.surfaceType == SurfaceType.Transparent)
            {
                AddLockableProperty(blendModeText, HDMaterialProperties.kBlendMode, () => systemData.blendMode, (newValue) => systemData.blendMode = newValue);
                AddLockableProperty(enableTransparentFogText, HDMaterialProperties.kEnableFogOnTransparent, () => builtinData.transparencyFog, (newValue) => builtinData.transparencyFog = newValue);
                AddLockableProperty(transparentZTestText, HDMaterialProperties.kZTestTransparent, () => systemData.zTest, (newValue) => systemData.zTest = newValue);
                AddLockableProperty(zWriteEnableText, HDMaterialProperties.kTransparentZWrite, () => systemData.transparentZWrite, (newValue) => systemData.transparentZWrite = newValue);
                AddLockableProperty(transparentCullModeText, HDMaterialProperties.kTransparentCullMode, () => systemData.transparentCullMode, (newValue) => systemData.transparentCullMode = newValue);
                AddLockableProperty(transparentSortPriorityText, HDMaterialProperties.kTransparentSortPriority, () => systemData.sortPriority, (newValue) => systemData.sortPriority = HDRenderQueue.ClampsTransparentRangePriority(newValue));
                AddLockableProperty(transparentBackfaceEnableText, HDMaterialProperties.kTransparentBackfaceEnable, () => builtinData.backThenFrontRendering, (newValue) => builtinData.backThenFrontRendering = newValue);
                AddLockableProperty(transparentDepthPrepassEnableText, HDMaterialProperties.kTransparentDepthPrepassEnable, () => builtinData.transparentDepthPrepass, (newValue) => builtinData.transparentDepthPrepass = newValue);
                AddLockableProperty(transparentDepthPostpassEnableText, HDMaterialProperties.kTransparentDepthPostpassEnable, () => builtinData.transparentDepthPostpass, (newValue) => builtinData.transparentDepthPostpass = newValue);
                AddLockableProperty(transparentWritingMotionVecText, HDMaterialProperties.kTransparentWritingMotionVec, () => builtinData.transparentWritesMotionVec, (newValue) => builtinData.transparentWritesMotionVec = newValue);

                if (lightingData != null)
                    AddLockableProperty(enableBlendModePreserveSpecularLightingText, HDMaterialProperties.kEnableBlendModePreserveSpecularLighting, () => lightingData.blendPreserveSpecular, (newValue) => lightingData.blendPreserveSpecular = newValue);
            }
            else
            {
                AddLockableProperty(opaqueCullModeText, HDMaterialProperties.kOpaqueCullMode, () => systemData.opaqueCullMode, (newValue) => systemData.opaqueCullMode = newValue);
            }
            context.globalIndentLevel--;

            // Alpha Test
            // TODO: AlphaTest is in SystemData but Alpha to Mask is in BuiltinData?
            AddLockableProperty(alphaCutoffEnableText, HDMaterialProperties.kAlphaCutoffEnabled, () => systemData.alphaTest, (newValue) => systemData.alphaTest = newValue);
            if (systemData.alphaTest)
            {
                context.globalIndentLevel++;
                AddLockableProperty(useShadowThresholdText, HDMaterialProperties.kUseShadowThreshold, () => builtinData.alphaTestShadow, (newValue) => builtinData.alphaTestShadow = newValue);
                AddLockableProperty(alphaToMaskText, HDMaterialProperties.kAlphaToMaskInspector, () => builtinData.alphaToMask, (newValue) => builtinData.alphaToMask = newValue);
                context.globalIndentLevel--;
            }

            // Misc
            if ((enabledFeatures & Features.ShowDoubleSidedNormal) != 0)
                AddLockableProperty(Styles.doubleSidedModeText, HDMaterialProperties.kDoubleSidedEnable, () => systemData.doubleSidedMode, (newValue) => systemData.doubleSidedMode = newValue);
            else
                AddLockableProperty(doubleSidedEnableText, HDMaterialProperties.kDoubleSidedEnable, () => systemData.doubleSidedMode != DoubleSidedMode.Disabled, (newValue) => systemData.doubleSidedMode = newValue ? DoubleSidedMode.Enabled : DoubleSidedMode.Disabled);
            if (lightingData != null)
                AddProperty(Styles.fragmentNormalSpace, () => lightingData.normalDropOffSpace, (newValue) => lightingData.normalDropOffSpace = newValue);

            // Misc Cont.
            if (lightingData != null)
            {
                AddLockableProperty(supportDecalsText, HDMaterialProperties.kEnableDecals, () => lightingData.receiveDecals, (newValue) => lightingData.receiveDecals = newValue);

                if (systemData.surfaceType == SurfaceType.Transparent)
                    AddLockableProperty(receivesSSRTransparentText, HDMaterialProperties.kReceivesSSRTransparent, () => lightingData.receiveSSRTransparent, (newValue) => lightingData.receiveSSRTransparent = newValue);
                else
                    AddLockableProperty(receivesSSRText, HDMaterialProperties.kReceivesSSR, () => lightingData.receiveSSR, (newValue) => lightingData.receiveSSR = newValue);

                AddProperty(enableGeometricSpecularAAText, () => lightingData.specularAA, (newValue) => lightingData.specularAA = newValue);
            }
            AddLockableProperty(depthOffsetEnableText, HDMaterialProperties.kDepthOffsetEnable, () => builtinData.depthOffset, (newValue) => builtinData.depthOffset = newValue);
        }
    }
}
