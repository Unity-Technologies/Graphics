using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    internal class ScriptableRendererFeatureEditor
    {
        private class Styles
        {
            public static readonly GUIContent RendererFeatureOrder =
                new GUIContent("Feature Order",
                    "A Renderer Feature is an asset that lets you add extra Render passes to a URP Renderer and configure their behavior.\n\n The Renderer Feature Order is executed from top to bottom.");

        }

        SerializedProperty m_rendererFeatures;
        ReorderableList m_rendererFeaturesList;
        Editor ownerEditor;

        internal ScriptableRendererFeatureEditor(ScriptableRendererDataEditor editor)
        {
            ownerEditor = editor;
            m_rendererFeatures = ownerEditor.serializedObject.FindProperty(nameof(ScriptableRendererData.m_RendererFeatures));
            CreateRendererReorderableList();
        }

        internal void DrawRendererFeatures()
        {
            m_rendererFeaturesList.DoLayoutList();
        }

        void CreateRendererReorderableList()
        {
            m_rendererFeaturesList = new ReorderableList(ownerEditor.serializedObject, m_rendererFeatures, true, true, true, true)
            {
                drawElementCallback = OnDrawElement,
                drawHeaderCallback = (Rect rect) => { EditorGUI.indentLevel++; EditorGUI.LabelField(rect, Styles.RendererFeatureOrder); EditorGUI.indentLevel--; },
                onAddDropdownCallback = OnAddDropdownCallback,
                onRemoveCallback = OnRemoveElement,
                elementHeightCallback = ElementHeightCallback,
            };
        }

        void OnAddDropdownCallback(Rect rect, ReorderableList reorderableList)
        {
            ScriptableRendererFeatureSelectionDropdown menu = new ScriptableRendererFeatureSelectionDropdown(m_rendererFeatures, ownerEditor);
            menu.Show(rect);
        }

        void OnRemoveElement(ReorderableList reorderableList)
        {
            m_rendererFeatures.DeleteArrayElementAtIndex(reorderableList.index);
        }

        void OnDrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            rect.y += 2;
            rect.height = EditorGUIUtility.singleLineHeight;
            SerializedProperty rendererFeature = m_rendererFeatures.GetArrayElementAtIndex(index);
            EditorGUI.PropertyField(rect, rendererFeature);
        }

        float ElementHeightCallback(int index)
        {
            float height = EditorGUI.GetPropertyHeight(m_rendererFeatures.GetArrayElementAtIndex(index), true);
            return height;
        }
    }
}
