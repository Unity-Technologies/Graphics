using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.HighDefinition;

#if ENABLE_VIRTUALTEXTURES
using UnityEngine.Rendering.VirtualTexturing;
#endif

namespace UnityEditor.Rendering.HighDefinition
{
    internal class VirtualTexturingSettingsUI
    {
        private ReorderableList m_GPUCacheSizeOverrideListStreaming;
        private SerializedProperty m_GPUCacheSizeOverridesPropertyStreaming;

        public SerializedObject serializedObject;
        private SerializedHDRenderPipelineAsset serializedRPAsset;

        private const int CPUCacheSizeMinValue = 64;
        private const int GPUCacheSizeMinValue = 32;

        public void OnGUI(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            CheckStyles();

            serializedObject = serialized.serializedObject;
            serializedRPAsset = serialized;

            EditorGUILayout.Space();

#if !ENABLE_VIRTUALTEXTURES
            EditorGUI.BeginDisabledGroup(true);
#endif

            using (var scope = new EditorGUI.ChangeCheckScope())
            {
                serialized.virtualTexturingSettings.streamingCpuCacheSizeInMegaBytes.intValue = Mathf.Max(CPUCacheSizeMinValue, EditorGUILayout.DelayedIntField(s_Styles.cpuCacheSize, serialized.virtualTexturingSettings.streamingCpuCacheSizeInMegaBytes.intValue));

                // GPU Cache size settings
                if (m_GPUCacheSizeOverrideListStreaming == null ||
                    m_GPUCacheSizeOverridesPropertyStreaming != serialized.virtualTexturingSettings.streamingGpuCacheSettings)
                {
                    m_GPUCacheSizeOverridesPropertyStreaming = serialized.virtualTexturingSettings.streamingGpuCacheSettings;
                    m_GPUCacheSizeOverrideListStreaming = CreateGPUCacheSizeOverrideList(m_GPUCacheSizeOverridesPropertyStreaming, DrawStreamingOverrideElement);
                }

                m_GPUCacheSizeOverrideListStreaming.DoLayoutList();
            }

#if !ENABLE_VIRTUALTEXTURES
            EditorGUI.EndDisabledGroup();
#endif

            serialized.serializedObject.ApplyModifiedProperties();
        }

        GPUCacheSettingSRP[] GetGPUCacheSizeOverrideArrayFromProperty(SerializedProperty property)
        {
            List<GPUCacheSettingSRP> settings = new List<GPUCacheSettingSRP>();
            for (int i = 0; i < property.arraySize; ++i)
            {
                SerializedProperty settingProperty = property.GetArrayElementAtIndex(i);
                settings.Add(new GPUCacheSettingSRP()
                    { format = (GraphicsFormat)settingProperty.FindPropertyRelative("format").intValue, sizeInMegaBytes = (uint)settingProperty.FindPropertyRelative("sizeInMegaBytes").intValue });
            }

            return settings.ToArray();
        }

        ReorderableList CreateGPUCacheSizeOverrideList(SerializedProperty property, ReorderableList.ElementCallbackDelegate drawCallback)
        {
            ReorderableList list = new ReorderableList(property.serializedObject, property);

            list.drawHeaderCallback =
                (Rect rect) =>
            {
                GUI.Label(rect, s_Styles.gpuCacheSize);
            };

            list.drawElementCallback = drawCallback;

#if ENABLE_VIRTUALTEXTURES
            list.onAddCallback = (l) =>
            {
                List<GraphicsFormat> availableFormats = new List<GraphicsFormat>(EditorHelpers.QuerySupportedFormats());

                // We can't just pass in existing settings as a parameter to CreateGPUCacheSizeOverrideList() because lambdas can't capture ref params.
                GPUCacheSettingSRP[] existingSettings = GetGPUCacheSizeOverrideArrayFromProperty(serializedRPAsset.virtualTexturingSettings.streamingGpuCacheSettings);
                RemoveOverriddenFormats(availableFormats, existingSettings);

                int index = property.arraySize;
                property.InsertArrayElementAtIndex(index);
                var newItemProperty = property.GetArrayElementAtIndex(index);
                newItemProperty.FindPropertyRelative("format").intValue = availableFormats.Count > 0 ? (int)availableFormats[0] : 0;
                newItemProperty.FindPropertyRelative("sizeInMegaBytes").intValue = 64;
            };
#endif

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
            if (format == "None") return GraphicsFormat.None;

            return (GraphicsFormat)Enum.Parse(typeof(GraphicsFormat), $"{format}_{channelTransform}");
        }

        void RemoveOverriddenFormats(List<GraphicsFormat> formats, GPUCacheSettingSRP[] settings)
        {
            foreach (var existingCacheSizeOverride in settings)
            {
                formats.Remove(existingCacheSizeOverride.format);
            }
        }

