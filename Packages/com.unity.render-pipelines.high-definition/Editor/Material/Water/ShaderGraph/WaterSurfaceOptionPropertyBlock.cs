// We share the name of the properties in the UI to avoid duplication
using static UnityEditor.Rendering.HighDefinition.SurfaceOptionUIBlock.Styles;
using static UnityEditor.Rendering.HighDefinition.ShaderGraph.AdvancedOptionsPropertyBlock.Styles;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    class WaterSurfaceOptionPropertyBlock : SurfaceOptionPropertyBlock
    {
        WaterData waterData;

        public WaterSurfaceOptionPropertyBlock(SurfaceOptionPropertyBlock.Features features, WaterData waterData) : base(features)
            => this.waterData = waterData;

        protected override void CreatePropertyGUI()
        {
            if (lightingData != null)
            {
                AddProperty(supportDecalsText, () => lightingData.receiveDecals, (newValue) => lightingData.receiveDecals = newValue);
                //AddProperty(receivesSSRTransparentText, () => lightingData.receiveSSRTransparent, (newValue) => lightingData.receiveSSRTransparent = newValue);
                //AddProperty(enableGeometricSpecularAAText, () => lightingData.specularAA, (newValue) => lightingData.specularAA = newValue);
            }

            if (Unsupported.IsDeveloperMode())
                AddProperty(debugSymbolsText, () => systemData.debugSymbols, (newValue) => systemData.debugSymbols = newValue);
        }
    }
}
