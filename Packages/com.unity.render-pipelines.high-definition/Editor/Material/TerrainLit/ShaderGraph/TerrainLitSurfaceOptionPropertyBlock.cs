using UnityEngine.Rendering.HighDefinition;
using UnityEngine;
using UnityEngine.UIElements;

// We share the name of the properties in the UI to avoid duplication
using static UnityEditor.Rendering.HighDefinition.SurfaceOptionUIBlock.Styles;
using static UnityEditor.Rendering.HighDefinition.LitSurfaceInputsUIBlock.Styles;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    class TerrainLitSurfaceOptionPropertyBlock : SurfaceOptionPropertyBlock
    {
        class Styles
        {
            public static GUIContent fragmentNormalSpace = new GUIContent("Fragment Normal Space", "Select the space use for normal map in Fragment shader in this shader graph.");
        }

        public TerrainLitSurfaceOptionPropertyBlock(Features features) : base(features)
        {

        }

        protected override void CreatePropertyGUI()
        {
            // properties of opaque type
            context.globalIndentLevel++;
            var renderingPassList = HDSubShaderUtilities.GetRenderingPassList(systemData.surfaceType == SurfaceType.Opaque, false); // Show after post process for unlit shaders
            var renderingPassValue = systemData.surfaceType == SurfaceType.Opaque ? HDRenderQueue.GetOpaqueEquivalent(systemData.renderQueueType) : HDRenderQueue.GetTransparentEquivalent(systemData.renderQueueType);
            var renderQueueType = systemData.surfaceType == SurfaceType.Opaque ? HDRenderQueue.RenderQueueType.Opaque : HDRenderQueue.RenderQueueType.Transparent;
            context.AddProperty(renderingPassText, new PopupField<HDRenderQueue.RenderQueueType>(renderingPassList, renderQueueType, HDSubShaderUtilities.RenderQueueName, HDSubShaderUtilities.RenderQueueName) { value = renderingPassValue }, (evt) =>
            {
                registerUndo(renderingPassText);
                if (systemData.TryChangeRenderingPass(evt.newValue))
                    onChange();
            });
            context.globalIndentLevel--;

            // Misc
            AddProperty(Styles.fragmentNormalSpace, () => lightingData.normalDropOffSpace, (newValue) => lightingData.normalDropOffSpace = newValue);

            AddProperty(alphaCutoffEnableText, () => systemData.alphaTest, (newValue) => systemData.alphaTest = newValue);

            AddProperty(depthOffsetEnableText, () => builtinData.depthOffset, (newValue) => builtinData.depthOffset = newValue);
            if (builtinData.depthOffset)
            {
                context.globalIndentLevel++;
                AddProperty(conservativeDepthOffsetEnableText, () => builtinData.conservativeDepthOffset, (newValue) => builtinData.conservativeDepthOffset = newValue);
                context.globalIndentLevel--;
            }

            // Misc Cont.
            AddProperty(supportDecalsText, () => lightingData.receiveDecals, (newValue) => lightingData.receiveDecals = newValue);
            AddProperty(receivesSSRText, () => lightingData.receiveSSR, (newValue) => lightingData.receiveSSR = newValue);
        }
    }
}
