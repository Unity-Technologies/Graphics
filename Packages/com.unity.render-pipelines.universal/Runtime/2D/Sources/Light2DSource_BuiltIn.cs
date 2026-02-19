#if UNITY_EDITOR
using System;
using UnityEditor;

namespace UnityEngine.Rendering.Universal
{
    [Serializable]
    class Light2DSource_BuiltIn : SelectionSource
    {
        public override void SetSourceType(SerializedObject serializedObject)
        {
            SerializedProperty lightType = serializedObject.FindProperty("m_LightType");
            lightType.intValue = m_SourceType;
        }

        public override void DrawUI(SerializedProperty property, SerializedObject serializedObject, UnityEngine.Object[] targets)
        {
        }

        public override int GetHashCode()
        {
            return m_SourceType;
        }

        public Light2DSource_BuiltIn(GUIContent menuName, Light2D.LightType sourceType, int priority)
        {
            m_SourceType = (int)sourceType;
            m_MenuName = menuName;
            m_MenuPriority = priority;
            m_HashCode = (int)Light2D.LightType.Global;
        }

    }
}
#endif
