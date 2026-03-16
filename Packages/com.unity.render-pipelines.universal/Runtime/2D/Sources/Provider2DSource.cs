#if UNITY_EDITOR
using System;
using UnityEditor;

namespace UnityEngine.Rendering.Universal
{
    [Serializable]
    internal class Provider2DSource : SelectionSource
    {
        [SerializeReference] public Provider2D m_Provider;

        public virtual void Initialize(Provider2D provider, Component component, int providerType)
        {
            m_SourceType = providerType;
            m_MenuName = provider.Internal_ProviderName(component.GetType().Name);
            m_MenuPriority = provider.MenuPriority();
            m_HashCode = LightUtility.ProviderToHash(provider, component);
            m_Provider = provider;
        }

        public override void DrawUI(SerializedProperty property, SerializedObject serializedObject, UnityEngine.Object[] targets)
        {
            SerializedProperty source = property.FindPropertyRelative("m_SelectedSource");
            SerializedProperty provideProperty = source.FindPropertyRelative("m_Provider");
            EditorGUILayout.PropertyField(provideProperty);
        }
    }
}
#endif
