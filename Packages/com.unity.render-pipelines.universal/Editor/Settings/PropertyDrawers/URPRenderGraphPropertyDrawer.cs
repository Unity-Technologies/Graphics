using UnityEditor.UIElements;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering.Universal
{
    [CustomPropertyDrawer(typeof(RenderGraphSettings))]
    class RenderGraphPropertyDrawer : PropertyDrawer
    {
        private VisualElement m_Root;
        private bool m_FirstTime = true;

        /// <inheritdoc/>
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            m_Root = new VisualElement();
            var enableCompatilityModeProp = property.FindPropertyRelative("m_EnableRenderCompatibilityMode");
            var enableCompatibilityMode = new PropertyField(enableCompatilityModeProp);

            m_Root.Add(enableCompatibilityMode);
            enableCompatibilityMode.RegisterValueChangeCallback((onchanged) =>
            {
                m_Root.Q<HelpBox>("HelpBoxWarning").style.display = (onchanged.changedProperty.boolValue) ? DisplayStyle.Flex : DisplayStyle.None;
                if (m_FirstTime)
                {
                    m_FirstTime = false;
                    return;
                }

                GraphicsSettings.GetRenderPipelineSettings<RenderGraphSettings>().NotifyValueChanged(onchanged.changedProperty.name);
            });

            m_Root.Add(new HelpBox("Unity no longer develops or improves the rendering path that does not use Render Graph API. Use the Render Graph API when developing new graphics features.", HelpBoxMessageType.Warning)
            {
                name = "HelpBoxWarning"
            });
            return m_Root;
        }
    }
}
