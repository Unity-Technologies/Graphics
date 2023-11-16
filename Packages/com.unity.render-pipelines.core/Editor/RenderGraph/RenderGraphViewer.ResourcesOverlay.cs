using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering
{
    public partial class RenderGraphViewer
    {
        [Icon("Packages/com.unity.render-pipelines.core/Editor/Icons/Processed/Resources Icon.asset")]
        [Overlay(typeof(RenderGraphViewer), ViewId, "Resources", defaultLayout = Layout.Panel,
            defaultDockZone = DockZone.LeftColumn, defaultDockPosition = DockPosition.Bottom)]
        class ResourcesOverlay : OverlayBase, ITransientOverlay
        {
            public const string ViewId = "RenderGraphViewer.ResourcesOverlay";

            const string k_TemplatePath =
                "Packages/com.unity.render-pipelines.core/Editor/UXML/RenderGraphViewer.Resources.uxml";

            static class Classes
            {
                public const string kResourceListItem = "resource-list__item";
                public const string kResourceIconContainer = "resource-icon-container";
                public const string kResourceIconImported = "resource-icon--imported";
                public const string kResourceIconGlobal = "resource-icon--global";
                public const string kResourceLineBreak = "resource-line-break";
                public const string kImportedResource = "imported-resource";
                public const string kResourceSelectionAnimation = "resource-list__item--selection-animation";
            }

            public bool visible => true;

            VisualElement m_Content;

            public override VisualElement CreatePanelContent()
            {
                Init("Resources");

                if (root == null)
                {
                    var visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_TemplatePath);
                    root = visualTreeAsset.Instantiate();
                    SetDisplayState(DisplayState.Empty);

                    var themeStyleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(EditorGUIUtility.isProSkin
                            ? k_DarkStylePath
                            : k_LightStylePath);
                    root.styleSheets.Add(themeStyleSheet);
                }

                if (m_Content != null)
                    AddContentToScrollView();

                return root;
            }

            void AddContentToScrollView()
            {
                if (!FindMainScrollView(out var mainScrollView))
                    return;

                mainScrollView.Clear();
                mainScrollView.Add(m_Content);

                SetDisplayState(DisplayState.Populated);
            }

            public void PopulateContents(RenderGraphViewer viewer)
            {
                m_Content = new VisualElement();
                int resourceItemIndex = 0;
                for (int type = 0; type < viewer.m_CurrentDebugData.resourceLists.Length; type++)
                {
                    var resourceTypeFoldout = new Foldout();
                    resourceTypeFoldout.text = $"{k_ResourceNames[type]}s";

                    var resources = viewer.m_CurrentDebugData.resourceLists[type];
                    for (int i = 0; i < resources.Count; i++)
                    {
                        var res = resources[i];
                        if (!viewer.IsResourceVisible(res, (RenderGraphResourceType) type))
                            continue;

                        var resourceItem = new Foldout();
                        resourceItem.text = res.name;
                        resourceItem.Q<Toggle>().tooltip = res.name;
                        resourceItem.value = false;
                        resourceItem.userData = resourceItemIndex;
                        resourceItem.AddToClassList(Classes.kResourceListItem);
                        resourceItemIndex++;

                        var iconContainer = new VisualElement();
                        iconContainer.AddToClassList(Classes.kResourceIconContainer);

                        var importedIcon = new VisualElement();
                        importedIcon.AddToClassList(Classes.kResourceIconImported);
                        importedIcon.tooltip = "Imported resource";
                        importedIcon.style.display = res.imported ? DisplayStyle.Flex : DisplayStyle.None;
                        iconContainer.Add(importedIcon);
                        resourceItem.Q<Toggle>().Add(iconContainer);

                        if ((RenderGraphResourceType) type == RenderGraphResourceType.Texture)
                        {
                            if (res.imported)
                                resourceItem.AddToClassList(Classes.kImportedResource);

                            var lineBreak = new VisualElement();
                            lineBreak.AddToClassList(Classes.kResourceLineBreak);
                            resourceItem.Add(lineBreak);
                            resourceItem.Add(new Label($"Size: {res.width}x{res.height}x{res.depth}"));
                            resourceItem.Add(new Label($"Format: {res.format.ToString()}"));
                            resourceItem.Add(new Label($"Clear: {res.clearBuffer}"));
                            resourceItem.Add(new Label($"BindMS: {res.bindMS}"));
                            resourceItem.Add(new Label($"Samples: {res.samples}"));
                            if (viewer.m_CurrentDebugData.isNRPCompiler)
                                resourceItem.Add(new Label($"Memoryless: {res.memoryless}"));
                        }

                        resourceTypeFoldout.Add(resourceItem);
                    }

                    if (resourceTypeFoldout.childCount > 0)
                        m_Content.Add(resourceTypeFoldout);
                }

                AddContentToScrollView();
            }

            public void ScrollTo(int resourceItemIndex)
            {
                if (root == null)
                    return;

                var scrollView = root.Q<ScrollView>();
                scrollView?.Query<Foldout>(classes: Classes.kResourceListItem).ForEach(foldout =>
                {
                    int itemIndex = (int) foldout.userData;
                    if (resourceItemIndex == itemIndex)
                    {
                        // Trigger animation
                        if (!collapsed)
                        {
                            foldout.AddToClassList(Classes.kResourceSelectionAnimation);
                            foldout.RegisterCallbackOnce<TransitionEndEvent>(_ =>
                                foldout.RemoveFromClassList(Classes.kResourceSelectionAnimation));
                        }

                        // Open foldout
                        foldout.value = true;
                        // Defer scrolling to allow foldout to be expanded first
                        scrollView.schedule.Execute(() => scrollView.ScrollTo(foldout)).StartingIn(50);
                    }
                });
            }
        }
    }
}
