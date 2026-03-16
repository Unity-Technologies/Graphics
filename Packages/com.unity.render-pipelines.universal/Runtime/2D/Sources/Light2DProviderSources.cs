#if UNITY_EDITOR
using System;
using UnityEditor;

namespace UnityEngine.Rendering.Universal
{
    [Serializable]
    internal class Light2DProviderSources : Provider2DSources<Light2DProvider, Light2DProviderSource>
    {

        static void SetSourceType(SerializedObject serializedObject, SerializedProperty selectionSources)
        {
            SetSourceType(selectionSources);

            Provider2DSource providerSource = selectionSources.FindPropertyRelative("m_SelectedSource")?.boxedValue as Provider2DSource;
            if (providerSource != null)
            {
                Object[] targets = serializedObject.targetObjects;
                for (int i = 0; i < targets.Length; i++)
                {
                    Light2D light = targets[i] as Light2D;
                    light.light2DProvider = providerSource.m_Provider as Light2DProvider;
                }
            }
        }
    }
}
#endif
