#if UNITY_EDITOR
using System;
using UnityEditor;

namespace UnityEngine.Rendering.Universal
{
    [Serializable]
    internal class Shadow2DSource_ShapeEditor : SelectionSource
    {
        public Shadow2DSource_ShapeEditor(GUIContent menuName, int priority)
        {
            m_MenuName = menuName;
            m_MenuPriority = priority;
        }

        public override int GetHashCode()
        {
            return (int)ShadowCaster2D.ShadowCastingSources.ShapeEditor;
        }

        public override void SetSourceType(SerializedObject serializedObject)
        {
            serializedObject.Update();

            SerializedProperty shadowCastingSource = serializedObject.FindProperty("m_ShadowCastingSource");
            shadowCastingSource.intValue = (int)ShadowCaster2D.ShadowCastingSources.ShapeEditor;

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
