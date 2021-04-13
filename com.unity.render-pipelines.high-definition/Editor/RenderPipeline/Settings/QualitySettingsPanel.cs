using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using UnityEditorInternal;
using System.Linq;

namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    /// Defines the provider of the quality settings panel for HDRP.
    /// </summary>
    class QualitySettingsPanel
    {
        static QualitySettingsPanelIMGUI s_IMGUIImpl = new QualitySettingsPanelIMGUI();

        /// <summary>
        /// Instantiate the <see cref="SettingsProvider"/> used for the Quality Settings Panel for the HDRP.
        /// </summary>
        /// <returns></returns>
        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            return new SettingsProvider("Project/Quality/HDRP", SettingsScope.Project)
            {
                activateHandler = s_IMGUIImpl.OnActivate,
                keywords = SettingsProvider.GetSearchKeywordsFromGUIContentProperties<QualitySettingsPanelIMGUI.Styles>()
                    .Concat(SettingsProvider.GetSearchKeywordsFromGUIContentProperties<HDRenderPipelineUI.Styles>())
                    .Concat(SettingsProvider.GetSearchKeywordsFromGUIContentProperties<HDRenderPipelineUI.Styles.GeneralSection>())
                    .ToArray(),
                guiHandler = s_IMGUIImpl.OnGUI,
            };
        }

        class QualitySettingsPanelIMGUI
        {
            public class Styles
            {
                public const int HDRPAssetListMinHeight = 150;
                public const int HDRPAssetListMaxHeight = 150;

                public static readonly GUIContent @default = new GUIContent("default");
                public const string hdrpSubtitleHelp = "HDRP Assets that are assigned either in Graphics settings or in any Quality Level will be listed here.";
            }

            static GUIContent s_CachedGUIContent = new GUIContent();

            Vector2 m_HDRPAssetListScrollView = Vector2.zero;
            List<HDRPAssetLocations> m_HDRPAssets = new List<HDRPAssetLocations>();
            ReorderableList m_HDRPAssetsUIList;
            Editor m_Cached;
            int m_SelectedHDRPAssetIndex = -1;

            public QualitySettingsPanelIMGUI()
            {
                m_HDRPAssetsUIList = new ReorderableList(m_HDRPAssets, typeof(HDRPAssetLocations), false, false, false, false)
                {
                    drawElementCallback = DrawHDRPAssetItem,
                    onSelectCallback = OnHDRPAssetSelected,
                };
            }

            /// <summary>
            /// Executed when activate is called from the settings provider.
            /// </summary>
            /// <param name="searchContext"></param>
            /// <param name="rootElement"></param>
            public void OnActivate(string searchContext, VisualElement rootElement)
            {
                m_HDRPAssets.Clear();
                PopulateHDRPAssetsFromQualitySettings(m_HDRPAssets);
            }

            /// <summary>
            /// entry point of the GUI of the HDRP's quality settings panel
            /// </summary>
            public void OnGUI(string searchContext)
            {
                // Draw HDRP asset list
                EditorGUILayout.LabelField(Styles.hdrpSubtitleHelp, EditorStyles.largeLabel, GUILayout.Height(22));
                m_HDRPAssetListScrollView = EditorGUILayout.BeginScrollView(
                    m_HDRPAssetListScrollView,
                    GUILayout.MinHeight(Styles.HDRPAssetListMinHeight),
                    GUILayout.MaxHeight(Styles.HDRPAssetListMaxHeight)
                );
                m_HDRPAssetsUIList.DoLayoutList();
                EditorGUILayout.EndScrollView();

                // Draw HDRP Asset inspector
                if (m_SelectedHDRPAssetIndex >= 0 && m_SelectedHDRPAssetIndex < m_HDRPAssets.Count)
                {
                    var asset = m_HDRPAssets[m_SelectedHDRPAssetIndex];
                    s_CachedGUIContent.text = asset.asset.name;
                    EditorGUILayout.LabelField(s_CachedGUIContent, EditorStyles.largeLabel);

                    Editor.CreateCachedEditor(asset.asset, typeof(HDRenderPipelineEditor), ref m_Cached);
                    ((HDRenderPipelineEditor)m_Cached).largeLabelWidth = false;
                    m_Cached.OnInspectorGUI();
                }
            }

            /// <summary>
            /// Draw an HDRP Asset item in the top list of the panel
            /// </summary>
            void DrawHDRPAssetItem(Rect rect, int index, bool isActive, bool isFocused)
            {
                void DrawTag(ref Rect _rect, GUIContent label)
                {
                    EditorStyles.label.CalcMinMaxWidth(label, out var minWidth, out var maxWidth);
                    minWidth += 6;
                    rect.x -= minWidth;
                    rect.width = minWidth;
                    GUI.Box(rect, label);
                    rect.x -= 2;
                }

                var asset = m_HDRPAssets[index];
                EditorGUI.LabelField(rect, asset.asset.name);

                rect.x = rect.x + rect.width;
                if (asset.isDefault)
                    DrawTag(ref rect, Styles.@default);
                for (var i = 0; i < asset.indices.Count; ++i)
                {
                    s_CachedGUIContent.text = QualitySettings.names[asset.indices[i]];
                    DrawTag(ref rect, s_CachedGUIContent);
                }
            }

            /// <summary>
            /// Called when an item is selected in the HDRPAsset list
            /// </summary>
            /// <param name="list"></param>
            void OnHDRPAssetSelected(ReorderableList list)
            {
                m_SelectedHDRPAssetIndex = list.index;
            }

            /// <summary>
            /// Compute HDRPAssetLocations[] from currently assign hdrp assets in quality settings
            /// </summary>
            static void PopulateHDRPAssetsFromQualitySettings(List<HDRPAssetLocations> target)
            {
                if (GraphicsSettings.renderPipelineAsset is HDRenderPipelineAsset hdrp)
                    target.Add(new HDRPAssetLocations(true, hdrp));

                var qualityLevelCount = QualitySettings.names.Length;
                for (var i = 0; i < qualityLevelCount; ++i)
                {
                    if (!(QualitySettings.GetRenderPipelineAssetAt(i) is HDRenderPipelineAsset hdrp2))
                        continue;

                    var index = target.FindIndex(a => a.asset == hdrp2);
                    if (index >= 0)
                        target[index].indices.Add(i);
                    else
                    {
                        var loc = new HDRPAssetLocations(false, hdrp2);
                        loc.indices.Add(i);
                        target.Add(loc);
                    }
                }

                target.Sort((l, r) => string.CompareOrdinal(l.asset.name, r.asset.name));
            }
        }

        struct HDRPAssetLocations
        {
            public readonly bool isDefault;
            public readonly List<int> indices;
            public readonly HDRenderPipelineAsset asset;

            public HDRPAssetLocations(bool isDefault, HDRenderPipelineAsset asset)
            {
                this.asset = asset;
                this.isDefault = isDefault;
                this.indices = new List<int>();
            }
        }
    }
}
