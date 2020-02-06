using System;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.VirtualTexturing;
using VirtualTexturingSettings = UnityEngine.Rendering.HighDefinition.VirtualTexturingSettings;

namespace UnityEditor.Rendering.HighDefinition
{
    [CustomEditor(typeof(VirtualTexturingSettings))]
    class VirtualTexturingSettingsEditor : HDBaseEditor<VirtualTexturingSettings>
    {
        sealed class Settings
        {
            internal VirtualTexturingSettings objReference;

            internal SerializedProperty cpuCacheSize;
            internal SerializedProperty gpuCacheSize;

            internal SerializedProperty gpuCacheSizeOverridesShared;
            internal SerializedProperty gpuCacheSizeOverridesStreaming;
            internal SerializedProperty gpuCacheSizeOverridesProcedural;
        }

        Settings m_Settings;

        private bool m_Dirty = false;
        private bool m_OverrideOverlap = false;

        private ReorderableList m_GPUCacheSizeOverrideListShared;
        private SerializedProperty m_GPUCacheSizeOverridesPropertyShared;

        private ReorderableList m_GPUCacheSizeOverrideListStreaming;
        private SerializedProperty m_GPUCacheSizeOverridesPropertyStreaming;

        private ReorderableList m_GPUCacheSizeOverrideListProcedural;
        private SerializedProperty m_GPUCacheSizeOverridesPropertyProcedural;

        protected override void OnEnable()
        {
            base.OnEnable();

            m_Settings = new Settings
            {
                objReference = m_Target,

                cpuCacheSize = properties.Find(x => x.cpuCache.sizeInMegaBytes),
                gpuCacheSize = properties.Find(x => x.gpuCache.sizeInMegaBytes),

                gpuCacheSizeOverridesShared = properties.Find(x => x.gpuCache.gpuCacheSizeOverridesShared),
                gpuCacheSizeOverridesStreaming = properties.Find(x => x.gpuCache.gpuCacheSizeOverridesStreaming),
                gpuCacheSizeOverridesProcedural = properties.Find(x => x.gpuCache.gpuCacheSizeOverridesProcedural),

            };
        }

        void ApplyChanges()
        {
            UnityEngine.Rendering.VirtualTexturing.System.ApplyVirtualTexturingSettings(m_Settings.objReference.GetSettings());
        }

        public override void OnInspectorGUI()
        {
            CheckStyles();

            serializedObject.Update();
            m_OverrideOverlap = false;

            EditorGUILayout.Space();

            using (var scope = new EditorGUI.ChangeCheckScope())
            {
                EditorGUILayout.PropertyField(m_Settings.cpuCacheSize, s_Styles.cpuCacheSize);
                EditorGUILayout.PropertyField(m_Settings.gpuCacheSize, s_Styles.gpuCacheSize);

                // GPU Cache size overrides
                if (m_GPUCacheSizeOverrideListShared == null ||
                    m_GPUCacheSizeOverridesPropertyShared != m_Settings.gpuCacheSizeOverridesShared)
                {
                    m_GPUCacheSizeOverridesPropertyShared = m_Settings.gpuCacheSizeOverridesShared;
                    m_GPUCacheSizeOverridesPropertyStreaming = m_Settings.gpuCacheSizeOverridesStreaming;
                    m_GPUCacheSizeOverridesPropertyProcedural = m_Settings.gpuCacheSizeOverridesProcedural;

                    m_GPUCacheSizeOverrideListShared = CreateGPUCacheSizeOverrideList(m_GPUCacheSizeOverridesPropertyShared, s_Styles.gpuCacheSizeOverridesShared, VirtualTexturingCacheUsage.Any, DrawSharedOverride);
                    m_GPUCacheSizeOverrideListStreaming = CreateGPUCacheSizeOverrideList(m_GPUCacheSizeOverridesPropertyStreaming, s_Styles.gpuCacheSizeOverridesStreaming, VirtualTexturingCacheUsage.Streaming, DrawStreamingOverride);
                    m_GPUCacheSizeOverrideListProcedural = CreateGPUCacheSizeOverrideList(m_GPUCacheSizeOverridesPropertyProcedural, s_Styles.gpuCacheSizeOverridesProcedural, VirtualTexturingCacheUsage.Procedural, DrawProceduralOverride);
                }

                EditorGUILayout.BeginVertical();
                GUILayout.Label(s_Styles.gpuCacheSizeOverrides);
                m_GPUCacheSizeOverrideListShared.DoLayoutList();
                m_GPUCacheSizeOverrideListStreaming.DoLayoutList();
                m_GPUCacheSizeOverrideListProcedural.DoLayoutList();

                if (m_OverrideOverlap)
                {
                    EditorGUILayout.HelpBox(s_Styles.gpuCacheSizeOverrideOverlap, MessageType.Warning);
                }

                EditorGUILayout.EndVertical();

                serializedObject.ApplyModifiedProperties();

                if (scope.changed)
                {
                    m_Dirty = true;
                }
            }

            EditorGUILayout.Space();

            if (m_Dirty)
            {
                if (GUILayout.Button("Apply"))
                {
                    ApplyChanges();
                    m_Dirty = false;
                }
            }

            EditorGUILayout.Space();

            serializedObject.ApplyModifiedProperties();
        }

