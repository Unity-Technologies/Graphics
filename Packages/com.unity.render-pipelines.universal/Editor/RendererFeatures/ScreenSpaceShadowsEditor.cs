using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    [CustomEditor(typeof(ScreenSpaceShadows))]
    internal class ScreenSpaceShadowsEditor : Editor
    {
        #region Serialized Properties
        private SerializedProperty m_SettingsProp;
        #endregion

        private bool m_IsInitialized = false;

        static class Styles
        {
            public static readonly string k_NoSettingsHelpBox = L10n.Tr("This feature resolves the cascaded shadows in screen space, so there is no options now. It might have additional settings later.");
        }

        private void Init()
        {
            m_SettingsProp = serializedObject.FindProperty("m_Settings");
            m_IsInitialized = true;
        }

        public override void OnInspectorGUI()
        {
            if (!m_IsInitialized)
            {
                Init();
            }

            EditorGUILayout.HelpBox(Styles.k_NoSettingsHelpBox, MessageType.Info);
        }
    }
}
