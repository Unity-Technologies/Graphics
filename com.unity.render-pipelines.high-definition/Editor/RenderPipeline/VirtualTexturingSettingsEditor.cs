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
            internal SerializedProperty self;
            internal UnityEngine.Rendering.VirtualTexturing.VirtualTexturingSettings objReference;

            internal SerializedProperty cpuCacheSize;
            internal SerializedProperty gpuCacheSize;
            internal SerializedProperty gpuCacheSizeOverrides;
        }

        Settings m_Settings;

        private bool m_Dirty = false;
        private ReorderableList m_GPUCacheSizeOverrideList;
        private SerializedProperty m_GPUCacheSizeOverrideProperty;

        protected override void OnEnable()
        {
            base.OnEnable();

            var serializedSettings = properties.Find(x => x.settings);

            var rp = new RelativePropertyFetcher<UnityEngine.Rendering.VirtualTexturing.VirtualTexturingSettings>(serializedSettings);

            m_Settings = new Settings
            {
                self = serializedSettings,
                objReference = m_Target.settings,

                cpuCacheSize = rp.Find(x => x.cpuCache.sizeInMegaBytes),
                gpuCacheSize = rp.Find(x => x.gpuCache.sizeInMegaBytes),
                gpuCacheSizeOverrides = rp.Find(x => x.gpuCache.sizeOverrides),
            };
        }

        void ApplyChanges()
        {
            UnityEngine.Rendering.VirtualTexturing.System.ApplyVirtualTexturingSettings(m_Settings.objReference);
        }

        public override void OnInspectorGUI()
        {
            CheckStyles();

            serializedObject.Update();

            EditorGUILayout.Space();

            using (var scope = new EditorGUI.ChangeCheckScope())
            {
                EditorGUILayout.PropertyField(m_Settings.cpuCacheSize, s_Styles.cpuCacheSize);
                EditorGUILayout.PropertyField(m_Settings.gpuCacheSize, s_Styles.gpuCacheSize);

                if (m_GPUCacheSizeOverrideList == null ||
                    m_GPUCacheSizeOverrideProperty != m_Settings.gpuCacheSizeOverrides)
                {
                    CreateGPUCacheSizeOverrideList();
                }

                EditorGUILayout.BeginVertical();
                m_GPUCacheSizeOverrideList.DoLayoutList();
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

        void CreateGPUCacheSizeOverrideList()
        {
            m_GPUCacheSizeOverrideProperty = m_Settings.gpuCacheSizeOverrides;
            m_GPUCacheSizeOverrideList = new ReorderableList(m_Settings.gpuCacheSizeOverrides.serializedObject, m_Settings.gpuCacheSizeOverrides);

            m_GPUCacheSizeOverrideList.drawHeaderCallback = (rect) => { EditorGUI.LabelField(rect, s_Styles.gpuCacheSizeOverrides); };

            m_GPUCacheSizeOverrideList.drawElementCallback = VirtualTexturingGPUCacheSizeOverridesGUI;

            m_GPUCacheSizeOverrideList.onAddCallback = (l) =>
            {
                m_Settings.gpuCacheSizeOverrides.InsertArrayElementAtIndex(m_Settings.gpuCacheSizeOverrides.arraySize);
            };
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
        void VirtualTexturingGPUCacheSizeOverridesGUI(Rect rect, int overrideIdx, bool active, bool focused)
        {
            List<GraphicsFormat> availableFormats = new List<GraphicsFormat>(EditorHelpers.QuerySupportedFormats());
            // Remove formats already overridden
            foreach (var existingCacheSizeOverride in m_Settings.objReference.gpuCache.sizeOverrides)
            {
                availableFormats.Remove(existingCacheSizeOverride.format);
            }
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

            var cacheSizeOverride = m_Settings.objReference.gpuCache.sizeOverrides[overrideIdx];
            var cacheSizeOverrideProperty = m_GPUCacheSizeOverrideProperty.GetArrayElementAtIndex(overrideIdx);

            GraphicsFormatToFormatAndChannelTransformString(cacheSizeOverride.format, out string formatString, out string channelTransformString);

            float overrideWidth = rect.width;

            // Format
            float formatLabelWidth = Math.Min(65, overrideWidth * 0.15f);
            rect.width = formatLabelWidth;
            EditorGUI.LabelField(rect, s_Styles.gpuCacheSizeOverrideFormat);

            rect.position += new Vector2(formatLabelWidth, 0);
            float formatWidth = overrideWidth * 0.2f;
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

                        m_Settings.objReference.gpuCache.sizeOverrides[overrideIdx].format = FormatAndChannelTransformStringToGraphicsFormat(localFormat, channelTransformString);
                        cacheSizeOverrideProperty.FindPropertyRelative("format").enumValueIndex = (int) cacheSizeOverride.format;
                        serializedObject.ApplyModifiedProperties();
                        m_Dirty = true;
                    });
                }
                if (!formatGroups.ContainsKey(formatString))
                {
                    // Already selected so nothing needs to happen.
                    menu.AddItem(new GUIContent(formatString), true, () => { });
                }
                menu.ShowAsContext();
            }

            // Channel transform
            rect.position += new Vector2(formatWidth, 0);
            float channelTransformWidth = overrideWidth * 0.15f; ;
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
                            m_Settings.objReference.gpuCache.sizeOverrides[overrideIdx].format = FormatAndChannelTransformStringToGraphicsFormat(formatString, localChannelTransform);
                            cacheSizeOverrideProperty.FindPropertyRelative("format").enumValueIndex = (int)cacheSizeOverride.format;
                            serializedObject.ApplyModifiedProperties();
                            m_Dirty = true;
                        });
                    }
                }
                // Already selected so nothing needs to happen.
                menu.AddItem(new GUIContent(channelTransformString), true, () => { });
                menu.ShowAsContext();
            }

            // Usage
            rect.position += new Vector2(channelTransformWidth + overrideWidth * 0.02f, 0);
            float usageLabelWidth = Math.Min(60, overrideWidth * 0.20f);
            rect.width = usageLabelWidth;
            EditorGUI.LabelField(rect, s_Styles.gpuCacheSizeOverrideUsage);

            rect.position += new Vector2(usageLabelWidth, 0);
            float usageWidth = overrideWidth * 0.15f; ;
            rect.width = usageWidth;
            if (EditorGUI.DropdownButton(rect, new GUIContent(cacheSizeOverride.usage.ToString()), FocusType.Keyboard))
            {
                GenericMenu menu = new GenericMenu();

                foreach(VirtualTexturingCacheUsage value in Enum.GetValues(typeof(VirtualTexturingCacheUsage)))
                {
                    string localString = value.ToString();
                    VirtualTexturingCacheUsage localEnum = value;
                    menu.AddItem(new GUIContent(localString), false, () =>
                    {
                        m_Settings.objReference.gpuCache.sizeOverrides[overrideIdx].usage = localEnum;
                        cacheSizeOverrideProperty.FindPropertyRelative("usage").enumValueIndex = (int) localEnum;
                        serializedObject.ApplyModifiedProperties();
                        m_Dirty = true;
                    });
                }

                // Already selected so nothing needs to happen.
                menu.AddItem(new GUIContent(cacheSizeOverride.usage.ToString()), true, () => { });

                menu.ShowAsContext();
            }

            // Size
            rect.position += new Vector2(usageWidth + overrideWidth * 0.02f, 0);
            float sizeLabelWidth = Math.Min(45, overrideWidth * 0.1f);
            rect.width = sizeLabelWidth;
            EditorGUI.LabelField(rect, s_Styles.gpuCacheSizeOverrideSize);

            rect.position += new Vector2(sizeLabelWidth, 0);
            float sizeWidth = overrideWidth * 0.1f;
            rect.width = sizeWidth;

            cacheSizeOverride.sizeInMegaBytes = (uint)Mathf.Max(2, EditorGUI.IntField(rect, (int)cacheSizeOverride.sizeInMegaBytes));
            cacheSizeOverrideProperty.FindPropertyRelative("sizeInMegaBytes").intValue = (int)cacheSizeOverride.sizeInMegaBytes;

            m_Settings.objReference.gpuCache.sizeOverrides[overrideIdx] = cacheSizeOverride;
        }

        sealed class Styles
        {
            public readonly GUIContent cpuCacheSize = new GUIContent("CPU Cache Size");
            public readonly GUIContent gpuCacheSize = new GUIContent("GPU Cache Size");
            public readonly GUIContent gpuCacheSizeOverrides = new GUIContent("GPU cache size overrides");

            public readonly GUIContent gpuCacheSizeOverrideFormat = new GUIContent("Format:", "Format (and channel transform)");
            public readonly GUIContent gpuCacheSizeOverrideSize = new GUIContent("Size:", "Size in MegaBytes");
            public readonly GUIContent gpuCacheSizeOverrideUsage = new GUIContent("Usage:", "Override will only be used when creating caches matching the usage of the cache size override");

            public Styles()
            {

            }
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
