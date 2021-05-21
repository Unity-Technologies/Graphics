using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.VFX;
using System.Collections.Generic;
using System.Linq;

using PositionType = UnityEngine.UIElements.Position;

namespace  UnityEditor.VFX.UI
{
    class VFXBlackboard : Blackboard, IControlledElement<VFXViewController>, IVFXMovable
    {
        VFXViewController m_Controller;
        Controller IControlledElement.controller
        {
            get { return m_Controller; }
        }
        public VFXViewController controller
        {
            get { return m_Controller; }
            set
            {
                if (m_Controller != value)
                {
                    if (m_Controller != null)
                    {
                        m_Controller.UnregisterHandler(this);
                    }
                    Clear();
                    m_Controller = value;

                    if (m_Controller != null)
                    {
                        m_Controller.RegisterHandler(this);
                    }

                    m_AddButton.SetEnabled(m_Controller != null);
                }
            }
        }

        new void Clear()
        {
            m_DefaultCategory.Clear();
            if (m_OutputCategory != null)
                m_OutputCategory.Clear();

            foreach (var cat in m_Categories)
            {
                cat.Value.RemoveFromHierarchy();
            }
            m_Categories.Clear();
        }

        VFXView m_View;

        Button m_AddButton;
        VisualElement m_LockedElement;

        public VFXBlackboard(VFXView view)
        {
            m_View = view;
            editTextRequested = OnEditName;
            addItemRequested = OnAddItem;

            this.scrollable = true;

            SetPosition(BoardPreferenceHelper.LoadPosition(BoardPreferenceHelper.Board.blackboard, defaultRect));

            m_DefaultCategory = new VFXBlackboardCategory() { title = "parameters" };
            Add(m_DefaultCategory);
            m_DefaultCategory.headerVisible = false;

            styleSheets.Add(VFXView.LoadStyleSheet("VFXBlackboard"));

            RegisterCallback<MouseDownEvent>(OnMouseClick, TrickleDown.TrickleDown);
            RegisterCallback<DragUpdatedEvent>(OnDragUpdatedEvent);
            RegisterCallback<DragPerformEvent>(OnDragPerformEvent);
            RegisterCallback<DragLeaveEvent>(OnDragLeaveEvent);
            RegisterCallback<KeyDownEvent>(OnKeyDown);

            focusable = true;

            m_AddButton = this.Q<Button>(name: "addButton");

            m_DragIndicator = new VisualElement();


            m_DragIndicator.name = "dragIndicator";
            m_DragIndicator.style.position = PositionType.Absolute;
            hierarchy.Add(m_DragIndicator);

            SetDragIndicatorVisible(false);

            Resizer resizer = this.Query<Resizer>();

            hierarchy.Add(new UnityEditor.Experimental.GraphView.ResizableElement());

            style.position = PositionType.Absolute;

            m_PathLabel = hierarchy.ElementAt(0).Q<Label>("subTitleLabel");
            m_PathLabel.RegisterCallback<MouseDownEvent>(OnMouseDownSubTitle);

            m_PathTextField = new TextField { visible = false };
            m_PathTextField.Q(TextField.textInputUssName).RegisterCallback<FocusOutEvent>(e => { OnEditPathTextFinished(); });
            m_PathTextField.Q(TextField.textInputUssName).RegisterCallback<KeyDownEvent>(OnPathTextFieldKeyPressed);
            hierarchy.Add(m_PathTextField);

            resizer.RemoveFromHierarchy();

            if (s_LayoutManual != null)
                s_LayoutManual.SetValue(this, false);

            m_LockedElement = new Label("Asset is locked");
            m_LockedElement.style.color = Color.white * 0.75f;
            m_LockedElement.style.position = PositionType.Absolute;
            m_LockedElement.style.left = 0f;
            m_LockedElement.style.right = new StyleLength(0f);
            m_LockedElement.style.top = new StyleLength(0f);
            m_LockedElement.style.bottom = new StyleLength(0f);
            m_LockedElement.style.unityTextAlign = TextAnchor.MiddleCenter;
            var fontSize = 54f;
            m_LockedElement.style.fontSize = new StyleLength(fontSize);
            m_LockedElement.style.paddingBottom = fontSize / 2f;
            m_LockedElement.style.paddingTop = fontSize / 2f;
            m_LockedElement.style.display = DisplayStyle.None;
            m_LockedElement.focusable = true;
            m_LockedElement.RegisterCallback<KeyDownEvent>(e => e.StopPropagation());
            Add(m_LockedElement);

            m_AddButton.SetEnabled(false);

            this.AddManipulator(new ContextualMenuManipulator(BuildContextualMenu));
        }

