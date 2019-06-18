using System.Linq;
using Object = UnityEngine.Object;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public class DefaultSettingsPanelProvider
    {
        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            return new SettingsProvider("Project/Default Settings/HDRP", SettingsScope.Project)
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
            VolumeProfile m_DefaultVolumeProfile;
            Editor m_Cached;


            public DefaultSettingsPanel(string searchContext)
            {
                m_DefaultVolumeProfile = GetOrCreateDefaultVolumeProfile();

                {
                    var title = new Label
                    {
                        text = "Volume Components"
                    };
                    title.AddToClassList("h1");
                    Add(title);
                }
                {
                    var inspectorContainer = new IMGUIContainer(Draw_VolumeInspector);
                    inspectorContainer.style.flexGrow = 1;
                    inspectorContainer.style.flexDirection = FlexDirection.Row;
                    Add(inspectorContainer);
                }
            }

            void Draw_VolumeInspector()
            {
                if (m_DefaultVolumeProfile == null || m_DefaultVolumeProfile.Equals(null))
                    m_DefaultVolumeProfile = GetOrCreateDefaultVolumeProfile();

                Editor.CreateCachedEditor(m_DefaultVolumeProfile,
                    Type.GetType("UnityEditor.Rendering.VolumeProfileEditor"), ref m_Cached);
                m_Cached.OnInspectorGUI();
            }

            static string k_DefaultVolumeAssetPath =
                $@"Packages/com.unity.render-pipelines.high-definition/Editor/RenderPipelineResources/{DefaultSettings.defaultVolumeProfileFileStem}.asset";
            static VolumeProfile GetOrCreateDefaultVolumeProfile()
            {
                // Search a VolumeProfile in the ResourceFolder with the name "DefaultSettingsVolumeProfile".
                var selectedAsset = DefaultSettings.defaultVolumeProfile;

                // Asset was found, we can return it.
                if (selectedAsset != null && !selectedAsset.Equals(null))
                    return selectedAsset;

                // There is no default asset, so create one from the asset provided by the package.
                var defaultAsset = AssetDatabase.LoadAssetAtPath<VolumeProfile>(k_DefaultVolumeAssetPath);
                Assert.IsNotNull(defaultAsset, "Default Volume Profile asset is missing");

                // Create resource folder if it is missing
                var targetPath = $"Assets/Resources/{DefaultSettings.defaultVolumeProfileFileStem}.asset";
                var parentPath = Path.GetDirectoryName(targetPath);
                if (!Directory.Exists(parentPath))
                    Directory.CreateDirectory(parentPath);

                // Deep copy of the VolumeProfile
                selectedAsset = Object.Instantiate(defaultAsset);
                selectedAsset.name = defaultAsset.name;
                AssetDatabase.CreateAsset(selectedAsset, targetPath);
                AssetDatabase.SaveAssets();

                AssetDatabase.StartAssetEditing();
                using (ListPool<VolumeComponent>.Get(out var components))
                {
                    for (int i = 0, c = selectedAsset.components.Count; i < c ; ++i)
                    {
                        var componentClone = Object.Instantiate(selectedAsset.components[i]);
                        componentClone.name = selectedAsset.components[i].name;
                        components.Add(componentClone);
                        AssetDatabase.AddObjectToAsset(componentClone, selectedAsset);
                    }

                    selectedAsset.components.Clear();
                    selectedAsset.components.AddRange(components);
                }
                AssetDatabase.StopAssetEditing();

                // Return the new default asset
                return selectedAsset;
            }
        }
    }

    class PreProcessBuild : IPreprocessBuildWithReport
    {
        public int callbackOrder { get; }

        public void OnPreprocessBuild(BuildReport report)
        {
            // Create default settings volume profile if required
            DefaultSettings.GetOrCreateDefaultVolume();
        }
    }
}
