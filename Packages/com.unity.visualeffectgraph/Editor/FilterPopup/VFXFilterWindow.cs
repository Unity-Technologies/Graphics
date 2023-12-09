using System;
using System.Collections.Generic;
using System.Linq;

using Unity.Profiling;
using Unity.UI.Builder;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.VFX.UI
{
    class VFXFilterWindow : EditorWindow
    {
        private sealed class Descriptor
        {
            private IVFXModelDescriptor m_Descriptor;
            public Descriptor(IVFXModelDescriptor descriptor, string match, string synonym) : this(descriptor.name, descriptor.category, descriptor, match, synonym) { }

            public Descriptor(string name, string category, IVFXModelDescriptor descriptor = null, string match = null, string synonym = null)
            {
                this.category = category;
                this.name = name;
                this.descriptor = descriptor;
                this.variant = descriptor?.variant;
                this.nameMatch = match;
                this.synonymMatch = synonym;
            }

            public string category { get; }
            public string[] synonyms { get; }
            public string name { get; }
            public IVFXModelDescriptor descriptor { get; }
            public Variant variant { get; }
            public IVFXModelDescriptor[] subVariants => this.descriptor?.subVariantDescriptors;
            public string nameMatch { get; }
            public string synonymMatch { get; }

            public string GetDocumentationLink() => this.descriptor.variant.GetDocumentationLink();
        }

        [Serializable]
        private struct Settings
        {
            [SerializeField] private List<string> categories;
            [SerializeField] private List<Color> colors;
            [SerializeField] private List<string> favorites;
            public bool showSubVariantsInSearchResults;

            public bool IsFavorite(Descriptor descriptor) => favorites?.Contains(descriptor.variant.GetUniqueIdentifier()) == true;

            public void AddFavorite(Descriptor descriptor)
            {
                var path = descriptor.variant.GetUniqueIdentifier();
                if (favorites?.Contains(path) == false)
                {
                    favorites.Add(path);
                }
                else if (favorites == null)
                {
                    favorites = new List<string> { path };
                }
            }

            public void RemoveFavorite(Descriptor descriptor) => favorites?.Remove(descriptor.variant.GetUniqueIdentifier());

            public bool TryGetCategoryColor(string category, out Color color)
            {
                var index = categories?.IndexOf(category) ?? -1;
                if (index != -1)
                {
                    color = colors[index];
                    return true;
                }

                color = default;
                return false;
            }

            public void SetCategoryColor(string category, Color color)
            {
                var index = categories?.IndexOf(category) ?? -1;
                if (index != -1)
                {
                    colors[index] = color;
                }
                else
                {
                    if (categories == null)
                    {
                        categories = new List<string>();
                        colors = new List<Color>();
                    }
                    categories.Add(category);
                    colors.Add(color);
                }
            }

            public void ResetCategoryColor(string category)
            {
                var index = categories?.IndexOf(category) ?? -1;
                if (index != -1)
                {
                    categories.RemoveAt(index);
                    colors.RemoveAt(index);
                }
            }
        }

        public interface IProvider
        {
            IEnumerable<IVFXModelDescriptor> GetDescriptors();

            void AddNode(Variant variant);

            Vector2 position { get; set; }
        }

        private static readonly ProfilerMarker s_GetMatchesPerfMarker = new("VFXFilterWindow.GetMatches");

        private const float DefaultWindowWidth = 700;
        private const float DefaultPanelWidth = 350;
        private const float MinWidth = 400f;
        private const float MinHeight = 320f;

        private IProvider m_Provider;
        private TreeView m_Treeview;
        private TreeView m_VariantTreeview;
        readonly List<TreeViewItemData<Descriptor>> m_TreeviewData = new();
        private TreeViewItemData<Descriptor> m_FavoriteCategory;
        private Label m_CategoryLabel;
        private ColorField m_CategoryColorField;
        private Button m_ResetCategoryColor;
        private string m_SearchPattern;
        private Toggle m_CollapseButton;
        private TwoPaneSplitView m_SplitPanel;
        private ToolbarSearchField m_SearchField;
        private Button m_HelpButton;
        private ToolbarBreadcrumbs m_Breadcrumbs;
        private Label m_NoSubvariantLabel;

        private float leftPanelWidth;
        private bool hideDetailsPanel;
        private Settings settings;
        private bool m_IsResizing;
        private Rect m_OriginalWindowPos;
        private Vector3 m_OriginalMousePos;
        private VFXView m_ParentView;

        private bool hasSearch => !string.IsNullOrEmpty(m_SearchPattern);

        public void ToggleSubVariantVisibility()
        {
            var toggle = rootVisualElement.Q<Toggle>("ListVariantToggle");
            toggle.value = !settings.showSubVariantsInSearchResults;
        }

        protected void OnLostFocus()
        {
            if (!HasOpenInstances<ColorPicker>())
            {
                Close();
            }
        }

        private void OnDisable()
        {
            SaveSettings();
            m_ParentView.Focus();
        }

        private void CreateGUI()
        {
            if (EditorGUIUtility.isProSkin)
            {
                rootVisualElement.AddToClassList("dark");
            }
            rootVisualElement.style.borderTopWidth = 1f;
            rootVisualElement.style.borderTopColor = new StyleColor(Color.black);
            rootVisualElement.style.borderBottomWidth = 1f;
            rootVisualElement.style.borderBottomColor = new StyleColor(Color.black);
            rootVisualElement.style.borderLeftWidth = 1f;
            rootVisualElement.style.borderLeftColor = new StyleColor(Color.black);
            rootVisualElement.style.borderRightWidth = 1f;
            rootVisualElement.style.borderRightColor = new StyleColor(Color.black);
            rootVisualElement.AddStyleSheetPath("VFXFilterWindow");
            rootVisualElement.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
            rootVisualElement.RegisterCallback<GeometryChangedEvent>(OnFirstDisplay);

            var tpl = VFXView.LoadUXML("VFXFilterWindow");
            tpl.CloneTree(rootVisualElement);

            m_SearchField = rootVisualElement.Q<ToolbarSearchField>();
            m_SearchField.RegisterCallback<ChangeEvent<string>>(OnSearchChanged);
            m_SearchField.RegisterCallback<KeyDownEvent>(OnKeyDown);
            var searchTextField = m_SearchField.Q<TextField>();
            searchTextField.selectAllOnFocus = false;
            searchTextField.selectAllOnMouseUp = false;

            m_SplitPanel = rootVisualElement.Q<TwoPaneSplitView>("SplitPanel");
            m_SplitPanel.fixedPaneInitialDimension = leftPanelWidth;

            var toggle = rootVisualElement.Q<Toggle>("ListVariantToggle");
            toggle.SetValueWithoutNotify(settings.showSubVariantsInSearchResults);
            toggle.RegisterCallback<ChangeEvent<bool>>(OnToggleSubVariantVisibility);

            m_CollapseButton = rootVisualElement.Q<Toggle>("CollapseButton");
            m_CollapseButton.SetValueWithoutNotify(hideDetailsPanel);
            m_CollapseButton.RegisterCallback<ChangeEvent<bool>>(OnToggleCollapse);
            rootVisualElement.Q<VisualElement>("DetailsPanel");

            m_Treeview = rootVisualElement.Q<TreeView>("ListOfNodes");
            m_Treeview.makeItem += MakeItem;
            m_Treeview.bindItem += (element, index) => BindItem(m_Treeview, element, index);
            m_Treeview.unbindItem += UnbindItem;
            m_Treeview.selectionChanged += OnSelectionChanged;
            m_Treeview.viewDataKey = null;

            m_VariantTreeview = rootVisualElement.Q<TreeView>("ListOfVariants");
            m_VariantTreeview.makeItem += MakeItem;
            m_VariantTreeview.bindItem += (element, index) => BindItem(m_VariantTreeview, element, index);
            m_VariantTreeview.unbindItem += UnbindItem;
            m_VariantTreeview.viewDataKey = null;

            m_CategoryLabel = rootVisualElement.Q<Label>("CategoryLabel");
            m_Breadcrumbs = rootVisualElement.Q<ToolbarBreadcrumbs>("Breadcrumbs");
            m_Breadcrumbs.SetEnabled(false);
            m_CategoryColorField = rootVisualElement.Q<ColorField>("CategoryColorField");
            m_CategoryColorField.RegisterCallback<ChangeEvent<Color>>(OnCategoryColorChanged);
            m_ResetCategoryColor = rootVisualElement.Q<Button>("ResetButton");
            m_ResetCategoryColor.RegisterCallback<ClickEvent>(OnResetCategoryColor);

            m_HelpButton = rootVisualElement.Q<Button>("HelpButton");
            m_HelpButton.clicked += OnDocumentation;
            m_NoSubvariantLabel = rootVisualElement.Q<Label>("NoSubvariantLabel");

            UpdateTree(m_Provider.GetDescriptors(), m_TreeviewData, true, true);
            m_Treeview.SetRootItems(m_TreeviewData);
            m_Treeview.RefreshItems();
            m_Treeview.SetSelectionById(m_FavoriteCategory.id);

            var resizer = rootVisualElement.Q<VisualElement>("Resizer");
            resizer.RegisterCallback<PointerDownEvent>(OnStartResize);
            resizer.RegisterCallback<PointerMoveEvent>(OnResize);
            resizer.RegisterCallback<PointerUpEvent>(OnEndResize);

            m_SearchField.Focus();
        }

        private void OnFirstDisplay(GeometryChangedEvent geometryChangedEvent)
        {
            rootVisualElement.UnregisterCallback<GeometryChangedEvent>(OnFirstDisplay);
            UpdateDetailsPanelVisibility();
        }

        private void OnToggleSubVariantVisibility(ChangeEvent<bool> evt)
        {
            settings.showSubVariantsInSearchResults = evt.newValue;
            UpdateSearchResult();
        }

        private void OnResetCategoryColor(ClickEvent evt)
        {
            var descriptor = (Descriptor)m_Treeview.selectedItem;
            settings.ResetCategoryColor(descriptor.name);
            var element = m_Treeview.GetRootElementForIndex(m_Treeview.selectedIndex);
            var label = element.Q<Label>("descriptorLabel");
            label.style.unityBackgroundImageTintColor = new StyleColor(StyleKeyword.Null);
            m_CategoryColorField.value = label.resolvedStyle.unityBackgroundImageTintColor;
        }

        private void OnCategoryColorChanged(ChangeEvent<Color> evt)
        {
            var descriptor = (Descriptor)m_Treeview.selectedItem;
            // In that case the descriptor is a category so the path is saved in the name
            settings.SetCategoryColor(descriptor.name, evt.newValue);
            var element = m_Treeview.GetRootElementForIndex(m_Treeview.selectedIndex);
            element.Q<Label>("descriptorLabel").style.unityBackgroundImageTintColor = evt.newValue;
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            switch (evt.keyCode)
            {
                case KeyCode.Escape:
                    Close();
                    return;
                case KeyCode.DownArrow:
                    if (m_SearchField.HasFocus())
                    {
                        m_Treeview.Focus();
                        if (m_Treeview.selectedIndex == -1)
                        {
                            m_Treeview.SetSelection(0);
                        }
                        else
                        {
                            m_Treeview.SetSelection(m_Treeview.selectedIndex + 1);
                        }
                    }
                    break;
                case KeyCode.UpArrow:
                    if (!m_SearchField.HasFocus() && m_Treeview.selectedIndex == 0)
                    {
                        m_SearchField.Focus();
                    }
                    else if (m_SearchField.HasFocus() && m_Treeview.selectedIndex > 0)
                    {
                        m_Treeview.SetSelection(m_Treeview.selectedIndex - 1);
                    }
                    break;
                case KeyCode.Return:
                    if (m_Treeview.selectedItem is Descriptor { variant: not null } descriptor)
                    {
                        AddNode(descriptor);
                    }
                    break;
                case KeyCode.RightArrow:
                case KeyCode.LeftArrow:
                    break;
                default:
                    if (!m_SearchField.HasFocus() && evt.modifiers is EventModifiers.None or EventModifiers.Shift)
                    {
                        m_SearchField.Focus();
                    }
                    break;
            }
        }

        private void OnToggleCollapse(ChangeEvent<bool> evt)
        {
            hideDetailsPanel = evt.newValue;

            var windowWidth = hideDetailsPanel ? m_Treeview.resolvedStyle.width + 2 : DefaultWindowWidth;
            position = new Rect(position.position, new Vector2(windowWidth, position.height));
            UpdateDetailsPanelVisibility();

            // Delay so that resolved style is computed
            EditorApplication.delayCall += SaveSettings;
        }

        private void OnSearchChanged(ChangeEvent<string> evt)
        {
            m_SearchPattern = evt.newValue.Trim().ToLower();
            UpdateSearchResult();
        }

        private void OnSelectionChanged(IEnumerable<object> selection)
        {
            var showDetails = false;
            var showVariants = false;
            var isFavorites = false;
            var showDocButton = false;
            var showNoSubvariantMessage = false;
            while (m_Breadcrumbs.childCount > 0)
                m_Breadcrumbs.PopItem();
            var categories = new List<string>();
            if (m_Treeview.selectedItem is not Descriptor descriptor)
            {
                return;
            }
            if (descriptor.variant != null)
            {
                showDetails = true;
                categories.AddRange(descriptor.category?.Split('/', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>());
                categories.Add(descriptor.name);

                showDocButton = descriptor.variant.modelType?.IsSubclassOf(typeof(VisualEffectSubgraph)) == false && descriptor.variant is not VFXModelDescriptorParameters.ParameterVariant;
                if (descriptor.subVariants != null)
                {
                    m_VariantTreeview.style.display = DisplayStyle.Flex;
                    List<TreeViewItemData<Descriptor>> treeviewData = new List<TreeViewItemData<Descriptor>>();
                    UpdateTree(descriptor.subVariants, treeviewData, false, false);
                    m_VariantTreeview.SetRootItems(treeviewData);
                    m_VariantTreeview.RefreshItems();
                    showVariants = true;
                }
                else
                {
                    showNoSubvariantMessage = true;
                }
            }
            else
            {
                categories.AddRange(descriptor.category.Split('/'));
                m_CategoryColorField.SetValueWithoutNotify(settings.TryGetCategoryColor(descriptor.name, out var color) ? color : Color.gray);
                isFavorites = m_Treeview.selectedItem == m_FavoriteCategory.data;
            }

            categories.ForEach(x => m_Breadcrumbs.PushItem(x));

            m_HelpButton.SetEnabled(showDocButton);
            m_VariantTreeview.style.display = showVariants ? DisplayStyle.Flex : DisplayStyle.None;
            m_NoSubvariantLabel.style.display = showNoSubvariantMessage ? DisplayStyle.Flex : DisplayStyle.None;
            m_CategoryLabel.style.display = (showDetails || isFavorites) ? DisplayStyle.None : DisplayStyle.Flex;
            m_CategoryColorField.style.display = (showDetails || isFavorites) ? DisplayStyle.None : DisplayStyle.Flex;
            m_ResetCategoryColor.style.display = (showDetails || isFavorites) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private void OnAddToFavorite(ClickEvent evt)
        {
            if (evt.target is Button { userData: Descriptor descriptor } button)
            {
                if (settings.IsFavorite(descriptor))
                {
                    var parent = button.GetFirstAncestorWithClass("unity-tree-view__item");
                    parent.RemoveFromClassList("favorite");
                    settings.RemoveFavorite(descriptor);
                    var idToRemove = m_FavoriteCategory.children.SingleOrDefault(x => x.data.name == descriptor.name && x.data.category == descriptor.category).id;
                    if (idToRemove > 0)
                    {
                        m_Treeview.TryRemoveItem(idToRemove);
                    }
                }
                else
                {
                    var parent = button.GetFirstAncestorWithClass("unity-tree-view__item");
                    parent.AddToClassList("favorite");
                    settings.AddFavorite(descriptor);
                    var newId = m_Treeview.viewController.GetAllItemIds().Max() + 1;
                    m_Treeview.AddItem(new TreeViewItemData<Descriptor>(newId, descriptor), m_FavoriteCategory.id);
                }

                if (!hideDetailsPanel)
                {
                    // Refresh details panel because if the state has change from the main panel, we must update the details panel
                    OnSelectionChanged(null);
                }
            }
        }

        private void OnDocumentation()
        {
            if (m_Treeview.selectedItem is Descriptor descriptor)
            {
                var docLink = descriptor.GetDocumentationLink();
                if (!string.IsNullOrEmpty(docLink))
                {
                    // Temporary until the version 17 documentation is online
                    docLink = docLink.Replace("17", "16");

                    Help.BrowseURL(string.Format(docLink, VFXHelpURLAttribute.version));
                }
            }
        }

        private void OnAddNode(ClickEvent evt)
        {
            if (evt.target is Label label)
            {
                var treeView = label.GetFirstAncestorOfType<TreeView>();
                if (evt.button == (int)MouseButton.LeftMouse && evt.clickCount == 2)
                {
                    AddNode((Descriptor)treeView.selectedItem);
                }
            }
        }

        private void OnToggleCategory(ClickEvent evt)
        {
            // The test on localPosition is to toggle expand state only when clicking on the left of the treeview item label
            if (evt.target is VisualElement element and not Toggle && evt.localPosition.x < 30)
            {
                var parent = element.GetFirstAncestorWithClass("unity-tree-view__item");
                if (parent != null)
                {
                    var toggle = parent.Q<Toggle>();
                    toggle.value = !toggle.value;
                }
            }
        }

        private void OnStartResize(PointerDownEvent evt)
        {
            if (evt.button == (int)MouseButton.LeftMouse)
            {
                m_IsResizing = true;
                evt.target.CaptureMouse();
                m_OriginalWindowPos = position;
                m_OriginalMousePos = evt.position;
            }
        }

        private void OnResize(PointerMoveEvent evt)
        {
            if (m_IsResizing)
            {
                var delta = evt.position - m_OriginalMousePos;
                var minWidth = hideDetailsPanel ? MinWidth / 2f : MinWidth;
                var size = new Vector2(
                    Math.Max(m_OriginalWindowPos.size.x + delta.x, minWidth),
                    Math.Max(m_OriginalWindowPos.size.y + delta.y, MinHeight));
                if (hideDetailsPanel)
                {
                    m_SplitPanel.CollapseChild(1);
                    m_SplitPanel.fixedPane.style.width = size.x;
                }


                position = new Rect(position.position, size);
                Repaint();
            }
        }

        private void OnEndResize(PointerUpEvent evt)
        {
            if (hideDetailsPanel)
            {
                leftPanelWidth = m_SplitPanel.fixedPaneInitialDimension;
            }
            evt.target.ReleaseMouse();
            m_IsResizing = false;
        }

        private void UnbindItem(VisualElement element, int index)
        {
            element.Clear();
            element.ClearClassList();

            var parent = element.GetFirstAncestorWithClass("unity-tree-view__item");
            parent.RemoveFromClassList("favorite");
            parent.RemoveFromClassList("treeleaf");
            parent.UnregisterCallback<ClickEvent>(OnToggleCategory);
            parent.visible = true;
        }

        private IEnumerable<Label> BuildHighlightedLabel(string text)
        {
            var tokens = text.Split('#', StringSplitOptions.RemoveEmptyEntries);
            foreach (var token in tokens)
            {
                var isHighlighted = token.StartsWith('@');
                var label = isHighlighted
                    ? new Label(token.Substring(1, token.Length - 1))
                    : new Label(token);
                if (isHighlighted)
                    label.AddToClassList("highlighted");
                yield return label;
            }
        }

        private void BindItem(TreeView treeview, VisualElement element, int index)
        {
            var item = treeview.GetItemDataForIndex<Descriptor>(index);
            element.AddToClassList("treenode");
            var parent = element.GetFirstAncestorWithClass("unity-tree-view__item");

            List<Label> labels;
            if (item.nameMatch != null)
            {
                labels = BuildHighlightedLabel(item.nameMatch).ToList();
            }
            else
            {
                labels = new List<Label> { new(item.name) };
                if (item.synonymMatch != null)
                {
                    labels.AddRange(BuildHighlightedLabel($" ({item.synonymMatch})"));
                }
            }

            if (item.variant != null)
            {
                if (settings.IsFavorite(item))
                {
                    parent.AddToClassList("favorite");
                }

                if (item.variant.supportFavorite)
                {
                    var favoriteButton = new Button { name = "favoriteButton", userData = item, tooltip = "Click toggle favorite state" };
                    favoriteButton.RegisterCallback<ClickEvent>(OnAddToFavorite);
                    element.Add(favoriteButton);
                }

                if (item.subVariants?.Length > 0)
                {
                    element.AddToClassList("has-sub-variant");
                }

                parent.AddToClassList("treeleaf");
                // This is to handle double click on variant
                parent.RegisterCallback<ClickEvent>(OnAddNode);
            }
            // This is a category
            else
            {
                if (treeview == m_Treeview)
                {
                    if (treeview.GetIdForIndex(index) == m_FavoriteCategory.id)
                    {
                        element.AddToClassList("favorite");
                    }
                    else if (settings.TryGetCategoryColor(item.name, out var color))
                    {
                        labels[0].style.unityBackgroundImageTintColor = color;
                    }
                }
                labels[0].AddToClassList("category");

                // This is to handle expand collapse on the whole category line (not only the small arrow)
                parent.RegisterCallback<ClickEvent>(OnToggleCategory);
            }

            var i = 0;
            labels[0].name = "descriptorLabel";
            foreach (var label in labels)
            {
                label.tooltip = item.name;
                label.AddToClassList("node-name");
                element.Insert(i++, label);
            }
            labels.Last().AddToClassList("last-node-name");
        }

        private VisualElement MakeItem() => new ();

        private void AddNode(Descriptor descriptor)
        {
            m_Provider.AddNode(descriptor.variant);
            Close();
        }

        private void UpdateDetailsPanelVisibility()
        {
            if (hideDetailsPanel)
            {
                m_SplitPanel.CollapseChild(1);
                m_CollapseButton.tooltip = "Show details panel";
            }
            else
            {
                m_SplitPanel.UnCollapse();
                m_CollapseButton.tooltip = "Hide details panel";
            }
            var splitter = rootVisualElement.Q<VisualElement>("unity-dragline-anchor");
            splitter.style.display = hideDetailsPanel ? DisplayStyle.None : DisplayStyle.Flex;
            m_Treeview.parent.style.flexShrink = 1;
            m_SplitPanel.fixedPaneInitialDimension = leftPanelWidth;
        }

        private void UpdateSearchResult()
        {
            UpdateTree(m_Provider.GetDescriptors(), m_TreeviewData, true, true);
            m_Treeview.SetRootItems(m_TreeviewData);
            m_Treeview.RefreshItems();
            if (hasSearch)
            {
                // Workaround because ExpandAll can change the selection without calling the callback
                var currentSelectedIndex = m_Treeview.selectedIndex;
                m_Treeview.ExpandAll();
                // Call OnSelectionChanged even if it didn't change so that search matches highlight are properly updated
                if (currentSelectedIndex != m_Treeview.selectedIndex || (hasSearch && currentSelectedIndex != -1))
                {
                    OnSelectionChanged(null);
                }
                var previousSelectedVariant = m_Treeview.selectedItem as Descriptor;
                SelectFirstNode(previousSelectedVariant?.variant?.name);
            }
        }

        private void SelectFirstNode(string previousSelectedVariant)
        {
            SelectFirstNodeRecurse(m_TreeviewData, previousSelectedVariant);
            // If previous selection is not found, just select the first variant result
            if (m_Treeview.selectedItem is null or Descriptor { variant: null })
            {
                SelectFirstNodeRecurse(m_TreeviewData, null);
            }

            if (m_Treeview.selectedIndex == -1)
            {
                m_Treeview.SetSelection(0);
            }
            m_Treeview.ScrollToItem(m_Treeview.selectedIndex);
        }

        private bool SelectFirstNodeRecurse(IEnumerable<TreeViewItemData<Descriptor>> data, string previousSelectedVariant)
        {
            foreach (var itemData in data)
            {
                if (itemData.data.variant != null)
                {
                    if (previousSelectedVariant == null || previousSelectedVariant == itemData.data.name)
                    {
                        m_Treeview.SetSelectionById(itemData.id);
                        return true;
                    }
                }

                if (SelectFirstNodeRecurse(itemData.children, previousSelectedVariant))
                {
                    return true;
                }
            }

            return false;
        }

        private void UpdateTree(IEnumerable<IVFXModelDescriptor> modelDescriptors, List<TreeViewItemData<Descriptor>> treeViewData, bool hasFavoriteCategory, bool groupUncategorized)
        {
            var favorites = hasFavoriteCategory ? new List<TreeViewItemData<Descriptor>>() : null;
            treeViewData.Clear();
            var id = 0;

            var patternTokens = m_SearchPattern?.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            foreach (var modelDescriptor in modelDescriptors
                         .OrderBy(x => !string.IsNullOrEmpty(x.category) ? x.category : "zzzzzzzz") // Hack to put variants without category to the end instead of at the beginning
                         .ThenBy(x => x.category?.Contains('/') == true ? 0 : 1) // The sorting is made to put folders before items
                         .ThenBy(x => x.name))
            {
                var category = !string.IsNullOrEmpty(modelDescriptor.category) ? modelDescriptor.category : (groupUncategorized ? "Subgraph" : string.Empty);
                var path = category.Split('/', StringSplitOptions.RemoveEmptyEntries);
                var currentFolders = treeViewData;

                var matchingDescriptors = GetMatches(modelDescriptor, patternTokens).ToArray();
                if (matchingDescriptors.Length == 0)
                {
                    continue;
                }

                foreach (var p in path)
                {
                    if (currentFolders.All(x => x.data.name != p))
                    {
                        string categoryMatch = null;
                        if (patternTokens != null)
                        {
                            IsSearchPatternMatch(p, m_SearchPattern, patternTokens, out categoryMatch);
                        }
                        var newFolder = new TreeViewItemData<Descriptor>(id++, new Descriptor(p, category, null, categoryMatch), new List<TreeViewItemData<Descriptor>>());
                        currentFolders.Add(newFolder);
                        currentFolders = (List<TreeViewItemData<Descriptor>>)newFolder.children;
                    }
                    else
                    {
                        currentFolders = (List<TreeViewItemData<Descriptor>>)currentFolders.Single(x => x.data.name == p).children;
                    }
                }

                for (var i = 0; i < matchingDescriptors.Length; i++)
                {
                    var descriptor = matchingDescriptors[i];
                    // When no search, only add main variant (which is the first one)
                    if (settings.showSubVariantsInSearchResults && hasSearch || i == 0)
                    {
                        currentFolders.Add(new TreeViewItemData<Descriptor>(id++, descriptor));
                    }
                    // But add any matching variant, even sub-variants even when there's no search pattern
                    if ((i == 0 || settings.showSubVariantsInSearchResults) && hasFavoriteCategory && settings.IsFavorite(descriptor))
                    {
                        favorites.Add(new TreeViewItemData<Descriptor>(id++, descriptor));
                    }
                }
            }

            if (hasFavoriteCategory)
            {
                m_FavoriteCategory = new TreeViewItemData<Descriptor>(id, new Descriptor("Favorites", string.Empty), favorites);
                treeViewData.Insert(0, m_FavoriteCategory);
            }
        }

        internal static void Show(VFXView parentView, Vector2 graphPosition, Vector2 screenPosition, IProvider provider)
        {
            CreateInstance<VFXFilterWindow>().Init(parentView, graphPosition, screenPosition, provider);
        }

        private IEnumerable<Descriptor> GetMatches(IVFXModelDescriptor modelDescriptor, string[] patternTokens)
        {
            s_GetMatchesPerfMarker.Begin();
            try
            {
                string match = null;
                string synonym = null;
                Descriptor descriptor = null;
                if (IsVariantMatch(modelDescriptor, patternTokens, out match, out synonym))
                {
                    descriptor = new Descriptor(modelDescriptor, match, synonym);
                    yield return descriptor;
                }

                if (settings.showSubVariantsInSearchResults)
                {
                    var subVariantsDescriptors = modelDescriptor.subVariantDescriptors;
                    if (subVariantsDescriptors != null)
                    {
                        foreach (var v in subVariantsDescriptors)
                        {
                            if (IsVariantMatch(v, patternTokens, out match, out synonym))
                            {
                                yield return new Descriptor(v, match, synonym);
                            }
                        }
                    }
                }
            }
            finally
            {
                s_GetMatchesPerfMarker.End();
            }
        }

        private bool IsVariantMatch(IVFXModelDescriptor modelDescriptor, string[] patternTokens, out string match, out string synonymMatch)
        {
            synonymMatch = match = null;
             return !hasSearch
                   || IsSearchPatternMatch(modelDescriptor.name, m_SearchPattern, patternTokens, out match)
                   || IsSearchPatternMatch(modelDescriptor.category, m_SearchPattern, patternTokens, out _)
                   || IsSearchPatternMatch(string.Join(", ", modelDescriptor.synonyms), m_SearchPattern, patternTokens, out synonymMatch);
        }

        private bool IsSearchPatternMatch(string source, string pattern, string[] patternTokens, out string matchHighlight)
        {
            matchHighlight = null;
            if (source == null)
                return false;

            var start = source.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
            if (start != -1)
            {
                matchHighlight = source.Insert(start + pattern.Length, "#").Insert(start, "#@");
                return true;
            }

            // Match all pattern tokens with the source tokens
            var sourceTokens = source.Split(' ');
            if (sourceTokens.Length >= patternTokens.Length)
            {
                var match = false;
                foreach (var token in patternTokens)
                {
                    start = matchHighlight?.IndexOf(token, StringComparison.OrdinalIgnoreCase) ?? source.IndexOf(token, StringComparison.OrdinalIgnoreCase);

                    if (start != -1)
                    {
                        match = true;
                        matchHighlight ??= source;
                        matchHighlight = matchHighlight.Insert(start + token.Length, "#").Insert(start, "#@");
                    }
                    else
                    {
                        match = false;
                        break;
                    }
                }

                if (match)
                    return true;
            }

            // Consider pattern as initials and match with source first letters (ex: SPSC => Set Position Shape Cone)
            var initialIndex = 0;
            var matchingIndices = new List<int>();
            for (int i = 0; i < source.Length; i++)
            {
                var c = source[i];
                if (c == ' ' || i == 0)
                {
                    while (!char.IsLetterOrDigit(c) && i < source.Length - 1)
                    {
                        c = source[++i];
                    }

                    if (i == source.Length - 1 && !char.IsLetterOrDigit(c))
                    {
                        return false;
                    }

                    if (initialIndex < pattern.Length)
                    {
                        if (char.ToLower(c) == pattern[initialIndex])
                        {
                            matchingIndices.Add(i);
                            initialIndex++;
                            if (initialIndex == pattern.Length)
                            {
                                matchHighlight = new string(source.SelectMany((x, k) => matchingIndices.Contains(k) ? new [] { '#', '@', x, '#' } : new []{ x }).ToArray());
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        private void Init(VFXView parentView, Vector2 graphPosition, Vector2 screenPosition, IProvider provider)
        {
            m_ParentView = parentView;
            m_Provider = provider;
            m_Provider.position = graphPosition;

            RestoreSettings(screenPosition);

            ShowPopup();

            Focus();

            wantsMouseMove = true;
        }

        private void RestoreSettings(Vector2 screenPosition)
        {
            hideDetailsPanel = SessionState.GetBool($"{nameof(VFXFilterWindow)}.{nameof(hideDetailsPanel)}", true);

            leftPanelWidth = SessionState.GetFloat($"{nameof(VFXFilterWindow)}.{nameof(leftPanelWidth)}", DefaultPanelWidth);
            var windowWidth = SessionState.GetFloat($"{nameof(VFXFilterWindow)}.WindowWidth", hideDetailsPanel ? leftPanelWidth : DefaultWindowWidth);
            var windowHeight = SessionState.GetFloat($"{nameof(VFXFilterWindow)}.WindowHeight", MinHeight);
            var topLeft = new Vector2(screenPosition.x - 24, screenPosition.y - 16);
            position = new Rect(topLeft, new Vector2(windowWidth, windowHeight));

            var settingsAsJson = EditorPrefs.GetString($"{nameof(VFXFilterWindow)}.{nameof(settings)}", null);
            settings = !string.IsNullOrEmpty(settingsAsJson) ? JsonUtility.FromJson<Settings>(settingsAsJson) : default;
        }

        private void SaveSettings()
        {
            leftPanelWidth = m_Treeview.resolvedStyle.width;
            SessionState.SetFloat($"{nameof(VFXFilterWindow)}.{nameof(leftPanelWidth)}", leftPanelWidth);
            SessionState.SetBool($"{nameof(VFXFilterWindow)}.{nameof(hideDetailsPanel)}", hideDetailsPanel);
            SessionState.SetFloat($"{nameof(VFXFilterWindow)}.WindowWidth", position.width);
            SessionState.SetFloat($"{nameof(VFXFilterWindow)}.WindowHeight", position.height);
            var json = JsonUtility.ToJson(settings);
            EditorPrefs.SetString($"{nameof(VFXFilterWindow)}.{nameof(settings)}", json);
        }
    }
}