        ReorderableList CreateGPUCacheSizeOverrideList(SerializedProperty property, GUIContent labelContent, VirtualTexturingCacheUsage usage, ReorderableList.ElementCallbackDelegate drawCallback)
        {
            ReorderableList list = new ReorderableList(property.serializedObject, property);

            list.drawHeaderCallback = (rect) => { EditorGUI.LabelField(rect, labelContent); };

            list.drawElementCallback = drawCallback;

            list.onAddCallback = (l) =>
            {
                List<GraphicsFormat> availableFormats = new List<GraphicsFormat>(EditorHelpers.QuerySupportedFormats());

                // We can't just pass in existing overrides as a paramater to CreateGPUCacheSizeOverrideList() because lambdas can't capture ref params.
                VirtualTexturingGPUCacheSizeOverride[] existingOverrides = m_Settings.objReference.gpuCache.gpuCacheSizeOverridesShared;
                if (usage == VirtualTexturingCacheUsage.Streaming)
                    existingOverrides = m_Settings.objReference.gpuCache.gpuCacheSizeOverridesStreaming;
                if (usage == VirtualTexturingCacheUsage.Procedural)
                    existingOverrides = m_Settings.objReference.gpuCache.gpuCacheSizeOverridesProcedural;
                RemoveOverriddenFormats(availableFormats, existingOverrides);

                int index = property.arraySize;
                property.InsertArrayElementAtIndex(index);
                var newItemProperty = property.GetArrayElementAtIndex(index);
                newItemProperty.FindPropertyRelative("format").intValue = availableFormats.Count > 0 ? (int)availableFormats[0] : 0;
                newItemProperty.FindPropertyRelative("usage").intValue = (int)usage;
                newItemProperty.FindPropertyRelative("sizeInMegaBytes").intValue = 64;
            };

            return list;
        }

        void GraphicsFormatToFormatAndChannelTransformString(GraphicsFormat graphicsFormat, out string format, out string channelTransform)
        {
            string formatString = graphicsFormat.ToString();
            int lastUnderscore = formatString.LastIndexOf('_');
            if (lastUnderscore < 0)
            {
                format = "None";
                channelTransform = "None";
                return;
            }
            format = formatString.Substring(0, lastUnderscore);
            channelTransform = formatString.Substring(lastUnderscore + 1);
        }
        GraphicsFormat FormatAndChannelTransformStringToGraphicsFormat(string format, string channelTransform)
        {
            return (GraphicsFormat)Enum.Parse(typeof(GraphicsFormat), $"{format}_{channelTransform}");
        }

        void RemoveOverriddenFormats(List<GraphicsFormat> formats, VirtualTexturingGPUCacheSizeOverride[] overrides)
        {
            foreach (var existingCacheSizeOverride in overrides)
            {
                formats.Remove(existingCacheSizeOverride.format);
            }
        }

