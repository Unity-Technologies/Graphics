using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using Unity.Profiling;
using Unity.UI.Builder;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace UnityEditor.VFX.UI
{
    class VFXFilterWindow : EditorWindow
    {
        internal class Descriptor
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
            public string name { get; }
            public IVFXModelDescriptor descriptor { get; }
            public Variant variant { get; }
            public IVFXModelDescriptor[] subVariants => this.descriptor?.subVariantDescriptors;
            public string nameMatch { get; }
            public string synonymMatch { get; }
            public float matchingScore { get; set; }

            public string GetDisplayName() => string.IsNullOrEmpty(nameMatch) ? name : nameMatch;
            // Only used for test purpose
            public string GetDisplayNameAndSynonym() => string.IsNullOrEmpty(synonymMatch) ? name : $"{name} ({synonymMatch})";
            public string GetDocumentationLink() => this.descriptor.variant.GetDocumentationLink();
        }

        private class Separator : Descriptor
        {
            public Separator(string name, string category, string match = null, string synonym = null) : base(name, category, null, match, synonym)
            {
            }
        }

        // This custom string comparer is used to sort path properly (Attribute should be listed before Attribute from Curve for instance)
        private class CategoryComparer : IComparer<string>
        {
            public int Compare(string x, string y)
            {
                // Hack to sort no category items at the end
                if (string.IsNullOrEmpty(x)) return 1;
                if (string.IsNullOrEmpty(y)) return -1;

                var xIndex = x.IndexOf('/');
                var yIndex = y.IndexOf('/');

                var comparison = 0;
                if (xIndex > 0 && yIndex < 0)
                {
                    comparison = string.Compare(x.Substring(0, xIndex), y, StringComparison.OrdinalIgnoreCase);
                }
                else if (xIndex < 0 && yIndex > 0)
                {
                    comparison = string.Compare(x, y.Substring(0, yIndex), StringComparison.OrdinalIgnoreCase);
                }
                else if (xIndex >= 0 && yIndex >= 0)
                {
                    comparison = string.Compare(x.Substring(0, xIndex), y.Substring(0, yIndex), StringComparison.OrdinalIgnoreCase);
                }

                if (comparison != 0)
                    return comparison;

                // Deeper categories are sorted at the end
                if (xIndex > 0 || yIndex > 0)
                {
                    var xDepth = x.Count(c => c == '/');
                    var yDepth = y.Count(c => c == '/');
                    if (xDepth < yDepth)
                        return -1;
                    else if (xDepth > yDepth)
                        return 1;
                }

                return string.Compare(x, y, StringComparison.OrdinalIgnoreCase);
            }
        }

        [Serializable]
        internal struct Settings
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
        private static readonly char[] s_MatchingSeparators = { ' ', '|', '_' };
        private static Regex s_NodeNameParser = new("(?<label>[|]?[^\\|]*)", RegexOptions.Compiled);
        private static CategoryComparer s_CategoryComparer = new CategoryComparer();
        private static List<string> s_PatternMatches = new List<string>();


        private const float DefaultWindowWidth = 700;
        private const float DefaultPanelWidth = 350;
        private const float MinWidth = 400f;
        private const float MinHeight = 320f;

        private IProvider m_Provider;
        private TreeView m_Treeview;
        private TreeView m_VariantTreeview;
        readonly List<TreeViewItemData<Descriptor>> m_TreeviewData = new();
        private TreeViewItemData<Descriptor> m_FavoriteCategory;
        private ColorField m_CategoryColorField;
        private Button m_ResetCategoryColor;
        private string m_SearchPattern;
        private Toggle m_CollapseButton;
        private TwoPaneSplitView m_SplitPanel;
        private ToolbarSearchField m_SearchField;
        private Button m_HelpButton;
        private Label m_Title;
        private Label m_NoSubvariantLabel;

        private float leftPanelWidth;
        private bool hideDetailsPanel;
        private Settings settings;
        private bool m_IsResizing;
        private Rect m_OriginalWindowPos;
        private Vector3 m_OriginalMousePos;

        private bool hasSearch => !string.IsNullOrEmpty(GetSearchPattern());

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
            m_Treeview.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            m_Treeview.makeItem += MakeItem;
            m_Treeview.bindItem += (element, index) => BindItem(m_Treeview, element, index);
            m_Treeview.unbindItem += UnbindItem;
            m_Treeview.selectionChanged += OnSelectionChanged;
            m_Treeview.viewDataKey = null;

            m_VariantTreeview = rootVisualElement.Q<TreeView>("ListOfVariants");
            m_VariantTreeview.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            m_VariantTreeview.makeItem += MakeItem;
            m_VariantTreeview.bindItem += (element, index) => BindItem(m_VariantTreeview, element, index);
            m_VariantTreeview.unbindItem += UnbindItem;
            m_VariantTreeview.viewDataKey = null;

            m_Title = rootVisualElement.Q<Label>("Title");
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
            m_Parent.window.m_DontSaveToLayout = true;
            rootVisualElement.UnregisterCallback<GeometryChangedEvent>(OnFirstDisplay);
            UpdateDetailsPanelVisibility();
        }

        private void OnToggleSubVariantVisibility(ChangeEvent<bool> evt)
        {
            settings.showSubVariantsInSearchResults = evt.newValue;
            if (hasSearch)
            {
                UpdateSearchResult(true);
            }
        }

        private void OnResetCategoryColor(ClickEvent evt)
        {
            var descriptor = (Descriptor)m_Treeview.selectedItem;
            settings.ResetCategoryColor(descriptor.name);
            var element = m_Treeview.GetRootElementForIndex(m_Treeview.selectedIndex);
            var label = element.Q<Label>("categoryLabel");
            label.style.unityBackgroundImageTintColor = new StyleColor(StyleKeyword.Null);
            m_CategoryColorField.value = label.resolvedStyle.unityBackgroundImageTintColor;
        }

        private void OnCategoryColorChanged(ChangeEvent<Color> evt)
        {
            var descriptor = (Descriptor)m_Treeview.selectedItem;
            // In that case the descriptor is a category so the path is saved in the name
            settings.SetCategoryColor(descriptor.name, evt.newValue);
            var element = m_Treeview.GetRootElementForIndex(m_Treeview.selectedIndex);
            element.Q<Label>("categoryLabel").style.unityBackgroundImageTintColor = evt.newValue;
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
            ToggleCollapse(evt.newValue);
        }

        private void OnSearchChanged(ChangeEvent<string> evt)
        {
            m_SearchPattern = evt.newValue.Trim().ToLower();
            UpdateSearchResult(false);
        }

        private void OnSelectionChanged(IEnumerable<object> selection)
        {
            var showDetails = false;
            var showVariants = false;
            var isFavorites = false;
            var showDocButton = false;
            var showNoSubvariantMessage = false;

            if (m_Treeview.selectedItem is not Descriptor descriptor)
            {
                return;
            }

            m_Title.text = descriptor.name.ToHumanReadable();
            if (descriptor.variant != null)
            {
                showDetails = true;
                showDocButton = descriptor.variant.modelType?.IsSubclassOf(typeof(VisualEffectSubgraph)) == false
                                && descriptor.variant is not VFXModelDescriptorParameters.ParameterVariant;
                var isDocAvailable = !string.IsNullOrEmpty(descriptor.GetDocumentationLink());
                m_HelpButton.SetEnabled(isDocAvailable);
                m_HelpButton.tooltip = isDocAvailable ? "Open node's documentation in a browser" : "Documentation is not yet available for this node";

                if (descriptor.subVariants?.Length > 0)
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
            else if (descriptor is not Separator)
            {
                m_CategoryColorField.SetValueWithoutNotify(settings.TryGetCategoryColor(descriptor.name, out var color) ? color : Color.gray);
                isFavorites = m_Treeview.selectedItem == m_FavoriteCategory.data;
            }
            else
            {
                showDetails = true;// Just to hide category UI
            }

            if (showDocButton)
            {
                m_HelpButton.RemoveFromClassList("hidden");
            }
            else
            {
                m_HelpButton.AddToClassList("hidden");
            }
            m_VariantTreeview.style.display = showVariants ? DisplayStyle.Flex : DisplayStyle.None;
            m_NoSubvariantLabel.style.display = showNoSubvariantMessage ? DisplayStyle.Flex : DisplayStyle.None;
            m_CategoryColorField.parent.style.display = (showDetails || isFavorites) ? DisplayStyle.None : DisplayStyle.Flex;
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
                    Help.BrowseURL(string.Format(docLink, Documentation.version));
                }
            }
        }

        private void OnAddNode(ClickEvent evt)
        {
            if (evt.target is not Button)
            {
                var treeView = ((VisualElement)evt.target).GetFirstAncestorOfType<TreeView>();
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
            parent.RemoveFromClassList("separator");
            parent.UnregisterCallback<ClickEvent>(OnToggleCategory);
            parent.UnregisterCallback<ClickEvent>(OnAddNode);
            parent.visible = true;
        }

        private IEnumerable<Label> HighlightedMatches(IEnumerable<Label> labels)
        {
            foreach (var label in labels)
            {
                if (label.text.IndexOf('@') < 0)
                {
                    yield return label;
                    continue;
                }

                var tokens = label.text.Split('#', StringSplitOptions.RemoveEmptyEntries);
                for (var i = 0; i < tokens.Length; i++)
                {
                    var token = tokens[i];
                    var isHighlighted = token.StartsWith('@');
                    var newLabel = label;
                    if (i > 0)
                    {
                        newLabel = new Label();
                        if (label.ClassListContains("setting"))
                            newLabel.AddToClassList("setting");
                    }

                    newLabel.text = isHighlighted
                        ? token.Substring(1, token.Length - 1)
                        : token;

                    if (isHighlighted)
                    {
                        newLabel.AddToClassList("highlighted");
                    }

                    // Use left, middle and right classes to properly join together text which is split across multiple labels
                    if (tokens.Length > 1)
                    {
                        if (i == 0)
                            newLabel.AddToClassList("left-part");
                        else if (i == tokens.Length - 1)
                            newLabel.AddToClassList("right-part");
                        else
                            newLabel.AddToClassList("middle-part");
                    }

                    yield return newLabel;
                }
            }
        }

        private void BindItem(TreeView treeview, VisualElement element, int index)
        {
            var item = treeview.GetItemDataForIndex<Descriptor>(index);
            element.AddToClassList("treenode");
            var parent = element.GetFirstAncestorWithClass("unity-tree-view__item");

            List<Label> labels = null;

            if (item is not Separator)
            {
                labels = HighlightedMatches(item.GetDisplayName().SplitTextIntoLabels("setting")).ToList();
            }
            else
            {
                parent.AddToClassList("separator");
                element.Add(new Label(item.name));
            }

            if (item.synonymMatch != null)
            {
                labels.AddRange(HighlightedMatches(new [] { new Label($" ({item.synonymMatch})") }));
            }

            if (item.variant != null)
            {
                if (settings.IsFavorite(item))
                {
                    parent.AddToClassList("favorite");
                }

                if (item.subVariants?.Length > 0)
                {
                    var showDetailsPanelButton = new Button(() => ToggleCollapseFromTreeview(false, index)) { name = "showDetailsPanelButton", tooltip = "Show node's sub-variants in details panel"};
                    element.Add(showDetailsPanelButton);
                }

                if (item.variant.supportFavorite)
                {
                    var favoriteButton = new Button { name = "favoriteButton", userData = item, tooltip = "Click toggle favorite state" };
                    favoriteButton.RegisterCallback<ClickEvent>(OnAddToFavorite);
                    element.Add(favoriteButton);
                }

                parent.AddToClassList("treeleaf");
                // This is to handle double click on variant
                parent.RegisterCallback<ClickEvent>(OnAddNode);
            }
            // This is a category
            else if (item is not Separator && item.name != null)
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
                labels[0].name = "categoryLabel"; // So we can retrieve it for custom color
                labels[0].AddToClassList("category");

                // This is to handle expand collapse on the whole category line (not only the small arrow)
                parent.RegisterCallback<ClickEvent>(OnToggleCategory);
            }

            if (labels != null)
            {
                var i = 0;
                foreach (var label in labels)
                {
                    label.tooltip = item.name.ToHumanReadable();
                    label.AddToClassList("node-name");
                    element.Insert(i++, label);
                }

                var spacer = new VisualElement();
                spacer.AddToClassList("nodes-label-spacer");
                element.Insert(i, spacer);
            }
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

        private void UpdateSearchResult(bool keepSelection)
        {
            var currentSelectedItem = m_Treeview.selectedItem as Descriptor;
            UpdateTree(m_Provider.GetDescriptors(), m_TreeviewData, true, true);
            m_Treeview.SetRootItems(m_TreeviewData);
            m_Treeview.RefreshItems();
            if (hasSearch)
            {
                // Workaround because ExpandAll can change the selection without calling the callback
                m_Treeview.ExpandAll();
                // Call OnSelectionChanged even if it didn't change so that search matches highlight are properly updated
                if (currentSelectedItem != m_Treeview.selectedItem || (hasSearch && currentSelectedItem == null))
                {
                    OnSelectionChanged(null);
                }

                SelectFirstNode(keepSelection ? currentSelectedItem?.name : null);
            }
        }

        private void SelectFirstNode(string currentSelectedItem)
        {
            SelectFirstNodeRecurse(m_TreeviewData, currentSelectedItem);

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

        private void UpdateTree(IEnumerable<IVFXModelDescriptor> modelDescriptors, List<TreeViewItemData<Descriptor>> treeViewData, bool isMainTree, bool groupUncategorized)
        {
            var favorites = isMainTree ? new List<TreeViewItemData<Descriptor>>() : null;
            treeViewData.Clear();
            var id = 0;

            var searchPattern = GetSearchPattern();
            var patternTokens = searchPattern?.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            foreach (var modelDescriptor in modelDescriptors
                         .OrderBy(x => x.category, s_CategoryComparer)
                         .ThenBy(x => x.name.ToHumanReadable()))
            {
                var category = !string.IsNullOrEmpty(modelDescriptor.category) ? modelDescriptor.category : (groupUncategorized ? "Subgraph" : string.Empty);
                var path = category.Split('/', StringSplitOptions.RemoveEmptyEntries);
                var currentFolders = treeViewData;

                var matchingDescriptors = GetMatches(modelDescriptor, searchPattern, patternTokens).ToArray();
                if (matchingDescriptors.Length == 0)
                {
                    continue;
                }

                var searchPatternLeft = searchPattern;
                foreach (var p in path)
                {
                    var containerName = p;
                    var isSeparator = containerName.StartsWith('#');
                    if (isSeparator)
                        containerName = containerName.Substring(2, p.Length - 2); // Skip first two characters because separator code is of the form #1, #2 ...
                    if (currentFolders.All(x => x.data.name != containerName))
                    {
                        string categoryMatch = null;
                        if (patternTokens != null)
                        {
                            GetTextMatchScore(containerName, ref searchPatternLeft, patternTokens, out categoryMatch);
                        }

                        if (!isSeparator)
                        {
                            var newFolder = new TreeViewItemData<Descriptor>(id++, new Descriptor(containerName, containerName, null, categoryMatch), new List<TreeViewItemData<Descriptor>>());
                            currentFolders.Add(newFolder);
                            currentFolders = (List<TreeViewItemData<Descriptor>>)newFolder.children;
                        }
                        else if (!hasSearch) // This is a separator, we skip separators when there's a search because of sorting that would mess up with them
                        {
                            currentFolders.Add(new TreeViewItemData<Descriptor>(id++, new Separator(containerName, containerName, null, categoryMatch)));
                        }
                    }
                    else if (!isSeparator)
                    {
                        currentFolders = (List<TreeViewItemData<Descriptor>>)currentFolders.Single(x => x.data.name == containerName).children;
                    }
                }

                for (var i = 0; i < matchingDescriptors.Length; i++)
                {
                    var descriptor = matchingDescriptors[i];
                    // When no search, only add main variant (which is the first one)
                    if (hasSearch && GetShowSubVariantInSearchResults() || i == 0)
                    {
                        currentFolders.Add(new TreeViewItemData<Descriptor>(id++, descriptor));
                    }
                    // But add any matching variant, even sub-variants even when there's no search pattern
                    if ((i == 0 || GetShowSubVariantInSearchResults()) && isMainTree && settings.IsFavorite(descriptor))
                    {
                        favorites.Add(new TreeViewItemData<Descriptor>(id++, descriptor));
                    }
                }
            }

            if (isMainTree)
            {
                m_FavoriteCategory = new TreeViewItemData<Descriptor>(id, new Descriptor("Favorites", string.Empty), favorites);
                treeViewData.Insert(0, m_FavoriteCategory);
            }

            if (hasSearch && isMainTree)
            {
                foreach (var treeViewItemData in treeViewData)
                {
                    SortSearchResult(treeViewItemData);
                }
            }
        }

        private void SortSearchResult(TreeViewItemData<Descriptor> treeViewItemData)
        {
            if (!treeViewItemData.hasChildren)
                return;
            var children = (List<TreeViewItemData<Descriptor>>)treeViewItemData.children;
            children.Sort((x, y) => y.data.matchingScore.CompareTo(x.data.matchingScore));
            foreach (var child in treeViewItemData.children)
            {
                SortSearchResult(child);
            }
        }

        private void ToggleCollapseFromTreeview(bool hide, int index = -1)
        {
            if (index >= 0)
            {
                m_Treeview.selectedIndex = index;
            }

            m_CollapseButton.value = hide;
        }

        private void ToggleCollapse(bool hide)
        {
            hideDetailsPanel = hide;

            var windowWidth = hideDetailsPanel ? m_Treeview.resolvedStyle.width + 2 : DefaultWindowWidth;
            position = new Rect(position.position, new Vector2(windowWidth, position.height));
            UpdateDetailsPanelVisibility();

            // Delay so that resolved style is computed
            EditorApplication.delayCall += SaveSettings;
        }

        internal static void Show(Vector2 graphPosition, Vector2 screenPosition, IProvider provider)
        {
            CreateInstance<VFXFilterWindow>().Init(graphPosition, screenPosition, provider);
        }

        private IEnumerable<Descriptor> GetMatches(IVFXModelDescriptor modelDescriptor, string pattern, string[] patternTokens)
        {
            s_GetMatchesPerfMarker.Begin();
            try
            {
                var score = GetVariantMatchScore(modelDescriptor, pattern, patternTokens, out var match, out var synonym);
                if (score > 0f)
                {
                    var descriptor = new Descriptor(modelDescriptor, match, synonym) { matchingScore = score };
                    yield return descriptor;
                }

                if (GetShowSubVariantInSearchResults())
                {
                    var subVariantsDescriptors = modelDescriptor.subVariantDescriptors;
                    if (subVariantsDescriptors != null)
                    {
                        foreach (var v in subVariantsDescriptors)
                        {
                            score = GetVariantMatchScore(v, pattern, patternTokens, out match, out synonym);
                            if (score > 0f)
                            {
                                yield return new Descriptor(v, match, synonym) { matchingScore = score };
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

        private float GetVariantMatchScore(IVFXModelDescriptor modelDescriptor, string pattern, string[] patternTokens, out string match, out string synonymMatch)
        {
            synonymMatch = match = null;
            if (!hasSearch)
                return 1f;

            var initialPatternLength = pattern.Length;
            var fixedPattern = pattern;
            var score = GetTextMatchScore(modelDescriptor.name, ref pattern, patternTokens, out match);
            if (pattern.Length > 0)
            {
                score += GetTextMatchScore(modelDescriptor.category, ref fixedPattern, patternTokens, out _);
            }
            if (pattern.Length > 0)
            {
                foreach (var synonym in modelDescriptor.synonyms)
                {
                    score += GetTextMatchScore(synonym, ref pattern, patternTokens, out synonymMatch);
                    if (pattern.Length == 0)
                        break;
                }
            }

            return initialPatternLength > 0 ? (pattern.Length == 0 ? score : 0) : 1f;
        }

        private float GetTextMatchScore(string text, ref string pattern, string[] patternTokens, out string matchHighlight)
        {
            var score = 0f;
            matchHighlight = null;
            if (string.IsNullOrEmpty(text))
                return 0f;
            if (string.IsNullOrEmpty(pattern))
                return 100f;

            var start = text.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
            if (start != -1)
            {
                matchHighlight = text.Insert(start + pattern.Length, "#").Insert(start, "#@");
                score = 10f + (float)pattern.Length / text.Length;
                pattern = string.Empty;
                return score;
            }

            // Match all pattern tokens with the source tokens
            var sourceTokens = text.Split(s_MatchingSeparators, StringSplitOptions.RemoveEmptyEntries).ToList();
            if (sourceTokens.Count >= patternTokens.Length)
            {
                s_PatternMatches.Clear();
                foreach (var token in patternTokens)
                {
                    foreach (var sourceToken in sourceTokens)
                    {
                        if (sourceToken.Contains(token, StringComparison.OrdinalIgnoreCase))
                        {
                            sourceTokens.Remove(sourceToken);
                            s_PatternMatches.Add(token);
                            pattern = pattern.Replace(token, string.Empty).Trim();
                            score += (float)token.Length / sourceToken.Length;
                            break;
                        }
                    }
                }

                if (s_PatternMatches.Count > 0)
                {
                    matchHighlight = text;
                    foreach (var match in s_PatternMatches)
                    {
                        matchHighlight = matchHighlight.Replace(match, $"#@{match}#", StringComparison.OrdinalIgnoreCase);
                    }

                    return score / text.Length;
                }
            }

            // Consider pattern as initials and match with source first letters (ex: SPSC => Set Position Shape Cone)
            var initialIndex = 0;
            var matchingIndices = new List<int>();
            for (int i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (c == ' ' || c == '|' || i == 0)
                {
                    while (!char.IsLetterOrDigit(c) && i < text.Length - 1)
                    {
                        c = text[++i];
                    }

                    if (i == text.Length - 1 && !char.IsLetterOrDigit(c))
                    {
                        break;
                    }

                    if (initialIndex < pattern.Length)
                    {
                        if (char.ToLower(c) == pattern[initialIndex])
                        {
                            matchingIndices.Add(i);
                            initialIndex++;
                            if (initialIndex == pattern.Length)
                            {
                                matchHighlight = new string(text.SelectMany((x, k) => matchingIndices.Contains(k) ? new [] { '#', '@', x, '#' } : new []{ x }).ToArray());
                                pattern = string.Empty;
                                return 1f;
                            }
                        }
                    }
                }
            }

            return score;
        }

        private void Init(Vector2 graphPosition, Vector2 screenPosition, IProvider provider)
        {
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

        private string GetSearchPattern()
        {
            if (m_SearchPattern?.StartsWith("+") == true)
                return m_SearchPattern.Substring(1, m_SearchPattern.Length - 1);
            return m_SearchPattern;
        }

        private bool GetShowSubVariantInSearchResults()
        {
            return settings.showSubVariantsInSearchResults || m_SearchPattern?.StartsWith("+") == true;
        }
    }
}
