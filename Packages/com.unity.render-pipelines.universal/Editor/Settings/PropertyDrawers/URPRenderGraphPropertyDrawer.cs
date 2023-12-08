using UnityEditor.UIElements;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering.Universal
{
    [CustomPropertyDrawer(typeof(RenderGraphSettings))]
    class RenderGraphPropertyDrawer : PropertyDrawer
    {
        VisualElement m_Root;

        private const string k_EnableRenderCompatibilityPropertyName = "m_EnableRenderCompatibilityMode";
        private const string k_EnableRenderCompatibilityModeLabel = "Compatibility Mode (Render Graph Disabled)";
        private const string k_EnableRenderCompatibilityModeHelpBoxLabel = "Unity no longer develops or improves the rendering path that does not use Render Graph API. Use the Render Graph API when developing new graphics features.";

        /// <inheritdoc/>
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            m_Root = new VisualElement();
            var enableCompatilityModeProp = property.FindPropertyRelative(k_EnableRenderCompatibilityPropertyName);
            var enableCompatibilityMode = new PropertyField(enableCompatilityModeProp, k_EnableRenderCompatibilityModeLabel);

            // UITK raises ValueChangeCallback at bind time, so we need to ignore the first event
            bool firstTime = true;

            m_Root.Add(enableCompatibilityMode);
            enableCompatibilityMode.RegisterValueChangeCallback((onchanged) =>
            {
                m_Root.Q<HelpBox>("HelpBoxWarning").style.display = (onchanged.changedProperty.boolValue) ? DisplayStyle.Flex : DisplayStyle.None;
                if (firstTime)
                {
                    firstTime = false;
                    return;
                }

                GraphicsSettings.GetRenderPipelineSettings<RenderGraphSettings>()?.NotifyValueChanged(onchanged.changedProperty.name);
            });

            m_Root.Add(new HelpBox(k_EnableRenderCompatibilityModeHelpBoxLabel, HelpBoxMessageType.Warning)
            {
                name = "HelpBoxWarning"
            });
            return m_Root;
        }
    }
}
