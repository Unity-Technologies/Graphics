using UnityEngine.Rendering.HighDefinition;
using UnityEngine;

// We share the name of the properties in the UI to avoid duplication
using static UnityEditor.Rendering.HighDefinition.SurfaceOptionUIBlock.Styles;
using static UnityEditor.Rendering.HighDefinition.LitSurfaceInputsUIBlock.Styles;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    class TerrainLitSurfaceOptionPropertyBlock : SurfaceOptionPropertyBlock
    {
        readonly TerrainLitData terrainLitData;

        public TerrainLitSurfaceOptionPropertyBlock(Features features, TerrainLitData terrainLitData) : base(features)
            => this.terrainLitData = terrainLitData;

        protected override void CreatePropertyGUI()
        {
            AddProperty(rayTracingText, () => terrainLitData.rayTracing, (newValue) => terrainLitData.rayTracing = newValue);

            systemData.surfaceType = SurfaceType.Opaque;
            systemData.doubleSidedMode = DoubleSidedMode.Disabled;
            lightingData = null;

            base.CreatePropertyGUI();
        }
    }
}
