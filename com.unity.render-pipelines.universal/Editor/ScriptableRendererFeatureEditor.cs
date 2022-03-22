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
    /// <summary>
    /// PropertyDrawer script for a <c>ScriptableRendererFeature</c> class.
    /// </summary>
    public class ScriptableRendererFeatureEditor
    {
        private class Styles
        {
            public static readonly GUIContent RendererFeatureOrder =
                new GUIContent("Feature Order",
                    "A Renderer Feature is an asset that lets you add extra Render passes to a URP Renderer and configure their behavior.\n\n The Renderer Feature Order is executed from top to bottom.");
            public static GUIContent rendererFeatureSettingsText = EditorGUIUtility.TrTextContent("Renderer Features", "Settings that configure the renderer features used by the renderer.");

        }

        internal static int CurrentRendererFeatureIndex = 0;
        SerializedProperty m_rendererFeatures;

        internal ScriptableRendererFeatureEditor(SerializedProperty rendererFeatures)
        {
            m_rendererFeatures = rendererFeatures;
        }

        internal void DrawRendererFeatures()
        {
            EditorGUILayout.Space();
            var prevIdent = EditorGUI.indentLevel;
            EditorGUILayout.BeginVertical("RL Header");
            EditorGUI.indentLevel = 1;
            var labelRect = GUILayoutUtility.GetRect(0, 20, GUILayout.ExpandWidth(true));
            labelRect.x -= 10f;
            EditorGUI.LabelField(labelRect, Styles.rendererFeatureSettingsText, EditorStyles.boldLabel);
            var menuRect = new Rect(labelRect.xMax - 7f, labelRect.y + 1, 16, 16);
            EditorGUILayout.EndVertical();
            EditorGUILayout.BeginVertical("RL Background");
            var e = Event.current;
            if (GUI.Button(menuRect, CoreEditorStyles.contextMenuIcon, CoreEditorStyles.contextMenuStyle))
                RendererFeatureListMenu(new Vector2(menuRect.x, menuRect.yMax), m_rendererFeatures);
            if (e.type == EventType.MouseDown)
            {
                if (labelRect.Contains(e.mousePosition))
                {
                    if (e.button != 0)
                        RendererFeatureListMenu(e.mousePosition, m_rendererFeatures);
                    e.Use();
                }
            }

            for (int i = 0; i < m_rendererFeatures.arraySize; i++)
            {
                CurrentRendererFeatureIndex = i;
                EditorGUILayout.PropertyField(m_rendererFeatures.GetArrayElementAtIndex(i));
            }

            CoreEditorUtils.DrawSplitter(true);
            EditorGUILayout.Space();
            if (GUILayout.Button(new GUIContent("Add Renderer Feature")))
            {
                ScriptableRendererFeatureSelectionDropdown menu = new ScriptableRendererFeatureSelectionDropdown(m_rendererFeatures);
                var rect = GUILayoutUtility.GetRect(new GUIContent("Add Renderer Feature"), EditorStyles.miniButton);
                menu.Show(rect);
            }
            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();
            EditorGUI.indentLevel = prevIdent;
        }

        internal static void RendererFeatureListMenu(Vector2 position, SerializedProperty rendererFeatureList)
        {
            // TODO: fix copy paste renderer features.
            var hasCopySettings = CopyPasteUtils.HasCopyObject(new RendererFeaturesListSerializationWrapper());
            var index = ScriptableRendererDataEditor.CurrentIndex;
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Copy"), false, () => WriteRendererFeatureList(rendererFeatureList, index));
            if (hasCopySettings)
                menu.AddItem(new GUIContent("Paste"), false, () => ParseRendererFeatureList(rendererFeatureList, index));
            else
                menu.AddDisabledItem(new GUIContent("Paste"), false);
            menu.DropDown(new Rect(position, Vector2.zero));
        }


        static void ParseRendererFeatureList(SerializedProperty rendererFeatureList, int rendererIndex)
        {// TODO: fix paste error.
            rendererFeatureList.serializedObject.Update();
            ScriptableRendererData renderer = (ScriptableRendererData)rendererFeatureList.serializedObject
                .FindProperty(nameof(UniversalRenderPipelineAsset.m_RendererDataReferenceList))
                .GetArrayElementAtIndex(rendererIndex).managedReferenceValue;
            var serializableRendererFeatures = new RendererFeaturesListSerializationWrapper();
            CopyPasteUtils.ParseObject(serializableRendererFeatures);
            renderer.m_RendererFeatures = serializableRendererFeatures.scriptableRendererFeatureList;
            rendererFeatureList.serializedObject.ApplyModifiedProperties();
            Debug.Log(EditorGUIUtility.systemCopyBuffer);

        }

        static void WriteRendererFeatureList(SerializedProperty rendererFeatureList, int rendererIndex)
        {
            rendererFeatureList.serializedObject.Update();
            ScriptableRendererData renderer = (ScriptableRendererData)rendererFeatureList.serializedObject
                .FindProperty(nameof(UniversalRenderPipelineAsset.m_RendererDataReferenceList))
                .GetArrayElementAtIndex(rendererIndex).managedReferenceValue;
            var serializableRendererFeatures = new RendererFeaturesListSerializationWrapper(renderer.m_RendererFeatures);
            CopyPasteUtils.WriteObject(serializableRendererFeatures);
            Debug.Log(EditorGUIUtility.systemCopyBuffer);
        }

        [Serializable]
        private class RendererFeaturesListSerializationWrapper
        {
            [SerializeReference]
            internal List<ScriptableRendererFeature> scriptableRendererFeatureList = null;
            internal RendererFeaturesListSerializationWrapper() { }
            internal RendererFeaturesListSerializationWrapper(List<ScriptableRendererFeature> rendererFeatureList)
            {
                scriptableRendererFeatureList = rendererFeatureList;
            }
        }
    }
}