        void GPUCacheSizeOverridesGUI(Rect rect, int overrideIdx, SerializedProperty overrideListProperty, VirtualTexturingGPUCacheSizeOverride[] overrideList)
        {
            var cacheSizeOverrideProperty = overrideListProperty.GetArrayElementAtIndex(overrideIdx);
            var cacheSizeOverride = overrideList[overrideIdx];

            List<GraphicsFormat> availableFormats = new List<GraphicsFormat>(EditorHelpers.QuerySupportedFormats());

            bool overrideOverlap = false;

            // Remove formats that are already in the shared list.
            if (overrideListProperty != m_GPUCacheSizeOverridesPropertyShared)
            {
                foreach (var existingCacheSizeOverride in m_Settings.objReference.gpuCache.gpuCacheSizeOverridesShared)
                {
                    availableFormats.Remove(existingCacheSizeOverride.format);
                    overrideOverlap |= existingCacheSizeOverride.format == cacheSizeOverride.format;
                }
            }

            m_OverrideOverlap |= overrideOverlap;

            RemoveOverriddenFormats(availableFormats, overrideList);

            // Group formats
            Dictionary<string, List<string>> formatGroups = new Dictionary<string, List<string>>();
            foreach (GraphicsFormat graphicsFormat in availableFormats)
            {
                GraphicsFormatToFormatAndChannelTransformString(graphicsFormat, out var format, out var channelTransform);
                if (!formatGroups.ContainsKey(format))
                {
                    formatGroups.Add(format, new List<string>());
                }
                formatGroups[format].Add(channelTransform);
            }

            GraphicsFormatToFormatAndChannelTransformString((GraphicsFormat)cacheSizeOverrideProperty.FindPropertyRelative("format").intValue, out string formatString, out string channelTransformString);

            // GUI Drawing

            float overrideWidth = rect.width;

            float spacing = Math.Min(5, overrideWidth * 0.02f);

            overrideWidth -= 2 * spacing;

            float overlapWarningWidth = 20;
            float formatLabelWidth = Math.Min(45, overrideWidth * 0.20f);
            float formatWidth = overrideWidth * 0.3f;
            float channelTransformWidth = overrideWidth * 0.25f;
            float sizeLabelWidth = Math.Min(30, overrideWidth * 0.13f);
            float sizeWidth = overrideWidth * 0.15f;

            // Overlap Warning

            rect.position -= new Vector2(overlapWarningWidth, 0);
            rect.width = overlapWarningWidth;

            if (overrideOverlap)
            {
                EditorGUI.HelpBox(rect, "", MessageType.Warning);
            }

            // Format
            rect.position += new Vector2(overlapWarningWidth, 0);
            rect.width = formatLabelWidth;
            EditorGUI.LabelField(rect, s_Styles.gpuCacheSizeOverrideFormat);

            rect.position += new Vector2(formatLabelWidth, 0);
            rect.width = formatWidth;
            if (EditorGUI.DropdownButton(rect, new GUIContent(formatString), FocusType.Keyboard))
            {
                GenericMenu menu = new GenericMenu();
                foreach (string possibleFormat in formatGroups.Keys)
                {
                    string localFormat = possibleFormat;
                    menu.AddItem(new GUIContent(localFormat), formatString == localFormat, () =>
                    {
                        // Make sure the channelTransform is valid for the format.
                        List<string> formatGroup = formatGroups[localFormat];
                        if (formatGroup.FindIndex((string possibleChannelTransform) => { return possibleChannelTransform == channelTransformString; }) == -1)
                        {
                            channelTransformString = formatGroup[0];
                        }

                        cacheSizeOverrideProperty.FindPropertyRelative("format").intValue = (int)FormatAndChannelTransformStringToGraphicsFormat(localFormat, channelTransformString);
                        serializedObject.ApplyModifiedProperties();
                    });
                }

                menu.ShowAsContext();
            }

            // Channel transform
            rect.position += new Vector2(formatWidth, 0);
            rect.width = channelTransformWidth;
            if (EditorGUI.DropdownButton(rect, new GUIContent(channelTransformString), FocusType.Keyboard))
            {
                GenericMenu menu = new GenericMenu();
                if (formatGroups.ContainsKey(formatString))
                {
                    List<string> possibleChannelTransforms = formatGroups[formatString];
                    foreach (string possibleChannelTransform in possibleChannelTransforms)
                    {
                        string localChannelTransform = possibleChannelTransform;
                        menu.AddItem(new GUIContent(localChannelTransform), false, () =>
                        {
                            GraphicsFormat format = FormatAndChannelTransformStringToGraphicsFormat(formatString, localChannelTransform);
                            cacheSizeOverrideProperty.FindPropertyRelative("format").intValue = (int)format;
                            serializedObject.ApplyModifiedProperties();
                        });
                    }
                }
                // Already selected so nothing needs to happen.
                menu.AddItem(new GUIContent(channelTransformString), true, () => { });
                menu.ShowAsContext();
            }

            // Size
            rect.position += new Vector2(channelTransformWidth + spacing, 0);
            rect.width = sizeLabelWidth;

            EditorGUI.LabelField(rect, s_Styles.gpuCacheSizeOverrideSize);

            rect.position += new Vector2(sizeLabelWidth, 0);
            rect.width = sizeWidth;

            cacheSizeOverride.sizeInMegaBytes = (uint) Mathf.Max(2,
                EditorGUI.IntField(rect, (int) cacheSizeOverride.sizeInMegaBytes));
            cacheSizeOverrideProperty.FindPropertyRelative("sizeInMegaBytes").intValue =
                (int) cacheSizeOverride.sizeInMegaBytes;
            serializedObject.ApplyModifiedProperties();
        }