        public void LockUI()
        {
            m_LockedElement.style.display = DisplayStyle.Flex;
            m_AddButton.SetEnabled(false);
        }

        public void UnlockUI()
        {
            m_LockedElement.style.display = DisplayStyle.None;
            m_AddButton.SetEnabled(m_Controller != null);
        }

        DropdownMenuAction.Status GetContextualMenuStatus()
        {
            //Use m_AddButton state which relies on locked & controller status
            if (m_AddButton.enabledSelf)
                return DropdownMenuAction.Status.Normal;
            return DropdownMenuAction.Status.Disabled;
        }

        void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("Select All", (a) => SelectAll(), (a) => GetContextualMenuStatus());
            evt.menu.AppendAction("Select Unused", (a) => SelectUnused(), (a) => GetContextualMenuStatus());
        }


        void SelectAll()
        {
            m_View.ClearSelection();
            this.Query<BlackboardField>().ForEach(t => m_View.AddToSelection(t));
        }

        void SelectUnused()
        {
            m_View.ClearSelection();

            var unused = unusedParameters.ToList();
            this.Query<BlackboardField>().Where(t=> unused.Contains(t.GetFirstAncestorOfType<VFXBlackboardRow>().controller.model) ).ForEach(t => m_View.AddToSelection(t));
        }

        IEnumerable<VFXParameter> unusedParameters
        {
            get
            {
                return controller.graph.children.OfType<VFXParameter>().Where(t => !(t.isOutput ? t.inputSlots : t.outputSlots).Any(s => s.HasLink(true)));
            }
        }

        Label m_PathLabel;
        TextField m_PathTextField;

        void OnMouseDownSubTitle(MouseDownEvent evt)
        {
            if (evt.clickCount == 2 && evt.button == (int)MouseButton.LeftMouse)
            {
                StartEditingPath();
                evt.PreventDefault();
            }
        }

        void StartEditingPath()
        {
            m_PathTextField.visible = true;

            m_PathTextField.value = m_PathLabel.text;
            m_PathTextField.style.position = PositionType.Absolute;
            var rect = m_PathLabel.ChangeCoordinatesTo(this, new Rect(Vector2.zero, m_PathLabel.layout.size));
            m_PathTextField.style.left = rect.xMin;
            m_PathTextField.style.top = rect.yMin;
            m_PathTextField.style.width = rect.width;
            m_PathTextField.style.fontSize = 11;
            m_PathTextField.style.marginLeft = 0;
            m_PathTextField.style.marginRight = 0;
            m_PathTextField.style.marginTop = 0;
            m_PathTextField.style.marginBottom = 0;

            m_PathLabel.visible = false;

            m_PathTextField.Q("unity-text-input").Focus();
            m_PathTextField.SelectAll();
        }

