using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering
{
    [CustomPropertyDrawer(typeof(RenderPipelineGraphicsSettingsContainer))]
    class RenderPipelineGraphicsSettingsContainerPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement { name = "GlobalSettingsContainer" };
            var graphicsSettings = property.FindPropertyRelative("m_SettingsList");
            Debug.Assert(graphicsSettings != null);

            root.Add(new PropertyField(graphicsSettings));
            return root;
        }
    }
}
