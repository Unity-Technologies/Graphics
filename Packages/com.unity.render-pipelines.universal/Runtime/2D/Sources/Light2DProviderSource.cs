#if UNITY_EDITOR
using System;
using UnityEditor;

namespace UnityEngine.Rendering.Universal
{
    [Serializable]
    internal class Light2DProviderSource : Provider2DSource
    {
        public override void SetSourceType(SerializedObject serializedObject)
        {
            serializedObject.Update();
            SerializedProperty lightType = serializedObject.FindProperty("m_LightType");
            SerializedProperty provider = serializedObject.FindProperty("m_Light2DProvider");

            lightType.intValue = m_SourceType;
            provider.boxedValue = m_Provider;
            m_Provider.OnSelected();
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
