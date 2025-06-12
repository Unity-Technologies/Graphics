#if URP_COMPATIBILITY_MODE
using UnityEditor.UIElements;
using UnityEngine.Rendering;
#endif
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering.Universal
{
    [CustomPropertyDrawer(typeof(RenderGraphSettings))]
    class RenderGraphPropertyDrawer : PropertyDrawer
    {
        VisualElement m_Root;

#if URP_COMPATIBILITY_MODE
        private const string k_EnableRenderCompatibilityPropertyName = "m_EnableRenderCompatibilityMode";
        private const string k_EnableRenderCompatibilityModeLabel = "Compatibility Mode (Render Graph Disabled)";
        private const string k_EnableRenderCompatibilityModeHelpBoxLabel = "Unity no longer develops or improves the rendering path that does not use Render Graph API. Use the Render Graph API when developing new graphics features.";

        bool m_EnableCompatibilityModeValue;
#endif

        /// <inheritdoc/>
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            m_Root = new VisualElement();

            m_Root.Add(new HelpBox()
            {
                messageType = HelpBoxMessageType.Info,
                text =
#if URP_COMPATIBILITY_MODE
                    "Compatibility Mode (Render Graph disabled) is currently available because URP_COMPATIBILITY_MODE is defined in Player Settings. This feature is deprecated and will be removed in a future version. It is recommended to remove this define, as it can improve compilation time and reduce the build size."
#else
                    "Compatibility Mode (Render Graph disabled) is deprecated from Unity 6.0, and the setting is hidden in Unity 6.3. Unity strips Compatibility Mode code to improve compilation time and reduce the build size. To enable Compatibility Mode, go to Edit > Project Settings > Player and add URP_COMPATIBILITY_MODE to the Scripting Define Symbols. This isn't recommended or supported."
#endif
            });
            
#if URP_COMPATIBILITY_MODE
            var enableCompatilityModeProp = property.FindPropertyRelative(k_EnableRenderCompatibilityPropertyName);
            var enableCompatibilityMode = new PropertyField(enableCompatilityModeProp, k_EnableRenderCompatibilityModeLabel);

            // UITK raises ValueChangeCallback at various times, so we need to track the actual value
            m_EnableCompatibilityModeValue = enableCompatilityModeProp.boolValue;

            m_Root.Add(enableCompatibilityMode);
            enableCompatibilityMode.RegisterValueChangeCallback((onchanged) =>
            {
                m_Root.Q<HelpBox>("HelpBoxWarning").style.display = (onchanged.changedProperty.boolValue) ? DisplayStyle.Flex : DisplayStyle.None;

                bool newValue = onchanged.changedProperty.boolValue;
                if (m_EnableCompatibilityModeValue != newValue)
                {
                    m_EnableCompatibilityModeValue = newValue;
                    GraphicsSettings.GetRenderPipelineSettings<RenderGraphSettings>()?.NotifyValueChanged(onchanged.changedProperty.name);
                }
            });

            m_Root.Add(new HelpBox(k_EnableRenderCompatibilityModeHelpBoxLabel, HelpBoxMessageType.Warning)
            {
                name = "HelpBoxWarning"
            });
#endif
            return m_Root;
        }
    }
}
