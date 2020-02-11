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
    [VolumeComponentEditor(typeof(VirtualTexturingSettings))]
    class VirtualTexturingSettingsEditor : VolumeComponentEditor
    {
        sealed class Settings
        {
            internal VirtualTexturingSettings objReference;

            internal SerializedProperty gpuCacheSizeOverridesShared;
            internal SerializedProperty gpuCacheSizeOverridesStreaming;
            internal SerializedProperty gpuCacheSizeOverridesProcedural;
        }

        Settings m_Settings;

        private bool m_OverrideOverlap = false;

        private SerializedDataParameter m_CPUCacheSizeInMegaBytes;
        private SerializedDataParameter m_GPUCacheSizeInMegaBytes;

        private ReorderableList m_GPUCacheSizeOverrideListShared;
        private SerializedProperty m_GPUCacheSizeOverridesPropertyShared;

        private ReorderableList m_GPUCacheSizeOverrideListStreaming;
        private SerializedProperty m_GPUCacheSizeOverridesPropertyStreaming;

        private ReorderableList m_GPUCacheSizeOverrideListProcedural;
        private SerializedProperty m_GPUCacheSizeOverridesPropertyProcedural;

        public override void OnEnable()
        {
            base.OnEnable();

            var properties = new PropertyFetcher<VirtualTexturingSettings>(serializedObject);

            m_CPUCacheSizeInMegaBytes = Unpack(properties.Find(x => x.cpuCacheSizeInMegaBytes));
            m_GPUCacheSizeInMegaBytes = Unpack(properties.Find(x => x.gpuCacheSizeInMegaBytes));

            m_Settings = new Settings
            {
                objReference = target as VirtualTexturingSettings,

                gpuCacheSizeOverridesShared = properties.Find(x => x.gpuCacheSizeOverridesShared),
                gpuCacheSizeOverridesStreaming = properties.Find(x => x.gpuCacheSizeOverridesStreaming),
                gpuCacheSizeOverridesProcedural = properties.Find(x => x.gpuCacheSizeOverridesProcedural),

            };
        }

        public override void OnInspectorGUI()
        {
            CheckStyles();

            serializedObject.Update();
            m_OverrideOverlap = false;

            EditorGUILayout.Space();

            using (var scope = new EditorGUI.ChangeCheckScope())
            {
                EditorGUILayout.BeginHorizontal();
                DrawOverrideCheckbox(m_CPUCacheSizeInMegaBytes);
                EditorGUI.BeginDisabledGroup(!m_CPUCacheSizeInMegaBytes.overrideState.boolValue);
                EditorGUILayout.DelayedIntField(m_CPUCacheSizeInMegaBytes.value, s_Styles.cpuCacheSize);
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                DrawOverrideCheckbox(m_GPUCacheSizeInMegaBytes);
                EditorGUI.BeginDisabledGroup(!m_GPUCacheSizeInMegaBytes.overrideState.boolValue);
                EditorGUILayout.DelayedIntField(m_GPUCacheSizeInMegaBytes.value, s_Styles.gpuCacheSize);
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.EndHorizontal();

                // GPU Cache size overrides
                if (m_GPUCacheSizeOverrideListShared == null ||
                    m_GPUCacheSizeOverridesPropertyShared != m_Settings.gpuCacheSizeOverridesShared)
                {
                    m_GPUCacheSizeOverridesPropertyShared = m_Settings.gpuCacheSizeOverridesShared;
                    m_GPUCacheSizeOverridesPropertyStreaming = m_Settings.gpuCacheSizeOverridesStreaming;
                    m_GPUCacheSizeOverridesPropertyProcedural = m_Settings.gpuCacheSizeOverridesProcedural;

                    m_GPUCacheSizeOverrideListShared = CreateGPUCacheSizeOverrideList(m_GPUCacheSizeOverridesPropertyShared, s_Styles.gpuCacheSizeOverridesShared, VirtualTexturingCacheUsage.Any, DrawSharedOverrideElement);
                    m_GPUCacheSizeOverrideListStreaming = CreateGPUCacheSizeOverrideList(m_GPUCacheSizeOverridesPropertyStreaming, s_Styles.gpuCacheSizeOverridesStreaming, VirtualTexturingCacheUsage.Streaming, DrawStreamingOverrideElement);
                    m_GPUCacheSizeOverrideListProcedural = CreateGPUCacheSizeOverrideList(m_GPUCacheSizeOverridesPropertyProcedural, s_Styles.gpuCacheSizeOverridesProcedural, VirtualTexturingCacheUsage.Procedural, DrawProceduralOverrideElement);
                }

                EditorGUILayout.BeginVertical();

                EditorGUILayout.BeginHorizontal();
                m_Settings.objReference.gpuCacheSizeOverridesOverridden = GUILayout.Toggle(m_Settings.objReference.gpuCacheSizeOverridesOverridden, "", GUILayout.Width(EditorGUIUtility.singleLineHeight));
                EditorGUI.BeginDisabledGroup(!m_Settings.objReference.gpuCacheSizeOverridesOverridden);
                GUILayout.Label(s_Styles.gpuCacheSizeOverrides);
                EditorGUILayout.EndHorizontal();

                m_GPUCacheSizeOverrideListShared.DoLayoutList();
                m_GPUCacheSizeOverrideListStreaming.DoLayoutList();
                m_GPUCacheSizeOverrideListProcedural.DoLayoutList();

                EditorGUI.EndDisabledGroup();

                if (m_OverrideOverlap)
                {
                    EditorGUILayout.HelpBox(s_Styles.gpuCacheSizeOverrideOverlap, MessageType.Warning);
                }

                EditorGUILayout.EndVertical();
            }

            serializedObject.ApplyModifiedProperties();
            }

            ReorderableList CreateGPUCacheSizeOverrideList(SerializedProperty property, GUIContent labelContent, VirtualTexturingCacheUsage usage, ReorderableList.ElementCallbackDelegate drawCallback)
            {
                ReorderableList list = new ReorderableList(property.serializedObject, property);

                list.drawHeaderCallback =
                    (Rect rect) =>
                    {
                        EditorGUI.LabelField(rect, s_Styles.gpuCacheSizeOverridesShared);
                    };

                list.drawElementCallback = drawCallback;

                list.onAddCallback = (l) =>
                {
                    List<GraphicsFormat> availableFormats = new List<GraphicsFormat>(EditorHelpers.QuerySupportedFormats());

                    // We can't just pass in existing overrides as a paramater to CreateGPUCacheSizeOverrideList() because lambdas can't capture ref params.
                    VirtualTexturingGPUCacheSizeOverride[] existingOverrides = m_Settings.objReference.gpuCacheSizeOverridesShared.ToArray();
                    if (usage == VirtualTexturingCacheUsage.Streaming)
                        existingOverrides = m_Settings.objReference.gpuCacheSizeOverridesStreaming.ToArray();
                    if (usage == VirtualTexturingCacheUsage.Procedural)
                        existingOverrides = m_Settings.objReference.gpuCacheSizeOverridesProcedural.ToArray();
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
                    foreach (var existingCacheSizeOverride in m_Settings.objReference.gpuCacheSizeOverridesShared)
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
                    EditorGUI.DelayedIntField(rect, (int) cacheSizeOverride.sizeInMegaBytes));
                cacheSizeOverrideProperty.FindPropertyRelative("sizeInMegaBytes").intValue =
                    (int) cacheSizeOverride.sizeInMegaBytes;
            }

            void DrawSharedOverrideElement(Rect rect, int overrideIdx, bool active, bool focused)
            {
                GPUCacheSizeOverridesGUI(rect, overrideIdx, m_GPUCacheSizeOverridesPropertyShared, m_Settings.objReference.gpuCacheSizeOverridesShared.ToArray());
            }

            void DrawStreamingOverrideElement(Rect rect, int overrideIdx, bool active, bool focused)
            {
                GPUCacheSizeOverridesGUI(rect, overrideIdx, m_GPUCacheSizeOverridesPropertyStreaming, m_Settings.objReference.gpuCacheSizeOverridesStreaming.ToArray());
            }

            void DrawProceduralOverrideElement(Rect rect, int overrideIdx, bool active, bool focused)
            {
                GPUCacheSizeOverridesGUI(rect, overrideIdx, m_GPUCacheSizeOverridesPropertyProcedural, m_Settings.objReference.gpuCacheSizeOverridesProcedural.ToArray());
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
