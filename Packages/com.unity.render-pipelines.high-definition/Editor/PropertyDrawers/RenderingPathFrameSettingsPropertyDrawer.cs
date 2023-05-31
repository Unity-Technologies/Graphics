using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [CustomPropertyDrawer(typeof(RenderingPathFrameSettings))]
    class RenderingPathFrameSettingsPropertyDrawer : RelativePropertiesDrawer
    {
        protected override string[] relativePropertiesNames => new[]
        {
            "m_Camera", "m_CustomOrBakedReflection", "m_RealtimeReflection"
        };
    }
}
