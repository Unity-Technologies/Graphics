
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
        private static Dictionary<Type, float> typeSizeMap = new Dictionary<Type, float>();

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
            float startHeight = position.y;
            EditorGUI.BeginProperty(position, label, property);
            toggle = shouldToggle(property);
            OnGUIHelper(ref position, property, label);
            typeSizeMap[this.GetType()] = position.y + position.height - startHeight;
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

            if (DrawHeaderToggleRect(position, new GUIContent(
                name.stringValue == rendererFeatureName ? name.stringValue : $"{name.stringValue} ({rendererFeatureName})",
                type.GetCustomAttribute<TooltipAttribute>()?.tooltip),
                property, isActiveState, attribute?.Documentation))
            {
                using (new EditorGUI.DisabledScope(!isActiveState.boolValue))
                {
                    EditorGUI.indentLevel = 1;
                    position.height = EditorGUIUtility.singleLineHeight + 2;
                    position.y += position.height;
                    if (!disallowMultipleRendererFeatures)
                    {
                        DrawProperty(ref position, name, Styles.Name);
                    }
                    OnGUIRendererFeature(ref position, property, label);
                    EditorGUI.indentLevel = 0;
                }
            }
        }

        public sealed override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return typeSizeMap.TryGetValue(this.GetType(), out var value) ? value : EditorGUIUtility.singleLineHeight + 2;
        }

        protected static void DrawProperty(ref Rect position, SerializedProperty property, GUIContent content)
        {
            position.height = EditorGUI.GetPropertyHeight(property, true);
            EditorGUI.PropertyField(position, property, content, true);
            position.y += position.height + 2;
        }

        protected abstract void OnGUIRendererFeature(ref Rect position, SerializedProperty property, GUIContent label);

        private bool DrawHeaderToggleRect(Rect rect, GUIContent title, SerializedProperty group, SerializedProperty activeField, string documentationURL)
        {
            var labelRect = rect;
            labelRect.xMin += 32f;
            labelRect.xMax -= 20f + 16 + 5;

            var foldoutRect = rect;
            foldoutRect.y += 1f;
            foldoutRect.width = 13f;
            foldoutRect.height = 13f;

            var toggleRect = rect;
            toggleRect.x += 16f;
            toggleRect.y += 2f;
            toggleRect.width = 13f;
            toggleRect.height = 13f;

            // Title
            using (new EditorGUI.DisabledScope(!activeField.boolValue))
                EditorGUI.LabelField(labelRect, title, EditorStyles.boldLabel);

            // Foldout
            if (toggle)
            {
                group.isExpanded = GUI.Toggle(foldoutRect, group.isExpanded, GUIContent.none, EditorStyles.foldout);
            }

            // Active checkbox
            activeField.boolValue = GUI.Toggle(toggleRect, activeField.boolValue, GUIContent.none, CoreEditorStyles.smallTickbox);

            // Context menu
            var contextMenuRect = new Rect(labelRect.xMax + 45, labelRect.y + 1f, 16, 16);


            // Documentation button
            CoreEditorUtils.ShowHelpButton(contextMenuRect, documentationURL, title);
            return group.isExpanded;
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

        protected override void OnGUIRendererFeature(ref Rect position, SerializedProperty property, GUIContent label)
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
                    DrawProperty(ref position, currentProperty, null);
                }
                while (currentProperty.NextVisible(false));
            }

        }
    }
}
