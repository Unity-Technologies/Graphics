using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    [CustomPropertyDrawer(typeof(URPShaderStrippingSetting))]
    class ShaderStrippingSettingPropertyDrawer : RelativePropertiesDrawer
    {
        protected override string[] relativePropertiesNames => new[]
        {
            "m_StripUnusedPostProcessingVariants", "m_StripUnusedVariants", "m_StripScreenCoordOverrideVariants"
        };
    }
}
