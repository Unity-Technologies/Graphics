using UnityEngine.Rendering.HighDefinition;
using UnityEngine;
using UnityEngine.UIElements;

// We share the name of the properties in the UI to avoid duplication
using static UnityEditor.Rendering.HighDefinition.SurfaceOptionUIBlock.Styles;
using static UnityEditor.Rendering.HighDefinition.LitSurfaceInputsUIBlock.Styles;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    enum TerrainSurfaceType
    {
        Opaque = SurfaceType.Opaque,
    }

    class TerrainLitSurfaceOptionPropertyBlock : SurfaceOptionPropertyBlock
    {
        readonly TerrainLitData terrainLitData;

        public TerrainLitSurfaceOptionPropertyBlock(Features features, TerrainLitData terrainLitData) : base(features)
            => this.terrainLitData = terrainLitData;

        protected override void CreatePropertyGUI()
        {
            AddProperty(rayTracingText, () => terrainLitData.rayTracing, (newValue) => terrainLitData.rayTracing = newValue);

            // Surface type
            AddProperty(surfaceTypeText, () => terrainLitData.terrainSurfaceType, (newValue) =>
            {
                // force to set terrain as opaque, just show it in the inspector
                systemData.surfaceType = SurfaceType.Opaque;
            });

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
            AddProperty(opaqueCullModeText, () => systemData.opaqueCullMode, (newValue) => systemData.opaqueCullMode = newValue);
            context.globalIndentLevel--;

            // Alpha Test
            AddProperty(alphaCutoffEnableText, () => systemData.alphaTest, (newValue) => systemData.alphaTest = newValue);
            if (systemData.alphaTest)
            {
                context.globalIndentLevel++;
                AddProperty(useShadowThresholdText, () => builtinData.alphaTestShadow, (newValue) => builtinData.alphaTestShadow = newValue);
                context.globalIndentLevel--;
            }

            // Misc
            AddProperty(doubleSidedEnableText, () => systemData.doubleSidedMode != DoubleSidedMode.Disabled, (newValue) => systemData.doubleSidedMode = newValue ? DoubleSidedMode.Enabled : DoubleSidedMode.Disabled);
            AddProperty(Styles.fragmentNormalSpace, () => terrainLitData.normalDropOffSpace, (newValue) => terrainLitData.normalDropOffSpace = newValue);

            // Misc Cont.
            AddProperty(supportDecalsText, () => terrainLitData.receiveDecals, (newValue) => terrainLitData.receiveDecals = newValue);
            AddProperty(receivesSSRText, () => terrainLitData.receiveSSR, (newValue) => terrainLitData.receiveSSR = newValue);
        }
    }
}
