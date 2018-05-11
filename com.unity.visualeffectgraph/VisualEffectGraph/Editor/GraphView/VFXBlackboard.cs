using System;
using UnityEditor.Experimental.UIElements;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEngine.Experimental.UIElements;

using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEditor.VFX;
using System.Collections.Generic;
using UnityEditor;
using System.Linq;
using System.Text;
using UnityEditor.Graphs;
using UnityEditor.SceneManagement;

namespace  UnityEditor.VFX.UI
{
    class VFXBlackboardPropertyView : VisualElement, IControlledElement, IControlledElement<VFXParameterController>
    {
        public VFXBlackboardPropertyView()
        {
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
        }

        public VFXBlackboardRow owner
        {
            get; set;
        }

        Controller IControlledElement.controller
        {
            get { return owner.controller; }
        }
        public VFXParameterController controller
        {
            get { return owner.controller; }
        }

        PropertyRM m_Property;
        PropertyRM m_MinProperty;
        PropertyRM m_MaxProperty;
        List<PropertyRM> m_SubProperties;

        IEnumerable<PropertyRM> allProperties
        {
            get
            {
                var result = Enumerable.Empty<PropertyRM>();

                if (m_ExposedProperty != null)
                    result = result.Concat(Enumerable.Repeat<PropertyRM>(m_ExposedProperty, 1));
                if (m_Property != null)
                    result = result.Concat(Enumerable.Repeat(m_Property, 1));
                if (m_SubProperties != null)
                    result = result.Concat(m_SubProperties);
                if (m_RangeProperty != null)
                    result = result.Concat(Enumerable.Repeat<PropertyRM>(m_RangeProperty, 1));
                if (m_MinProperty != null)
                    result = result.Concat(Enumerable.Repeat(m_MinProperty, 1));
                if (m_MaxProperty != null)
                    result = result.Concat(Enumerable.Repeat(m_MaxProperty, 1));

                return result;
            }
        }


        void GetPreferedWidths(ref float labelWidth)
        {
            foreach (var port in allProperties)
            {
                float portLabelWidth = port.GetPreferredLabelWidth() + 5;

                if (labelWidth < portLabelWidth)
                {
                    labelWidth = portLabelWidth;
                }
            }
        }

        void ApplyWidths(float labelWidth)
        {
            foreach (var port in allProperties)
            {
                port.SetLabelWidth(labelWidth);
            }
        }

        void CreateSubProperties(ref int insertIndex, List<int> fieldPath)
        {
            var subControllers = controller.GetSubControllers(fieldPath);

            var subFieldPath = new List<int>();
            int cpt = 0;
            foreach (var subController in subControllers)
            {
                PropertyRM prop = PropertyRM.Create(subController, 85);
                if (prop != null)
                {
                    m_SubProperties.Add(prop);
                    Insert(insertIndex++, prop);
                }
                if (prop == null || !prop.showsEverything)
                {
                    subFieldPath.Clear();
                    subFieldPath.AddRange(fieldPath);
                    subFieldPath.Add(cpt);
                    CreateSubProperties(ref insertIndex, subFieldPath);
                }
                ++cpt;
            }
        }

        BoolPropertyRM m_RangeProperty;
        BoolPropertyRM m_ExposedProperty;

        IPropertyRMProvider m_RangeProvider;

        public new void Clear()
        {
            m_ExposedProperty = null;
            m_RangeProperty = null;
        }

