using System;
using UnityEditor.Experimental.UIElements;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEngine.Experimental.UIElements;
using UnityEditor.VFX;
using System.Collections.Generic;
using UnityEditor;
using System.Linq;

namespace  UnityEditor.VFX.UI
{
    class VFXBlackboardPropertyView : VisualElement, IControlledElement, IControlledElement<VFXParameterController>
    {
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
                float portLabelWidth = port.GetPreferredLabelWidth();

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
                PropertyRM prop = PropertyRM.Create(subController, 55);
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

        public void Clear()
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

            float labelWidth = 70;
            GetPreferedWidths(ref labelWidth);
            ApplyWidths(labelWidth);

            foreach (var prop in allProperties)
            {
                prop.Update();
            }
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
            m_Field.typeText = controller.portType != null ? controller.portType.UserFriendlyName() : "null" ;

            // if the order or exposed change, let the event be caught by the VFXBlackboard
            if (controller.order == m_CurrentOrder && controller.exposed == m_CurrentExposed)
            {
                e.StopPropagation();
            }
            m_CurrentOrder = controller.order;
            m_CurrentExposed = controller.exposed;

            m_Properties.SelfChange(e.change);

            expanded = controller.expanded;

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
            foreach (var param in m_ExposedParameters.Values)
            {
                param.RemoveFromHierarchy();
            }
        }

        const string blackBoardPositionPref = "VFXBlackboardRect";


        Rect LoadBlackBoardPosition()
        {
            string str = EditorPrefs.GetString(blackBoardPositionPref);
            Rect blackBoardPosition = new Rect(100, 100, 300, 500);
            if (!string.IsNullOrEmpty(str))
            {
                var rectValues = str.Split(',');

                if (rectValues.Length == 4)
                {
                    float x, y, width, height;
                    if (float.TryParse(rectValues[0], out x) && float.TryParse(rectValues[1], out y) && float.TryParse(rectValues[2], out width) && float.TryParse(rectValues[3], out height))
                    {
                        blackBoardPosition = new Rect(x, y, width, height);
                    }
                }
            }

            return blackBoardPosition;
        }

        void SaveBlackboardPosition(Rect r)
        {
            EditorPrefs.SetString(blackBoardPositionPref, string.Format("{0},{1},{2},{3}", r.x, r.y, r.width, r.height));
        }

        public VFXBlackboard()
        {
            RegisterCallback<ControllerChangedEvent>(OnControllerChanged);
            editTextRequested = OnEditName;
            moveItemRequested = OnMoveItem;
            addItemRequested = OnAddItem;

            this.scrollable = true;


            SetPosition(LoadBlackBoardPosition());

            m_ExposedSection = new BlackboardSection() { title = "parameters"};
            Add(m_ExposedSection);

            AddStyleSheetPath("VFXBlackboard");
        }

        BlackboardSection m_ExposedSection;

        void OnAddParameter(object parameter)
        {
            m_Controller.AddVFXParameter(Vector2.zero, (VFXModelDescriptorParameters)parameter);
        }

        void OnAddItem(Blackboard bb)
        {
            GenericMenu menu = new GenericMenu();

            foreach (var parameter in VFXLibrary.GetParameters())
            {
                VFXParameter model = parameter.model as VFXParameter;

                var type = model.type;

                menu.AddItem(EditorGUIUtility.TextContent(type.UserFriendlyName()), false, OnAddParameter, parameter);
            }

            menu.ShowAsContext();
        }

        void OnEditName(Blackboard bb, VisualElement element, string value)
        {
            if (element is VFXBlackboardField)
            {
                (element as VFXBlackboardField).controller.exposedName = value;
            }
        }

        void OnMoveItem(Blackboard bb, int index, VisualElement element)
        {
            if (element is BlackboardField)
            {
                controller.SetParametersOrder((element as VFXBlackboardField).controller, index);
            }
        }

        Dictionary<VFXParameterController, VFXBlackboardRow> m_ExposedParameters = new Dictionary<VFXParameterController, VFXBlackboardRow>();


        public VFXBlackboardRow GetRowFromController(VFXParameterController controller)
        {
            VFXBlackboardRow row = null;
            m_ExposedParameters.TryGetValue(controller, out row);

            return row;
        }

        void SyncParameters(BlackboardSection section, HashSet<VFXParameterController> actualControllers , Dictionary<VFXParameterController, VFXBlackboardRow> parameters)
        {
            foreach (var removedControllers in parameters.Where(t => !actualControllers.Contains(t.Key)).ToArray())
            {
                removedControllers.Value.RemoveFromHierarchy();
                parameters.Remove(removedControllers.Key);
            }

            foreach (var addedController in actualControllers.Where(t => !parameters.ContainsKey(t)).ToArray())
            {
                VFXBlackboardRow row = new VFXBlackboardRow();

                section.Add(row);

                row.controller = addedController;

                parameters[addedController] = row;
            }

            if (parameters.Count > 0)
            {
                var orderedParameters = parameters.OrderBy(t => t.Key.order).Select(t => t.Value).ToArray();

                if (section.ElementAt(0) != orderedParameters[0])
                {
                    orderedParameters[0].SendToBack();
                }

                for (int i = 1; i < orderedParameters.Length; ++i)
                {
                    if (section.ElementAt(i) != orderedParameters[i])
                    {
                        orderedParameters[i].PlaceInFront(orderedParameters[i - 1]);
                    }
                }
            }
        }

        void OnControllerChanged(ControllerChangedEvent e)
        {
            if (e.controller == controller || e.controller is VFXParameterController) //optim : reorder only is only the order has changed
            {
                HashSet<VFXParameterController> actualControllers = new HashSet<VFXParameterController>(controller.parameterControllers);
                SyncParameters(m_ExposedSection, actualControllers, m_ExposedParameters);
            }
        }

        public void OnMoved()
        {
            SaveBlackboardPosition(GetPosition());
        }
    }
}
