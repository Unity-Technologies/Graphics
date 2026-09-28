using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor.Categorization;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering.Converter
{
    /// <summary>
    /// User data stored in toggle elements for folder items in the tree view.
    /// Contains the node data and optional callback for event cleanup.
    /// </summary>
    internal struct FolderToggleUserData
    {
        public TreeNodeData nodeData;
        public EventCallback<ChangeEvent<bool>> callback;
    }

    internal class RenderPipelineConverterVisualElement : VisualElement
    {
        const string k_Uxml = "Packages/com.unity.render-pipelines.core/Editor-PrivateShared/Tools/Converter/Window/RenderPipelineConverterVisualElement.uxml";
        const string k_Uss = "Packages/com.unity.render-pipelines.core/Editor-PrivateShared/Tools/Converter/Window/RenderPipelineConverterVisualElement.uss";

        const string k_ColumnName = "name";
        const string k_ColumnInfo = "info";
        const string k_ColumnState = "state";

        static Lazy<VisualTreeAsset> s_VisualTreeAsset = new Lazy<VisualTreeAsset>(() => AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_Uxml));
        static Lazy<StyleSheet> s_StyleSheet = new Lazy<StyleSheet>(() => AssetDatabase.LoadAssetAtPath<StyleSheet>(k_Uss));

        Node<ConverterInfo> m_ConverterInfo;

        public string displayName => m_ConverterInfo.name;
        public string description => m_ConverterInfo.description;

        public ConverterState state => m_ConverterInfo.data.state;
        public IRenderPipelineConverter converter => m_ConverterInfo.data.converter as IRenderPipelineConverter;

        public bool isSelectedAndEnabled => converter.isEnabled && state.isSelected;

        VisualElement m_RootVisualElement;
        HeaderFoldout m_HeaderFoldout;
        VisualElement m_ListViewHeader;
        HelpBox m_NoItemsFound;
        HelpBox m_PressScan;
        RenderPipelineConverterVisualElementListFilter m_Filter;
        MultiColumnTreeView m_TreeView;

        public Action converterSelected;

        private bool m_IsEnabled;

        private void SetEnabled(bool value, bool force = false)
        {
            if (m_IsEnabled != value || force)
            {
                m_IsEnabled = value;
                m_HeaderFoldout.tooltip = (m_IsEnabled) ? description : converter.isDisabledMessage;
                m_HeaderFoldout.SetEnabled(m_IsEnabled);
            }
        }

        public RenderPipelineConverterVisualElement(Node<ConverterInfo> converterInfo)
        {
            m_ConverterInfo = converterInfo;

            m_RootVisualElement = new VisualElement();
            s_VisualTreeAsset.Value.CloneTree(m_RootVisualElement);
            m_RootVisualElement.styleSheets.Add(s_StyleSheet.Value);

            m_HeaderFoldout = m_RootVisualElement.Q<HeaderFoldout>("conveterFoldout");
            m_HeaderFoldout.text = displayName;

            SetEnabled(converter.isEnabled, true);
            m_HeaderFoldout.schedule.Execute(() => SetEnabled(converter.isEnabled)).Every(500);

            m_HeaderFoldout.value = state.isExpanded;
            m_HeaderFoldout.RegisterCallback<ChangeEvent<bool>>((evt) =>
            {
                state.isExpanded = evt.newValue;
            });
            m_HeaderFoldout.showEnableCheckbox = true;
            m_HeaderFoldout.documentationURL = converterInfo.helpUrl;

            // Add context menu for expand/collapse all
            m_HeaderFoldout.contextMenuGenerator = () =>
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("Expand All"), false, () => ExpandCollapseAll(true));
                menu.AddItem(new GUIContent("Collapse All"), false, () => ExpandCollapseAll(false));
                return menu;
            };

            m_HeaderFoldout.enableToggle.SetValueWithoutNotify(state.isSelected);
            m_HeaderFoldout.enableToggle.RegisterCallback<ClickEvent>((evt) =>
            {
                state.isSelected = !state.isSelected;
                converterSelected?.Invoke();
                UpdateConversionInfo();
            });

            var allLabel = m_RootVisualElement.Q<Label>("all");
            allLabel.RegisterCallback<ClickEvent>((evt) =>
            {
                SetItemsActive(true);
                Refresh();
            });
            var noneLabel = m_RootVisualElement.Q<Label>("none");
            noneLabel.RegisterCallback<ClickEvent>((evt) =>
            {
                SetItemsActive(false);
                Refresh();
            });

            m_ListViewHeader = m_RootVisualElement.Q("listViewHeader");

            m_NoItemsFound = m_RootVisualElement.Q<HelpBox>("noItemsFoundHelpBox");
            m_NoItemsFound.style.display = DisplayStyle.None;

            m_PressScan = m_RootVisualElement.Q<HelpBox>("pressScanHelpBox");

            m_TreeView = m_RootVisualElement.Q<MultiColumnTreeView>("converterItemsTreeView");

            // Ensure tree is built (may be needed after assembly reload since OnAfterDeserialize only marks dirty)
            state.ApplyFilter();
            m_TreeView.SetRootItems<TreeNodeData>(state.filteredItemsTree);

            // Disable column sorting - items are pre-sorted (folders first, then by name)
            m_TreeView.sortingMode = ColumnSortingMode.None;

            // Register callbacks to track expanded/collapsed state in real-time
            m_TreeView.itemExpandedChanged += (args) =>
            {
                // Get the tree node data directly and update its folder state
                var nodeData = m_TreeView.GetItemDataForId<TreeNodeData>(args.id);
                if (nodeData != null && nodeData.itemState is FolderItemState folderState)
                {
                    folderState.isExpanded = args.isExpanded;

                    // Handle recursive collapse/expand (Alt+click or programmatic)
                    // If this item has children, update their states recursively
                    UpdateDescendantExpandedStates(nodeData, args.isExpanded);
                }
            };

            m_Filter = m_RootVisualElement.Q<RenderPipelineConverterVisualElementListFilter>("listViewFilter");
            m_Filter.Bind(state);
            m_Filter.onFilterChanged += () =>
            {
                state.ApplyFilter();
                m_TreeView.SetRootItems<TreeNodeData>(state.filteredItemsTree);
                m_TreeView.RefreshItems();
                RestoreTreeViewExpandedState();
            };

            var nameColumn = m_TreeView.columns[k_ColumnName];
            nameColumn.makeCell = MakeNameCell;
            nameColumn.bindCell = BindNameCell;
            nameColumn.unbindCell = UnbindNameCell;

            var infoColumn = m_TreeView.columns[k_ColumnInfo];
            infoColumn.stretchable = true;
            infoColumn.makeCell = () =>
            {
                var label = new Label();
                label.AddToClassList("render-pipeline-converter-items-name-label");
                return label;
            };
            infoColumn.bindCell = (VisualElement element, int index) =>
            {
                var nodeData = m_TreeView.GetItemDataForIndex<TreeNodeData>(index);
                var label = (element as Label);

                if (nodeData.isFolder)
                {
                    // Show selected of total count
                    int totalCount = 0;
                    int selectedCount = GetSelectedChildCount(nodeData, out totalCount);
                    label.text = $"{selectedCount} of {totalCount}";
                    label.tooltip = string.Empty;
                }
                else if (nodeData.itemState != null)
                {
                    label.text = nodeData.itemState.item.info;
                    label.tooltip = nodeData.itemState.item.info;
                }
            };

            var stateColumn = m_TreeView.columns[k_ColumnState];
            stateColumn.makeCell = () => new Image();
            stateColumn.bindCell = (VisualElement element, int index) =>
            {
                var nodeData = m_TreeView.GetItemDataForIndex<TreeNodeData>(index);
                var image = (element as Image);

                if (nodeData.isFolder)
                {
                    // Folders don't show conversion status
                    image.image = null;
                    image.tooltip = "";
                }
                else if (nodeData.itemState != null)
                {
                    (Status Status, string Message) conversionResult = nodeData.itemState.conversionResult;

                    Texture2D icon = null;
                    string tooltip = conversionResult.Message;
                    Status status = conversionResult.Status;
                    switch (status)
                    {
                        case Status.Pending:
                            icon = CoreEditorStyles.iconPending;
                            if (string.IsNullOrEmpty(tooltip))
                                tooltip = "This item is pending conversion. Click the Convert button to convert it.";
                            break;
                        case Status.Error:
                            icon = CoreEditorStyles.iconFail;
                            break;
                        case Status.Warning:
                            icon = CoreEditorStyles.iconWarn;
                            break;
                        case Status.Success:
                            icon = CoreEditorStyles.iconComplete;
                            break;
                    }

                    image.image = icon;
                    image.tooltip = tooltip;
                }
            };

            Add(m_RootVisualElement);

            // Set up event handlers for state changes
            // This is needed both for new states and after assembly reload
            SetupStateEventHandlers();

            // Restore tree view expanded state after assembly reload
            RestoreTreeViewExpandedState();

            Refresh();
        }

        private void SetupStateEventHandlers()
        {
            // Set up event handlers on root items to refresh UI when selection changes
            // This needs to be called both during initial scan and after assembly reload
            state.SetupRootEventHandlers((isSelected) =>
            {
                m_TreeView.RefreshItems();
                UpdateSelectedConverterItemsLabel();
                UpdateAllNoneLabels();
            });
        }

        private void SetItemsActive(bool value)
        {
            state.SetAllItemsSelected(value);
        }

        public void UpdateInfo()
        {
            UpdateConversionInfo();
            UpdateSelectedConverterItemsLabel();
            UpdateAllNoneLabels();
        }

        public void OnSelectionChanged(bool isSelected)
        {
            UpdateSelectedConverterItemsLabel();
            UpdateAllNoneLabels();
        }

        void UpdateSelectedConverterItemsLabel()
        {
            var text = $" ({state.selectedItemsCount} of {state.totalItemsCount})";
            m_RootVisualElement.Q<Label>("converterStats").text = text;
        }

        void UpdateAllNoneLabels()
        {
            var allLabel = m_RootVisualElement.Q<Label>("all");
            var noneLabel = m_RootVisualElement.Q<Label>("none");

            var count = state.totalItemsCount;
            int selectedCount = state.selectedItemsCount;

            bool noneSelected = selectedCount == 0;
            bool allSelected = selectedCount == count;
            SetSelected(allLabel, allSelected);
            SetSelected(noneLabel, noneSelected);
        }

        void SetSelected(Label label, bool selected)
        {
            if (selected)
            {
                label.RemoveFromClassList("not_selected");
                label.AddToClassList("selected");
            }
            else
            {
                label.RemoveFromClassList("selected");
                label.AddToClassList("not_selected");
            }
        }

        void UpdateConversionInfo()
        {
            if (state.isInitialized)
            {
                m_PressScan.style.display = DisplayStyle.None;
                if (state.totalItemsCount > 0)
                {
                    state.ApplyFilter();
                    m_NoItemsFound.style.display = DisplayStyle.None;
                    m_ListViewHeader.style.display = DisplayStyle.Flex;
                    m_TreeView.style.display = DisplayStyle.Flex;
                    m_Filter.Update(state);
                }
                else
                {
                    m_NoItemsFound.style.display = DisplayStyle.Flex;
                    m_ListViewHeader.style.display = DisplayStyle.None;
                    m_TreeView.style.display = DisplayStyle.None;
                }
            }
            else
            {
                m_PressScan.style.display = DisplayStyle.Flex;

                m_NoItemsFound.style.display = DisplayStyle.None;
                m_ListViewHeader.style.display = DisplayStyle.None;
                m_TreeView.style.display = DisplayStyle.None;
            }
        }

        private void ExpandAllFolders(IEnumerable<TreeViewItemData<TreeNodeData>> items)
        {
            foreach (var item in items)
            {
                if (item.data.isFolder && item.data.itemState is FolderItemState folderState)
                {
                    // Set the folder state to expanded
                    folderState.isExpanded = true;
                    m_TreeView.ExpandItem(item.id);

                    // Recursively expand children
                    if (item.hasChildren)
                    {
                        ExpandAllFolders(item.children);
                    }
                }
            }
        }

        private static int GetSelectedChildCount(TreeNodeData node, out int totalCount)
        {
            totalCount = 0;
            if (node.childItems == null)
                return 0;

            int selectedCount = 0;
            foreach (var child in node.childItems)
            {
                if (child.item.isEnabled)
                {
                    totalCount++;
                    if (child.isSelected == true)
                        selectedCount++;
                }
            }
            return selectedCount;
        }

        public void Refresh()
        {
            m_TreeView.RefreshItems();
            UpdateInfo();
            m_HeaderFoldout.SetEnabled(converter.isEnabled);
        }

        /// <summary>
        /// Restores tree view expanded state from folder states.
        /// Should only be called after SetRootItems, not on every refresh.
        /// </summary>
        private void RestoreTreeViewExpandedState()
        {
            if (m_TreeView == null)
                return;

            RestoreExpandedStateRecursive(state.filteredItemsTree);
        }

        private void RestoreExpandedStateRecursive(IEnumerable<TreeViewItemData<TreeNodeData>> items)
        {
            foreach (var item in items)
            {
                if (item.data.isFolder && item.data.itemState is FolderItemState folderState)
                {
                    if (folderState.isExpanded)
                        m_TreeView.ExpandItem(item.id);
                    else
                        m_TreeView.CollapseItem(item.id);

                    if (item.hasChildren)
                        RestoreExpandedStateRecursive(item.children);
                }
            }
        }

        public void Scan(Action onScanFinish)
        {
            state.Clear();
            state.isLoading = true;
            converter.Scan(OnConverterCompleteDataCollection);

            void OnConverterCompleteDataCollection(List<IRenderPipelineConverterItem> items)
            {
                foreach(var item in items)
                {
                    state.AddItem(item);
                }

                // Set up event handlers (using the shared method)
                SetupStateEventHandlers();

                state.isLoading = false;
                state.isInitialized = true;

                // Expand the foldout when we perform a search
                state.isExpanded = true;
                m_HeaderFoldout.value = true;

                // Build the tree structure from the items
                state.ApplyFilter();

                m_TreeView.SetRootItems<TreeNodeData>(state.filteredItemsTree);
                m_TreeView.Rebuild();

                // Expand all folders recursively by default
                ExpandAllFolders(state.filteredItemsTree);

                // Refresh filter toggle states (filter was reset to All in Clear())
                m_Filter.RefreshToggleStates();

                Refresh();
                onScanFinish?.Invoke();
            }
        }

        public void Convert(string progressTitle, StringBuilder sb)
        {
            int selectedPendingCount = state.selectedPending;
            if (selectedPendingCount == 0)
            {
                sb.AppendLine($"[{displayName}] Skipping conversion.");
                return;
            }

            sb.AppendLine($"[{displayName}]");

            converter.BeforeConvert();
            int itemToConvertIndex = 0;
            foreach (var itemState in state.GetAllLeafItems())
            {
                // Skip invalid items (can happen after failed deserialization)
                if (itemState.item == null)
                    continue;

                // Skip items that are not selected or already converted
                if (itemState.hasConverted || itemState.isSelected != true)
                    continue;

                var itemName = itemState.item.name ?? "(unnamed)";

                if (EditorUtility.DisplayCancelableProgressBar(progressTitle,
                    $"({itemToConvertIndex + 1} of {selectedPendingCount}) {itemName}",
                    itemToConvertIndex / (float)selectedPendingCount))
                    break;

                try
                {
                    var status = converter.Convert(itemState.item, out var message);
                    switch (status)
                    {
                        case Status.Pending:
                            throw new InvalidOperationException("Converter returned a pending status when converting. This is not supported.");
                        case Status.Error:
                        case Status.Warning:
                            sb.AppendLine($"\t- {itemName} ({status}) ({message})");
                            break;
                        case Status.Success:
                        {
                            sb.AppendLine($"\t- {itemName} ({status})");
                            message = "Conversion successful!";
                        }
                            break;
                    }

                    // Update the tuple as a whole
                    itemState.conversionResult = (status, message);
                }
                catch(Exception ex)
                {
                    Debug.LogError($"Exception {ex.Message} while converting {itemName} from {displayName}");
                }
                itemToConvertIndex++;
            }
            converter.AfterConvert();

            Refresh();

            sb.AppendLine(state.ToString());
        }

        private VisualElement MakeNameCell()
        {
            // Create a container with toggle + icon + label in horizontal layout
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            container.style.alignItems = Align.Center;

            var toggle = new Toggle();
            toggle.AddToClassList("render-pipeline-converter-items-toggle");
            toggle.name = "item-toggle";

            var icon = new Image();
            icon.AddToClassList("render-pipeline-converter-items-icon");
            icon.name = "item-icon";

            var label = new Label();
            label.AddToClassList("render-pipeline-converter-items-name-label");
            label.name = "item-label";

            container.Add(toggle);
            container.Add(icon);
            container.Add(label);

            return container;
        }

        private void BindNameCell(VisualElement element, int index)
        {
            TreeNodeData nodeData = m_TreeView.GetItemDataForIndex<TreeNodeData>(index);

            var container = element;
            var toggle = container.Q<Toggle>("item-toggle");
            var icon = container.Q<Image>("item-icon");
            var label = container.Q<Label>("item-label");

            if (toggle == null || icon == null || label == null || nodeData == null)
                return;

            // Store nodeData in userData for unbind
            label.userData = nodeData;

            // Set label text
            label.text = nodeData.displayName;

            // Bind icon
            if (nodeData.itemState != null)
            {
                var itemIcon = nodeData.itemState.item.icon;
                if (itemIcon != null)
                {
                    icon.image = itemIcon;
                    icon.style.display = DisplayStyle.Flex;
                }
                else
                {
                    icon.image = null;
                    icon.style.display = DisplayStyle.None;
                }
            }
            else
            {
                icon.image = null;
                icon.style.display = DisplayStyle.None;
            }

            // Bind toggle
            if (nodeData.isFolder)
            {
                // FOLDER NODE - Tri-state checkbox
                toggle.style.display = DisplayStyle.Flex;
                toggle.style.visibility = Visibility.Visible;

                // Check if folder is enabled (at least one child is enabled)
                bool folderEnabled = nodeData.itemState?.item.isEnabled ?? false;
                toggle.SetEnabled(folderEnabled);

                var (isChecked, isIndeterminate) = nodeData.GetFolderCheckboxState();

                toggle.showMixedValue = isIndeterminate;
                toggle.SetValueWithoutNotify(isChecked);

                if (folderEnabled)
                {
                    if (isIndeterminate)
                    {
                        toggle.tooltip = "Some items selected - click to select all";
                    }
                    else
                    {
                        toggle.tooltip = isChecked ? "All items selected - click to deselect all" : "No items selected - click to select all";
                    }

                    // Create callback that captures the current nodeData
                    EventCallback<ChangeEvent<bool>> callback = (evt) =>
                    {
                        bool newValue = evt.newValue;

                        // If currently indeterminate, clicking should select all (set to true)
                        var (currentIsChecked, currentIsIndeterminate) = nodeData.GetFolderCheckboxState();
                        if (currentIsIndeterminate)
                            newValue = true;

                        nodeData.itemState.SetSelectedWithoutNotify(newValue);
                        Refresh();
                    };

                    // Store callback in toggle's userData for later cleanup
                    toggle.userData = new FolderToggleUserData
                    {
                        nodeData = nodeData,
                        callback = callback
                    };

                    toggle.RegisterValueChangedCallback(callback);
                }
                else
                {
                    // Folder is disabled - show reason in tooltip
                    toggle.tooltip = nodeData.itemState?.item.isDisabledMessage ?? "All items in this folder are already converted or disabled";
                    // Store nodeData for consistency (no callback since it's disabled)
                    toggle.userData = new FolderToggleUserData
                    {
                        nodeData = nodeData,
                        callback = null
                    };
                }
            }
            else if (nodeData.itemState != null)
            {
                // LEAF NODE - Regular checkbox
                toggle.showMixedValue = false;
                toggle.style.display = DisplayStyle.Flex;
                toggle.style.visibility = Visibility.Visible;

                if (nodeData.itemState.item.isEnabled)
                {
                    toggle.SetEnabled(true);
                    toggle.tooltip = "Select/Deselect this item for conversion";
                    toggle.SetValueWithoutNotify(nodeData.itemState.isSelected ?? false);

                    // Store nodeData for cleanup
                    toggle.userData = nodeData;

                    // Register click to toggle selection - root event handler will refresh UI
                    toggle.RegisterCallback<ClickEvent>(nodeData.itemState.OnSelectionChanged);

                    // Register click for label
                    label.RegisterCallback<ClickEvent>(nodeData.itemState.OnClicked);
                }
                else
                {
                    toggle.SetEnabled(false);
                    toggle.tooltip = nodeData.itemState.item.isDisabledMessage;
                    toggle.SetValueWithoutNotify(false);

                    // Store nodeData even for disabled items
                    toggle.userData = nodeData;
                }
            }
            else
            {
                toggle.style.display = DisplayStyle.None;
            }
        }

        private void UnbindNameCell(VisualElement element, int index)
        {
            var container = element;
            var toggle = container.Q<Toggle>("item-toggle");
            var label = container.Q<Label>("item-label");

            if (toggle == null || label == null)
                return;

            // Unregister callbacks based on what was stored in userData
            // Check if it's a folder with callback stored
            if (toggle.userData is FolderToggleUserData folderData && folderData.callback != null)
            {
                // Unregister the folder's value changed callback
                toggle.UnregisterValueChangedCallback(folderData.callback);
                toggle.userData = null;
            }
            // Or if it's a leaf item with click callback
            else if (toggle.userData is TreeNodeData leafNode)
            {
                if (leafNode.itemState != null)
                {
                    toggle.UnregisterCallback<ClickEvent>(leafNode.itemState.OnSelectionChanged);
                }
                toggle.userData = null;
            }

            // Unregister label click callback
            if (label.userData is TreeNodeData labelNode && labelNode.itemState != null)
            {
                label.UnregisterCallback<ClickEvent>(labelNode.itemState.OnClicked);
                label.userData = null;
            }
        }

        private void ExpandCollapseAll(bool expand)
        {
            if (state.filteredItemsTree == null || state.filteredItemsTree.Count == 0)
                return;

            ExpandCollapseAllRecursive(state.filteredItemsTree, expand);
            // State is automatically updated via itemExpandedChanged callback
        }

        private void ExpandCollapseAllRecursive(IEnumerable<TreeViewItemData<TreeNodeData>> items, bool expand)
        {
            foreach (var item in items)
            {
                if (item.data.isFolder && item.data.itemState is FolderItemState folderState)
                {
                    // Explicitly update state (don't rely solely on callback)
                    folderState.isExpanded = expand;

                    if (expand)
                        m_TreeView.ExpandItem(item.id);
                    else
                        m_TreeView.CollapseItem(item.id);

                    if (item.hasChildren)
                        ExpandCollapseAllRecursive(item.children, expand);
                }
            }
        }

        /// <summary>
        /// Updates the expanded state of all descendant folders when a parent folder is collapsed/expanded.
        /// This ensures recursive collapse (Alt+click) persists correctly across assembly reloads.
        /// </summary>
        private void UpdateDescendantExpandedStates(TreeNodeData parentNode, bool isExpanded)
        {
            // Get the TreeViewItemData for this node to access its children
            var treeItem = FindTreeViewItem(state.filteredItemsTree, parentNode);
            if (treeItem.HasValue && treeItem.Value.hasChildren)
            {
                UpdateDescendantExpandedStatesRecursive(treeItem.Value.children, isExpanded);
            }
        }

        /// <summary>
        /// Recursively updates expanded state for all descendant folders
        /// </summary>
        private void UpdateDescendantExpandedStatesRecursive(IEnumerable<TreeViewItemData<TreeNodeData>> items, bool isExpanded)
        {
            foreach (var item in items)
            {
                if (item.data.isFolder && item.data.itemState is FolderItemState folderState)
                {
                    folderState.isExpanded = isExpanded;

                    if (item.hasChildren)
                        UpdateDescendantExpandedStatesRecursive(item.children, isExpanded);
                }
            }
        }

        /// <summary>
        /// Finds a TreeViewItemData that contains the specified TreeNodeData
        /// </summary>
        private TreeViewItemData<TreeNodeData>? FindTreeViewItem(IEnumerable<TreeViewItemData<TreeNodeData>> items, TreeNodeData targetNode)
        {
            foreach (var item in items)
            {
                if (item.data == targetNode)
                    return item;

                if (item.hasChildren)
                {
                    var found = FindTreeViewItem(item.children, targetNode);
                    if (found.HasValue)
                        return found;
                }
            }

            return null;
        }
    }
}