        void OnPathTextFieldKeyPressed(KeyDownEvent evt)
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
                default:
                    break;
            }
        }

        void OnEditPathTextFinished()
        {
            m_PathLabel.visible = true;
            m_PathTextField.visible = false;

            var newPath = m_PathTextField.text;

            controller.graph.categoryPath = newPath;
            m_PathLabel.text = newPath;
        }

        static System.Reflection.PropertyInfo s_LayoutManual = typeof(VisualElement).GetProperty("isLayoutManual", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        void OnKeyDown(KeyDownEvent e)
        {
            if (e.keyCode == KeyCode.F2)
            {
                var graphView = GetFirstAncestorOfType<VFXView>();

                var field = graphView.selection.OfType<VFXBlackboardField>().FirstOrDefault();
                if (field != null)
                {
                    field.OpenTextEditor();
                }
                else
                {
                    var category = graphView.selection.OfType<VFXBlackboardCategory>().FirstOrDefault();

                    if (category != null)
                    {
                        category.OpenTextEditor();
                    }
                }
            }
        }

        private void SetDragIndicatorVisible(bool visible)
        {
            if (visible && (m_DragIndicator.parent == null))
            {
                hierarchy.Add(m_DragIndicator);
                m_DragIndicator.visible = true;
            }
            else if ((visible == false) && (m_DragIndicator.parent != null))
            {
                hierarchy.Remove(m_DragIndicator);
            }
        }

        VisualElement m_DragIndicator;


        int InsertionIndex(Vector2 pos)
        {
            VisualElement owner = contentContainer != null ? contentContainer : this;
            Vector2 localPos = this.ChangeCoordinatesTo(owner, pos);

            if (owner.ContainsPoint(localPos))
            {
                int defaultCatIndex = IndexOf(m_DefaultCategory);

                for (int i = defaultCatIndex + 1; i < childCount; ++i)
                {
                    VFXBlackboardCategory cat = ElementAt(i) as VFXBlackboardCategory;
                    if (cat == null)
                    {
                        return i;
                    }

                    Rect rect = cat.layout;

                    if (localPos.y <= (rect.y + rect.height / 2))
                    {
                        return i;
                    }
                }
                return childCount;
            }
            return -1;
        }

        int m_InsertIndex;

        void OnDragUpdatedEvent(DragUpdatedEvent e)
        {
            var selection = DragAndDrop.GetGenericData("DragSelection") as List<ISelectable>;

            if (selection == null)
            {
                SetDragIndicatorVisible(false);
                return;
            }

            if (selection.Any(t => !(t is VFXBlackboardCategory)))
            {
                SetDragIndicatorVisible(false);
                return;
            }

            Vector2 localPosition = e.localMousePosition;

            m_InsertIndex = InsertionIndex(localPosition);

            if (m_InsertIndex != -1)
            {
                float indicatorY = 0;

                if (m_InsertIndex == childCount)
                {
                    if (childCount > 0)
                    {
                        VisualElement lastChild = this[childCount - 1];

                        indicatorY = lastChild.ChangeCoordinatesTo(this, new Vector2(0, lastChild.layout.height + lastChild.resolvedStyle.marginBottom)).y;
                    }
                    else
                    {
                        indicatorY = this.contentRect.height;
                    }
                }
                else
                {
                    VisualElement childAtInsertIndex = this[m_InsertIndex];

                    indicatorY = childAtInsertIndex.ChangeCoordinatesTo(this, new Vector2(0, -childAtInsertIndex.resolvedStyle.marginTop)).y;
                }

                SetDragIndicatorVisible(true);

                m_DragIndicator.style.top =  indicatorY - m_DragIndicator.resolvedStyle.height * 0.5f;

                DragAndDrop.visualMode = DragAndDropVisualMode.Move;
            }
            else
            {
                SetDragIndicatorVisible(false);
            }
            e.StopPropagation();
        }

        public int GetCategoryIndex(VFXBlackboardCategory cat)
        {
            return IndexOf(cat) - IndexOf(m_DefaultCategory) - 1;
        }

        void OnDragPerformEvent(DragPerformEvent e)
        {
            SetDragIndicatorVisible(false);
            var selection = DragAndDrop.GetGenericData("DragSelection") as List<ISelectable>;
            if (selection == null)
            {
                return;
            }

            var category = selection.OfType<VFXBlackboardCategory>().FirstOrDefault();
            if (category == null)
            {
                return;
            }

            if (m_InsertIndex != -1)
            {
                if (m_InsertIndex > IndexOf(category))
                    --m_InsertIndex;
                controller.MoveCategory(category.title, m_InsertIndex - IndexOf(m_DefaultCategory) - 1);
            }

            SetDragIndicatorVisible(false);
            e.StopPropagation();
        }

        void OnDragLeaveEvent(DragLeaveEvent e)
        {
            SetDragIndicatorVisible(false);
        }

        public void ValidatePosition()
        {
            BoardPreferenceHelper.ValidatePosition(this, m_View, defaultRect);
        }

        static readonly Rect defaultRect = new Rect(100, 100, 300, 500);

        void OnMouseClick(MouseDownEvent e)
        {
            m_View.SetBoardToFront(this);
        }

        void OnAddParameter(object parameter)
        {
            var selectedCategory = m_View.selection.OfType<VFXBlackboardCategory>().FirstOrDefault();
            VFXParameter newParam = m_Controller.AddVFXParameter(Vector2.zero, (VFXModelDescriptorParameters)parameter);
            if (selectedCategory != null && newParam != null)
                newParam.category = selectedCategory.title;

            newParam.SetSettingValue("m_Exposed", true);
        }

        void OnAddOutputParameter(object parameter)
        {
            var selectedCategory = m_View.selection.OfType<VFXBlackboardCategory>().FirstOrDefault();
            VFXParameter newParam = m_Controller.AddVFXParameter(Vector2.zero, (VFXModelDescriptorParameters)parameter);
            newParam.isOutput = true;
        }

        private static IEnumerable<VFXModelDescriptor> GetSortedParameters()
        {
            return VFXLibrary.GetParameters().OrderBy(o => o.name);
        }

        void OnAddItem(Blackboard bb)
        {
            GenericMenu menu = new GenericMenu();

            if (!(controller.model.subgraph is VisualEffectSubgraphOperator))
            {
                menu.AddItem(EditorGUIUtility.TrTextContent("Category"), false, OnAddCategory);
                menu.AddSeparator(string.Empty);
            }

            foreach (var parameter in GetSortedParameters())
            {
                VFXParameter model = parameter.model as VFXParameter;

                var type = model.type;
                if (type == typeof(GPUEvent))
                    continue;

                menu.AddItem(EditorGUIUtility.TextContent(type.UserFriendlyName()), false, OnAddParameter, parameter);
            }

            menu.ShowAsContext();
        }

        public void SetCategoryName(VFXBlackboardCategory cat, string newName)
        {
            int index = GetCategoryIndex(cat);

            bool succeeded = controller.SetCategoryName(index, newName);

            if (succeeded)
            {
                m_Categories.Remove(cat.title);
                cat.title = newName;
                m_Categories.Add(newName, cat);
            }
        }

        void OnAddCategory()
        {
            string newCategoryName = EditorGUIUtility.TrTextContent("new category").text;
            int cpt = 1;

            if(controller.graph.UIInfos.categories != null)
            {
                while (controller.graph.UIInfos.categories.Any(t => t.name == newCategoryName))
                {
                    newCategoryName = string.Format(EditorGUIUtility.TrTextContent("new category {0}").text, cpt++);
                }
            }
            else
            {
                controller.graph.UIInfos.categories = new List<VFXUI.CategoryInfo>();
            }

            controller.graph.UIInfos.categories.Add(new VFXUI.CategoryInfo() { name = newCategoryName });
            controller.graph.Invalidate(VFXModel.InvalidationCause.kUIChanged);
        }

        void OnEditName(Blackboard bb, VisualElement element, string value)
        {
            if (element is VFXBlackboardField)
            {
                (element as VFXBlackboardField).controller.exposedName = value;
            }
        }

        public void OnMoveParameter(IEnumerable<VFXBlackboardRow> rows, VFXBlackboardCategory category, int index)
        {
            //TODO sort elements
            foreach (var row in rows)
            {
                if (category == m_DefaultCategory || category == m_OutputCategory)
                    controller.SetParametersOrder(row.controller, index++, category == m_DefaultCategory);
                else
                    controller.SetParametersOrder(row.controller, index++, category == m_DefaultCategory ? "" : category.title);
            }
        }

        public void SetCategoryExpanded(VFXBlackboardCategory category, bool expanded)
        {
            if (category == m_OutputCategory)
            {
                m_OutputCategory.expanded = !m_OutputCategory.expanded;

                PlayerPrefs.SetInt("VFX.blackboard.outputexpanded", m_OutputCategory.expanded ? 1 : 0);
            }
            else
                controller.SetCategoryExpanded(category.title, expanded);
        }

        VFXBlackboardCategory m_DefaultCategory;
        VFXBlackboardCategory m_OutputCategory;
        Dictionary<string, VFXBlackboardCategory> m_Categories = new Dictionary<string, VFXBlackboardCategory>();


        public VFXBlackboardRow GetRowFromController(VFXParameterController controller)
        {
            VFXBlackboardCategory cat = null;
            VFXBlackboardRow row = null;
            if (string.IsNullOrEmpty(controller.model.category))
            {
                row = m_DefaultCategory.GetRowFromController(controller);
            }
            else if (m_Categories.TryGetValue(controller.model.category, out cat))
            {
                row = cat.GetRowFromController(controller);
            }

            return row;
        }

        void OnAddOutputParameterMenu()
        {
            GenericMenu menu = new GenericMenu();

            foreach (var parameter in GetSortedParameters())
            {
                VFXParameter model = parameter.model as VFXParameter;

                var type = model.type;
                if (type == typeof(GPUEvent))
                    continue;

                menu.AddItem(EditorGUIUtility.TextContent(type.UserFriendlyName()), false, OnAddOutputParameter, parameter);
            }

            menu.ShowAsContext();
        }

        Dictionary<string, bool> m_ExpandedStatus = new Dictionary<string, bool>();
        void IControlledElement.OnControllerChanged(ref ControllerChangedEvent e)
        {
            if (e.controller != controller && !(e.controller is VFXParameterController)) //optim : reorder only is only the order has changed
                return;

            if (e.controller == controller && e.change == VFXViewController.Change.assetName)
            {
                title = controller.name;
                return;
            }

            if (controller.model.subgraph is VisualEffectSubgraphOperator && m_OutputCategory == null)
            {
                m_OutputCategory = new VFXBlackboardCategory() { title = "Output" };
                m_OutputCategory.headerVisible = true;
                m_OutputCategory.expanded = PlayerPrefs.GetInt("VFX.blackboard.outputexpanded", 0) != 0;
                Add(m_OutputCategory);

                var addOutputButton = new Button() { name = "addOutputButton", text = "+" };
                addOutputButton.clicked += OnAddOutputParameterMenu;
                var sectionHeader = m_OutputCategory.Q("sectionHeader");
                var spacer = new VisualElement();
                spacer.style.flexGrow = 1;
                sectionHeader.Add(spacer);
                sectionHeader.Add(addOutputButton);

                m_OutputCategory.AddToClassList("output");
            }
            else if (!(controller.model.subgraph is VisualEffectSubgraphOperator) && m_OutputCategory != null)
            {
                Remove(m_OutputCategory);
                m_OutputCategory = null;
            }

            var actualControllers = new HashSet<VFXParameterController>(controller.parameterControllers.Where(t => !t.isOutput && string.IsNullOrEmpty(t.model.category)));
            m_DefaultCategory.SyncParameters(actualControllers);

            var orderedCategories = controller.graph.UIInfos.categories;
            var newCategories = new List<VFXBlackboardCategory>();

            if (orderedCategories != null)
            {
                foreach (var catModel in controller.graph.UIInfos.categories)
                {
                    VFXBlackboardCategory cat = null;
                    if (!m_Categories.TryGetValue(catModel.name, out cat))
                    {
                        cat = new VFXBlackboardCategory() {title = catModel.name };
                        cat.SetSelectable();
                        m_Categories.Add(catModel.name, cat);
                    }
                    m_ExpandedStatus[catModel.name] = !catModel.collapsed;

                    newCategories.Add(cat);
                }

                foreach (var category in m_Categories.Keys.Except(orderedCategories.Select(t => t.name)).ToArray())
                {
                    m_Categories[category].RemoveFromHierarchy();
                    m_Categories.Remove(category);
                    m_ExpandedStatus.Remove(category);
                }
            }

            var prevCat = m_DefaultCategory;

            foreach (var cat in newCategories)
            {
                if (cat.parent == null)
                    Insert(IndexOf(prevCat) + 1, cat);
                else
                    cat.PlaceInFront(prevCat);
                prevCat = cat;
            }
            if (m_OutputCategory != null)
                m_OutputCategory.PlaceInFront(prevCat);

            foreach (var cat in newCategories)
            {
                actualControllers = new HashSet<VFXParameterController>(controller.parameterControllers.Where(t => t.model.category == cat.title && !t.isOutput));
                cat.SyncParameters(actualControllers);
                cat.expanded = m_ExpandedStatus[cat.title];
            }

            if (m_OutputCategory != null)
            {
                var outputControllers = new HashSet<VFXParameterController>(controller.parameterControllers.Where(t => t.isOutput));
                m_OutputCategory.SyncParameters(outputControllers);
            }

            m_PathLabel.text = controller.graph.categoryPath;
        }

        public override void UpdatePresenterPosition()
        {
            BoardPreferenceHelper.SavePosition(BoardPreferenceHelper.Board.blackboard, GetPosition());
        }

        public void OnMoved()
        {
            BoardPreferenceHelper.SavePosition(BoardPreferenceHelper.Board.blackboard, GetPosition());
        }
    }
}
