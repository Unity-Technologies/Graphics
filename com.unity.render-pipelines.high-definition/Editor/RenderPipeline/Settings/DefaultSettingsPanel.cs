using System;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering.HighDefinition
{
    class DefaultSettingsPanelProvider
    {
        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            return new SettingsProvider("Project/HDRP Default Settings", SettingsScope.Project)
            {
                activateHandler = (searchContext, rootElement) =>
                {
                    HDEditorUtils.AddStyleSheets(rootElement);

                    var panel = new DefaultSettingsPanel(searchContext);
                    panel.style.flexGrow = 1;

                    rootElement.Add(panel);
                },
                keywords = new [] { "default", "hdrp" }
            };
        }

        class DefaultSettingsPanel : VisualElement
        {
            VolumeComponentListEditor m_ComponentList;
            Editor m_Cached;

            public DefaultSettingsPanel(string searchContext)
            {
                var scrollView = new ScrollView();
                {
                    var title = new Label
                    {
                        text = "General Settings"
                    };
                    title.AddToClassList("h1");
                    scrollView.contentContainer.Add(title);
                }
                {
                    var generalSettingsInspector = new IMGUIContainer(Draw_GeneralSettings);
                    generalSettingsInspector.style.marginLeft = 5;
                    scrollView.contentContainer.Add(generalSettingsInspector);
                }
                {
                    var space = new VisualElement();
                    space.style.height = 10;
                    scrollView.contentContainer.Add(space);
                }
                {
                    var title = new Label
                    {
                        text = "Frame Settings"
                    };
                    title.AddToClassList("h1");
                    scrollView.contentContainer.Add(title);
                }
                {
                    var generalSettingsInspector = new IMGUIContainer(Draw_DefaultFrameSettings);
                    generalSettingsInspector.style.marginLeft = 5;
                    scrollView.contentContainer.Add(generalSettingsInspector);
                }
                {
                    var space = new VisualElement();
                    space.style.height = 10;
                    scrollView.contentContainer.Add(space);
                }
                {
                    var title = new Label
                    {
                        text = "Volume Components"
                    };
                    title.AddToClassList("h1");
                    scrollView.contentContainer.Add(title);
                }
                {
                    var volumeInspector = new IMGUIContainer(Draw_VolumeInspector);
                    volumeInspector.style.flexGrow = 1;
                    volumeInspector.style.flexDirection = FlexDirection.Row;
                    scrollView.contentContainer.Add(volumeInspector);
                }

                Add(scrollView);
            }

            private static GUIContent k_DefaultHDRPAsset = new GUIContent("Asset with the default settings");
            void Draw_GeneralSettings()
            {
                var hdrpAsset = HDRenderPipeline.defaultAsset;
                if (hdrpAsset == null)
                {
                    EditorGUILayout.HelpBox("Base SRP Asset is not an HDRenderPipelineAsset.", MessageType.Warning);
                    return;
                }

                GUI.enabled = false;
                EditorGUILayout.ObjectField(k_DefaultHDRPAsset, hdrpAsset, typeof(HDRenderPipelineAsset), false);
                GUI.enabled = true;

                var serializedObject = new SerializedObject(hdrpAsset);
                var serializedHDRPAsset = new SerializedHDRenderPipelineAsset(serializedObject);

                HDRenderPipelineUI.GeneralSection.Draw(serializedHDRPAsset, null);

                serializedObject.ApplyModifiedProperties();
            }

            private static GUIContent k_DefaultVolumeProfileLabel = new GUIContent("Default Volume Profile Asset");
            void Draw_VolumeInspector()
            {
                var hdrpAsset = HDRenderPipeline.defaultAsset;
                if (hdrpAsset == null)
                    return;

                var asset = EditorDefaultSettings.GetOrAssignDefaultVolumeProfile();

                var newAsset = (VolumeProfile)EditorGUILayout.ObjectField(k_DefaultVolumeProfileLabel, asset, typeof(VolumeProfile), false);
                if (newAsset != null && newAsset != asset)
                {
                    asset = newAsset;
                    hdrpAsset.defaultVolumeProfile = asset;
                    EditorUtility.SetDirty(hdrpAsset);
                }

                Editor.CreateCachedEditor(asset,
                    Type.GetType("UnityEditor.Rendering.VolumeProfileEditor"), ref m_Cached);
                m_Cached.OnInspectorGUI();
            }

            void Draw_DefaultFrameSettings()
            {
                var hdrpAsset = HDRenderPipeline.defaultAsset;
                if (hdrpAsset == null)
                    return;

                var serializedObject = new SerializedObject(hdrpAsset);
                var serializedHDRPAsset = new SerializedHDRenderPipelineAsset(serializedObject);

                HDRenderPipelineUI.FrameSettingsSection.Draw(serializedHDRPAsset, null);
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
