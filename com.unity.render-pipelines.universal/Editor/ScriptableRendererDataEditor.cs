using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    /// <summary>
    /// Editor script for a <c>ScriptableRendererData</c> class.
    /// </summary>
    public abstract class CachedScriptableRendererDataEditor
    {
        public SerializedProperty serializedProperty;
        public ScriptableRendererData data;
        public SerializedProperty name { get; }
        public SerializedProperty index { get; }
        public ScriptableRendererFeatureEditor rendererFeatureEditor; // TODO: Rework this after the renderer features is done.

        public CachedScriptableRendererDataEditor(SerializedProperty serializedProperty)
        {
            this.serializedProperty = serializedProperty;
            data = (ScriptableRendererData)serializedProperty.managedReferenceValue;

            name = serializedProperty.FindPropertyRelative(nameof(ScriptableRendererData.name));
            index = serializedProperty.FindPropertyRelative(nameof(ScriptableRendererData.index));
            rendererFeatureEditor = new ScriptableRendererFeatureEditor(serializedProperty.FindPropertyRelative(nameof(ScriptableRendererData.m_RendererFeatures)));
        }
    }

    [CustomPropertyDrawer(typeof(ScriptableRendererData), true)]
    public abstract class ScriptableRendererDataEditor : PropertyDrawer
    {
        protected static List<CachedScriptableRendererDataEditor> s_CachedRendererEditors = new();
        internal static int CurrentIndex = 0;

        public virtual void DrawHeader<TDataEditor>(
            TDataEditor cachedData,
            CoreEditorDrawer<TDataEditor>.ActionDrawer DrawRenderer, CoreEditorDrawer<TDataEditor>.ActionDrawer DrawRendererAdditional)
            where TDataEditor : CachedScriptableRendererDataEditor
        {
            ExpandedStateList<ScriptableRendererData> rendererState = RenderersFoldoutStates.GetRenderersShowState();
            AdditionalPropertiesStateList<ScriptableRendererData> rendererAdditionalShowState = RenderersFoldoutStates.GetAdditionalRenderersShowState();
            CoreEditorDrawer<TDataEditor>.AdditionalPropertiesFoldoutGroup(new GUIContent($"{cachedData.index.intValue} - {cachedData.name.stringValue}"),
                1 << cachedData.index.intValue, rendererState,
                1 << cachedData.index.intValue, rendererAdditionalShowState,
                DrawRenderer, DrawRendererAdditional, AddOptionsMenu, FoldoutOption.Boxed | FoldoutOption.Indent).Draw(cachedData, null);
        }

        public virtual void DrawHeader<TDataEditor>(
            TDataEditor cachedData,
            CoreEditorDrawer<TDataEditor>.ActionDrawer DrawRenderer)
            where TDataEditor : CachedScriptableRendererDataEditor
        {
            ExpandedStateList<ScriptableRendererData> rendererState = RenderersFoldoutStates.GetRenderersShowState();
            CoreEditorDrawer<TDataEditor>.FoldoutGroup(new GUIContent($"{cachedData.index.intValue} - {cachedData.name.stringValue}"),
                1 << cachedData.index.intValue, rendererState,
                FoldoutOption.Boxed | FoldoutOption.Indent, (_, cacheData) => OptionsMenu(cacheData), DrawRenderer).Draw(cachedData, null);
        }

        protected abstract CachedScriptableRendererDataEditor Init(SerializedProperty property);

        /// <inheritdoc/>
        public override sealed void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            CurrentIndex = property.FindPropertyRelative(nameof(ScriptableRendererData.index)).intValue;
            int size = s_CachedRendererEditors.Count;
            if (size <= CurrentIndex)
            {
                for (int i = size; i <= CurrentIndex; i++)
                {
                    s_CachedRendererEditors.Add(null);
                }
            }
            if (s_CachedRendererEditors[CurrentIndex] == null || s_CachedRendererEditors[CurrentIndex].serializedProperty != property)
            {
                s_CachedRendererEditors[CurrentIndex] = Init(property);
            }

            EditorGUI.BeginProperty(position, label, property);
            OnGUI(s_CachedRendererEditors[CurrentIndex], property);
            EditorGUI.EndProperty();
        }

        protected virtual void OnGUI(CachedScriptableRendererDataEditor cachedEditorData, SerializedProperty property)
        {
            //TODO: Add custom renderer functionallity.
        }

        public sealed override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return -4f;
        }

        static void OptionsMenu<TDataEditor>(TDataEditor cacheData)
        where TDataEditor : CachedScriptableRendererDataEditor
        {
            var menu = new GenericMenu();
            AddOptionsMenu(menu, cacheData);
            menu.ShowAsContext();
        }

        static void AddOptionsMenu<TDataEditor>(GenericMenu menu, TDataEditor cacheData)
        where TDataEditor : CachedScriptableRendererDataEditor
        {
            var property = cacheData.serializedProperty;
            int index = property.FindPropertyRelative(nameof(ScriptableRendererData.index)).intValue;
            SerializedProperty rendererList = property.serializedObject.FindProperty(nameof(UniversalRenderPipelineAsset.m_RendererDataReferenceList));
            var isTop = index == 0;
            var isBottom = index == rendererList.arraySize - 1;
            var isDefault = index == property.serializedObject.FindProperty(nameof(UniversalRenderPipelineAsset.m_DefaultRendererIndex)).intValue;
            var hasCopySettings = HasCopyRenderer(property);

            if (isTop)
                menu.AddDisabledItem(new GUIContent("Move Up"), false);
            else
                menu.AddItem(new GUIContent("Move Up"), false, () => SwitchRenderers(rendererList, index, index - 1));
            if (isBottom)
                menu.AddDisabledItem(new GUIContent("Move Down"), false);
            else
                menu.AddItem(new GUIContent("Move Down"), false, () => SwitchRenderers(rendererList, index, index + 1));
            menu.AddSeparator("");

            if (isDefault)
                menu.AddDisabledItem(new GUIContent("Remove"), false);
            else
                menu.AddItem(new GUIContent("Remove"), false, () => RemoveRenderer(property));
            menu.AddSeparator("");

            menu.AddItem(new GUIContent("Copy Settings"), false, () => WriteRenderer(property));
            if (hasCopySettings)
                menu.AddItem(new GUIContent("Paste Settings"), false, () => ParseRenderer(property));
            else
                menu.AddDisabledItem(new GUIContent("Paste Settings"), false);
            menu.AddSeparator("");
        }

        static void RemoveRenderer(SerializedProperty property)
        {
            var serializedObject = property.serializedObject;
            serializedObject.Update();
            var index = property.FindPropertyRelative(nameof(ScriptableRendererData.index)).intValue;
            var defaultIndex = property.serializedObject.FindProperty(nameof(UniversalRenderPipelineAsset.m_DefaultRendererIndex));
            var rendererList = serializedObject.FindProperty(nameof(UniversalRenderPipelineAsset.m_RendererDataReferenceList));

            if (index < defaultIndex.intValue) defaultIndex.intValue -= 1;

            for (int i = index; i < rendererList.arraySize - 1; i++)
            {
                RenderersFoldoutStates.GetRendererState(i).CopyKeys(RenderersFoldoutStates.GetRendererState(i + 1));
            }
            RenderersFoldoutStates.GetRenderersShowState().removeFlagAtIndex(index);
            RenderersFoldoutStates.GetAdditionalRenderersShowState().removeFlagAtIndex(index);

            s_CachedRendererEditors.RemoveAt(index);
            rendererList.DeleteArrayElementAtIndex(index);

            serializedObject.ApplyModifiedProperties();
        }

        static void SwitchRenderers(SerializedProperty rendererList, int indexA, int indexB)
        {
            rendererList.serializedObject.Update();
            var rendererAProp = rendererList.GetArrayElementAtIndex(indexA);
            var rendererBProp = rendererList.GetArrayElementAtIndex(indexB);
            var renderer = rendererAProp.managedReferenceValue;
            rendererAProp.managedReferenceValue = rendererBProp.managedReferenceValue;
            rendererBProp.managedReferenceValue = renderer;

            RenderersFoldoutStates.SwapRendererStates(indexA, indexB);

            var showState = RenderersFoldoutStates.GetRenderersShowState();
            bool show = showState.GetExpandedAreas(1 << indexA);
            showState.SetExpandedAreas(1 << indexA, showState.GetExpandedAreas(1 << indexB));
            showState.SetExpandedAreas(1 << indexB, show);

            var showAdditionalState = RenderersFoldoutStates.GetAdditionalRenderersShowState();
            show = showAdditionalState.GetAdditionalPropertiesState(1 << indexA);
            showAdditionalState.SetAdditionalPropertiesState(1 << indexA, showAdditionalState.GetAdditionalPropertiesState(1 << indexB));
            showAdditionalState.SetAdditionalPropertiesState(1 << indexB, show);

            rendererList.serializedObject.ApplyModifiedProperties();
        }

        static bool HasCopyRenderer(SerializedProperty property)
        {
            string text = EditorGUIUtility.systemCopyBuffer;
            if (string.IsNullOrEmpty(text))
                return false;
            var prefix = RendererPrefix(property);
            if (!text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return false;
            return true;
        }

        static void ParseRenderer(SerializedProperty property)
        {
            string text = EditorGUIUtility.systemCopyBuffer;
            if (string.IsNullOrEmpty(text))
                return;
            var prefix = RendererPrefix(property);
            if (!text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return;
            try
            {
                EditorJsonUtility.FromJsonOverwrite(text.Substring(prefix.Length), property.managedReferenceValue);
            }
            catch (ArgumentException)
            {
                return;
            }
        }

        static void WriteRenderer(SerializedProperty property)
        {
            EditorGUIUtility.systemCopyBuffer = RendererPrefix(property) + EditorJsonUtility.ToJson(property.managedReferenceValue);
        }

        static string RendererPrefix(SerializedProperty property)
        {
            return property.managedReferenceValue.GetType().FullName + "JSON:";
        }
    }
}
