#if UNITY_EDITOR
using System;
using UnityEditor;

namespace UnityEngine.Rendering.Universal
{
    [Serializable]
    internal class SelectionSource
    {
        [SerializeField] public int        m_HashCode;
        [SerializeField] public int        m_SourceType;
        [SerializeField] public int        m_MenuPriority;
        [SerializeField] public GUIContent m_MenuName;

        public override int GetHashCode()
        {
            Debug.Assert(m_HashCode != 0, "m_HashCode was not set");
            return m_HashCode;
        }

        public GUIContent GetSourceName() { return m_MenuName; }

        public virtual void SetSourceType(SerializedObject serializedObject) { }

        public virtual void DrawUI(SerializedProperty property, SerializedObject serializedObject, UnityEngine.Object[] targets) { }
    }
}
#endif
