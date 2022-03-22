
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
    public abstract class ScriptableRendererFeaturePropertyDrawer : PropertyDrawer
    {
        private bool toggle = true;
        protected SerializedProperty storedProperty = null;

        private struct Styles
        {
            public static GUIContent Name = EditorGUIUtility.TrTextContent("Name", "This is the name of the Renderer Feature.");
        }

        internal virtual bool shouldToggle(SerializedProperty property)
        {
            return true;
        }

        public sealed override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property != storedProperty)
                Init(property);

            float startHeight = position.y;
            EditorGUI.BeginProperty(position, label, property);
            toggle = shouldToggle(property);
            OnGUIHelper(ref position, property, label);
            EditorGUI.EndProperty();
        }

        private void OnGUIHelper(ref Rect position, SerializedProperty property, GUIContent label)
        {
            Type type = property.managedReferenceValue.GetType();

            SerializedProperty isActiveState = property.FindPropertyRelative(nameof(ScriptableRendererFeature.isActive));
            SerializedProperty name = property.FindPropertyRelative(nameof(ScriptableRendererFeature.name));
            RendererFeatureInfoAttribute attribute = type.GetCustomAttribute<RendererFeatureInfoAttribute>();
            bool disallowMultipleRendererFeatures = false;
            string rendererFeatureName = type.Name;
            if (attribute != null)
            {
                disallowMultipleRendererFeatures = attribute.DisallowMultipleRendererFeatures;
                int nameId = attribute.Path.Length - 1;
                rendererFeatureName = attribute.Path[nameId];
            }
            CoreEditorUtils.DrawSplitter(true);
            if (CoreEditorUtils.DrawHeaderToggle(
                new GUIContent(name.stringValue == rendererFeatureName ? name.stringValue : $"{name.stringValue} ({rendererFeatureName})",
                type.GetCustomAttribute<TooltipAttribute>()?.tooltip),
                property, isActiveState,
                (pos) => RendererFeatureMenu(pos, property),
                null, null,
                type.GetCustomAttribute<URPHelpURLAttribute>()?.URL, true, false))
            {
                using (new EditorGUI.DisabledScope(!isActiveState.boolValue))
                {
                    if (!disallowMultipleRendererFeatures)
                    {
                        EditorGUILayout.PropertyField(name, Styles.Name, true);
                        EditorGUILayout.Space();
                        EditorGUILayout.Space();
                        EditorGUILayout.Space();
                        EditorGUILayout.Space();
                    }
                    OnGUIRendererFeature(property);
                }
                EditorGUILayout.Space();
            }
        }

        public sealed override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return -4f;
        }

        void RendererFeatureMenu(Vector2 position, SerializedProperty rendererFeature)
        {
            var rendererFeatureList = rendererFeature.serializedObject.FindProperty(nameof(UniversalRenderPipelineAsset.m_RendererDataReferenceList))
                .GetArrayElementAtIndex(ScriptableRendererDataEditor.CurrentIndex)
                .FindPropertyRelative(nameof(ScriptableRendererData.m_RendererFeatures));
            int len = rendererFeatureList.arraySize;
            int index = ScriptableRendererFeatureEditor.CurrentRendererFeatureIndex;
            bool isTop = index == 0;
            bool isBottom = index == len - 1;
            bool hasCopy = CopyPasteUtils.HasCopyObject(rendererFeature.managedReferenceValue);
            var menu = new GenericMenu();
            if (!isTop)
                menu.AddItem(new GUIContent("Move Up"), false, () => SwitchRendererFeatures(rendererFeatureList, index, index - 1));
            else
                menu.AddDisabledItem(new GUIContent("Move Up"), false);
            if (!isBottom)
                menu.AddItem(new GUIContent("Move Down"), false, () => SwitchRendererFeatures(rendererFeatureList, index, index + 1));
            else
                menu.AddDisabledItem(new GUIContent("Move Down"), false);
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Remove"), false, () => RemoveRendererFeature(rendererFeatureList, index));
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Copy"), false, () =>
            {
                rendererFeature.serializedObject.Update();
                CopyPasteUtils.WriteObject(rendererFeature.managedReferenceValue);
            });
            if (hasCopy)
                menu.AddItem(new GUIContent("Paste"), false, () =>
                {
                    rendererFeature.serializedObject.Update();
                    CopyPasteUtils.ParseObject(rendererFeature.managedReferenceValue);
                    rendererFeature.serializedObject.ApplyModifiedProperties();
                });
            else
                menu.AddDisabledItem(new GUIContent("Paste"), false);
            menu.DropDown(new Rect(position, Vector2.zero));
        }

        protected abstract void Init(SerializedProperty property);
        protected abstract void OnGUIRendererFeature(SerializedProperty property);

        static void SwitchRendererFeatures(SerializedProperty rendererFeatureList, int indexA, int indexB)
        {
            rendererFeatureList.serializedObject.Update();
            var rendererAProp = rendererFeatureList.GetArrayElementAtIndex(indexA);
            var rendererBProp = rendererFeatureList.GetArrayElementAtIndex(indexB);
            var renderer = rendererAProp.managedReferenceValue;
            rendererAProp.managedReferenceValue = rendererBProp.managedReferenceValue;
            rendererBProp.managedReferenceValue = renderer;
            rendererFeatureList.serializedObject.ApplyModifiedProperties();
        }
        static void RemoveRendererFeature(SerializedProperty rendererFeatureList, int index)
        {
            rendererFeatureList.serializedObject.Update();
            rendererFeatureList.DeleteArrayElementAtIndex(index);
            rendererFeatureList.serializedObject.ApplyModifiedProperties();
        }
    }

    [CustomPropertyDrawer(typeof(ScriptableRendererFeature), false)]
    public class ScriptableRendererFeaturePropertyDrawerDefault : ScriptableRendererFeaturePropertyDrawer
    {
        private struct Styles
        {
            public static GUIContent Name = EditorGUIUtility.TrTextContent("Name", "This is the name of the Renderer Feature.");
        }

        internal override bool shouldToggle(SerializedProperty property)
        {
            SerializedProperty currentProperty = property.Copy();
            SerializedProperty nextRendererFeature = property.Copy();

            nextRendererFeature.NextVisible(false);
            currentProperty.NextVisible(true);//name field
            currentProperty.NextVisible(true);

            return !SerializedProperty.EqualContents(currentProperty, nextRendererFeature);
        }

        protected override void Init(SerializedProperty property)
        {
        }

        protected override void OnGUIRendererFeature(SerializedProperty property)
        {
            SerializedProperty currentProperty = property.Copy();
            SerializedProperty nextRendererFeature = property.Copy();
            {
                nextRendererFeature.NextVisible(false);
                currentProperty.NextVisible(true);
            }
            if (currentProperty.NextVisible(true))
            {
                do
                {
                    if (SerializedProperty.EqualContents(currentProperty, nextRendererFeature))
                        break;
                    EditorGUILayout.PropertyField(currentProperty);
                }
                while (currentProperty.NextVisible(false));
            }

        }
    }
}