        public void SelfChange(int change)
        {
            if (change == VFXParameterController.ValueChanged)
            {
                foreach (var prop in allProperties)
                {
                    prop.Update();
                }
                return;
            }
            int insertIndex = 0;

            if (m_ExposedProperty == null)
            {
                m_ExposedProperty = new BoolPropertyRM(new SimplePropertyRMProvider<bool>("Exposed", () => controller.exposed, t => controller.exposed = t), 55);
                Insert(insertIndex++, m_ExposedProperty);
            }
            else
            {
                insertIndex++;
            }

            if (m_Property == null || !m_Property.IsCompatible(controller))
            {
                if (m_Property != null)
                {
                    m_Property.RemoveFromHierarchy();
                }
                m_Property = PropertyRM.Create(controller, 55);
                if (m_Property != null)
                {
                    Insert(insertIndex++, m_Property);

                    if (m_SubProperties != null)
                    {
                        foreach (var prop in m_SubProperties)
                        {
                            prop.RemoveFromHierarchy();
                        }
                    }
                    m_SubProperties = new List<PropertyRM>();
                    List<int> fieldpath = new List<int>();
                    if (!m_Property.showsEverything)
                    {
                        CreateSubProperties(ref insertIndex, fieldpath);
                    }
                }
            }
            else
            {
                insertIndex += 1 + m_SubProperties.Count;
            }

            if (controller.canHaveRange)
            {
                if (m_MinProperty == null || !m_MinProperty.IsCompatible(controller.minController))
                {
                    if (m_MinProperty != null)
                        m_MinProperty.RemoveFromHierarchy();
                    m_MinProperty = PropertyRM.Create(controller.minController, 55);
                }
                if (m_MaxProperty == null || !m_MaxProperty.IsCompatible(controller.minController))
                {
                    if (m_MaxProperty != null)
                        m_MaxProperty.RemoveFromHierarchy();
                    m_MaxProperty = PropertyRM.Create(controller.maxController, 55);
                }

                if (m_RangeProperty == null)
                {
                    m_RangeProperty = new BoolPropertyRM(new SimplePropertyRMProvider<bool>("Range", () => controller.hasRange, t => controller.hasRange = t), 55);
                }
                Insert(insertIndex++, m_RangeProperty);

                if (controller.hasRange)
                {
                    if (m_MinProperty.parent == null)
                    {
                        Insert(insertIndex++, m_MinProperty);
                        Insert(insertIndex++, m_MaxProperty);
                    }
                }
                else if (m_MinProperty.parent != null)
                {
                    m_MinProperty.RemoveFromHierarchy();
                    m_MaxProperty.RemoveFromHierarchy();
                }
            }
            else
            {
                if (m_MinProperty != null)
                {
                    m_MinProperty.RemoveFromHierarchy();
                    m_MinProperty = null;
                }
                if (m_MaxProperty != null)
                {
                    m_MaxProperty.RemoveFromHierarchy();
                    m_MaxProperty = null;
                }
                if (m_RangeProperty != null)
                {
                    m_RangeProperty.RemoveFromHierarchy();
                    m_RangeProperty = null;
                }
            }


            foreach (var prop in allProperties)
            {
                prop.Update();
            }
        }

        void OnAttachToPanel(AttachToPanelEvent e)
        {
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        void OnGeometryChanged(GeometryChangedEvent e)
        {
            if (panel != null)
            {
                float labelWidth = 70;
                GetPreferedWidths(ref labelWidth);
                ApplyWidths(labelWidth);
            }
            UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }
    }
    class VFXBlackboardField : BlackboardField, IControlledElement, IControlledElement<VFXParameterController>
    {
        public VFXBlackboardRow owner
        {
            get; set;
        }

        public VFXBlackboardField() : base()
        {
            RegisterCallback<MouseEnterEvent>(OnMouseHover);
            RegisterCallback<MouseLeaveEvent>(OnMouseHover);
        }

        Controller IControlledElement.controller
        {
            get { return owner.controller; }
        }
        public VFXParameterController controller
        {
            get { return owner.controller; }
        }

        public void SelfChange()
        {
            if (controller.exposed)
            {
                icon = Resources.Load<Texture2D>("VFX/exposed dot");
            }
            else
            {
                icon = null;
            }
        }

        void OnMouseHover(EventBase evt)
        {
            VFXView view = GetFirstAncestorOfType<VFXView>();
            if (view != null)
            {
                foreach (var parameter in view.graphElements.ToList().OfType<VFXParameterUI>().Where(t => t.controller.parentController == controller))
                {
                    if (evt.GetEventTypeId() == MouseEnterEvent.TypeId())
                        parameter.pseudoStates |= PseudoStates.Hover;
                    else
                        parameter.pseudoStates &= ~PseudoStates.Hover;
                }
            }
        }
    }

    class VFXBlackboardRow : BlackboardRow, IControlledElement<VFXParameterController>
    {
        VFXBlackboardField m_Field;

        VFXBlackboardPropertyView m_Properties;
        public VFXBlackboardRow() : this(new VFXBlackboardField() { name = "vfx-field" }, new VFXBlackboardPropertyView() { name = "vfx-properties" })
        {
            Button button = this.Q<Button>("expandButton");

            if (button != null)
            {
                button.clickable.clicked += OnExpand;
            }

            clippingOptions = ClippingOptions.ClipAndCacheContents;
        }

