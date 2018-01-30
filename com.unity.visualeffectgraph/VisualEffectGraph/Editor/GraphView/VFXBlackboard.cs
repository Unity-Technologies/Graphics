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
    class VFXBlackboardPropertyView : VisualElement, IControlledElement, IControlledElement<VFXParametersController>
    {
        public VFXBlackboardRow owner
        {
            get; set;
        }

        Controller IControlledElement.controller
        {
            get { return owner.controller; }
        }
        public VFXParametersController controller
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

                if (m_Property != null)
                    result = result.Concat(Enumerable.Repeat(m_Property, 1));
                if (m_SubProperties != null)
                    result = result.Concat(m_SubProperties);
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

        void CreateSubProperties(List<int> fieldPath)
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
                    Add(prop);
                }
                if (prop == null || !prop.showsEverything)
                {
                    subFieldPath.Clear();
                    subFieldPath.AddRange(fieldPath);
                    subFieldPath.Add(cpt);
                    CreateSubProperties(subFieldPath);
                }
                ++cpt;
            }
        }

        public void SelfChange()
        {
            if (m_Property == null || !m_Property.IsCompatible(controller))
            {
                m_Property = PropertyRM.Create(controller, 55);
                if (m_Property != null)
                {
                    Add(m_Property);
                    m_SubProperties = new List<PropertyRM>();
                    List<int> fieldpath = new List<int>();
                    if (!m_Property.showsEverything)
                    {
                        CreateSubProperties(fieldpath);
                    }
                    else if (controller.canHaveMinMax)
                    {
                        m_MinProperty = PropertyRM.Create(controller.minController, 55);
                        m_MaxProperty = PropertyRM.Create(controller.maxController, 55);

                        Add(m_MinProperty);
                        Add(m_MaxProperty);
                    }
                }
            }
            foreach (var subProp in allProperties)
            {
                subProp.Update();
            }
            float labelWidth = 70;
            GetPreferedWidths(ref labelWidth);
            ApplyWidths(labelWidth);
        }
    }
    class VFXBlackboardField : BlackboardField, IControlledElement, IControlledElement<VFXParametersController>
    {
        public VFXBlackboardRow owner
        {
            get; set;
        }

        public VFXBlackboardField() : base()
        {}

        Controller IControlledElement.controller
        {
            get { return owner.controller; }
        }
        public VFXParametersController controller
        {
            get { return owner.controller; }
        }
    }

    class VFXBlackboardRow : BlackboardRow, IControlledElement<VFXParametersController>
    {
        VFXBlackboardField m_Field;

        VFXBlackboardPropertyView m_Properties;
        public VFXBlackboardRow() : this(new VFXBlackboardField() { name = "vfx-field" }, new VFXBlackboardPropertyView() { name = "vfx-properties" })
        {}

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
            m_Field.typeText = controller.portType.UserFriendlyName();

            // if the order or exposed change, let the event be caught by the VFXBlackboard
            if (controller.order == m_CurrentOrder && controller.exposed == m_CurrentExposed)
            {
                m_CurrentOrder = controller.order;
                m_CurrentExposed = controller.exposed;
                e.StopPropagation();
            }

            m_Properties.SelfChange();
        }

        VFXParametersController m_Controller;
        Controller IControlledElement.controller
        {
            get { return m_Controller; }
        }
        public VFXParametersController controller
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
    class VFXBlackboard : Blackboard, IControlledElement<VFXViewController>
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
                    m_Controller = value;

                    if (m_Controller != null)
                    {
                        m_Controller.RegisterHandler(this);
                    }
                }
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


            SetPosition(LoadBlackBoardPosition());

            m_ExposedSection = new BlackboardSection() { title = "exposed"};
            Add(m_ExposedSection);
            m_PrivateSection = new BlackboardSection() { title = "private" };
            Add(m_PrivateSection);

            AddStyleSheetPath("VFXBlackboard");
        }

        BlackboardSection m_ExposedSection;
        BlackboardSection m_PrivateSection;

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

        Dictionary<VFXParametersController, VFXBlackboardRow> m_ExposedParameters = new Dictionary<VFXParametersController, VFXBlackboardRow>();
        Dictionary<VFXParametersController, VFXBlackboardRow> m_PrivateParameters = new Dictionary<VFXParametersController, VFXBlackboardRow>();


        void SyncParameters(BlackboardSection section, HashSet<VFXParametersController> actualControllers , Dictionary<VFXParametersController, VFXBlackboardRow> parameters)
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
            if (e.controller == controller || e.controller is VFXParametersController) //optim : reorder only is only the order has changed
            {
                HashSet<VFXParametersController> actualControllers = new HashSet<VFXParametersController>(controller.parametersController.Where(t => t.exposed));
                SyncParameters(m_ExposedSection, actualControllers, m_ExposedParameters);
                actualControllers = new HashSet<VFXParametersController>(controller.parametersController.Where(t => !t.exposed));
                SyncParameters(m_PrivateSection, actualControllers, m_PrivateParameters);
            }
        }

        public override void UpdatePresenterPosition()
        {
            base.UpdatePresenterPosition();

            SaveBlackboardPosition(GetPosition());
        }
    }
}
