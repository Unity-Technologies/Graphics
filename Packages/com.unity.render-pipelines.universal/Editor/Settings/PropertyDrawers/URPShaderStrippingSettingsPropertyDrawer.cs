using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    [CustomPropertyDrawer(typeof(URPShaderStrippingSetting))]
    class ShaderStrippingSettingPropertyDrawer : RelativePropertiesDrawer
    {
        protected override string[] relativePropertiesNames => new[]
        {
            "m_StripUnusedPostProcessingVariantsAndResources", "m_StripUnusedVariants", "m_Strip2DUnusedVariants", "m_StripScreenCoordOverrideVariants"
        };
    }
}
