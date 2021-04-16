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
            AddProperty(surfaceTypeText, systemData.surfaceTypeProp, () => {
                systemData.TryChangeRenderingPass(systemData.renderQueueType);
                onChange();
            });

            context.globalIndentLevel++;
            var renderingPassList = HDSubShaderUtilities.GetRenderingPassList(systemData.surfaceType == SurfaceType.Opaque, enabledFeatures == Features.Unlit); // Show after post process for unlit shaders
            var renderingPassValue = systemData.surfaceType == SurfaceType.Opaque ? HDRenderQueue.GetOpaqueEquivalent(systemData.renderQueueType) : HDRenderQueue.GetTransparentEquivalent(systemData.renderQueueType);
            // It is possible when switching from Unlit with an after postprocess pass to any kind of lit shader to get an out of array value. In this case we switch back to default.
            if (!HDSubShaderUtilities.IsValidRenderingPassValue(renderingPassValue, enabledFeatures == Features.Unlit))
            {
                renderingPassValue = systemData.surfaceType == SurfaceType.Opaque ? HDRenderQueue.RenderQueueType.Opaque : HDRenderQueue.RenderQueueType.Transparent;
            }
            var renderQueueType = systemData.surfaceType == SurfaceType.Opaque ? HDRenderQueue.RenderQueueType.Opaque : HDRenderQueue.RenderQueueType.Transparent;

            context.AddProperty(renderingPassText, 0, new PopupField<HDRenderQueue.RenderQueueType>(renderingPassList, renderQueueType, HDSubShaderUtilities.RenderQueueName, HDSubShaderUtilities.RenderQueueName) { value = renderingPassValue }, (evt) =>
            {
                registerUndo(renderingPassText);
                if (systemData.TryChangeRenderingPass(evt.newValue))
                    onChange();
            }, systemData.renderQueueTypeProp.GetExposeField(onChange, registerUndo));

            if (systemData.surfaceType == SurfaceType.Transparent)
            {
                AddProperty(blendModeText, systemData.blendModeProp);
                AddProperty(enableTransparentFogText, builtinData.transparencyFogProp);
                AddProperty(transparentZTestText, systemData.zTestProp);
                AddProperty(zWriteEnableText, systemData.transparentZWriteProp);
                AddProperty(transparentCullModeText, systemData.transparentCullModeProp);
                AddProperty(transparentSortPriorityText, systemData.sortPriorityProp);
                AddProperty(transparentBackfaceEnableText, builtinData.backThenFrontRenderingProp);
                AddProperty(transparentDepthPrepassEnableText, builtinData.transparentDepthPrepassProp);
                AddProperty(transparentDepthPostpassEnableText, builtinData.transparentDepthPostpassProp);
                AddProperty(transparentWritingMotionVecText, builtinData.transparentWritesMotionVecProp);

                if (lightingData != null)
                    AddProperty(enableBlendModePreserveSpecularLightingText, () => lightingData.blendPreserveSpecular, (newValue) => lightingData.blendPreserveSpecular = newValue);
            }
            else
            {
                AddProperty(opaqueCullModeText, systemData.opaqueCullModeProp);
            }
            context.globalIndentLevel--;

            // Alpha Test
            // TODO: AlphaTest is in SystemData but Alpha to Mask is in BuiltinData?
            AddProperty(alphaCutoffEnableText, systemData.alphaTestProp);
            if (systemData.alphaTestProp.IsExposed || systemData.alphaTestProp.value)
            {
                AddProperty(alphaCutoffShadowText, builtinData.alphaTestShadowProp, 1);
                AddProperty(alphaToMaskText, builtinData.alphaToMaskProp, 1);
            }

            // Misc
            if ((enabledFeatures & Features.ShowDoubleSidedNormal) != 0)
                AddProperty(Styles.doubleSidedModeText, systemData.doubleSidedModeProp);
            else
                AddProperty(doubleSidedEnableText,
                    () => systemData.doubleSidedMode != DoubleSidedMode.Disabled,
                    (newValue) => systemData.doubleSidedMode = newValue ? DoubleSidedMode.Enabled : DoubleSidedMode.Disabled,
                    systemData.doubleSidedModeProp.GetExposeField(onChange, registerUndo)
                );
            if (lightingData != null)
                AddProperty(Styles.fragmentNormalSpace, () => lightingData.normalDropOffSpace, (newValue) => lightingData.normalDropOffSpace = newValue);

            // Misc Cont.
            if (lightingData != null)
            {
                AddProperty(supportDecalsText, lightingData.receiveDecalsProp);

                if (systemData.surfaceType == SurfaceType.Transparent)
                    AddProperty(receivesSSRTransparentText, lightingData.receiveSSRTransparentProp);
                else
                    AddProperty(receivesSSRText, lightingData.receiveSSRProp);

                AddProperty(enableGeometricSpecularAAText, () => lightingData.specularAA, (newValue) => lightingData.specularAA = newValue);
            }

            AddProperty(depthOffsetEnableText, builtinData.depthOffsetProp);
            if (builtinData.depthOffsetProp.IsExposed || builtinData.depthOffsetProp.value)
                AddProperty(conservativeDepthOffsetEnableText, builtinData.conservativeDepthOffsetProp, 1);
        }
    }
}
