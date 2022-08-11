using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.Rendering;
using UnityEditorInternal;
using System;

namespace UnityEditor.Rendering.HighDefinition
{
    class DiffusionProfileSettingsListUI
    {
        ReorderableList m_DiffusionProfileList;
        SerializedProperty m_Property;
        string m_ListName;

        const string k_DefaultListName = "Diffusion Profile List";
        const string k_MultiEditionUnsupported = "Diffusion Profile List: Multi-edition is not supported";


        public DiffusionProfileSettingsListUI(string listName = k_DefaultListName)
        {
            m_ListName = listName;
        }

        public void OnGUI(SerializedProperty parameter)
        {
            if (parameter.hasMultipleDifferentValues)
            {
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.LabelField(k_MultiEditionUnsupported);

                return;
            }

            if (m_DiffusionProfileList == null || m_Property != parameter)
                CreateReorderableList(parameter);

            EditorGUILayout.BeginVertical();
            m_DiffusionProfileList.DoLayoutList();
            EditorGUILayout.EndVertical();
        }

        public Action<SerializedProperty, Rect, int> drawElement;

        void CreateReorderableList(SerializedProperty parameter)
        {
            m_Property = parameter;
            m_DiffusionProfileList = new ReorderableList(parameter.serializedObject, parameter, true, true, true, true);

            m_DiffusionProfileList.drawHeaderCallback = (rect) =>
            {
                EditorGUI.LabelField(rect, m_ListName);
            };

            m_DiffusionProfileList.drawElementCallback = (rect, index, active, focused) =>
            {
                rect.height = EditorGUIUtility.singleLineHeight;
                if (drawElement != null)
                    drawElement(parameter.GetArrayElementAtIndex(index), rect, index);
            };

            m_DiffusionProfileList.onCanAddCallback = l => l.count < DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT - 1;

            m_DiffusionProfileList.onAddCallback = (l) =>
            {
                if (parameter.arraySize >= DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT - 1)
                {
                    Debug.LogError("Limit of 15 diffusion profiles reached.");
                    return;
                }

                parameter.InsertArrayElementAtIndex(parameter.arraySize);
                parameter.serializedObject.ApplyModifiedProperties();
            };
        }
    }
}