        void OnExpand()
        {
            controller.expanded = expanded;
        }

        public VFXBlackboardField field
        {
            get
            {
                return m_Field;
            }
        }

        private VFXBlackboardRow(VFXBlackboardField field, VFXBlackboardPropertyView property) : base(field, property)
        {
            m_Field = field;
            m_Properties = property;

            m_Field.owner = this;
            m_Properties.owner = this;

            RegisterCallback<ControllerChangedEvent>(OnControllerChanged);
        }

        public int m_CurrentOrder;
        public bool m_CurrentExposed;

        void OnControllerChanged(ControllerChangedEvent e)
        {
            m_Field.text = controller.exposedName;
            m_Field.typeText = controller.portType != null ? controller.portType.UserFriendlyName() : "null";

            // if the order or exposed change, let the event be caught by the VFXBlackboard
            if (controller.order == m_CurrentOrder && controller.exposed == m_CurrentExposed)
            {
                e.StopPropagation();
            }
            m_CurrentOrder = controller.order;
            m_CurrentExposed = controller.exposed;

            expanded = controller.expanded;

            m_Properties.SelfChange(e.change);

            m_Field.SelfChange();
        }

        VFXParameterController m_Controller;
        Controller IControlledElement.controller
        {
            get { return m_Controller; }
        }
        public VFXParameterController controller
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
                    m_Controller = value;
                    m_Properties.Clear();

