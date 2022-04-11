// We share the name of the properties in the UI to avoid duplication
using static UnityEditor.Rendering.HighDefinition.DistortionUIBlock.Styles;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    class HDUnlitDistortionPropertyBlock : DistortionPropertyBlock
    {
        HDUnlitData unlitData;

        public HDUnlitDistortionPropertyBlock(HDUnlitData unlitData) => this.unlitData = unlitData;

        protected override void CreatePropertyGUI()
        {
            base.CreatePropertyGUI();
            if (builtinData.distortion)
                AddProperty(distortionOnlyText, () => unlitData.distortionOnly, (newValue) => unlitData.distortionOnly = newValue, 1);
        }
    }
}
