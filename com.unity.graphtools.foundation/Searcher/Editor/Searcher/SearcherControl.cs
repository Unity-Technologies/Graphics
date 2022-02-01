using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Searcher
{
    /// <summary>
    /// Main searcher element: search bar, list view and preview/details panel
    /// </summary>
    class SearcherControl : VisualElement
    {
        // Window constants.
        const string k_WindowTitleContainer = "windowTitleContainer";
        const string k_DetailsPanelToggleName = "detailsPanelToggle";
        const string k_WindowTitleLabel = "windowTitleLabel";
        const string k_WindowDetailsPanel = "windowDetailsVisualContainer";
        const string k_WindowResultsScrollViewName = "windowResultsScrollView";
        const string k_WindowSearchTextFieldName = "searchBox";
        const string k_WindowAutoCompleteLabelName = "autoCompleteLabel";
        const string k_SearchPlaceholderLabelName = "searchPlaceholderLabel";
        const string k_WindowResizerName = "windowResizer";
        const string k_WindowSearcherPanel = "searcherVisualContainer";
        const string k_StatusLabelName = "statusLabel";
        const int k_TabCharacter = 9;

        public static float DefaultSearchPanelWidth => 300f;
        public static float DefaultDetailsPanelWidth => 200f;
        public static float DefaultHeight => 300f;

        const int k_DefaultExtraWidthForDetailsPanel = 12;

        const string k_PreviewToggleClassName = "unity-item-library-preview-toggle";
        const string k_DetailsToggleCheckedClassName = k_PreviewToggleClassName + "__checked";
        const string k_HideDetailsTooltip = "Hide Preview";
        const string k_ShowDetailsTooltip = "Show Preview";
        const string k_SearchplaceholderlabelHiddenClassName = "searchPlaceholderLabel__hidden";

        float m_DetailsPanelExtraWidth;
        Label m_AutoCompleteLabel;
        Label m_SearchPlaceholderLabel;
        IEnumerable<SearcherItem> m_Results;
        List<SearcherItem> m_VisibleResults;
        HashSet<SearcherItem> m_ExpandedResults;
        HashSet<SearcherItem> m_MultiSelectSelection;
        Dictionary<SearcherItem, Toggle> m_SearchItemToVisualToggle;
        Searcher m_Searcher;
        string m_SuggestedTerm;
        string m_Text = string.Empty;

        Action<SearcherItem> m_SelectionCallback;
        Action<Searcher.AnalyticsEvent> m_AnalyticsDataCallback;
        Action<float> m_DetailsVisibilityCallback;

        ListView m_ListView;
        TextField m_SearchTextField;
        VisualElement m_TitleContainer;
        VisualElement m_SearchTextInput;
        VisualElement m_DetailsPanel;
        VisualElement m_SearcherPanel;
        Button m_ConfirmButton;
        Toggle m_DetailsToggle;
        Label m_StatusLabel;

        internal Label TitleLabel { get; }
        internal VisualElement TitleContainer => m_TitleContainer;
        internal VisualElement Resizer { get; }

        public string Status
        {
            get => m_StatusLabel.text;
            private set => m_StatusLabel.text = value;
        }

        public SearcherControl()
        {
            const string tpl = "Packages/com.unity.graphtools.foundation/Searcher/Editor/Templates/SearcherWindow.uxml";
            const string stylesheetDir = "Packages/com.unity.graphtools.foundation/Searcher/Editor/Templates/";
            const string stylesheetPath = stylesheetDir + "Searcher.uss";
            const string darkStylesheetPath = stylesheetDir + "Searcher_dark.uss";
            const string lightStylesheetPath = stylesheetDir + "Searcher_light.uss";

            // Load window template.
            var windowUxmlTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(tpl);
            if (windowUxmlTemplate == null)
            {
                Debug.Log("Failed to load template " + tpl);
            }

            var stylesheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(stylesheetPath);
            if (stylesheet == null)
            {
                Debug.Log("Failed to load stylesheet " + stylesheet);
            }

            var darkStylesheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(darkStylesheetPath);
            if (darkStylesheet == null)
            {
                Debug.Log("Failed to load stylesheet " + darkStylesheet);
            }

            // Clone Window Template.
            var windowRootVisualElement = windowUxmlTemplate.CloneTree();
            windowRootVisualElement.styleSheets.Add(stylesheet);

            // TODO VladN: fix for light skin, when GTF supports light skin we should only load dark or light
            windowRootVisualElement.styleSheets.Add(darkStylesheet);
            if (!EditorGUIUtility.isProSkin)
            {
                var lightStyleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(lightStylesheetPath);
                if (lightStyleSheet == null)
                {
                    Debug.Log("Failed to load stylesheet " + lightStyleSheet);
                }
                windowRootVisualElement.styleSheets.Add(lightStyleSheet);
            }

            windowRootVisualElement.AddToClassList("content");

            windowRootVisualElement.StretchToParentSize();

            // Add Window VisualElement to window's RootVisualContainer
            Add(windowRootVisualElement);

            m_VisibleResults = new List<SearcherItem>();
            m_ExpandedResults = new HashSet<SearcherItem>();
            m_MultiSelectSelection = new HashSet<SearcherItem>();
            m_SearchItemToVisualToggle = new Dictionary<SearcherItem, Toggle>();

            m_ListView = this.Q<ListView>(k_WindowResultsScrollViewName);

            if (m_ListView != null)
            {
                m_ListView.bindItem = Bind;
                m_ListView.RegisterCallback<KeyDownEvent>(SetSelectedElementInResultsList);

#if UNITY_2020_1_OR_NEWER
                m_ListView.onItemsChosen += obj => OnListViewSelect((SearcherItem)obj.FirstOrDefault());
                m_ListView.onSelectionChange += selectedItems => m_Searcher.Adapter.OnSelectionChanged(selectedItems.OfType<SearcherItem>().ToList());
#else
                m_ListView.onItemChosen += obj => OnListViewSelect((SearcherItem)obj);
                m_ListView.onSelectionChanged += selectedItems => m_Searcher.Adapter.OnSelectionChanged(selectedItems.OfType<SearcherItem>());
#endif
                m_ListView.focusable = true;
                m_ListView.tabIndex = 1;
            }

            m_TitleContainer = this.Q(k_WindowTitleContainer);

            m_DetailsPanel = this.Q(k_WindowDetailsPanel);

            TitleLabel = this.Q<Label>(k_WindowTitleLabel);

            m_SearcherPanel = this.Q(k_WindowSearcherPanel);

            m_DetailsToggle = this.Q<Toggle>(k_DetailsPanelToggleName);
            m_DetailsToggle.AddToClassList(k_PreviewToggleClassName);

            m_SearchTextField = this.Q<TextField>(k_WindowSearchTextFieldName);
            if (m_SearchTextField != null)
            {
                m_SearchTextField.focusable = true;
                m_SearchTextField.RegisterCallback<InputEvent>(OnSearchTextFieldTextChanged);

                m_SearchTextInput = m_SearchTextField.Q(TextInputBaseField<string>.textInputUssName);
                m_SearchTextInput.RegisterCallback<KeyDownEvent>(OnSearchTextFieldKeyDown);
            }

            m_AutoCompleteLabel = this.Q<Label>(k_WindowAutoCompleteLabelName);
            m_SearchPlaceholderLabel = this.Q<Label>(k_SearchPlaceholderLabelName);

            Resizer = this.Q(k_WindowResizerName);

            m_ConfirmButton = this.Q<Button>("confirmButton");
            m_ConfirmButton.clicked += OnConfirmMultiselect;

            m_StatusLabel = this.Q<Label>(k_StatusLabelName);

            RegisterCallback<AttachToPanelEvent>(OnEnterPanel);
            RegisterCallback<DetachFromPanelEvent>(OnLeavePanel);

            // TODO: HACK - ListView's scroll view steals focus using the scheduler.
            EditorApplication.update += HackDueToListViewScrollViewStealingFocus;

            style.flexGrow = 1;
            m_DetailsPanelExtraWidth = k_DefaultExtraWidthForDetailsPanel;
        }

        void OnConfirmMultiselect()
        {
            if (m_MultiSelectSelection.Count == 0)
            {
                m_SelectionCallback(null);
                return;
            }
            foreach (SearcherItem item in m_MultiSelectSelection)
            {
                m_SelectionCallback(item);
            }
        }

        void HackDueToListViewScrollViewStealingFocus()
        {
            m_SearchTextInput?.Focus();
            // ReSharper disable once DelegateSubtraction
            EditorApplication.update -= HackDueToListViewScrollViewStealingFocus;
        }

        void OnEnterPanel(AttachToPanelEvent e)
        {
            RegisterCallback<KeyDownEvent>(OnKeyDown);
        }

        void OnLeavePanel(DetachFromPanelEvent e)
        {
            UnregisterCallback<KeyDownEvent>(OnKeyDown);
        }

        void OnKeyDown(KeyDownEvent e)
        {
            if (e.keyCode == KeyCode.Escape)
            {
                CancelSearch();
            }
        }

        void OnListViewSelect(SearcherItem item)
        {
            if (!m_Searcher.Adapter.MultiSelectEnabled)
            {
                m_SelectionCallback(item);
            }
            else
            {
                ToggleItemForMultiSelect(item, !m_MultiSelectSelection.Contains(item));
            }
        }

        void CancelSearch()
        {
            OnSearchTextFieldTextChanged(InputEvent.GetPooled(m_Text, string.Empty));
            OnListViewSelect(null);
            m_AnalyticsDataCallback?.Invoke(new Searcher.AnalyticsEvent(Searcher.AnalyticsEvent.EventType.Cancelled, m_SearchTextField.value));
        }

        public void Setup(Searcher searcher, Action<SearcherItem> selectionCallback, Action<Searcher.AnalyticsEvent> analyticsDataCallback, Action<float> detailsVisibilityCallback)
        {
            m_Searcher = searcher;

            m_SelectionCallback = selectionCallback;
            m_AnalyticsDataCallback = analyticsDataCallback;
            m_DetailsVisibilityCallback = detailsVisibilityCallback;

            if (m_Searcher.Adapter.MultiSelectEnabled)
            {
                AddToClassList("searcher__multiselect");
            }

            if (m_Searcher.Adapter.HasDetailsPanel)
            {
                m_Searcher.Adapter.InitDetailsPanel(m_DetailsPanel);
                ResetSplitterRatio();
                m_DetailsPanel.style.flexGrow = m_Searcher.Adapter.InitialSplitterDetailRatio;
                m_SearcherPanel.style.flexGrow = 1;

                var showPreview = m_Searcher.IsPreviewPanelVisible();
                m_DetailsToggle.SetValueWithoutNotify(showPreview);
                SetDetailsPanelVisibility(showPreview);
                m_DetailsToggle.RegisterValueChangedCallback(OnDetailsToggleValueChange);
                m_TitleContainer.Add(m_DetailsToggle);
            }
            else
            {
                m_DetailsPanel.AddToClassList("hidden");

                var splitter = m_DetailsPanel.parent;

                splitter.parent.Insert(0, m_SearcherPanel);
                splitter.parent.Insert(1, m_DetailsPanel);

                splitter.RemoveFromHierarchy();
            }

            TitleLabel.text = m_Searcher.Adapter.Title;
            if (string.IsNullOrEmpty(TitleLabel.text))
            {
                TitleLabel.parent.style.visibility = Visibility.Hidden;
                TitleLabel.parent.style.position = Position.Absolute;
            }

            // Add a single dummy SearcherItem to warn users that data is not ready to display yet
            m_VisibleResults = new List<SearcherItem> { new SearcherItem("Indexing databases...") };
            m_ListView.itemsSource = m_VisibleResults;
            m_ListView.makeItem = MakeItem;
            RefreshListView();
            SetSelectedElementInResultsList(0);

            // leave some time for searcher to have time to display before refreshing
            // Otherwise if Refresh takes long, the searcher isn't shown at all until refresh is done
            // this happens the first time you open the searcher and your items have a lengthy "Indexing" process
            schedule.Execute(Refresh).ExecuteLater(100);
        }

        void ResetSplitterRatio()
        {
            m_DetailsPanel.style.flexGrow = 1;
            m_SearcherPanel.style.flexGrow = 1;
        }

        void SetDetailsPanelVisibility(bool showDetails)
        {
            // if details panel is still visible, store width that isn't taken into account by splitter ratio
            if (!m_DetailsPanel.ClassListContains("hidden"))
            {
                float widthDiff = m_DetailsPanel.resolvedStyle.paddingLeft + m_DetailsPanel.resolvedStyle.paddingRight +
                                  m_DetailsPanel.resolvedStyle.marginLeft + m_DetailsPanel.resolvedStyle.marginRight +
                                  m_DetailsPanel.resolvedStyle.borderLeftWidth +
                                  m_DetailsPanel.resolvedStyle.borderRightWidth;
                if (widthDiff > 0.4f || widthDiff < -0.4f)
                {
                    m_DetailsPanelExtraWidth = widthDiff;
                }
            }

            m_DetailsToggle.EnableInClassList(k_DetailsToggleCheckedClassName, showDetails);
            m_DetailsToggle.tooltip = showDetails ? k_HideDetailsTooltip : k_ShowDetailsTooltip;

            // hide or show the details/preview element
            m_DetailsPanel.EnableInClassList("hidden", !showDetails);

            // Move elements in or out of the splitter and disable it depending on visibility
            VisualElement splitter;
            if (!showDetails)
            {
                splitter = m_DetailsPanel.parent;
                splitter.parent.Add(m_SearcherPanel);
                splitter.SetEnabled(false);
                m_SearcherPanel.style.flexGrow = 1;
            }
            else
            {
                splitter = m_DetailsPanel.parent.Q("splitter");
                splitter.SetEnabled(true);
                splitter.Insert(0, m_SearcherPanel);
            }
        }

        void OnDetailsToggleValueChange(ChangeEvent<bool> evt)
        {
            var showDetails = evt.newValue;
            var rightPartWidth = m_DetailsPanel.resolvedStyle.width;
            SetDetailsPanelVisibility(showDetails);
            m_Searcher.SetPreviewPanelVisibility(showDetails);
            if (showDetails)
            {
                var leftPartWidth = m_SearcherPanel.resolvedStyle.width;
                var searcherToDetailsRatio = m_SearcherPanel.resolvedStyle.flexGrow / m_DetailsPanel.resolvedStyle.flexGrow;
                rightPartWidth = leftPartWidth / searcherToDetailsRatio + m_DetailsPanelExtraWidth;
            }

            m_DetailsVisibilityCallback?.Invoke(showDetails ? + rightPartWidth : - rightPartWidth);
        }

        void Refresh()
        {
            var query = m_Text;

            m_SearchPlaceholderLabel.EnableInClassList(k_SearchplaceholderlabelHiddenClassName, !string.IsNullOrEmpty(query));
            m_Results = m_Searcher.Search(query);
            GenerateVisibleResults();

            var visibleIndex = 0;
            m_SuggestedTerm = string.Empty;

            var results = m_Results.ToList();
            if (results.Any())
            {
                var cursorIndex = m_SearchTextField.cursorIndex;

                if (query.Length > 0)
                {
                    var maxScore = results.Max(i => i.lastSearchScore);
                    var scrollToItem = results.First();
                    if (maxScore > 0)
                    {
                        scrollToItem = results.First(i => i.lastSearchScore == maxScore);
                    }

                    // The first item in the results is always the highest scored item.
                    // We want to scroll to and select this item.
                    visibleIndex = m_VisibleResults.IndexOf(scrollToItem);

                    var strings = scrollToItem.Name.Split(' ');
                    var wordStartIndex = cursorIndex == 0 ? 0 : query.LastIndexOf(' ', cursorIndex - 1) + 1;
                    var word = query.Substring(wordStartIndex, cursorIndex - wordStartIndex);

                    if (word.Length > 0)
                        foreach (var t in strings)
                        {
                            if (t.StartsWith(word, StringComparison.OrdinalIgnoreCase))
                            {
                                m_SuggestedTerm = t;
                                break;
                            }
                        }
                }
            }

            m_ListView.itemsSource = m_VisibleResults;
            m_ListView.makeItem = MakeItem;
            RefreshListView();

            SetSelectedElementInResultsList(visibleIndex);
        }

        VisualElement MakeItem()
        {
            VisualElement item = m_Searcher.Adapter.MakeItem();
            if (m_Searcher.Adapter.MultiSelectEnabled)
            {
                var selectionToggle = item.Q<Toggle>("itemToggle");
                if (selectionToggle != null)
                {
                    selectionToggle.RegisterValueChangedCallback(changeEvent =>
                    {
                        SearcherItem searcherItem = item.userData as SearcherItem;
                        ToggleItemForMultiSelect(searcherItem, changeEvent.newValue);
                    });
                }
            }
            item.RegisterCallback<MouseDownEvent>(ExpandOrCollapse);
            return item;
        }

        void GenerateVisibleResults()
        {
            if (string.IsNullOrEmpty(m_Text))
            {
                m_ExpandedResults.Clear();
                RemoveChildrenFromResults();
                return;
            }

            RegenerateVisibleResults();
            ExpandAllParents();
        }

        void ExpandAllParents()
        {
            m_ExpandedResults.Clear();
            foreach (var item in m_VisibleResults)
                if (item.HasChildren)
                    m_ExpandedResults.Add(item);
        }

        void RemoveChildrenFromResults()
        {
            m_VisibleResults.Clear();
            var parents = new HashSet<SearcherItem>();

            foreach (var item in m_Results.Where(i => !parents.Contains(i)))
            {
                var currentParent = item;

                while (true)
                {
                    if (currentParent.Parent == null)
                    {
                        if (parents.Contains(currentParent))
                            break;

                        parents.Add(currentParent);
                        m_VisibleResults.Add(currentParent);
                        break;
                    }

                    currentParent = currentParent.Parent;
                }
            }

            if (m_Searcher.SortComparison != null)
                m_VisibleResults.Sort(m_Searcher.SortComparison);
        }

        void RegenerateVisibleResults()
        {
            var idSet = new HashSet<SearcherItem>();
            m_VisibleResults.Clear();

            foreach (var item in m_Results.Where(item => !idSet.Contains(item)))
            {
                idSet.Add(item);
                m_VisibleResults.Add(item);

                var currentParent = item.Parent;
                while (currentParent != null)
                {
                    if (!idSet.Contains(currentParent))
                    {
                        idSet.Add(currentParent);
                        m_VisibleResults.Add(currentParent);
                    }

                    currentParent = currentParent.Parent;
                }

                AddResultChildren(item, idSet);
            }

            var comparison = m_Searcher.SortComparison ?? ((i1, i2) =>
            {
                var result = i1.Database.Id - i2.Database.Id;
                return result != 0 ? result : i1.Id - i2.Id;
            });
            m_VisibleResults.Sort(comparison);
        }

        void AddResultChildren(SearcherItem item, ISet<SearcherItem> idSet)
        {
            if (!item.HasChildren)
                return;
            if (m_Searcher.Adapter.AddAllChildResults)
            {
                //add all children results for current search term
                // eg "Book" will show both "Cook Book" and "Cooking" as children
                foreach (var child in item.Children)
                {
                    if (!idSet.Contains(child))
                    {
                        idSet.Add(child);
                        m_VisibleResults.Add(child);
                    }

                    AddResultChildren(child, idSet);
                }
            }
            else
            {
                foreach (var child in item.Children)
                {
                    //only add child results if the child matches the search term
                    // eg "Book" will show "Cook Book" but not "Cooking" as a child
                    if (!m_Results.Contains(child))
                        continue;

                    if (!idSet.Contains(child))
                    {
                        idSet.Add(child);
                        m_VisibleResults.Add(child);
                    }

                    AddResultChildren(child, idSet);
                }
            }
        }

        bool HasChildResult(SearcherItem item)
        {
            if (m_Results.Contains(item))
                return true;

            foreach (var child in item.Children)
            {
                if (HasChildResult(child))
                    return true;
            }

            return false;
        }

        ItemExpanderState GetExpanderState(int index)
        {
            var item = m_VisibleResults[index];

            foreach (var child in item.Children)
            {
                if (!m_VisibleResults.Contains(child) && !HasChildResult(child))
                    continue;

                return m_ExpandedResults.Contains(item) ? ItemExpanderState.Expanded : ItemExpanderState.Collapsed;
            }

            return ItemExpanderState.Hidden;
        }

        void Bind(VisualElement target, int index)
        {
            var item = m_VisibleResults[index];
            var expanderState = GetExpanderState(index);
            var expander = m_Searcher.Adapter.Bind(target, item, expanderState, m_Text);
            var selectionToggle = target.Q<Toggle>("itemToggle");
            if (selectionToggle != null)
            {
                selectionToggle.SetValueWithoutNotify(m_MultiSelectSelection.Contains(item));
                m_SearchItemToVisualToggle[item] = selectionToggle;
            }
        }

        void ToggleItemForMultiSelect(SearcherItem item, bool selected)
        {
            if (selected)
            {
                m_MultiSelectSelection.Add(item);
            }
            else
            {
                m_MultiSelectSelection.Remove(item);
            }

            Toggle toggle;
            if (m_SearchItemToVisualToggle.TryGetValue(item, out toggle))
            {
                toggle.SetValueWithoutNotify(selected);
            }

            foreach (var child in item.Children)
            {
                ToggleItemForMultiSelect(child, selected);
            }
        }

        static void GetItemsToHide(SearcherItem parent, ref HashSet<SearcherItem> itemsToHide)
        {
            if (!parent.HasChildren)
            {
                itemsToHide.Add(parent);
                return;
            }

            foreach (var child in parent.Children)
            {
                itemsToHide.Add(child);
                GetItemsToHide(child, ref itemsToHide);
            }
        }

        void HideUnexpandedItems()
        {
            // Hide unexpanded children.
            var itemsToHide = new HashSet<SearcherItem>();
            foreach (var item in m_VisibleResults)
            {
                if (m_ExpandedResults.Contains(item))
                    continue;

                if (!item.HasChildren)
                    continue;

                if (itemsToHide.Contains(item))
                    continue;

                // We need to hide its children.
                GetItemsToHide(item, ref itemsToHide);
            }

            foreach (var item in itemsToHide)
                m_VisibleResults.Remove(item);
        }

        void RefreshListView()
        {
            m_SearchItemToVisualToggle.Clear();
#if UNITY_2021_2_OR_NEWER
            m_ListView.Rebuild();
#else
            m_ListView.Refresh();
#endif
        }

        // ReSharper disable once UnusedMember.Local
        void RefreshListViewOn()
        {
            // TODO: Call ListView.Refresh() when it is fixed.
            // Need this workaround until then.
            // See: https://fogbugz.unity3d.com/f/cases/1027728/
            // And: https://gitlab.internal.unity3d.com/upm-packages/editor/com.unity.searcher/issues/9

            var scrollView = m_ListView.Q<ScrollView>();

            var scroller = scrollView?.Q<Scroller>("VerticalScroller");
            if (scroller == null)
                return;

            var oldValue = scroller.value;
            scroller.value = oldValue + 1.0f;
            scroller.value = oldValue - 1.0f;
            scroller.value = oldValue;
        }

        void Expand(SearcherItem item)
        {
            m_ExpandedResults.Add(item);

            RegenerateVisibleResults();
            HideUnexpandedItems();

            RefreshListView();
        }

        void Collapse(SearcherItem item)
        {
            // if it's already collapsed or not collapsed
            if (!m_ExpandedResults.Remove(item))
            {
                // this case applies for a left arrow key press
                if (item.Parent != null)
                    SetSelectedElementInResultsList(m_VisibleResults.IndexOf(item.Parent));

                // even if it's a root item and has no parents, do nothing more
                return;
            }

            RegenerateVisibleResults();
            HideUnexpandedItems();

            // TODO: understand what happened
            RefreshListView();

            // RefreshListViewOn();
        }

        void ExpandOrCollapse(MouseDownEvent evt)
        {
            if (!(evt.target is VisualElement target))
                return;

            VisualElement itemElement = target.GetFirstAncestorOfType<TemplateContainer>();
            var expandingItemName = "expanderIcon";
            if (target.name != expandingItemName)
                target = itemElement.Q(expandingItemName);

            if (target == null
                || !(itemElement?.userData is SearcherItem item)
                || !item.HasChildren
                || !target.ClassListContains("Expanded") && !target.ClassListContains("Collapsed"))
                return;

            if (!m_ExpandedResults.Contains(item))
                Expand(item);
            else
                Collapse(item);

            evt.StopImmediatePropagation();
        }

        void OnSearchTextFieldTextChanged(InputEvent inputEvent)
        {
            var text = inputEvent.newData;

            if (string.Equals(text, m_Text))
                return;

            // This is necessary due to OnTextChanged(...) being called after user inputs that have no impact on the text.
            // Ex: Moving the caret.
            m_Text = text;

            // If backspace is pressed and no text remain, clear the suggestion label.
            if (string.IsNullOrEmpty(text))
            {
                // Display the unfiltered results list.
                Refresh();

                m_AutoCompleteLabel.text = String.Empty;
                m_SuggestedTerm = String.Empty;

                SetSelectedElementInResultsList(0);

                return;
            }

            Refresh();

            // Calculate the start and end indexes of the word being modified (if any).
            var cursorIndex = m_SearchTextField.cursorIndex;

            // search toward the beginning of the string starting at the character before the cursor
            // +1 because we want the char after a space, or 0 if the search fails
            var wordStartIndex = cursorIndex == 0 ? 0 : (text.LastIndexOf(' ', cursorIndex - 1) + 1);

            // search toward the end of the string from the cursor index
            var wordEndIndex = text.IndexOf(' ', cursorIndex);
            if (wordEndIndex == -1) // no space found, assume end of string
                wordEndIndex = text.Length;

            // Clear the suggestion term if the caret is not within a word (both start and end indexes are equal, ex: (space)caret(space))
            // or the user didn't append characters to a word at the end of the query.
            if (wordStartIndex == wordEndIndex || wordEndIndex < text.Length)
            {
                m_AutoCompleteLabel.text = string.Empty;
                m_SuggestedTerm = string.Empty;
                return;
            }

            var word = text.Substring(wordStartIndex, wordEndIndex - wordStartIndex);

            if (!string.IsNullOrEmpty(m_SuggestedTerm))
            {
                var wordSuggestion =
                    word + m_SuggestedTerm.Substring(word.Length, m_SuggestedTerm.Length - word.Length);
                text = text.Remove(wordStartIndex, word.Length);
                text = text.Insert(wordStartIndex, wordSuggestion);
                m_AutoCompleteLabel.text = text;
            }
            else
            {
                m_AutoCompleteLabel.text = String.Empty;
            }
        }

        void OnSearchTextFieldKeyDown(KeyDownEvent keyDownEvent)
        {
            // First, check if we cancelled the search.
            if (keyDownEvent.keyCode == KeyCode.Escape)
            {
                CancelSearch();
                return;
            }

            // For some reason the KeyDown event is raised twice when entering a character.
            // As such, we ignore one of the duplicate event.
            // This workaround was recommended by the Editor team. The cause of the issue relates to how IMGUI works
            // and a fix was not in the works at the moment of this writing.
            if (keyDownEvent.character == k_TabCharacter)
            {
                // Prevent switching focus to another visual element.
                keyDownEvent.PreventDefault();

                return;
            }

            // If Tab is pressed, complete the query with the suggested term.
            if (keyDownEvent.keyCode == KeyCode.Tab)
            {
                // Used to prevent the TAB input from executing it's default behavior. We're hijacking it for auto-completion.
                keyDownEvent.PreventDefault();

                if (!string.IsNullOrEmpty(m_SuggestedTerm))
                {
                    SelectAndReplaceCurrentWord();
                    m_AutoCompleteLabel.text = string.Empty;

                    // TODO: Revisit, we shouldn't need to do this here.
                    m_Text = m_SearchTextField.text;

                    Refresh();

                    m_SuggestedTerm = string.Empty;
                }
            }
            else
            {
                SetSelectedElementInResultsList(keyDownEvent);
            }
        }

        void SelectAndReplaceCurrentWord()
        {
            var s = m_SearchTextField.value;
            var lastWordIndex = s.LastIndexOf(' ');
            lastWordIndex++;

            var newText = s.Substring(0, lastWordIndex) + m_SuggestedTerm;

            // Wait for SelectRange api to reach trunk
            //#if UNITY_2018_3_OR_NEWER
            //            m_SearchTextField.value = newText;
            //            m_SearchTextField.SelectRange(m_SearchTextField.value.Length, m_SearchTextField.value.Length);
            //#else
            // HACK - relies on the textfield moving the caret when being assigned a value and skipping
            // all low surrogate characters
            var magicMoveCursorToEndString = new string('\uDC00', newText.Length);
            m_SearchTextField.value = magicMoveCursorToEndString;
            m_SearchTextField.value = newText;

            //#endif
        }

        void SetSelectedElementInResultsList(KeyDownEvent keyDownEvent)
        {
            int index;
            switch (keyDownEvent.keyCode)
            {
                case KeyCode.Escape:
                    OnListViewSelect(null);
                    m_AnalyticsDataCallback?.Invoke(new Searcher.AnalyticsEvent(Searcher.AnalyticsEvent.EventType.Cancelled, m_SearchTextField.value));
                    break;
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    if (m_ListView.selectedIndex != -1)
                    {
                        OnListViewSelect((SearcherItem)m_ListView.selectedItem);
                        m_AnalyticsDataCallback?.Invoke(new Searcher.AnalyticsEvent(Searcher.AnalyticsEvent.EventType.Picked, m_SearchTextField.value));
                    }
                    else
                    {
                        OnListViewSelect(null);
                        m_AnalyticsDataCallback?.Invoke(new Searcher.AnalyticsEvent(Searcher.AnalyticsEvent.EventType.Cancelled, m_SearchTextField.value));
                    }
                    break;
                case KeyCode.LeftArrow:
                    index = m_ListView.selectedIndex;
                    if (index >= 0 && index < m_ListView.itemsSource.Count)
                        Collapse(m_ListView.selectedItem as SearcherItem);
                    break;
                case KeyCode.RightArrow:
                    index = m_ListView.selectedIndex;
                    if (index >= 0 && index < m_ListView.itemsSource.Count)
                        Expand(m_ListView.selectedItem as SearcherItem);
                    break;
                case KeyCode.UpArrow:
                    index = m_ListView.selectedIndex - 1;
                    SelectItemInListView(index);
                    break;
                case KeyCode.DownArrow:
                    index = m_ListView.selectedIndex + 1;
                    SelectItemInListView(index);
                    break;
                case KeyCode.PageUp:
                    index = 0;
                    SelectItemInListView(index);
                    break;
                case KeyCode.PageDown:
                    index = m_ListView.itemsSource.Count - 1;
                    SelectItemInListView(index);
                    break;
            }
        }

        void SelectItemInListView(int index)
        {
            if (index >= 0 && index < m_ListView.itemsSource.Count)
            {
                m_ListView.selectedIndex = index;
                m_ListView.ScrollToItem(index);
            }
        }

        void SetSelectedElementInResultsList(int selectedIndex)
        {
            var newIndex = selectedIndex >= 0 && selectedIndex < m_VisibleResults.Count ? selectedIndex : -1;
            if (newIndex < 0)
                return;

            m_ListView.selectedIndex = newIndex;
            m_ListView.ScrollToItem(m_ListView.selectedIndex);
        }
    }
}
