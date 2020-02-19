using System;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.VirtualTexturing;

#if ENABLE_VIRTUALTEXTURES
namespace UnityEditor.Rendering.HighDefinition
{
    class VirtualTexturingSettingsUI
    {
        private ReorderableList m_GPUCacheSizeOverrideListStreaming;
        private SerializedProperty m_GPUCacheSizeOverridesPropertyStreaming;

        public SerializedObject serializedObject;
        private SerializedHDRenderPipelineAsset serializedRPAsset;

        public void OnGUI(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            CheckStyles();

            serializedObject = serialized.serializedObject;
            serializedRPAsset = serialized;

            serializedObject.Update();

            EditorGUILayout.Space();

            using (var scope = new EditorGUI.ChangeCheckScope())
            {
                EditorGUILayout.DelayedIntField(serialized.virtualTexturingSettings.cpuCacheSizeInMegaBytes, s_Styles.cpuCacheSize); ;
                EditorGUILayout.DelayedIntField(serialized.virtualTexturingSettings.gpuCacheSizeInMegaBytes, s_Styles.gpuCacheSize);

                // GPU Cache size overrides
                if (m_GPUCacheSizeOverrideListStreaming == null ||
                    m_GPUCacheSizeOverridesPropertyStreaming != serialized.virtualTexturingSettings.gpuCacheSizeOverridesStreaming)
                {
                    m_GPUCacheSizeOverridesPropertyStreaming = serialized.virtualTexturingSettings.gpuCacheSizeOverridesStreaming;
                    m_GPUCacheSizeOverrideListStreaming = CreateGPUCacheSizeOverrideList(m_GPUCacheSizeOverridesPropertyStreaming, DrawStreamingOverrideElement);
                }

                m_GPUCacheSizeOverrideListStreaming.DoLayoutList();
            }

            serialized.serializedObject.ApplyModifiedProperties();
        }

        VirtualTexturingGPUCacheSizeOverride[] GetGPUCacheSizeOverrideArrayFromProperty(SerializedProperty property)
        {
            List<VirtualTexturingGPUCacheSizeOverride> overrides = new List<VirtualTexturingGPUCacheSizeOverride>();
            for (int i = 0; i < property.arraySize; ++i)
            {
                SerializedProperty overrideProperty = property.GetArrayElementAtIndex(i);
                overrides.Add(new VirtualTexturingGPUCacheSizeOverride()
                    { format = (GraphicsFormat)overrideProperty.FindPropertyRelative("format").intValue, usage = (VirtualTexturingCacheUsage)overrideProperty.FindPropertyRelative("usage").intValue, sizeInMegaBytes = (uint)overrideProperty.FindPropertyRelative("sizeInMegaBytes").intValue });
            }

            return overrides.ToArray();
        }

            ReorderableList CreateGPUCacheSizeOverrideList(SerializedProperty property, ReorderableList.ElementCallbackDelegate drawCallback)
            {
                ReorderableList list = new ReorderableList(property.serializedObject, property);

                list.drawHeaderCallback =
                    (Rect rect) =>
                    {
                        EditorGUI.LabelField(rect, s_Styles.gpuCacheSizeOverrides);
                    };

                list.drawElementCallback = drawCallback;

                list.onAddCallback = (l) =>
                {
                    List<GraphicsFormat> availableFormats = new List<GraphicsFormat>(EditorHelpers.QuerySupportedFormats());

                    // We can't just pass in existing overrides as a parameter to CreateGPUCacheSizeOverrideList() because lambdas can't capture ref params.
                    VirtualTexturingGPUCacheSizeOverride[] existingOverrides = GetGPUCacheSizeOverrideArrayFromProperty(serializedRPAsset.virtualTexturingSettings.gpuCacheSizeOverridesStreaming);
                    RemoveOverriddenFormats(availableFormats, existingOverrides);

                    int index = property.arraySize;
                    property.InsertArrayElementAtIndex(index);
                    var newItemProperty = property.GetArrayElementAtIndex(index);
                    newItemProperty.FindPropertyRelative("format").intValue = availableFormats.Count > 0 ? (int)availableFormats[0] : 0;
                    newItemProperty.FindPropertyRelative("usage").intValue = (int)VirtualTexturingCacheUsage.Streaming;
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

                float formatLabelWidth = Math.Min(60, overrideWidth * 0.25f);
                float formatWidth = overrideWidth * 0.3f;
                float channelTransformWidth = overrideWidth * 0.25f;
                float sizeLabelWidth = Math.Min(45, overrideWidth * 0.2f);
                float sizeWidth = overrideWidth * 0.15f;

                // Format
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

            void DrawStreamingOverrideElement(Rect rect, int overrideIdx, bool active, bool focused)
            {
                GPUCacheSizeOverridesGUI(rect, overrideIdx, m_GPUCacheSizeOverridesPropertyStreaming, GetGPUCacheSizeOverrideArrayFromProperty(serializedRPAsset.virtualTexturingSettings.gpuCacheSizeOverridesStreaming));
            }

            sealed class Styles
            {
                public readonly GUIContent cpuCacheSize = new GUIContent("CPU Cache Size", "Amount of CPU memory (in MB) that can be allocated by the Virtual Texturing system to use to cache texture data.");
                public readonly GUIContent gpuCacheSize = new GUIContent("GPU Cache Size", "Amount of GPU memory (in MB) that can be allocated per format by the Virtual Texturing system to use to cache texture data.");
                public readonly GUIContent gpuCacheSizeOverrides = new GUIContent("Streaming GPU Cache Size Overrides", "Override the GPU cache size per format for Streaming Virtual Texturing.");

                public readonly GUIContent gpuCacheSizeOverrideFormat = new GUIContent("Format", "Format and channel transform that will be overridden.");
                public readonly GUIContent gpuCacheSizeOverrideSize = new GUIContent("Size", "Size (in MB) of the override.");
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
#endif
