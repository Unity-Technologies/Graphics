#if UNITY_EDITOR
using System;
using UnityEditor;

namespace UnityEngine.Rendering.Universal
{
    [Serializable]
    internal class Shadow2DProviderSource : Provider2DSource
    {
        [SerializeField] Component m_Component;

        public override void Initialize(Provider2D provider, Component component, int providerType) 
        {
            base.Initialize(provider, component, providerType);
            m_Component = component;
        }

        public override int GetHashCode()
        {
            return LightUtility.ProviderToHash(m_Provider, m_Component);
        }

        public override void SetSourceType(SerializedObject serializedObject)
        {
            serializedObject.Update();
            SerializedProperty lightType = serializedObject.FindProperty("m_ShadowCastingSource");
            SerializedProperty provider = serializedObject.FindProperty("m_ShadowShape2DProvider");
            SerializedProperty component = serializedObject.FindProperty("m_ShadowShape2DComponent");
            lightType.intValue = m_SourceType;
            provider.boxedValue = m_Provider;
            component.boxedValue = m_Component;

            m_Provider.OnSelected();
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
