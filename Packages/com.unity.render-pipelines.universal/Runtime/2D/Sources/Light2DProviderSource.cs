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

            // If m_Provider is not null, it's a custom provider, so set light type to Provider (5)
            // Otherwise it's a built-in type, use m_SourceType
            if (m_Provider != null)
            {
                lightType.intValue = (int)Light2D.LightType.Provider;
            }
            else
            {
                lightType.intValue = m_SourceType;
            }

            provider.boxedValue = m_Provider;
            if (m_Provider != null)
                m_Provider.OnSelected();
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