        void GPUCacheSizeOverridesGUI(Rect rect, int settingIdx, SerializedProperty settingListProperty, GPUCacheSettingSRP[] settingList)
        {
            var cacheSizeOverrideProperty = settingListProperty.GetArrayElementAtIndex(settingIdx);
            var cacheSizeOverride = settingList[settingIdx];

#if ENABLE_VIRTUALTEXTURES
            List<GraphicsFormat> availableFormats = new List<GraphicsFormat>(EditorHelpers.QuerySupportedFormats());
            // None is used for a default cache size.
            availableFormats.Add(GraphicsFormat.None);

            RemoveOverriddenFormats(availableFormats, settingList);

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
#endif

            GraphicsFormat serializedFormat = (GraphicsFormat)cacheSizeOverrideProperty.FindPropertyRelative("format").intValue;
            GraphicsFormatToFormatAndChannelTransformString(serializedFormat, out string formatString, out string channelTransformString);


            // GUI Drawing

            float settingWidth = rect.width;

            float spacing = Math.Min(5, settingWidth * 0.02f);

            settingWidth -= 2 * spacing;

            float formatLabelWidth = Math.Min(60, settingWidth * 0.25f);
            float formatWidth = settingWidth * 0.3f;
            float channelTransformWidth = settingWidth * 0.25f;
            float sizeLabelWidth = Math.Min(45, settingWidth * 0.2f);
            float sizeWidth = settingWidth * 0.15f;

            // Format
            rect.width = formatLabelWidth;
            EditorGUI.LabelField(rect, s_Styles.gpuCacheSizeOverrideFormat);

            rect.position += new Vector2(formatLabelWidth, 0);
            rect.width = formatWidth;
            if (EditorGUI.DropdownButton(rect, new GUIContent(formatString), FocusType.Keyboard))
            {
#if ENABLE_VIRTUALTEXTURES
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
#endif
            }

            // Channel transform
            rect.position += new Vector2(formatWidth, 0);
            rect.width = channelTransformWidth;

            List<string> possibleChannelTransforms = new List<string>();

#if ENABLE_VIRTUALTEXTURES
            if (formatGroups.ContainsKey(formatString))
            {
                possibleChannelTransforms = formatGroups[formatString];
            }
#endif

            EditorGUI.BeginDisabledGroup(possibleChannelTransforms.Count == 0);
            {
                if (serializedFormat != GraphicsFormat.None && EditorGUI.DropdownButton(rect, new GUIContent(channelTransformString), FocusType.Keyboard))
                {
#if ENABLE_VIRTUALTEXTURES
                    GenericMenu menu = new GenericMenu();
                    possibleChannelTransforms.Add(channelTransformString);
                    possibleChannelTransforms.Sort();

                    foreach (string possibleChannelTransform in possibleChannelTransforms)
                    {
                        string localChannelTransform = possibleChannelTransform;
                        menu.AddItem(new GUIContent(localChannelTransform), localChannelTransform == channelTransformString, () =>
                        {
                            GraphicsFormat format = FormatAndChannelTransformStringToGraphicsFormat(formatString, localChannelTransform);
                            cacheSizeOverrideProperty.FindPropertyRelative("format").intValue = (int)format;
                            serializedObject.ApplyModifiedProperties();
                        });
                    }

                    menu.ShowAsContext();
#endif
                }
            }
            EditorGUI.EndDisabledGroup();

            // Size
            rect.position += new Vector2(channelTransformWidth + spacing, 0);
            rect.width = sizeLabelWidth;

            EditorGUI.LabelField(rect, s_Styles.gpuCacheSizeOverrideSize);

            rect.position += new Vector2(sizeLabelWidth, 0);
            rect.width = sizeWidth;

            cacheSizeOverride.sizeInMegaBytes = (uint)Mathf.Max(GPUCacheSizeMinValue,
                EditorGUI.DelayedIntField(rect, (int)cacheSizeOverride.sizeInMegaBytes));
            cacheSizeOverrideProperty.FindPropertyRelative("sizeInMegaBytes").intValue =
                (int)cacheSizeOverride.sizeInMegaBytes;
        }

        void DrawStreamingOverrideElement(Rect rect, int settingIdx, bool active, bool focused)
        {
            GPUCacheSizeOverridesGUI(rect, settingIdx, m_GPUCacheSizeOverridesPropertyStreaming, GetGPUCacheSizeOverrideArrayFromProperty(serializedRPAsset.virtualTexturingSettings.streamingGpuCacheSettings));
        }

        sealed class Styles
        {
            public readonly GUIContent cpuCacheSize = new GUIContent("CPU Cache Size", "Amount of CPU memory (in MB) that can be allocated by the Streaming Virtual Texturing system to use to cache texture data.");
            public readonly GUIContent gpuCacheSize = new GUIContent("GPU Cache Size per Format", "Amount of GPU memory (in MB) that can be allocated per format by the Streaming Virtual Texturing system to cache texture data. The value assigned to None is used for all unspecified formats.");

            public readonly GUIContent gpuCacheSizeOverrideFormat = new GUIContent("Format", "Format and channel transform that will be overridden.");
            public readonly GUIContent gpuCacheSizeOverrideSize = new GUIContent("Size", "Size (in MB) of the setting.");
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
