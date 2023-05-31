using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    [CustomPropertyDrawer(typeof(ShaderStrippingSetting))]
    class ShaderStrippingSettingPropertyDrawer : RelativePropertiesDrawer
    {
        protected override string[] relativePropertiesNames => new[]
        {
            "m_ExportShaderVariants", "m_ShaderVariantLogLevel", "m_StripRuntimeDebugShaders"
        };
    }
}
