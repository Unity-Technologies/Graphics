using System;
using System.Linq;
using System.Collections.Generic;

using UnityEditor.Experimental;
using UnityEditor.Experimental.GraphView;
using UnityEditor.VFX.Block;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.VFX;
using UnityEngine.UIElements;

using PositionType = UnityEngine.UIElements.Position;

namespace UnityEditor.VFX.UI
{
    interface IParameterItem
    {
        VFXGraph graph { get; set; }
        string title { get; set; }
        // This property is used to add an extra margin below the last item (done in css)
        bool isLast { get; set; }
        bool isExpanded { get; set; }
        int index { get; set; }
        int id { get; set; }
        bool canRename { get; }
        ISelectable selectable { get; set; }
        bool Accept(IParameterItem item);
    }

    interface IParameterCategory
    {
        bool isRoot { get; }
    }

    class ParameterItem : IParameterItem
    {
        protected ParameterItem(string title, int id,  bool isExpanded)
        {
            this.title = title;
            this.id = id;
            this.isExpanded = isExpanded;
        }

        public VFXGraph graph { get; set; }
        public virtual string title { get; set; }
        public bool isLast { get; set; }
        public bool isExpanded { get; set; }

        public int index { get; set; }
        public int id { get; set; }
        public virtual bool canRename => true;
        public ISelectable selectable { get; set; }

        public virtual bool Accept(IParameterItem item) => false;
    }

    class PropertyCategory : ParameterItem, IParameterCategory
    {
        public PropertyCategory(string title, int id, bool isRoot, bool isExpanded) : base(title, id, isExpanded)
        {
            this.isRoot = isRoot;
        }
        public bool isRoot { get; }
        public override bool canRename => !isRoot;

        public override bool Accept(IParameterItem item)
        {
            return item is PropertyItem;
        }
    }

    class PropertyItem : ParameterItem
    {
        public PropertyItem(VFXParameterController controller, int id) : base(null, id, false)
        {
            this.controller = controller;
        }
        public VFXParameterController controller { get; }

        public override string title => controller.exposedName;
    }

    class OutputCategory : PropertyCategory
    {
        public const string Label = "Output";
        public OutputCategory(bool isExpanded, int id) : base(Label, id, false, isExpanded) {}
        public override bool canRename => false;
    }

    class AttributeItem : ParameterItem
    {
        public AttributeItem(string name, CustomAttributeUtility.Signature type, int id, string description, bool isExpanded, bool isEditable, IEnumerable<string> subgraphUse) : base(name, id, false)
        {
            this.title = name;
            this.type = type;
            this.description = description;
            this.isEditable = isEditable;
            this.subgraphUse = subgraphUse?.ToArray();
            this.isBuiltIn = VFXAttributesManager.ExistsBuiltInOnly(name);
            this.isReadOnly = Array.FindIndex(VFXAttributesManager.GetBuiltInNamesAndCombination(false, false, true, false).ToArray(), x => x == name) != -1;
            this.isExpanded = isExpanded;
        }

        public CustomAttributeUtility.Signature type { get; set; }
        public bool isEditable { get; }
        public string[] subgraphUse { get; }
        public bool isBuiltIn { get; }
        public bool isReadOnly { get; }
        public string description { get; set; }
        public override bool canRename => !isBuiltIn && isEditable;
    }

    class AttributeCategory : ParameterItem, IParameterCategory
    {
        public AttributeCategory(string title, int id, bool isRoot, bool isExpanded) : base(title, id, isExpanded)
        {
            this.isRoot = isRoot;
        }
        public bool isRoot { get; }
        public override bool canRename => false;
    }

    class AttributeSeparator : AttributeCategory
    {
        public AttributeSeparator(string title, int id, bool isRoot, bool isExpanded) : base(title, id, isRoot, isExpanded)
        {
        }
    }

    class CustomAttributeCategory : AttributeCategory
    {
        public const string Title = "Custom Attributes";
        public CustomAttributeCategory(int id, bool isExpanded) : base("Custom Attributes", id, false, isExpanded) { }
    }

    class VFXBlackboard : Blackboard, IVFXMovable, IControlledElement<VFXViewController>
    {
        [Flags]
        enum ViewMode
        {
            Properties = 0x1,
            Attributes = 0x2,
            All = Properties | Attributes,
        }

        const string PropertiesCategoryTitle = "Properties";
        const string BuiltInAttributesCategoryTitle = "Built-in Attributes";
        const string AttributesCategoryTitle = "Attributes";

