using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Searcher
{
    /// <summary>
    /// Main searcher element: search bar, list view and preview/details panel
    /// </summary>
    class SearcherControl : VisualElement
    {
        const string k_TemplateName = "SearcherWindow.uxml";
        const string k_StylesheetName = "Searcher.uss";

        // Window constants.
        const string k_WindowTitleContainer = "windowTitleContainer";
        const string k_DetailsPanelToggleName = "detailsPanelToggle";
        const string k_WindowTitleLabel = "windowTitleLabel";
        const string k_SearchBoxContainerName = "windowSearchBoxVisualContainer";
        const string k_WindowDetailsPanel = "windowDetailsVisualContainer";
        const string k_WindowResultsScrollViewName = "windowResultsScrollView";
        const string k_WindowSearchTextFieldName = "searchBox";
        const string k_WindowAutoCompleteLabelName = "autoCompleteLabel";
        const string k_SearchPlaceholderLabelName = "searchPlaceholderLabel";
        const string k_WindowResizerName = "windowResizer";
        const string k_WindowSearcherPanel = "searcherVisualContainer";
        const string k_ConfirmButtonName = "confirmButton";
        const int k_TabCharacter = 9;

        public static float DefaultSearchPanelWidth => 300f;
        public static float DefaultDetailsPanelWidth => 200f;
        public static float DefaultHeight => 300f;

        const int k_DefaultExtraWidthForDetailsPanel = 12;

        const string k_PreviewToggleClassName = "unity-item-library-preview-toggle";
        const string k_DetailsToggleCheckedClassName = k_PreviewToggleClassName + "--checked";

        const string k_HideDetailsTooltip = "Hide Preview";
        const string k_ShowDetailsTooltip = "Show Preview";
        const string k_SearchplaceholderlabelHiddenClassName = "searchPlaceholderLabel--hidden";

        float m_DetailsPanelExtraWidth;
        Label m_AutoCompleteLabel;
        Label m_SearchPlaceholderLabel;
        Searcher m_Searcher;
        string m_SuggestedCompletion;
        string m_Text = string.Empty;

        Action<Searcher.AnalyticsEvent> m_AnalyticsDataCallback;
        Action<float> m_DetailsVisibilityCallback;

        SearcherTreeView m_TreeView;
        TextField m_SearchTextField;
        VisualElement m_TitleContainer;
        VisualElement m_SearchTextInput;
        VisualElement m_DetailsPanel;
        VisualElement m_SearcherPanel;
        Toggle m_DetailsToggle;
        Action<SearcherItem> m_SelectionCallback;

        internal Label TitleLabel { get; }
        internal VisualElement TitleContainer => m_TitleContainer;
        internal VisualElement Resizer { get; }

        public SearcherControl()
        {
            this.AddStylesheetWithSkinVariants(k_StylesheetName);

            var windowUxmlTemplate = VisualElementsHelpers.LoadUXML(k_TemplateName);
            VisualElement rootElement = windowUxmlTemplate.CloneTree();
            rootElement.AddToClassList("content");
            rootElement.AddToClassList("unity-theme-env-variables");
            rootElement.AddToClassList("item-library-theme");
            rootElement.StretchToParentSize();
            Add(rootElement);

            var listView = this.Q<ListView>(k_WindowResultsScrollViewName);

            if (listView != null)
            {
                m_TreeView = new SearcherTreeView
                {
#if UNITY_2021_2_OR_NEWER
                    fixedItemHeight = 25,
#else
                    itemHeight = 25,
#endif
                    focusable = true,
                    tabIndex = 1
                };
                m_TreeView.OnModelViewSelectionChange += OnTreeviewSelectionChange;
                var listViewParent = listView.parent;
                listViewParent.Insert(0, m_TreeView);
                listView.RemoveFromHierarchy();
            }

            m_TitleContainer = this.Q(k_WindowTitleContainer);

            var searchBox = this.Q(k_SearchBoxContainerName);
            searchBox.AddToClassList(SearchFieldBase<TextField, string>.ussClassName);

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
                m_SearchTextInput.RegisterCallback<KeyDownEvent>(OnSearchTextFieldKeyDown, TrickleDown.TrickleDown);

                m_SearchTextField.AddToClassList(TextInputBaseField<string>.ussClassName);
            }

            m_AutoCompleteLabel = this.Q<Label>(k_WindowAutoCompleteLabelName);
            m_SearchPlaceholderLabel = this.Q<Label>(k_SearchPlaceholderLabelName);

            Resizer = this.Q(k_WindowResizerName);

            var confirmButton = this.Q<Button>(k_ConfirmButtonName);
            confirmButton.clicked += m_TreeView.ConfirmMultiselect;

            // TODO: HACK - ListView's scroll view steals focus using the scheduler.
            EditorApplication.update += HackDueToListViewScrollViewStealingFocus;

            style.flexGrow = 1;
            m_DetailsPanelExtraWidth = k_DefaultExtraWidthForDetailsPanel;
        }

        void OnTreeviewSelectionChange(IReadOnlyList<ISearcherTreeItemView> selection)
        {
            var selectedItems = selection
                .OfType<ISearcherItemView>()
                .Select(siv => siv.SearcherItem)
                .ToList();
            m_Searcher.Adapter.OnSelectionChanged(selectedItems);
            if (m_Searcher.Adapter.HasDetailsPanel)
            {
                m_Searcher.Adapter.UpdateDetailsPanel(selectedItems.FirstOrDefault());
            }
        }

        void HackDueToListViewScrollViewStealingFocus()
        {
            m_SearchTextInput?.Focus();
            // ReSharper disable once DelegateSubtraction
            EditorApplication.update -= HackDueToListViewScrollViewStealingFocus;
        }

        void OnKeyDown(KeyDownEvent e)
        {
            if (e.keyCode == KeyCode.Escape)
            {
                m_SelectionCallback(null);
                e.StopPropagation();
            }
        }

        public void Setup(Searcher searcher, Action<SearcherItem> selectionCallback, Action<Searcher.AnalyticsEvent> analyticsDataCallback, Action<float> detailsVisibilityCallback)
        {
            m_Searcher = searcher;
            m_AnalyticsDataCallback = analyticsDataCallback;
            m_SelectionCallback = selectionCallback;

            if (!string.IsNullOrEmpty(searcher?.Adapter.CustomStyleSheetPath))
                this.AddStylesheetWithSkinVariantsByPath(searcher.Adapter.CustomStyleSheetPath);

            m_TreeView.Setup(searcher, SelectionCallback);
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

            // leave some time for searcher to have time to display before refreshing
            // Otherwise if Refresh takes long, the searcher isn't shown at all until refresh is done
            // this happens the first time you open the searcher and your items have a lengthy "Indexing" process
            schedule.Execute(Refresh).ExecuteLater(100);
        }

        void SelectionCallback(SearcherItem item)
        {
            var eventType = item == null ? Searcher.AnalyticsEvent.EventType.Cancelled : Searcher.AnalyticsEvent.EventType.Picked;
            m_AnalyticsDataCallback?.Invoke(new Searcher.AnalyticsEvent(eventType, m_SearchTextField.value));
            m_SelectionCallback(item);
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
            var noQuery = string.IsNullOrEmpty(query);

            m_SearchPlaceholderLabel.EnableInClassList(k_SearchplaceholderlabelHiddenClassName, !string.IsNullOrEmpty(query));
            var results = m_Searcher.Search(query).ToList();

            m_SuggestedCompletion = string.Empty;

            if (results.Any() && !noQuery)
            {
                m_SuggestedCompletion = GetAutoCompletionSuggestion(query, results);
            }

            m_TreeView.ViewMode =
                noQuery ? SearcherResultsViewMode.Hierarchy : SearcherResultsViewMode.Flat;
            m_TreeView.SetResults(results);
        }

        static string GetAutoCompletionSuggestion(string query, IReadOnlyList<SearcherItem> results)
        {
            var bestMatch = results
                .Select(si => si.Name)
                .FirstOrDefault(n => n.StartsWith(query, StringComparison.OrdinalIgnoreCase));
            if (bestMatch != null && bestMatch.Length > query.Length && bestMatch[query.Length] != ' ')
            {
                var lastSpace = bestMatch.IndexOf(' ', query.Length);
                int completionSize = lastSpace == -1 ? bestMatch.Length : lastSpace;
                var autoCompletionSuggestion = bestMatch.Substring(query.Length, completionSize - query.Length);
                return autoCompletionSuggestion;
            }
            return string.Empty;
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
                m_SuggestedCompletion = String.Empty;

                return;
            }

            Refresh();

            if (!string.IsNullOrEmpty(m_SuggestedCompletion))
            {
                m_AutoCompleteLabel.text = text + m_SuggestedCompletion;
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
                m_SelectionCallback(null);
                keyDownEvent.StopPropagation();
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

                if (!string.IsNullOrEmpty(m_SuggestedCompletion))
                {
                    SelectAndReplaceCurrentWord();
                    m_AutoCompleteLabel.text = string.Empty;

                    // TODO: Revisit, we shouldn't need to do this here.
                    m_Text = m_SearchTextField.text;

                    Refresh();

                    m_SuggestedCompletion = string.Empty;
                }
            }
            else
            {
                keyDownEvent.StopPropagation();
                using (KeyDownEvent eKeyDown = KeyDownEvent.GetPooled(keyDownEvent.character, keyDownEvent.keyCode,
                    keyDownEvent.modifiers))
                {
                    eKeyDown.target = m_TreeView;
                    SendEvent(eKeyDown);
                }
            }
        }

        void SelectAndReplaceCurrentWord()
        {
            var newText = m_SearchTextField.value + m_SuggestedCompletion;

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
    }
}
