using System;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering
{
    public partial class RenderGraphViewer
    {
        static readonly string[] k_PassTypeNames =
        {
            "Legacy Render Pass",
            "Unsafe Render Pass",
            "Raster Render Pass",
            "Compute Pass"
        };

        static partial class Names
        {
            public const string kPanelContainer = "panel-container";
            public const string kResourceListFoldout = "panel-resource-list";
            public const string kPassListFoldout = "panel-pass-list";
            public const string kResourceSearchField = "resource-search-field";
            public const string kPassSearchField = "pass-search-field";
        }
        static partial class Classes
        {
            public const string kPanelListLineBreak = "panel-list__line-break";
            public const string kPanelListItem = "panel-list__item";
            public const string kPanelListItemSelectionAnimation = "panel-list__item--selection-animation";
            public const string kPanelResourceListItem = "panel-resource-list__item";
            public const string kPanelPassListItem = "panel-pass-list__item";
            public const string kSubHeaderText = "sub-header-text";
            public const string kInfoFoldout = "info-foldout";
            public const string kInfoFoldoutSecondaryText = "info-foldout__secondary-text";
            public const string kCustomFoldoutArrow = "custom-foldout-arrow";
        }

        static readonly System.Text.RegularExpressions.Regex k_TagRegex = new ("<[^>]*>");
        const string k_SelectionColorBeginTag = "<mark=#3169ACAB>";
        const string k_SelectionColorEndTag = "</mark>";

        TwoPaneSplitView m_SidePanelSplitView;
        bool m_ResourceListExpanded = true;
        bool m_PassListExpanded = true;
        float m_SidePanelVerticalAspectRatio = 0.5f;
        float m_SidePanelFixedPaneHeight = 0;
        float m_ContentSplitViewFixedPaneWidth = 280;

        Dictionary<VisualElement, List<TextElement>> m_ResourceDescendantCache = new ();
        Dictionary<VisualElement, List<TextElement>> m_PassDescendantCache = new ();

        void InitializeSidePanel()
        {
            m_SidePanelSplitView = rootVisualElement.Q<TwoPaneSplitView>(Names.kPanelContainer);
            rootVisualElement.RegisterCallback<GeometryChangedEvent>(_ =>
            {
                SaveSplitViewFixedPaneHeight(); // Window resized - save the current pane height
                UpdatePanelHeights();
            });

            var contentSplitView = rootVisualElement.Q<TwoPaneSplitView>(Names.kContentContainer);
            contentSplitView.fixedPaneInitialDimension = m_ContentSplitViewFixedPaneWidth;
            contentSplitView.fixedPaneIndex = 1;
            contentSplitView.fixedPane?.RegisterCallback<GeometryChangedEvent>(_ =>
            {
                float? w = contentSplitView.fixedPane?.resolvedStyle?.width;
                if (w.HasValue)
                    m_ContentSplitViewFixedPaneWidth = w.Value;
            });

            // Callbacks for dynamic height allocation between resource & pass lists
            HeaderFoldout resourceListFoldout = rootVisualElement.Q<HeaderFoldout>(Names.kResourceListFoldout);
            resourceListFoldout.value = m_ResourceListExpanded;
            resourceListFoldout.RegisterValueChangedCallback(evt =>
            {
                if (m_ResourceListExpanded)
                    SaveSplitViewFixedPaneHeight(); // Closing the foldout - save the current pane height

                m_ResourceListExpanded = resourceListFoldout.value;
                UpdatePanelHeights();
            });
            resourceListFoldout.icon = m_ResourceListIcon;
            resourceListFoldout.contextMenuGenerator = () => CreateContextMenu(resourceListFoldout.Q<ScrollView>());

            HeaderFoldout passListFoldout = rootVisualElement.Q<HeaderFoldout>(Names.kPassListFoldout);
            passListFoldout.value = m_PassListExpanded;
            passListFoldout.RegisterValueChangedCallback(evt =>
            {
                if (m_PassListExpanded)
                    SaveSplitViewFixedPaneHeight(); // Closing the foldout - save the current pane height

                m_PassListExpanded = passListFoldout.value;
                UpdatePanelHeights();
            });
            passListFoldout.icon = m_PassListIcon;
            passListFoldout.contextMenuGenerator = () => CreateContextMenu(passListFoldout.Q<ScrollView>());

            // Search fields
            var resourceSearchField = rootVisualElement.Q<ToolbarSearchField>(Names.kResourceSearchField);
            resourceSearchField.placeholderText = "Search";
            resourceSearchField.RegisterValueChangedCallback(evt => OnSearchFilterChanged(m_ResourceDescendantCache, evt.newValue));

            var passSearchField = rootVisualElement.Q<ToolbarSearchField>(Names.kPassSearchField);
            passSearchField.placeholderText = "Search";
            passSearchField.RegisterValueChangedCallback(evt => OnSearchFilterChanged(m_PassDescendantCache, evt.newValue));
        }

        bool IsSearchFilterMatch(string str, string searchString, out int startIndex, out int endIndex)
        {
            startIndex = -1;
            endIndex = -1;

            startIndex = str.IndexOf(searchString, 0, StringComparison.CurrentCultureIgnoreCase);
            if (startIndex == -1)
                return false;

            endIndex = startIndex + searchString.Length - 1;
            return true;
        }

        private IVisualElementScheduledItem m_PreviousSearch;
        private string m_PendingSearchString = string.Empty;
        private const int k_SearchStringLimit = 15;
        void OnSearchFilterChanged(Dictionary<VisualElement, List<TextElement>> elementCache, string searchString)
        {
            // Ensure the search string is within the allowed length limit (15 chars max)
            if (searchString.Length > k_SearchStringLimit)
            {
                searchString = searchString[..k_SearchStringLimit];  // Trim to max 15 chars
                Debug.LogWarning("[Render Graph Viewer] Search string limit exceeded: " + k_SearchStringLimit);
            }

            // If the search string hasn't changed, avoid repeating the same search
            if (m_PendingSearchString == searchString)
                return;

            m_PendingSearchString = searchString;

            if (m_PreviousSearch != null && m_PreviousSearch.isActive)
                m_PreviousSearch.Pause();

            m_PreviousSearch = rootVisualElement
                .schedule
                .Execute(() =>
                {
                    PerformSearchAsync(elementCache, searchString);
                })
                .StartingIn(5); // Avoid spamming multiple search if the user types really fast
        }

        private void PerformSearchAsync(Dictionary<VisualElement, List<TextElement>> elementCache, string searchString)
        {
            // Display filter
            foreach (var (foldout, descendants) in elementCache)
            {
                bool anyDescendantMatchesSearch = false;
                foreach (var elem in descendants)
                {
                    // Remove any existing highlight
                    var text = elem.text;
                    var hasHighlight = k_TagRegex.IsMatch(text);
                    text = k_TagRegex.Replace(text, string.Empty);
                    if (!IsSearchFilterMatch(text, searchString, out int startHighlight, out int endHighlight))
                    {
                        if (hasHighlight)
                            elem.text = text;
                        continue;
                    }


                    text = text.Insert(startHighlight, k_SelectionColorBeginTag);
                    text = text.Insert(endHighlight + k_SelectionColorBeginTag.Length + 1, k_SelectionColorEndTag);
                    elem.text = text;
                    anyDescendantMatchesSearch = true;
                }
                foldout.style.display = anyDescendantMatchesSearch ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        void SetChildFoldoutsExpanded(VisualElement elem, bool expanded)
        {
            elem.Query<Foldout>().ForEach(f => f.value = expanded);
        }

        GenericMenu CreateContextMenu(VisualElement content)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Collapse All"), false, () => SetChildFoldoutsExpanded(content, false));
            menu.AddItem(new GUIContent("Expand All"), false, () => SetChildFoldoutsExpanded(content, true));
            return menu;
        }

        void PopulateResourceList()
        {
            ScrollView content = rootVisualElement.Q<HeaderFoldout>(Names.kResourceListFoldout).Q<ScrollView>();
            content.Clear();

            UpdatePanelHeights();

            m_ResourceDescendantCache.Clear();

            int visibleResourceIndex = 0;
            foreach (var visibleResourceElement in m_ResourceElementsInfo)
            {
                var resourceData = m_CurrentDebugData.resourceLists[(int)visibleResourceElement.type][visibleResourceElement.index];

                var resourceItem = new Foldout();
                resourceItem.text = resourceData.name;
                resourceItem.value = false;
                resourceItem.userData = visibleResourceIndex;
                resourceItem.AddToClassList(Classes.kPanelListItem);
                resourceItem.AddToClassList(Classes.kPanelResourceListItem);
                resourceItem.AddToClassList(Classes.kCustomFoldoutArrow);
                visibleResourceIndex++;

                var iconContainer = new VisualElement();
                iconContainer.AddToClassList(Classes.kResourceIconContainer);

                var importedIcon = new VisualElement();
                importedIcon.AddToClassList(Classes.kResourceIconImported);
                importedIcon.tooltip = "Imported resource";
                importedIcon.style.display = resourceData.imported ? DisplayStyle.Flex : DisplayStyle.None;
                iconContainer.Add(importedIcon);

                var foldoutCheckmark = resourceItem.Q("unity-checkmark");
                // Add resource type icon before the label
                foldoutCheckmark.parent.Insert(1, CreateResourceTypeIcon(visibleResourceElement.type));
                foldoutCheckmark.parent.Add(iconContainer);
                foldoutCheckmark.BringToFront(); // Move foldout checkmark to the right

                // Add imported icon to the right of the foldout checkmark
                var toggleContainer = resourceItem.Q<Toggle>();
                toggleContainer.tooltip = resourceData.name;

                RenderGraphResourceType type = visibleResourceElement.type;
                if (type == RenderGraphResourceType.Texture && resourceData.textureData != null)
                {
                    var lineBreak = new VisualElement();
                    lineBreak.AddToClassList(Classes.kPanelListLineBreak);
                    resourceItem.Add(lineBreak);
                    resourceItem.Add(new Label($"Size: {resourceData.textureData.width}x{resourceData.textureData.height}x{resourceData.textureData.depth}"));
                    resourceItem.Add(new Label($"Format: {resourceData.textureData.format.ToString()}"));
                    resourceItem.Add(new Label($"Clear: {resourceData.textureData.clearBuffer}"));
                    resourceItem.Add(new Label($"BindMS: {resourceData.textureData.bindMS}"));
                    resourceItem.Add(new Label($"Samples: {resourceData.textureData.samples}"));
                    if (m_CurrentDebugData.isNRPCompiler)
                        resourceItem.Add(new Label($"Memoryless: {resourceData.memoryless}"));
                }
                else if (type == RenderGraphResourceType.Buffer && resourceData.bufferData != null)
                {
                    var lineBreak = new VisualElement();
                    lineBreak.AddToClassList(Classes.kPanelListLineBreak);
                    resourceItem.Add(lineBreak);
                    resourceItem.Add(new Label($"Count: {resourceData.bufferData.count}"));
                    resourceItem.Add(new Label($"Stride: {resourceData.bufferData.stride}"));
                    resourceItem.Add(new Label($"Target: {resourceData.bufferData.target.ToString()}"));
                    resourceItem.Add(new Label($"Usage: {resourceData.bufferData.usage.ToString()}"));
                }

                content.Add(resourceItem);

                m_ResourceDescendantCache[resourceItem] = resourceItem.Query().Descendents<TextElement>().ToList();
            }
        }

        void PopulatePassList()
        {
            HeaderFoldout headerFoldout = rootVisualElement.Q<HeaderFoldout>(Names.kPassListFoldout);
            if (!m_CurrentDebugData.isNRPCompiler)
            {
                headerFoldout.style.display = DisplayStyle.None;
                return;
            }
            headerFoldout.style.display = DisplayStyle.Flex;

            ScrollView content = headerFoldout.Q<ScrollView>();
            content.Clear();

            UpdatePanelHeights();

            m_PassDescendantCache.Clear();

            void CreateTextElement(VisualElement parent, string text, string className = null)
            {
                var textElement = new TextElement();
                textElement.text = text;
                if (className != null)
                    textElement.AddToClassList(className);
                parent.Add(textElement);
            }

            HashSet<int> addedPasses = new HashSet<int>();

            foreach (var visiblePassElement in m_PassElementsInfo)
            {
                if (addedPasses.Contains(visiblePassElement.passId))
                    continue; // Add only one item per merged pass group

                List<RenderGraph.DebugData.PassData> passDatas = new();
                List<string> passNames = new();
                var groupedPassIds = GetGroupedPassIds(visiblePassElement.passId);
                foreach (int groupedId in groupedPassIds) {
                    addedPasses.Add(groupedId);
                    passDatas.Add(m_CurrentDebugData.passList[groupedId]);
                    passNames.Add(m_CurrentDebugData.passList[groupedId].name);
                }

                var passItem = new Foldout();
                var passesText = string.Join(", ", passNames);
                passItem.text = $"<b>{passesText}</b>";
                passItem.Q<Toggle>().tooltip = passesText;
                passItem.value = false;
                passItem.userData = m_PassIdToVisiblePassIndex[visiblePassElement.passId];
                passItem.AddToClassList(Classes.kPanelListItem);
                passItem.AddToClassList(Classes.kPanelPassListItem);

                //Native pass info (duplicated for each pass group so just look at the first)
                var firstPassData = passDatas[0];
                var nativePassInfo = firstPassData.nrpInfo?.nativePassInfo;

                if (nativePassInfo != null)
                {
                    if (nativePassInfo.mergedPassIds.Count == 1)
                        CreateTextElement(passItem, "Native Pass was created from Raster Render Pass.");
                    else if (nativePassInfo.mergedPassIds.Count > 1)
                        CreateTextElement(passItem, $"Native Pass was created by merging {nativePassInfo.mergedPassIds.Count} Raster Render Passes.");

                    CreateTextElement(passItem, "Pass break reasoning", Classes.kSubHeaderText);
                    CreateTextElement(passItem, nativePassInfo.passBreakReasoning);
                }
                else
                {
                    CreateTextElement(passItem, "Pass break reasoning", Classes.kSubHeaderText);
                    var msg = $"This is a {k_PassTypeNames[(int) firstPassData.type]}. Only Raster Render Passes can be merged.";
                    msg = msg.Replace("a Unsafe", "an Unsafe");
                    CreateTextElement(passItem, msg);
                }

                if (nativePassInfo != null)
                {
                    CreateTextElement(passItem, "Render Graph Pass Info", Classes.kSubHeaderText);
                    foreach (int passId in groupedPassIds)
                    {
                        var pass = m_CurrentDebugData.passList[passId];
                        Debug.Assert(pass.nrpInfo != null); // This overlay currently assumes NRP compiler

                        var passFoldout = new Foldout();
                        passFoldout.text = $"<b>{pass.name}</b> ({k_PassTypeNames[(int) pass.type]})";

                        var foldoutTextElement = passFoldout.Q<TextElement>(className: Foldout.textUssClassName);
                        foldoutTextElement.displayTooltipWhenElided = false; // no tooltip override when ellipsis is active

                        bool hasSubpassIndex = pass.nativeSubPassIndex != -1;
                        if (hasSubpassIndex)
                        {
                            // Abuse Foldout to allow two-line header: add line break <br> at the end of the actual foldout text to increase height,
                            // then inject a second label into the hierarchy starting with a line break to offset it to the second line.
                            passFoldout.text += "<br>";
                            Label subpassIndexLabel = new Label($"<br>Subpass #{pass.nativeSubPassIndex}");
                            subpassIndexLabel.AddToClassList(Classes.kInfoFoldoutSecondaryText);
                            foldoutTextElement.Add(subpassIndexLabel);
                        }

                        passFoldout.AddToClassList(Classes.kInfoFoldout);
                        passFoldout.AddToClassList(Classes.kCustomFoldoutArrow);
                        passFoldout.Q<Toggle>().tooltip = $"The {k_PassTypeNames[(int) pass.type]} <b>{pass.name}</b> belongs to native subpass {pass.nativeSubPassIndex}.";

                        var foldoutCheckmark = passFoldout.Q("unity-checkmark");
                        foldoutCheckmark.BringToFront(); // Move foldout checkmark to the right

                        var lineBreak = new VisualElement();
                        lineBreak.AddToClassList(Classes.kPanelListLineBreak);
                        passFoldout.Add(lineBreak);

                        CreateTextElement(passFoldout,
                            $"Attachment dimensions: {pass.nrpInfo.width}x{pass.nrpInfo.height}x{pass.nrpInfo.volumeDepth}");
                        CreateTextElement(passFoldout, $"Has depth attachment: {pass.nrpInfo.hasDepth}");
                        CreateTextElement(passFoldout, $"MSAA samples: {pass.nrpInfo.samples}");
                        CreateTextElement(passFoldout, $"Async compute: {pass.async}");

                        passItem.Add(passFoldout);
                    }

                    CreateTextElement(passItem, "Attachment Load/Store Actions", Classes.kSubHeaderText);
                    if (nativePassInfo != null && nativePassInfo.attachmentInfos.Count > 0)
                    {
                        foreach (var attachmentInfo in nativePassInfo.attachmentInfos)
                        {
                            var attachmentFoldout = new Foldout();

                            string subResourceText = string.Empty;
                            if (attachmentInfo.attachment.mipLevel > 0) subResourceText += $" Mip:{attachmentInfo.attachment.mipLevel}";
                            if (attachmentInfo.attachment.depthSlice > 0) subResourceText += $" Slice:{attachmentInfo.attachment.depthSlice}";

                            // Abuse Foldout to allow two-line header (same as above)
                            attachmentFoldout.text = $"<b>{attachmentInfo.resourceName + subResourceText}</b><br>";
                            Label attachmentIndexLabel = new Label($"<br>Attachment #{attachmentInfo.attachmentIndex}");
                            attachmentIndexLabel.AddToClassList(Classes.kInfoFoldoutSecondaryText);

                            var foldoutTextElement = attachmentFoldout.Q<TextElement>(className: Foldout.textUssClassName);
                            foldoutTextElement.displayTooltipWhenElided = false; // no tooltip override when ellipsis is active
                            foldoutTextElement.Add(attachmentIndexLabel);

                            attachmentFoldout.AddToClassList(Classes.kInfoFoldout);
                            attachmentFoldout.AddToClassList(Classes.kCustomFoldoutArrow);
                            attachmentFoldout.Q<Toggle>().tooltip = $"Texture <b>{attachmentInfo.resourceName}</b> is bound at attachment index {attachmentInfo.attachmentIndex}.";

                            var foldoutCheckmark = attachmentFoldout.Q("unity-checkmark");
                            foldoutCheckmark.BringToFront(); // Move foldout checkmark to the right

                            var lineBreak = new VisualElement();
                            lineBreak.AddToClassList(Classes.kPanelListLineBreak);
                            attachmentFoldout.Add(lineBreak);

                            attachmentFoldout.Add(new TextElement
                            {
                                text = $"<b>Load action:</b> {attachmentInfo.attachment.loadAction}\n- {attachmentInfo.loadReason}"
                            });

                            bool addMsaaInfo = !string.IsNullOrEmpty(attachmentInfo.storeMsaaReason);
                            string resolvedTexturePrefix = addMsaaInfo ? "Resolved surface: " : "";

                            string storeActionText = $"<b>Store action:</b> {attachmentInfo.attachment.storeAction}" +
                                                     $"\n - {resolvedTexturePrefix}{attachmentInfo.storeReason}";

                            if (addMsaaInfo)
                            {
                                string msaaTexturePrefix = "MSAA surface: ";
                                storeActionText += $"\n - {msaaTexturePrefix}{attachmentInfo.storeMsaaReason}";
                            }

                            attachmentFoldout.Add(new TextElement { text = storeActionText });

                            passItem.Add(attachmentFoldout);
                        }
                    }
                    else
                    {
                        CreateTextElement(passItem, "No attachments.");
                    }
                }

                content.Add(passItem);

                m_PassDescendantCache[passItem] = passItem.Query().Descendents<TextElement>().ToList();
            }
        }

        void SaveSplitViewFixedPaneHeight()
        {
            m_SidePanelFixedPaneHeight = m_SidePanelSplitView.fixedPane?.resolvedStyle?.height ?? 0;
        }

        void UpdatePanelHeights()
        {
            bool passListExpanded = m_PassListExpanded && (m_CurrentDebugData != null && m_CurrentDebugData.isNRPCompiler);
            const int kFoldoutHeaderHeightPx = 18;
            const int kFoldoutHeaderExpandedMinHeightPx = 50;
            const int kWindowExtraMarginPx = 6;

            var resourceList = rootVisualElement.Q<HeaderFoldout>(Names.kResourceListFoldout);
            var passList = rootVisualElement.Q<HeaderFoldout>(Names.kPassListFoldout);

            resourceList.style.minHeight = kFoldoutHeaderHeightPx;
            passList.style.minHeight = kFoldoutHeaderHeightPx;

            float panelHeightPx = position.height - kHeaderContainerHeightPx - kWindowExtraMarginPx;
            if (!m_ResourceListExpanded)
            {
                m_SidePanelSplitView.fixedPaneInitialDimension = kFoldoutHeaderHeightPx;
            }
            else if (!passListExpanded)
            {
                m_SidePanelSplitView.fixedPaneInitialDimension = panelHeightPx - kFoldoutHeaderHeightPx;
            }
            else
            {
                // Update aspect ratio in case user has dragged the split view
                if (m_SidePanelFixedPaneHeight > kFoldoutHeaderHeightPx && m_SidePanelFixedPaneHeight < panelHeightPx - kFoldoutHeaderHeightPx)
                {
                    m_SidePanelVerticalAspectRatio = m_SidePanelFixedPaneHeight / panelHeightPx;
                }
                m_SidePanelSplitView.fixedPaneInitialDimension = panelHeightPx * m_SidePanelVerticalAspectRatio;

                resourceList.style.minHeight = kFoldoutHeaderExpandedMinHeightPx;
                passList.style.minHeight = kFoldoutHeaderExpandedMinHeightPx;
            }

            // Ensure fixed pane initial dimension gets applied in case it has already been set
            m_SidePanelSplitView.fixedPane.style.height = m_SidePanelSplitView.fixedPaneInitialDimension;

            // Disable drag line when one of the foldouts is collapsed
            var dragLine = m_SidePanelSplitView.Q("unity-dragline");
            var dragLineAnchor = m_SidePanelSplitView.Q("unity-dragline-anchor");
            if (!m_ResourceListExpanded || !passListExpanded)
            {
                dragLine.pickingMode = PickingMode.Ignore;
                dragLineAnchor.pickingMode = PickingMode.Ignore;
            }
            else
            {
                dragLine.pickingMode = PickingMode.Position;
                dragLineAnchor.pickingMode = PickingMode.Position;
            }
        }

        void ScrollToPass(int visiblePassIndex)
        {
            var passFoldout = rootVisualElement.Q<HeaderFoldout>(Names.kPassListFoldout);
            ScrollToFoldout(passFoldout, visiblePassIndex);
        }

        void ScrollToResource(int visibleResourceIndex)
        {
            var resourceFoldout = rootVisualElement.Q<HeaderFoldout>(Names.kResourceListFoldout);
            ScrollToFoldout(resourceFoldout, visibleResourceIndex);
        }

        void ScrollToFoldout(VisualElement parent, int index)
        {
            ScrollView scrollView = parent.Q<ScrollView>();
            scrollView.Query<Foldout>(classes: Classes.kPanelListItem).ForEach(foldout =>
            {
                if (index == (int) foldout.userData)
                {
                    // Trigger animation
                    foldout.AddToClassList(Classes.kPanelListItemSelectionAnimation);

                    // This repaint hack is needed because transition animations have poor framerate. So we are hooking to editor update
                    // loop for the duration of the animation to force repaints and have a smooth highlight animation.
                    // See https://jira.unity3d.com/browse/UIE-1326
                    EditorApplication.update += Repaint;

                    foldout.RegisterCallbackOnce<TransitionEndEvent>(_ =>
                    {
                        // "Highlight in" animation finished
                        foldout.RemoveFromClassList(Classes.kPanelListItemSelectionAnimation);
                        foldout.RegisterCallbackOnce<TransitionEndEvent>(_ =>
                        {
                            // "Highlight out" animation finished
                            EditorApplication.update -= Repaint;
                        });
                    });

                    // Open foldout
                    foldout.value = true;
                    // Defer scrolling to allow foldout to be expanded first
                    scrollView.schedule.Execute(() => scrollView.ScrollTo(foldout)).StartingIn(50);
                }
            });
        }
    }
}