        static readonly Rect defaultRect = new Rect(100, 100, 300, 500);
        static System.Reflection.PropertyInfo s_LayoutManual = typeof(VisualElement).GetProperty("isLayoutManual", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        readonly VFXView m_View;
        readonly Button m_AddButton;
        readonly List<TreeViewItemData<IParameterItem>> m_ParametersController = new ();

        VFXViewController m_Controller;
        Image m_VCSStatusImage;
        TreeView m_Treeview;
        Label m_PathLabel;
        TextField m_PathTextField;
        bool m_CanEdit;
        bool m_IsChangingSelection;
        ViewMode m_ViewMode;
        List<string> m_pendingSelectionItems = new ();

        Controller IControlledElement.controller => m_Controller;

        public VFXViewController controller
        {
            get => m_Controller;
            set
            {
                if (m_Controller != value)
                {
                    m_pendingSelectionItems.Clear();
                    if (m_Controller != null)
                    {
                        m_Controller.UnregisterHandler(this);
                    }
                    Clear();
                    m_Controller = value;

                    if (m_Controller != null)
                    {
                        m_Controller.RegisterHandler(this);
                        Update(true);
                    }
                    m_AddButton.SetEnabled(m_Controller != null);
                }
            }
        }

        private bool isPropertiesCategoryExpanded
        {
            get => EditorPrefs.GetBool("VFXBlackboard.isPropertiesCategoryExpanded");
            set => EditorPrefs.SetBool("VFXBlackboard.isPropertiesCategoryExpanded", value);
        }

        private bool isOutputCategoryExpanded
        {
            get => EditorPrefs.GetBool("VFXBlackboard.isOutputCategoryExpanded");
            set => EditorPrefs.SetBool("VFXBlackboard.isOutputCategoryExpanded", value);
        }

        private bool isAttributesCategoryExpanded
        {
            get => EditorPrefs.GetBool("VFXBlackboard.isAttributesCategoryExpanded");
            set => EditorPrefs.SetBool("VFXBlackboard.isAttributesCategoryExpanded", value);
        }

        private bool isBuiltInAttributesCategoryExpanded
        {
            get => EditorPrefs.GetBool("VFXBlackboard.isBuiltInAttributesCategoryExpanded");
            set => EditorPrefs.SetBool("VFXBlackboard.isBuiltInAttributesCategoryExpanded", value);
        }

        private bool isCustomAttributesCategoryExpanded
        {
            get => EditorPrefs.GetBool("VFXBlackboard.isCustomAttributesCategoryExpanded");
            set => EditorPrefs.SetBool("VFXBlackboard.isCustomAttributesCategoryExpanded", value);
        }

        new void Clear()
        {
            m_ParametersController.Clear();
            m_Treeview.SetRootItems(m_ParametersController);
            m_Treeview.Rebuild();
        }

        public VFXBlackboard(VFXView view)
        {
            m_View = view;
            m_ViewMode = ViewMode.All;
            addItemRequested = OnAddItemButton;

            this.scrollable = false;

            SetPosition(BoardPreferenceHelper.LoadPosition(BoardPreferenceHelper.Board.blackboard, defaultRect));

            m_Treeview = new TreeView
            {
                reorderable = true,
                selectionType = SelectionType.Multiple,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight
            };

            m_Treeview.canStartDrag += OnCanDragStart;
            m_Treeview.dragAndDropUpdate += OnDragAndDropUpdate;
            m_Treeview.handleDrop += OnHandleDrop;
            m_Treeview.setupDragAndDrop += OnSetupDragAndDrop;

            m_Treeview.makeItem += MakeItem;
            m_Treeview.bindItem += BindItem;
            m_Treeview.unbindItem += UnbindItem;
            m_Treeview.selectionChanged += OnSelectionChanged;
            // Trickle down because on macOS the context menu is opened on PointerDown event which stop the event propagation
            m_Treeview.RegisterCallback<PointerDownEvent>(OnTreeViewPointerDown, TrickleDown.TrickleDown);
            Add(m_Treeview);

            var tabsContainer = new VisualElement { name = "tabsContainer" };
            var allTab = new Toggle { text = "All", value = true };
            var propertiesTab = new Toggle { text = "Properties" };
            var attributesTab = new Toggle { text = "Attributes" };
            tabsContainer.Add(new VisualElement { name = "bottomBorder" });
            tabsContainer.Add(allTab);
            tabsContainer.Add(propertiesTab);
            tabsContainer.Add(attributesTab);
            tabsContainer.Add(new VisualElement { name = "spacer" });
            allTab.RegisterCallback<ChangeEvent<bool>, ViewMode>(OnTabChanged, ViewMode.All);
            propertiesTab.RegisterCallback<ChangeEvent<bool>, ViewMode>(OnTabChanged, ViewMode.Properties);
            attributesTab.RegisterCallback<ChangeEvent<bool>, ViewMode>(OnTabChanged, ViewMode.Attributes);
            Insert(0, tabsContainer);

            styleSheets.Add(VFXView.LoadStyleSheet("VFXBlackboard"));

            RegisterCallback<FocusInEvent>(OnGetFocus);
            RegisterCallback<KeyDownEvent>(OnKeyDown);

            focusable = true;

            m_AddButton = this.Q<Button>(name: "addButton");
            m_AddButton.style.width = 27;
            m_AddButton.style.height = 27;
            m_AddButton.SetEnabled(false);

            Resizer resizer = this.Query<Resizer>();
            resizer.RemoveFromHierarchy();

            hierarchy.Insert(0, new ResizableElement());

            style.position = PositionType.Absolute;

            var labelContainer = this.Q<VisualElement>("labelContainer");
            m_PathLabel = labelContainer.Q<Label>("subTitleLabel");
            m_PathLabel.RegisterCallback<MouseDownEvent>(OnMouseDownSubTitle);

            m_PathTextField = new TextField();
            m_PathTextField.style.display = DisplayStyle.None;
            m_PathTextField.Q(TextField.textInputUssName).RegisterCallback<FocusOutEvent>(OnEditPathTextFinished, TrickleDown.TrickleDown);
            m_PathTextField.Q(TextField.textInputUssName).RegisterCallback<KeyDownEvent>(OnPathTextFieldKeyPressed, TrickleDown.TrickleDown);
            labelContainer.Add(m_PathTextField);

            if (s_LayoutManual != null)
                s_LayoutManual.SetValue(this, false);

            this.AddManipulator(new ContextualMenuManipulator(BuildContextualMenu));
        }

        private void OnGetFocus(FocusInEvent evt)
        {
            m_View.SetBoardToFront(this);
        }

        private void OnTreeViewPointerDown(PointerDownEvent evt)
        {
            if (evt.button == (int)MouseButton.RightMouse && evt.clickCount == 1 && evt.target is VFXBlackboardFieldBase field)
            {
                if (!m_Treeview.selectedItems.Contains(field.item))
                {
                    m_Treeview.SetSelectionById(field.item.id);
                }
            }
        }

        private StartDragArgs OnSetupDragAndDrop(SetupDragAndDropArgs arg)
        {
            var startArgs = arg.startDragArgs;
            var items = arg.selectedIds.Select(x => m_Treeview.GetItemDataForId<IParameterItem>(x)).ToList();
            items.ForEach(x => x.graph = controller.graph);
            startArgs.SetGenericData("DragSelection", items);
            return startArgs;
        }

        private void OnSelectionChanged(IEnumerable<object> selectedItems)
        {
            if (m_IsChangingSelection)
                return;

            var newSelection = selectedItems.ToArray();
            if (newSelection.Length > 0)
            {
                try
                {
                    m_IsChangingSelection = true;
                    m_View.ClearSelectionFast();
                    IParameterItem lastPendingItem = null;
                    foreach (var item in newSelection)
                    {
                        if (item is IParameterItem parameterItem)
                        {
                            if (parameterItem.selectable != null)
                            {
                                AddToSelection(parameterItem.selectable);
                            }
                            else
                            {
                                lastPendingItem = parameterItem;
                            }
                        }
                    }

                    if (lastPendingItem != null)
                    {
                        m_pendingSelectionItems.Add(lastPendingItem.title);
                        m_Treeview.ScrollToItemById(lastPendingItem.id);
                    }
                }
                finally
                {
                    m_IsChangingSelection = false;
                }
            }
        }

        private DragVisualMode OnHandleDrop(HandleDragAndDropArgs arg)
        {
            if (arg.parentId < 0)
            {
                return DragVisualMode.Rejected;
            }

            var fieldId = new List<int>();
            var childIndex = arg.childIndex;
            // Reorder
            if (arg.dropPosition == DragAndDropPosition.BetweenItems)
            {
                // Moving an attribute
                if (m_Treeview.selectedItem is AttributeItem)
                {
                    foreach (var currentItem in m_Treeview.selectedItems.OfType<AttributeItem>())
                    {
                        fieldId.Add(currentItem.id);
                        currentItem.graph.SetCustomAttributeOrder(currentItem.title, arg.childIndex);
                    }
                }
                // Moving a property
                else if (m_Treeview.selectedItem is PropertyItem)
                {
                    foreach (var currentItem in m_Treeview.selectedItems.OfType<PropertyItem>())
                    {
                        fieldId.Add(currentItem.id);
                        var parentCategory = m_Treeview.GetItemDataForId<IParameterItem>(arg.parentId) as PropertyCategory;
                        if (parentCategory.isRoot)
                        {
                            currentItem.controller.model.category = string.Empty;
                            if (currentItem.controller.isOutput)
                            {
                                currentItem.controller.model.SetSettingValue("m_Exposed", true);
                            }
                            currentItem.controller.isOutput = false;
                        }
                        else if (parentCategory is OutputCategory)
                        {
                            currentItem.controller.model.category = string.Empty;
                            currentItem.controller.isOutput = true;
                            currentItem.controller.model.SetSettingValue("m_Exposed", false);
                        }
                        else
                        {
                            currentItem.controller.model.category = parentCategory.title;
                        }

                        controller.SetParametersOrder(currentItem.controller, arg.childIndex);
                    }
                }
                // Moving a category
                else if (m_Treeview.selectedItem is PropertyCategory category)
                {
                    var parentItem = m_Treeview.GetItemDataForId<IParameterItem>(arg.parentId);
                    if (arg.parentId < 0 || parentItem is PropertyCategory { isRoot: true } and not OutputCategory)
                    {
                        var parentItemData = m_ParametersController.Find(x => x.id == arg.parentId);
                        var rootPropertiesCount = parentItemData.children.Count(x => x.data is PropertyItem);
                        fieldId.Add(category.id);
                        controller.MoveCategory(category.title, childIndex - rootPropertiesCount);
                    }
                }
            }
            // Change category
            else if (m_Treeview.GetItemDataForId<IParameterItem>(arg.parentId) is PropertyCategory propertyCategory)
            {
                foreach (var currentItem in m_Treeview.selectedItems.OfType<PropertyItem>())
                {
                    fieldId.Add(currentItem.id);
                    if (currentItem.controller.isOutput && propertyCategory is not OutputCategory)
                    {
                        currentItem.controller.model.category = FilterOutReservedCategoryName(propertyCategory.title);
                        currentItem.controller.isOutput = false;
                        currentItem.controller.exposed = true;
                    }
                    else if (propertyCategory is OutputCategory && currentItem.controller.isOutput == false)
                    {
                        currentItem.controller.model.category = string.Empty;
                        currentItem.controller.isOutput = true;
                        currentItem.controller.exposed = false;
                    }
                    else
                    {
                        var category = FilterOutReservedCategoryName(propertyCategory.title);
                        m_View.controller.ChangeCategory(currentItem.controller.model, category);
                    }

                    // Moving to the root "Properties" category. We want to move the item as last parameter, but above the first category
                    if (string.Compare(propertyCategory.title, PropertiesCategoryTitle, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        var propertiesTreeviewItem = m_ParametersController.SelectMany(GetDataRecursive).Single(x => string.Compare(x.data.title, PropertiesCategoryTitle, StringComparison.OrdinalIgnoreCase) == 0);
                        childIndex = propertiesTreeviewItem.children.TakeWhile(x => x.data is PropertyItem).Count();
                    }
                }
            }

            if (fieldId.Count > 0)
            {
                m_IsChangingSelection = true;
                try
                {
                    foreach (var id in fieldId)
                    {
                        m_Treeview.viewController.Move(id, arg.parentId, childIndex, true);
                    }

                    UpdateLastCategoryItem(arg.parentId);
                    m_Treeview.ClearSelection();

                }
                finally
                {
                    m_IsChangingSelection = false;
                }
                    
                return DragVisualMode.Move;
            }

            return DragVisualMode.Rejected;
        }

        // Mark last item with a uss class is usefull to give an extra bottom margin to last item
        private void UpdateLastCategoryItem(int id)
        {
            var parentData = m_ParametersController.SelectMany(GetDataRecursive).Single(x => x.id == id);
            if (parentData.hasChildren)
            {
                var childItems = parentData.children.Where(x => x.data is PropertyItem or AttributeItem).ToArray();
                for (var i = 0; i < childItems.Length; i++)
                {
                    var child = childItems[i];
                    if (i == childItems.Length - 1)
                    {
                        child.data.isLast = true;
                        m_Treeview.GetRootElementForId(child.id)?.AddToClassList("last");
                    }
                    else
                    {
                        m_Treeview.GetRootElementForId(child.id)?.RemoveFromClassList("last");
                        child.data.isLast = false;
                    }
                }
            }
        }

        private DragVisualMode OnDragAndDropUpdate(HandleDragAndDropArgs arg)
        {
            m_Treeview.ReleaseMouse();

            var parentItem = m_Treeview.GetItemDataForId<IParameterItem>(arg.parentId);

            if (arg.dropPosition == DragAndDropPosition.OverItem)
            {
                foreach (var selectedItem in m_Treeview.selectedItems)
                {
                    if (selectedItem is PropertyItem)
                    {
                        return DragVisualMode.Move;
                    }

                    if (selectedItem is PropertyCategory { isRoot: false })
                    {
                        return parentItem.Accept(selectedItem as IParameterItem) ? DragVisualMode.Move : DragVisualMode.Rejected;
                    }
                }
            }
            // Allow properties or categories to be moved inside the root category
            else if (arg.dropPosition == DragAndDropPosition.BetweenItems && parentItem is PropertyCategory { isRoot: true})
            {
                return DragVisualMode.Move;
            }
            // Allow properties only to be moved inside a non-root category
            else if (arg.dropPosition == DragAndDropPosition.BetweenItems && parentItem is PropertyCategory && m_Treeview.selectedItems.All(x => x is PropertyAttribute))
            {
                return DragVisualMode.Move;
            }
            else if (arg.dropPosition == DragAndDropPosition.BetweenItems && parentItem is CustomAttributeCategory && m_Treeview.selectedItems.All(x => x is AttributeItem))
            {
                return DragVisualMode.Move;
            }
            else if (arg.dropPosition == DragAndDropPosition.OutsideItems)
            {
                return DragVisualMode.Move;
            }
            else if (arg.dropPosition == DragAndDropPosition.BetweenItems && parentItem is PropertyCategory && m_Treeview.selectedItem is PropertyItem)
            {
                return DragVisualMode.Move;
            }

            return DragVisualMode.Rejected;
        }

        private bool OnCanDragStart(CanStartDragArgs arg)
        {
            return m_Treeview.selectedItems.Any(x => x is PropertyItem or AttributeItem or PropertyCategory);
        }

        private void OnTabChanged(ChangeEvent<bool> evt, ViewMode viewMode)
        {
            if (evt.newValue)
            {
                var tabsContainer = this.Q<VisualElement>("tabsContainer");
                tabsContainer.Query<Toggle>().ForEach(x => { x.value = x == evt.target; });
                m_ViewMode = viewMode;
                Update(true);
            }
        }

        private void UnbindItem(VisualElement element, int index)
        {
            element.parent.parent.RemoveFromClassList("category");
            element.parent.parent.RemoveFromClassList("sub-category");
            element.parent.parent.RemoveFromClassList("collapsed");
            element.parent.parent.RemoveFromClassList("root");
            element.parent.parent.RemoveFromClassList("item");
            element.parent.parent.RemoveFromClassList("last");
            element.parent.parent.RemoveFromClassList("built-in");
            element.parent.parent.RemoveFromClassList("sub-graph");
            element.parent.parent.RemoveFromClassList("separator");
            element.ClearClassList();


            // work around to avoid losing selection with virtualized treeview
            bool oldChangingSelection = m_IsChangingSelection;
            m_IsChangingSelection = true;
            try
            {
                element.Clear();
            }
            finally
            {
                m_IsChangingSelection = oldChangingSelection;
            }
        }

        private void BindItem(VisualElement element, int index)
        {
            element.SetEnabled(m_CanEdit);
            var item = m_Treeview.GetItemDataForIndex<IParameterItem>(index);
            item.index = index;
            item.id = m_Treeview.viewController.GetIdForIndex(index);
            var rootElement = element.parent.parent;
            if (!item.isExpanded)
                rootElement.AddToClassList("collapsed");

            // This is a hack to put the expand/collapse button above the item so that we can interact with it
            var toggle = rootElement.Q<Toggle>();
            toggle.BringToFront();
            element.AddToClassList("blackboardRowContainer");
            switch (item)
            {
                case PropertyCategory category when m_ViewMode.HasFlag(ViewMode.Properties):
                {
                    rootElement.AddToClassList(category.isRoot ? "category" : "sub-category");
                    var blackboardCategory = new VFXBlackboardCategory(category) { title = item.title };
                    category.selectable = blackboardCategory;
                    element.Add(blackboardCategory);
                    break;
                }
                case PropertyItem parameterItem when m_ViewMode.HasFlag(ViewMode.Properties):
                    if (string.IsNullOrEmpty(parameterItem.controller.model.category) && !parameterItem.controller.isOutput)
                    {
                        // This is to set a smaller indentation
                        element.AddToClassList("no-category");
                    }
                    rootElement.AddToClassList("item");
                    var bbRow = new VFXBlackboardRow(parameterItem, parameterItem.controller);
                    parameterItem.selectable = bbRow.field;
                    element.Add(bbRow);
                    break;
                case AttributeSeparator separator:
                {
                    rootElement.AddToClassList("separator");
                    separator.selectable = null;
                    element.Add(new Label(separator.title));
                    break;
                }
                case AttributeCategory category when m_ViewMode.HasFlag(ViewMode.Attributes):
                {
                    rootElement.AddToClassList(category.isRoot ? "category" : "sub-category");
                    var propertyRow = new VFXBlackboardCategory(category) { title = item.title };
                    category.selectable = propertyRow;
                    element.Add(propertyRow);
                    break;
                }
                case AttributeItem attributeItem when m_ViewMode.HasFlag(ViewMode.Attributes):
                    rootElement.AddToClassList("item");
                    var attributeRow = new VFXBlackboardAttributeRow(attributeItem);
                    attributeItem.selectable = attributeRow.field;
                    element.Add(attributeRow);
                    if (!attributeItem.isEditable)
                    {
                        rootElement.AddToClassList(attributeItem.subgraphUse?.Length > 0 ? "sub-graph" : "built-in");
                    }
                    break;
            }

            if (m_pendingSelectionItems.Contains(item.title))
            {
                m_Treeview.AddToSelection(index);
                m_pendingSelectionItems.Remove(item.title);
            }
            toggle.RegisterCallback<ChangeEvent<bool>, IParameterItem>(OnToggleExpandRow, item);
            rootElement.AddToClassList(item.isLast ? "last" : null);
        }

        private void OnToggleExpandRow(ChangeEvent<bool> evt, IParameterItem item)
        {
            if (evt.target is Toggle toggle)
            {
                if (toggle.value)
                {
                    toggle.parent.RemoveFromClassList("collapsed");
                }
                else
                {
                    toggle.parent.AddToClassList("collapsed");
                }

                item.isExpanded = toggle.value;
                if (item is OutputCategory)
                {
                    isOutputCategoryExpanded = evt.newValue;
                }
                else if (item is PropertyCategory category)
                {
                    if (category.isRoot)
                        isPropertiesCategoryExpanded = evt.newValue;
                    else
                        m_View.controller.SetCategoryExpanded(category.title, evt.newValue);
                }
                else if (item is AttributeCategory attributeCategory)
                {
                    switch (attributeCategory.title)
                    {
                        case AttributesCategoryTitle:
                            isAttributesCategoryExpanded = evt.newValue;
                            break;
                        case BuiltInAttributesCategoryTitle:
                            isBuiltInAttributesCategoryExpanded = evt.newValue;
                            break;
                        case CustomAttributeCategory.Title:
                            isCustomAttributesCategoryExpanded = evt.newValue;
                            break;
                    }
                }
            }
        }

        private VisualElement MakeItem() => new VisualElement();

        public void LockUI()
        {
            if (!m_CanEdit)
            {
                return;
            }

            CreateVCSImageIfNeeded();

            m_VCSStatusImage.style.display = DisplayStyle.Flex;
            m_CanEdit = false;
            m_AddButton.tooltip = "Check out to modify";
            UpdateSubtitle();
            // We need to refresh the treeview items to enable them
            m_Treeview.RefreshItems();
        }

        public void UnlockUI()
        {
            if (m_CanEdit)
            {
                return;
            }

            if (m_VCSStatusImage != null)
            {
                m_VCSStatusImage.style.display = DisplayStyle.None;
            }

            m_CanEdit = true;
            m_AddButton.tooltip = "Click to add a property or attribute";
            UpdateSubtitle();
            // We need to refresh the treeview items to enable them
            m_Treeview.RefreshItems();
        }

        public void AddPendingSelection(string itemName)
        {
            m_pendingSelectionItems.Add(itemName);
        }

        private void CreateVCSImageIfNeeded()
        {
            if (m_VCSStatusImage == null)
            {
                m_VCSStatusImage = new Image();
                m_VCSStatusImage.style.position = PositionType.Absolute;
                m_VCSStatusImage.style.left = 12;
                m_VCSStatusImage.style.top = 12;
                m_VCSStatusImage.style.width = 14;
                m_VCSStatusImage.style.height = 14;
                m_VCSStatusImage.style.alignSelf = Align.Center;
                m_VCSStatusImage.image = EditorGUIUtility.LoadIcon(EditorResources.iconsPath + "VersionControl/P4_OutOfSync.png");

                m_AddButton.Add(m_VCSStatusImage);
            }
        }

        private void UpdateSubtitle()
        {
            m_PathLabel.style.display = DisplayStyle.None;

            if (controller != null && controller.graph != null)
            {
                var hasCategory = !string.IsNullOrEmpty(controller.graph.categoryPath);
                var isSubgraph = controller.graph.visualEffectResource.isSubgraph;

                if (hasCategory)
                {
                    m_PathLabel.style.display = DisplayStyle.Flex;
                    m_PathLabel.text = controller.graph.categoryPath;
                    m_PathLabel.style.unityFontStyleAndWeight = FontStyle.Normal;
                }
                else if (isSubgraph)
                {
                    m_PathLabel.style.display = DisplayStyle.Flex;
                    m_PathLabel.text = "Enter subgraph category path here";
                    m_PathLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
                }

                if (!m_CanEdit)
                {
                    m_PathLabel.tooltip = "Check out to modify";
                }
            }
        }

        public void AddCategory(string initialName)
        {
            var categories = m_Controller.graph.UIInfos.categories.Select(x => x.name).ToHashSet();
            var newCategoryName = VFXParameterController.MakeNameUnique(initialName, categories);

            controller.graph.UIInfos.categories ??= new List<VFXUI.CategoryInfo>();
            controller.graph.UIInfos.categories.Add(new VFXUI.CategoryInfo { name = newCategoryName });
            m_Controller.graph.Invalidate(VFXModel.InvalidationCause.kUIChanged);

            var parentId = m_ParametersController.SelectMany(GetDataRecursive).Single(x => string.Compare(x.data.title, PropertiesCategoryTitle, StringComparison.OrdinalIgnoreCase) == 0).id;
            var newId = m_Treeview.viewController.GetAllItemIds().Max() + 1;
            m_Treeview.AddItem(new TreeViewItemData<IParameterItem>(newId, new PropertyCategory(newCategoryName, newId, false, false)), parentId, -1, true);

            isPropertiesCategoryExpanded = true;
            ExpandItem(PropertiesCategoryTitle);
            OpenTextEditor<VFXBlackboardCategory>(newCategoryName);
        }

        public string DuplicateCategory(string category, string[] parametersToDuplicate = null)
        {
            // Create treeview item first so that duplicated category's parameters (if any) will find the parent category
            var newCategoryName = VFXParameterController.MakeNameUnique(category, controller.graph.UIInfos.categories?.Select(x => x.name).ToHashSet() ?? new HashSet<string>());
            var parentId = m_ParametersController.SelectMany(GetDataRecursive).Single(x => x.children.Any(x => string.Compare(x.data.title, category, StringComparison.OrdinalIgnoreCase) == 0)).id;
            var newId = m_Treeview.viewController.GetAllItemIds().Max() + 1;
            m_Treeview.AddItem(new TreeViewItemData<IParameterItem>(newId, new PropertyCategory(newCategoryName, newId, false, true)), parentId, -1, true);
            m_View.DuplicateBlackBoardCategory(category, null, parametersToDuplicate);
            m_Treeview.ExpandItem(newId, false);
            m_Treeview.selectedIndex = m_Treeview.viewController.GetIndexForId(newId);

            return newCategoryName;
        }

        public void RemoveCategory(string categoryName)
        {
            var treeviewItem = m_ParametersController.SelectMany(GetDataRecursive).Single(x => string.Compare(x.data.title, categoryName, StringComparison.OrdinalIgnoreCase) == 0);
            m_Treeview.TryRemoveItem(treeviewItem.id);
        }

        public void AddParameter(VFXParameterController parameterController, bool notify = false)
        {
            var categoryName = string.IsNullOrEmpty(parameterController.model.category)
                ? parameterController.isOutput ? OutputCategory.Label : PropertiesCategoryTitle
                : parameterController.model.category;
            var parentData = m_ParametersController.SelectMany(GetDataRecursive).Single(x => string.Compare(x.data.title, categoryName, StringComparison.OrdinalIgnoreCase) == 0);

            var newId = m_Treeview.viewController.GetAllItemIds().Max() + 1;
            var newData = new PropertyItem(parameterController, newId);

            var childIndex = -1;
            // When adding to the root "Properties" category we want to insert the item as last parameter, but above the first category
            if (string.Compare(categoryName, PropertiesCategoryTitle, StringComparison.OrdinalIgnoreCase) == 0)
            {
                var propertiesTreeviewItem = m_ParametersController.SelectMany(GetDataRecursive).Single(x => string.Compare(x.data.title, PropertiesCategoryTitle, StringComparison.OrdinalIgnoreCase) == 0);
                childIndex = propertiesTreeviewItem.children.TakeWhile(x => x.data is PropertyItem).Count();
            }

            m_Treeview.AddItem(new TreeViewItemData<IParameterItem>(newId, newData), parentData.id, childIndex, notify);
            if (notify)
            {
                UpdateLastCategoryItem(parentData.id);
            }

            isPropertiesCategoryExpanded = true;
            ExpandItem(PropertiesCategoryTitle);
            if (!string.IsNullOrEmpty(categoryName))
            {
                ExpandItem(categoryName);
                m_View.controller.SetCategoryExpanded(categoryName, true);
            }
        }

        public string DuplicateParameter(VFXBlackboardField parameterField)
        {
            var newController = m_View.DuplicateBlackboardField(parameterField);

            var parentTreeviewItem = m_ParametersController.SelectMany(GetDataRecursive).Single(x => x.children.Any(x => x.data == parameterField.PropertyItem));

            var lastParentChild = parentTreeviewItem.children.LastOrDefault(x => x.data is PropertyItem);
            var insertIndex = lastParentChild.data == null ? -1 : m_Treeview.viewController.GetIndexForId(lastParentChild.id);
            var newId = m_Treeview.viewController.GetAllItemIds().Max() + 1;
            var parameterData = new PropertyItem(newController, newId) { isLast = true };

            m_Treeview.AddItem(new TreeViewItemData<IParameterItem>(newId, parameterData), parentTreeviewItem.id, insertIndex, true);
            UpdateLastCategoryItem(parentTreeviewItem.id);
            m_Treeview.selectedIndex = m_Treeview.viewController.GetIndexForId(newId);
            return newController.exposedName;
        }

        public bool RemoveParameter(IParameterItem parameterItem)
        {
            if (m_View.TryRemoveParameter(((PropertyItem)parameterItem).controller))
            {
                var treeviewItem = m_ParametersController.SelectMany(GetDataRecursive).Single(x => x.data == parameterItem);
                return m_Treeview.TryRemoveItem(treeviewItem.id);
            }

            return false;
        }

        public bool RemoveCustomAttribute(IParameterItem attributeItem)
        {
            if (m_View.TryRemoveCustomAttribute(attributeItem.title))
            {
                var treeviewItem = m_ParametersController.SelectMany(GetDataRecursive).Single(x => x.data == attributeItem);
                return m_Treeview.TryRemoveItem(treeviewItem.id);
            }

            return false;
        }

        private IEnumerable<TreeViewItemData<IParameterItem>> GetDataRecursive(TreeViewItemData<IParameterItem> data)
        {
            return new[] { data }.Union(data.children.SelectMany(GetDataRecursive));
        }

        private DropdownMenuAction.Status GetContextualMenuStatus()
        {
            //Use m_AddButton state which relies on locked & controller status
            return m_AddButton.enabledSelf && m_CanEdit
                ? DropdownMenuAction.Status.Normal
                : DropdownMenuAction.Status.Disabled;
        }

        private void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (m_ViewMode.HasFlag(ViewMode.Properties) && this.HasItemOfType<PropertyItem>(x => true))
            {
                evt.menu.AppendAction("Select All Properties", (a) => SelectAllProperties(), (a) => GetContextualMenuStatus());
                evt.menu.AppendAction("Select Unused Properties", (a) => SelectUnusedProperties(),(a) => GetContextualMenuStatus());
            }

            if (m_ViewMode.HasFlag(ViewMode.Attributes) && this.HasItemOfType<AttributeItem>(x => !x.isBuiltIn))
            {
                if (m_ViewMode.HasFlag(ViewMode.Properties))
                    evt.menu.AppendSeparator(string.Empty);
                evt.menu.AppendAction("Select All Custom Attributes", (a) => SelectAllCustomAttributes(), (a) => GetContextualMenuStatus());
                evt.menu.AppendAction("Select Unused Custom Attributes", (a) => SelectUnusedCustomAttributes(),(a) => GetContextualMenuStatus());
            }
        }

        private bool HasItemOfType<T>(Func<T,bool> predicate) where T: IParameterItem
        {
            return m_ParametersController.SelectMany(GetDataRecursive).Where(x => x.data is T).Any(x => predicate((T)x.data));
        }

        private void ExpandCategories(IEnumerable<string> categories)
        {
            var categoriesToExpand = categories
                .Concat(new []{ PropertiesCategoryTitle })
                .Distinct()
                .ToArray();
            foreach (var blackboardCategory in this.Query<VFXBlackboardCategory>().ToList())
            {
                if (categoriesToExpand.Contains(blackboardCategory.title))
                {
                    m_Treeview.ExpandItem(blackboardCategory.category.id);
                }
            }
        }

        private void SelectAllCustomAttributes()
        {
            m_View.ClearSelection();
            m_Treeview.ClearSelection();
            // Expand categories so child elements can be selected
            var categoriesToExpand = new []{ AttributesCategoryTitle, CustomAttributeCategory.Title };
            ExpandCategories(categoriesToExpand);
            this.Query<VFXBlackboardAttributeField>()
                .Where(x => !x.attribute.isBuiltIn)
                .ForEach(x => m_Treeview.AddToSelection(x.attribute.index));
        }

        private void SelectUnusedCustomAttributes()
        {
            m_View.ClearSelection();
            m_Treeview.ClearSelection();
            // Expand categories so child elements can be selected
            var categoriesToExpand = new []{ AttributesCategoryTitle, CustomAttributeCategory.Title };
            ExpandCategories(categoriesToExpand);
            var unused = m_View.controller.graph.GetUnusedCustomAttributes().ToArray();
            this.Query<VFXBlackboardAttributeField>()
                .Where(x => unused.Contains(x.attribute.title))
                .ForEach(x => m_Treeview.AddToSelection(x.attribute.index));
        }

        private void SelectAllProperties()
        {
            m_View.ClearSelection();
            m_Treeview.ClearSelection();
            // Expand categories so child elements can be selected
            ExpandCategories(controller.parameterControllers.Select(x => x.model.category));
            this.Query<VFXBlackboardField>().ForEach(x => m_Treeview.AddToSelection(x.PropertyItem.index));
        }

        private void SelectUnusedProperties()
        {
            m_View.ClearSelection();
            m_Treeview.ClearSelection();
            var unused = unusedParameters.ToList();
            // Expand categories so child elements can be selected
            ExpandCategories(unused.Select(x => x.category));
            this.Query<VFXBlackboardField>().Where(x => unused.Contains(x.controller.model)).ForEach(x => m_Treeview.AddToSelection(x.PropertyItem.index));
        }

        IEnumerable<VFXParameter> unusedParameters
        {
            get
            {
                return controller.parameterControllers
                    .Select(x => x.model)
                    .Where(t => !(t.isOutput ? t.inputSlots : t.outputSlots).Any(s => s.HasLink(true)));
            }
        }

        private void OnMouseDownSubTitle(MouseDownEvent evt)
        {
            if (evt.clickCount == 2 && evt.button == (int)MouseButton.LeftMouse && m_CanEdit && controller != null)
            {
                StartEditingPath();
                evt.StopPropagation();
                focusController.IgnoreEvent(evt);
            }
        }

        private void StartEditingPath()
        {
            m_PathTextField.style.display = DisplayStyle.Flex;
            m_PathTextField.value = m_PathLabel.text;
            m_PathLabel.style.display = DisplayStyle.None;

            m_PathTextField.style.fontSize = 11;
            m_PathTextField.Q("unity-text-input").Focus();
            m_PathTextField.SelectAll();
        }

        private void OnPathTextFieldKeyPressed(KeyDownEvent evt)
        {
            switch (evt.keyCode)
            {
                case KeyCode.Escape:
                    m_PathTextField.Q("unity-text-input").Blur();
                    break;
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    m_PathTextField.Q("unity-text-input").Blur();
                    break;
            }
        }

        private void OnEditPathTextFinished(FocusOutEvent evt)
        {
            m_PathTextField.style.display = DisplayStyle.None;
            m_PathLabel.style.display = DisplayStyle.Flex;

            controller.graph.categoryPath = m_PathTextField.text;
            UpdateSubtitle();
        }

        private void OnKeyDown(KeyDownEvent e)
        {
            if (e.keyCode == KeyCode.F2)
            {
                if (m_Treeview.selectedItem is IParameterItem { canRename: true } item && item.selectable is IBlackBoardElementWithTitle elementWithTitle)
                {
                    elementWithTitle.OpenTextEditor();
                }
            }
            // Prevent graph canvas framing
            else if (e.keyCode == KeyCode.F)
            {
                e.StopPropagation();
            }
        }

        public void ValidatePosition()
        {
            BoardPreferenceHelper.ValidatePosition(this, m_View, defaultRect);
        }

        private void OnAddParameter(object parameter)
        {
            var descriptor = (VFXModelDescriptorParameters)parameter;
            var newParam = m_Controller.AddVFXParameter(Vector2.zero, descriptor.variant);

            var categoryName = string.Empty;
            switch (m_Treeview.selectedItem)
            {
                case OutputCategory:
                    newParam.isOutput = true;
                    break;
                case PropertyCategory { isRoot: false } category:
                    categoryName = category.title;
                    break;
                case PropertyItem propertyItem:
                    var parentId = m_Treeview.GetParentIdForIndex(propertyItem.index);
                    var parent = m_Treeview.GetItemDataForId<IParameterItem>(parentId);
                    categoryName = parent.title;
                    newParam.isOutput = parent is OutputCategory;
                    break;
            }

            newParam.category = FilterOutReservedCategoryName(categoryName);
            if (!newParam.isOutput)
            {
                newParam.SetSettingValue("m_Exposed", true);
            }

            // We must delay because the VFXParameterController will be added on new graph update
            EditorApplication.delayCall += () => AddParameterIfNeeded(newParam, m_Controller);
        }

        private void AddParameterIfNeeded(VFXParameter parameter, VFXViewController vfxViewController)
        {
            // Check if parameter has not already been added by an Update because of a postprocess
            if (m_ParametersController.SelectMany(GetDataRecursive).All(x => string.Compare(parameter.exposedName, x.data.title, StringComparison.OrdinalIgnoreCase) != 0))
            {
                AddParameter(vfxViewController.GetParameterController(parameter), true);
            }

            OpenTextEditor<VFXBlackboardField>(parameter.exposedName);
        }

        private void OnAddCustomAttribute(object parameter)
        {
            var newName = m_Controller.graph.attributesManager.FindUniqueName("CustomAttribute");
            if (m_Controller.graph.TryAddCustomAttribute(newName, (VFXValueType)parameter, string.Empty, false, out var newCustomAttribute))
            {
                var parentTreeviewItem = m_ParametersController.SelectMany(GetDataRecursive).Single(x => string.Compare(x.data.title, CustomAttributeCategory.Title, StringComparison.OrdinalIgnoreCase) == 0);
                var newId = m_Treeview.viewController.GetAllItemIds().Max() + 1;
                var newData = new AttributeItem(newCustomAttribute.name, CustomAttributeUtility.GetSignature(newCustomAttribute.type), newId, string.Empty, false, true, null) { isLast = true };
                m_Treeview.AddItem(new TreeViewItemData<IParameterItem>(newId, newData), parentTreeviewItem.id);
                UpdateLastCategoryItem(parentTreeviewItem.id);
            }

            isAttributesCategoryExpanded = true;
            ExpandItem(AttributesCategoryTitle);
            isCustomAttributesCategoryExpanded = true;
            ExpandItem(CustomAttributeCategory.Title);
            OpenTextEditor<VFXBlackboardAttributeField>(newCustomAttribute.name);
        }

        private static IEnumerable<VFXModelDescriptorParameters> GetSortedParameters()
        {
            foreach (var desc in VFXLibrary.GetParameters().OrderBy(o => o.name))
            {
                var type = desc.model.type;
                var attribute = VFXLibrary.GetAttributeFromSlotType(type);
                if (attribute != null && attribute.usages.HasFlag(VFXTypeAttribute.Usage.ExcludeFromProperty))
                    continue;

                yield return desc;
            }
        }

        void OnAddItemButton(Blackboard bb)
        {
            if (!m_CanEdit)
            {
                return;
            }

            var menu = new GenericMenu();
            var showProperties = m_ViewMode.HasFlag(ViewMode.Properties);
            var showAttributes = m_ViewMode.HasFlag(ViewMode.Attributes);

            // Properties
            if (showProperties)
            {
                var prexif = showAttributes ? "Property/" : string.Empty;
                menu.AddItem(EditorGUIUtility.TrTextContent($"{prexif}Category"), false, OnAddCategory);
                menu.AddSeparator(prexif);
                var parameters = GetSortedParameters().ToArray();
                foreach (var parameter in parameters)
                {
                    menu.AddItem(EditorGUIUtility.TextContent($"{prexif}{parameter.name}"), false, OnAddParameter, parameter);
                }
            }

            // Attributes
            if (showAttributes)
            {
                var prefix = showProperties ? "Attribute/" : string.Empty;
                foreach (var type in Enum.GetValues(typeof(CustomAttributeUtility.Signature)).Cast<CustomAttributeUtility.Signature>())
                {
                    menu.AddItem(EditorGUIUtility.TextContent($"{prefix}{type.ToString()}"), false, OnAddCustomAttribute, CustomAttributeUtility.GetValueType(type));
                }
            }

            menu.ShowAsContext();
        }

        public void SetCategoryName(VFXBlackboardCategory cat, string newTitle)
        {
            if (TryGetValidCategoryName(ref newTitle))
            {
                if (controller.RenameCategory(cat.title, newTitle))
                {
                    cat.title = newTitle;
                    cat.category.title = newTitle;
                    return;
                }
            }

            cat.title = cat.category.title;
        }

        private bool TryGetValidCategoryName(ref string candidateName)
        {
            candidateName = candidateName.Trim();
            candidateName = FilterOutReservedCategoryName(candidateName);

            return candidateName.Length > 0;
        }

        void OnAddCategory()
        {
            AddCategory("new category");
        }

        public VFXBlackboardRow GetRowFromController(VFXParameterController parameterController)
        {
            return m_Treeview.Query<VFXBlackboardRow>().Where(x => x.controller == parameterController).First();
        }

        public VisualElement GetAttributeRowFromName(string attributeName)
        {
            return m_Treeview.Query<VFXBlackboardAttributeRow>().Where(x => x.attribute.title == attributeName).First();
        }

        public IEnumerable<VFXBlackboardAttributeRow> GetAttributeRowsFromNames(string[] attributeNames)
        {
            return m_Treeview.Query<VFXBlackboardAttributeRow>().Where(x => attributeNames.Any(y => VFXAttributeHelper.IsMatching(y, x.attribute.title, true))).ToList();
        }

        public void ClearAllAttributesHighlights()
        {
            m_Treeview.Query<VFXBlackboardAttributeRow>().ForEach(r => r.RemoveFromClassList("hovered"));
        }

        public void OnControllerChanged(ref ControllerChangedEvent e)
        {
            switch (e.change)
            {
                case VFXViewController.Change.destroy:
                    title = null;
                    break;
                case VFXViewController.Change.assetName when e.controller == controller:
                    title = controller.name;
                    break;
            }
        }

        public void UpdateSelection()
        {
            if (m_IsChangingSelection)
                return;

            try
            {
                m_IsChangingSelection = true;
                m_Treeview.ClearSelection();
                foreach (var selectedField in m_View.selection.OfType<VFXBlackboardFieldBase>().ToArray())
                {
                    m_Treeview.AddToSelection(selectedField.item.index);
                }
            }
            finally
            {
                m_IsChangingSelection = false;
            }
        }

        public override void ClearSelection()
        {
            // Do nothing!!
        }

        public void EmptySelection()
        {
            m_Treeview.ClearSelection();
            m_pendingSelectionItems.Clear();
        }

        public void Update(bool force = false)
        {
            if (controller == null || controller.graph == null || m_View.controller == null || (!force && m_ParametersController.Count > 0))
                return;

            Profiler.BeginSample("VFXBlackboard.Update");
            try
            {
                var groupId = 0;
                m_ParametersController.Clear();

                /////////////
                // Properties
                if (m_ViewMode.HasFlag(ViewMode.Properties))
                {
                    var groupedParameterByCategory = new Dictionary<string, List<VFXParameterController>>();
                    var categoryInfos = controller.graph.UIInfos.categories;
                    for (var i = 0; i < categoryInfos.Count; i++)
                    {
                        groupedParameterByCategory[categoryInfos[i].name] = new List<VFXParameterController>();
                    }

                    // Parameters with no category
                    groupedParameterByCategory[string.Empty] = new List<VFXParameterController>();

                    // Add an empty output category for subgraph operators if there's no output property yet
                    if (controller.model.subgraph is VisualEffectSubgraphOperator)
                    {
                        groupedParameterByCategory[OutputCategory.Label] = new List<VFXParameterController>();
                    }

                    var parameterControllers = controller.parameterControllers.ToArray();
                    for (var i = 0; i < parameterControllers.Length; i++)
                    {
                        var parameterController = parameterControllers[i];
                        var category = parameterController.isOutput
                            ? OutputCategory.Label
                            : parameterController.model.category ?? string.Empty;
                        groupedParameterByCategory[category].Add(parameterController);
                    }

                    // Add all parameters (with or without category and also output parameters)
                    var categoryItems = new List<TreeViewItemData<IParameterItem>>();
                    foreach (var pair in groupedParameterByCategory.OrderBy(x => SortCategory(x.Key, x.Value)))
                    {
                        categoryItems.AddRange(CreateParameterItem(ref groupId, pair.Key, pair.Value));
                    }

                    m_ParametersController.Add(new TreeViewItemData<IParameterItem>(groupId, new PropertyCategory(PropertiesCategoryTitle, groupId++, true, isPropertiesCategoryExpanded), categoryItems));
                }

                /////////////
                // Attributes
                if (m_ViewMode.HasFlag(ViewMode.Attributes))
                {
                    var builtInAttributeCategories = VFXAttributesManager
                        .GetBuiltInAttributesOrCombination(true, false, true, true)
                        .Except(new []{ VFXAttribute.EventCount })
                        .GroupBy(x => x.category)
                        .OrderBy(x => x.Key).ToArray();
                    var builtInAttributeCategoryItems = new List<TreeViewItemData<IParameterItem>>(builtInAttributeCategories.Length);
                    for (var i = 0; i < builtInAttributeCategories.Length; i++)
                    {
                        var categoryItems = new List<TreeViewItemData<IParameterItem>>(builtInAttributeCategories[i].Count());
                        foreach (var attribute in builtInAttributeCategories[i].OrderBy(x => x.name))
                        {
                            categoryItems.Add(new TreeViewItemData<IParameterItem>(groupId, new AttributeItem(attribute.name, CustomAttributeUtility.GetSignature(attribute.type), groupId++, attribute.description, false, false, null)));
                        }
                        var category = builtInAttributeCategories[i].Key;
                        builtInAttributeCategoryItems.Add(new TreeViewItemData<IParameterItem>(groupId, new AttributeSeparator(category.Substring(2, category.Length - 2), groupId++, false, true), categoryItems));
                    }

                    var builtInAttributesRoot = new TreeViewItemData<IParameterItem>(groupId, new AttributeCategory(BuiltInAttributesCategoryTitle, groupId++, false, isBuiltInAttributesCategoryExpanded), builtInAttributeCategoryItems);

                    var customAttributes = m_View.controller.graph.customAttributes.ToArray();
                    var customAttributesItems = new List<TreeViewItemData<IParameterItem>>(customAttributes.Length);
                    for (var i = 0; i < customAttributes.Length; i++)
                    {
                        var customAttribute = customAttributes[i];
                        customAttributesItems.Add(new TreeViewItemData<IParameterItem>(groupId, new AttributeItem(customAttribute.attributeName, customAttribute.type, groupId++, customAttribute.description, customAttribute.isExpanded, !customAttribute.isReadOnly, customAttribute.usedInSubgraphs)));
                    }

                    var customAttributesRoot = new TreeViewItemData<IParameterItem>(groupId, new CustomAttributeCategory(groupId++, isCustomAttributesCategoryExpanded), customAttributesItems);
                    if (customAttributesRoot.hasChildren)
                    {
                        customAttributesRoot.children.Last().data.isLast = true;
                    }

                    var allAttributes = new List<TreeViewItemData<IParameterItem>> { customAttributesRoot, builtInAttributesRoot };
                    m_ParametersController.Add(new TreeViewItemData<IParameterItem>(groupId, new AttributeCategory(AttributesCategoryTitle, groupId, true, isAttributesCategoryExpanded), allAttributes));
                }

                m_Treeview.SetRootItems(m_ParametersController);
                UpdateSelection();
                m_Treeview.RefreshItems();
                UpdateSubtitle();
                SynchronizeExpandState();
                if (m_pendingSelectionItems.Count > 0)
                {
                    var lastItemToSelect = m_ParametersController.SelectMany(GetDataRecursive).LastOrDefault(x => m_pendingSelectionItems.Contains(x.data.title));
                    if (lastItemToSelect.data != null)
                    {
                        m_Treeview.ScrollToItemById(lastItemToSelect.id);
                    }
                    else
                    {
                        m_pendingSelectionItems.Clear();
                    }
                }
            }
            finally
            {
                Profiler.EndSample();
            }
        }

        private int SortCategory(string category, List<VFXParameterController> parameters)
        {
            switch (category)
            {
                case "":
                    return 0;
                case OutputCategory.Label:
                    return 1;
                default:
                    return m_View.controller.GetCategoryIndex(category) + 2;
            }
        }

        private void SynchronizeExpandState()
        {
            bool needRefresh = false;
            foreach(var id in m_Treeview.viewController.GetAllItemIds())
            {
                var item = m_Treeview.GetItemDataForId<IParameterItem>(id);
                if (item is not IParameterCategory)
                {
                    continue;
                }

                var isCurrentlyExpanded = m_Treeview.IsExpanded(id);
                var hasChildren = m_Treeview.viewController.HasChildren(id);
                if (item.isExpanded && !isCurrentlyExpanded && hasChildren)
                {
                    needRefresh = true;
                    m_Treeview.viewController.ExpandItem(id, false, false);
                }
                else if (!item.isExpanded && isCurrentlyExpanded)
                {
                    needRefresh = false;
                    m_Treeview.viewController.CollapseItem(id, false);
                }

                if (item.isExpanded && hasChildren && m_Treeview.GetRootElementForId(id) is { } root)
                {
                    root.RemoveFromClassList("collapsed");
                }
            }

            if (needRefresh)
            {
                m_Treeview.RefreshItems();
            }
        }

        private void OpenTextEditor<T>(string itemName) where T : VisualElement, IBlackBoardElementWithTitle
        {
            var item = m_ParametersController
                .SelectMany(GetDataRecursive)
                .Select(x => x.data)
                .Single(x => string.Compare(x.title, itemName, StringComparison.OrdinalIgnoreCase) == 0);
            m_Treeview.ScrollToItemById(item.id);

            var field = m_Treeview.Query<T>().Where(x => x.text == itemName).First();
            if (field != null)
            {
                field.OpenTextEditor();
                m_Treeview.ScrollTo(field);
                m_Treeview.selectedIndex = item.index;
            }
        }

        private IEnumerable<TreeViewItemData<IParameterItem>> CreateParameterItem(ref int groupId, string category, List<VFXParameterController> parameterControllers)
        {
            var isOutput = string.Compare(category, OutputCategory.Label, StringComparison.OrdinalIgnoreCase) == 0;
            var hasCategory = !string.IsNullOrEmpty(category);
            var id = hasCategory ? groupId + 1 : groupId;
            var items = parameterControllers
                .OrderBy(x => x.model.order)
                .Select(x => new TreeViewItemData<IParameterItem>(id, new PropertyItem(x, id++)))
                .ToList();
            if (items.LastOrDefault() is { data: not null } last)
            {
                last.data.isLast = true;
            }
            if (hasCategory)
            {
                m_View.controller.GetCategoryExpanded(category, out var isExpanded);
                var data = !isOutput
                    ? new PropertyCategory(category, id, false, isExpanded)
                    : new OutputCategory(isOutputCategoryExpanded, id);
                var categoryItem = new[] { new TreeViewItemData<IParameterItem>(groupId, data, items) };
                groupId = id;
                return categoryItem;
            }

            groupId = id;
            return items;
        }

        public override void UpdatePresenterPosition()
        {
            BoardPreferenceHelper.SavePosition(BoardPreferenceHelper.Board.blackboard, GetPosition());
        }

        public void OnMoved()
        {
            BoardPreferenceHelper.SavePosition(BoardPreferenceHelper.Board.blackboard, GetPosition());
        }

        public void UpdateCustomAttribute(string oldName, string newName)
        {
            var attributeRow = (VFXBlackboardAttributeRow)GetAttributeRowFromName(oldName);
            if (attributeRow != null && controller.graph.attributesManager.TryFind(newName, out var attribute))
            {
                attributeRow.Update(attribute.name, attribute.type, attribute.description);
            }
        }

        private void ExpandItem(string itemName)
        {
            var treeviewItem = m_ParametersController.SelectMany(GetDataRecursive).Single(x => string.Compare(x.data.title, itemName) == 0);
            m_Treeview.viewController.ExpandItem(treeviewItem.id, false, true);
        }

        private string FilterOutReservedCategoryName(string category)
        {
            switch (category)
            {
                case PropertiesCategoryTitle:
                case BuiltInAttributesCategoryTitle:
                case OutputCategory.Label:
                case CustomAttributeCategory.Title:
                    return string.Empty;
            }

            return category;
        }
    }
}
