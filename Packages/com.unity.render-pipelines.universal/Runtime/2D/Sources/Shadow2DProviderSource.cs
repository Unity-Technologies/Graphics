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

            // Both of these go through SerializedProperty. Writing the provider straight onto the
            // component instead -- as this did -- is undone by the ApplyModifiedProperties below, which
            // writes back the buffer read by Update() above and so restores the previous provider. The
            // source Component was written through a property and did stick, leaving the caster with a
            // provider and a component that do not belong together; the built-in providers then cast
            // that component to the type they expect and throw InvalidCastException on the next enable
            // or disable.
            provider.managedReferenceValue = m_Provider as ShadowShape2DProvider;
            component.objectReferenceValue = m_Component;

            if (m_Provider != null)
                m_Provider.OnSelected();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
