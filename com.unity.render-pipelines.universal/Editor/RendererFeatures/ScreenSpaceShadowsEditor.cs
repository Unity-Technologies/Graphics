using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    [CustomPropertyDrawer(typeof(ScreenSpaceShadows), false)]
    internal class ScreenSpaceShadowsEditor : ScriptableRendererFeaturePropertyDrawer
    {
        #region Serialized Properties
        private SerializedProperty m_SettingsProp;
        #endregion

        private struct Styles
        {
            public static GUIContent Description = EditorGUIUtility.TrTextContent("Description", "This feature resolves the cascaded shadows in screen space, so there is no options now. It might have additional settings later.");
        }

        protected override void Init(SerializedProperty property)
        {
            m_SettingsProp = property.FindPropertyRelative("m_Settings");
        }

        protected override void OnGUIRendererFeature(SerializedProperty property)
        {
            EditorGUILayout.PropertyField(m_SettingsProp, Styles.Description);
        }
    }
}