        void DrawSharedOverride(Rect rect, int overrideIdx, bool active, bool focused)
        {
            GPUCacheSizeOverridesGUI(rect, overrideIdx, m_GPUCacheSizeOverridesPropertyShared, m_Settings.objReference.gpuCache.gpuCacheSizeOverridesShared);
        }

        void DrawStreamingOverride(Rect rect, int overrideIdx, bool active, bool focused)
        {
            GPUCacheSizeOverridesGUI(rect, overrideIdx, m_GPUCacheSizeOverridesPropertyStreaming, m_Settings.objReference.gpuCache.gpuCacheSizeOverridesStreaming);
        }

        void DrawProceduralOverride(Rect rect, int overrideIdx, bool active, bool focused)
        {
            GPUCacheSizeOverridesGUI(rect, overrideIdx, m_GPUCacheSizeOverridesPropertyProcedural, m_Settings.objReference.gpuCache.gpuCacheSizeOverridesProcedural);
        }

        sealed class Styles
        {
            public readonly GUIContent cpuCacheSize = new GUIContent("CPU Cache Size", "Amount of CPU memory (in MB) that can be allocated by the Virtual Texturing system to use to cache texture data.");
            public readonly GUIContent gpuCacheSize = new GUIContent("GPU Cache Size", "Amount of GPU memory (in MB) that can be allocated per format by the Virtual Texturing system to use to cache texture data.");
            public readonly GUIContent gpuCacheSizeOverrides = new GUIContent("GPU Cache Size Overrides", "Override the GPU cache size per format and per usage: Streaming, Procedural or both.");
            public readonly GUIContent gpuCacheSizeOverridesShared = new GUIContent("Shared");
            public readonly GUIContent gpuCacheSizeOverridesStreaming = new GUIContent("Streaming");
            public readonly GUIContent gpuCacheSizeOverridesProcedural = new GUIContent("Procedural");

            public readonly GUIContent gpuCacheSizeOverrideFormat = new GUIContent("Format", "Format and channel transform that will be overridden.");
            public readonly GUIContent gpuCacheSizeOverrideSize = new GUIContent("Size", "Size (in MB) of the override.");
            public readonly string gpuCacheSizeOverrideOverlap = "One or more formats are overridden in both the shared list and a specific usage list. Please make sure that overrides do not overlap.";
        }

        static Styles s_Styles;

        // Can't use a static initializer in case we need to create GUIStyle in the Styles class as
        // these can only be created with an active GUI rendering context
        void CheckStyles()
        {
            if (s_Styles == null)
                s_Styles = new Styles();
        }
    }
}
