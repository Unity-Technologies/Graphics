using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering.HighDefinition
{
    public class QualitySettingsPanel
    {
        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            return new SettingsProvider("Project/Quality/HDRP", SettingsScope.Project)
            {
                activateHandler = (searchContext, rootElement) =>
                {
                    HDEditorUtils.AddStyleSheets(rootElement, HDEditorUtils.FormatingPath);
                    HDEditorUtils.AddStyleSheets(rootElement, HDEditorUtils.QualitySettingsSheetPath);

                    var panel = new QualitySettingsPanelVisualElement(searchContext);
                    panel.style.flexGrow = 1;

                    rootElement.Add(panel);
                },
                keywords = new [] { "quality", "hdrp" }
            };
        }

        class HDRPAssetHeaderEntry : VisualElement
        {
            TextElement m_Element;
            List<Label> m_ConfigNames;

            public HDRPAssetHeaderEntry()
            {
                m_Element = new Label();
                m_ConfigNames = new List<Label>();

                m_Element.style.paddingLeft = 5;
                m_Element.style.flexGrow = 1;
                m_Element.style.flexDirection = FlexDirection.Row;
                m_Element.style.unityTextAlign = TextAnchor.MiddleLeft;

                Add(m_Element);
            }

            public void Bind(int index, HDRPAssetLocations asset)
            {
                // Update number of label in cache for this row
                var desiredNumberOfConfig = asset.indices.Count + (asset.isDefault ? 1 : 0);
                var configNameCountDiff = desiredNumberOfConfig - m_ConfigNames.Count;
                for (var i = 0; i < -configNameCountDiff; ++i)
                {
                    Remove(m_ConfigNames[0]);
                    m_ConfigNames.RemoveAt(0);
                }
                for (var i = 0; i < configNameCountDiff; ++i)
                {
                    var newText = new Label();
                    newText.AddToClassList("unity-quality-entry-tag");
                    Add(newText);
                    m_ConfigNames.Add(newText);
                }
                Assert.AreEqual(desiredNumberOfConfig, m_ConfigNames.Count);

                m_Element.text = asset.asset.name;
                for (var i = 0; i < asset.indices.Count; ++i)
                    m_ConfigNames[i].text = QualitySettings.names[asset.indices[i]];
                if (asset.isDefault)
                    m_ConfigNames[m_ConfigNames.Count - 1].text = "default";

                RemoveFromClassList("even");
                RemoveFromClassList("odd");
                AddToClassList((index & 1) == 1 ? "odd" : "even");
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

        class QualitySettingsPanelVisualElement : VisualElement
        {
            List<HDRPAssetLocations> m_HDRPAssets;
            Label m_InspectorTitle;
            ListView m_HDRPAssetList;
            Editor m_Cached;

            public QualitySettingsPanelVisualElement(string searchContext)
            {
                // Get the assigned HDRP assets
                m_HDRPAssets = new List<HDRPAssetLocations>();
                if (GraphicsSettings.renderPipelineAsset is HDRenderPipelineAsset hdrp)
                    m_HDRPAssets.Add(new HDRPAssetLocations(true, hdrp));

                var qualityLevelCount = QualitySettings.names.Length;
                for (var i = 0; i < qualityLevelCount; ++i)
                {
                    if (!(QualitySettings.GetRenderPipelineAssetAt(i) is HDRenderPipelineAsset hdrp2))
                        continue;

                    var index = m_HDRPAssets.FindIndex(a => a.asset == hdrp2);
                    if (index >= 0)
                        m_HDRPAssets[index].indices.Add(i);
                    else
                    {
                        var loc = new HDRPAssetLocations(false, hdrp2);
                        loc.indices.Add(i);
                        m_HDRPAssets.Add(loc);
                    }
                }

                m_HDRPAssets.Sort((l, r) => string.CompareOrdinal(l.asset.name, r.asset.name));

                // title
                var title = new Label()
                {
                    text = "Quality"
                };
                title.AddToClassList("h1");

                // Header
                var headerBox = new VisualElement();
                headerBox.style.height = 200;

                m_HDRPAssetList = new ListView()
                {
                    bindItem = (el, i) => ((HDRPAssetHeaderEntry)el).Bind(i, m_HDRPAssets[i]),
                    itemHeight = (int)EditorGUIUtility.singleLineHeight + 3,
                    selectionType = SelectionType.Single,
                    itemsSource = m_HDRPAssets,
                    makeItem = () => new HDRPAssetHeaderEntry(),
                };
                m_HDRPAssetList.AddToClassList("unity-quality-header-list");
#if UNITY_2020_1_OR_NEWER
                m_HDRPAssetList.onSelectionChange += OnSelectionChange;
#else
                m_HDRPAssetList.onSelectionChanged += OnSelectionChanged;
#endif

                headerBox.Add(m_HDRPAssetList);

                m_InspectorTitle = new Label();
                m_InspectorTitle.text = "No asset selected";
                m_InspectorTitle.AddToClassList("h1");

                // Inspector
                var inspector = new IMGUIContainer(DrawInspector);
                inspector.style.flexGrow = 1;
                inspector.style.flexDirection = FlexDirection.Row;

                var inspectorBox = new ScrollView();
                inspectorBox.style.flexGrow = 1;
                inspectorBox.style.flexDirection = FlexDirection.Row;
                inspectorBox.contentContainer.Add(inspector);

                Add(title);
                Add(headerBox);
                Add(m_InspectorTitle);
                Add(inspectorBox);
            }

#if UNITY_2020_1_OR_NEWER
            void OnSelectionChange(IEnumerable<object> obj)
#else
            void OnSelectionChanged(List<object> obj)
#endif
            {
                m_InspectorTitle.text = m_HDRPAssets[m_HDRPAssetList.selectedIndex].asset.name;
            }

            void DrawInspector()
            {
                var selected = m_HDRPAssetList.selectedIndex;
                if (selected < 0)
                    return;

                Editor.CreateCachedEditor(m_HDRPAssets[selected].asset, typeof(HDRenderPipelineEditor), ref m_Cached);
                ((HDRenderPipelineEditor) m_Cached).largeLabelWidth = false;
                m_Cached.OnInspectorGUI();
            }
        }
    }
}
