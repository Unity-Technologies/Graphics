using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering.HighDefinition
{
    [CustomPropertyDrawer(typeof(CustomPostProcessOrdersSettings))]
    class CustomPostProcessOrdersSettingsPropertyDrawer : RelativePropertiesDrawer
    {
        protected override string[] relativePropertiesNames => new[]
        {
            "m_BeforeTAACustomPostProcesses",
            "m_BeforePostProcessCustomPostProcesses",
            "m_BeforeTransparentCustomPostProcesses",
            "m_AfterPostProcessBlursCustomPostProcesses",
            "m_AfterPostProcessCustomPostProcesses"
        };
    }
}
