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
        private readonly LightingData lightingData;
        readonly TerrainLitData terrainLitData;

        public TerrainLitSurfaceOptionPropertyBlock(Features features, LightingData lightingData, TerrainLitData terrainLitData) : base(features)
        {
            this.lightingData = lightingData;
            this.terrainLitData = terrainLitData;
        }

        protected override void CreatePropertyGUI()
        {
            // TODO : support for raytracing later
            //AddProperty(rayTracingText, () => terrainLitData.rayTracing, (newValue) => terrainLitData.rayTracing = newValue);

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

            // Misc
            AddProperty(Styles.fragmentNormalSpace, () => lightingData.normalDropOffSpace, (newValue) => lightingData.normalDropOffSpace = newValue);

            // Misc Cont.
            AddProperty(supportDecalsText, () => lightingData.receiveDecals, (newValue) => lightingData.receiveDecals = newValue);
            AddProperty(receivesSSRText, () => lightingData.receiveSSR, (newValue) => lightingData.receiveSSR = newValue);
        }
    }
}