                    if (m_Controller != null)
                    {
                        m_CurrentOrder = m_Controller.order;
                        m_CurrentExposed = m_Controller.exposed;
                        m_Controller.RegisterHandler(this);
                    }
                }
            }
        }
    }
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
                }
            }
        }

        new void Clear()
        {
            m_DefaultCategory.Clear();

            foreach (var cat in m_Categories)
            {
                cat.Value.RemoveFromHierarchy();
            }
            m_Categories.Clear();
        }

        VFXView m_View;

        public VFXBlackboard(VFXView view)
        {
            m_View = view;
            RegisterCallback<ControllerChangedEvent>(OnControllerChanged);
            editTextRequested = OnEditName;
            addItemRequested = OnAddItem;

            this.scrollable = true;

            SetPosition(BoardPreferenceHelper.LoadPosition(BoardPreferenceHelper.Board.blackboard, defaultRect));

            m_DefaultCategory = new VFXBlackboardCategory() { title = "parameters"};
            Add(m_DefaultCategory);
            m_DefaultCategory.headerVisible = false;

            AddStyleSheetPath("VFXBlackboard");

            RegisterCallback<MouseDownEvent>(OnMouseClick, Capture.Capture);


            RegisterCallback<DragUpdatedEvent>(OnDragUpdatedEvent);
            RegisterCallback<DragPerformEvent>(OnDragPerformEvent);
            RegisterCallback<DragLeaveEvent>(OnDragLeaveEvent);


            m_DragIndicator = new VisualElement();

            m_DragIndicator.name = "dragIndicator";
            m_DragIndicator.style.positionType = PositionType.Absolute;
            shadow.Add(m_DragIndicator);

            clippingOptions = ClippingOptions.ClipContents;
            SetDragIndicatorVisible(false);

        }

        private void SetDragIndicatorVisible(bool visible)
        {
            if (visible && (m_DragIndicator.parent == null))
            {
                shadow.Add(m_DragIndicator);
                m_DragIndicator.visible = true;
            }
            else if ((visible == false) && (m_DragIndicator.parent != null))
            {
                shadow.Remove(m_DragIndicator);
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

                for(int i = defaultCatIndex +1 ; i< childCount; ++i)
                {
                    VFXBlackboardCategory cat = ElementAt(i) as VFXBlackboardCategory;
                    if(cat == null)
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

            if( selection.Any(t=> ! (t is VFXBlackboardCategory)))
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
                    if( childCount > 0)
                    {
                        VisualElement lastChild = this[childCount - 1];

                        indicatorY = lastChild.ChangeCoordinatesTo(this, new Vector2(0, lastChild.layout.height + lastChild.style.marginBottom)).y;
                    }
                    else
                    {
                        indicatorY = this.contentRect.height;
                    }
                }
                else
                {
                    VisualElement childAtInsertIndex = this[m_InsertIndex];

                    indicatorY = childAtInsertIndex.ChangeCoordinatesTo(this, new Vector2(0, -childAtInsertIndex.style.marginTop)).y;
                }

                SetDragIndicatorVisible(true);

                m_DragIndicator.style.positionTop =  indicatorY - m_DragIndicator.style.height * 0.5f;

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
            if( category == null)
            {
                return;
            }

            if (m_InsertIndex != -1)
            {
                if( m_InsertIndex > IndexOf(category) )
                       --m_InsertIndex;
                controller.MoveCategory(category.title,m_InsertIndex - IndexOf(m_DefaultCategory) - 1);
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
            m_Controller.AddVFXParameter(Vector2.zero, (VFXModelDescriptorParameters)parameter);
        }

        void OnAddItem(Blackboard bb)
        {
            GenericMenu menu = new GenericMenu();


            menu.AddItem(EditorGUIUtility.TrTextContent("category"),false,OnAddCategory);
            menu.AddSeparator(string.Empty);

            foreach (var parameter in VFXLibrary.GetParameters())
            {
                VFXParameter model = parameter.model as VFXParameter;

                var type = model.type;

                menu.AddItem(EditorGUIUtility.TextContent(type.UserFriendlyName()), false, OnAddParameter, parameter);
            }

            menu.ShowAsContext();
        }


        public void SetCategoryName(VFXBlackboardCategory cat,string newName)
        {
            int index = GetCategoryIndex(cat);

            bool succeeded = controller.SetCategoryName(index,newName);

            if( succeeded)
            {
                m_Categories.Remove(cat.title);
                cat.title = newName;
                m_Categories.Add(newName,cat);
            }
        }

        void OnAddCategory()
        {
            string newCategoryName = EditorGUIUtility.TrTextContent("new category").text;
            int cpt = 1;
            while( controller.graph.UIInfos.categories.Contains(newCategoryName))
            {
                newCategoryName = string.Format(EditorGUIUtility.TrTextContent("new category {0}").text,cpt++);
            }

            controller.graph.UIInfos.categories.Add(newCategoryName);
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
            foreach(var row in rows)
            {
                controller.SetParametersOrder(row.controller, index++,category == m_DefaultCategory ? "" : category.title);
            }
        }


        VFXBlackboardCategory m_DefaultCategory;
        Dictionary<string,VFXBlackboardCategory> m_Categories = new Dictionary<string,VFXBlackboardCategory>();



        public VFXBlackboardRow GetRowFromController(VFXParameterController controller)
        {
            VFXBlackboardCategory cat = null;
            VFXBlackboardRow row = null;
            if( m_Categories.TryGetValue(controller.model.category,out cat ))
            {
                row = cat.GetRowFromController(controller);
            }

            return row;
        }


        struct CategoryNOrder
        {
            public string category;
            public int order;
        }

        void OnControllerChanged(ControllerChangedEvent e)
        {
            if (e.controller == controller || e.controller is VFXParameterController) //optim : reorder only is only the order has changed
            {
                var orderedCategories = controller.graph.UIInfos.categories;

                var newCategories = new List<VFXBlackboardCategory>();

                foreach(var catName in controller.graph.UIInfos.categories)
                {
                    VFXBlackboardCategory cat = null;
                    if( ! m_Categories.TryGetValue(catName,out cat))
                    {
                        cat = new VFXBlackboardCategory(){title =  catName};
                        cat.SetSelectable();
                        m_Categories.Add(catName,cat);
                    }

                    newCategories.Add(cat);
                }

                foreach( var category in m_Categories.Keys.Except(orderedCategories).ToArray() )
                {
                    m_Categories[category].RemoveFromHierarchy();
                    m_Categories.Remove(category);
                }

                var prevCat = m_DefaultCategory;

                foreach(var cat in newCategories)
                {
                    if(cat.parent == null)
                        Insert(IndexOf(prevCat)+1,cat);
                    else
                        cat.PlaceInFront(prevCat);
                    prevCat = cat;
                }

                var actualControllers = new HashSet<VFXParameterController>(controller.parameterControllers.Where(t=>string.IsNullOrEmpty(t.model.category)));
                m_DefaultCategory.SyncParameters(actualControllers);


                foreach(var cat in newCategories)
                {
                    actualControllers = new HashSet<VFXParameterController>(controller.parameterControllers.Where(t=>t.model.category == cat.title));
                    cat.SyncParameters(actualControllers);
                }
            }
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
