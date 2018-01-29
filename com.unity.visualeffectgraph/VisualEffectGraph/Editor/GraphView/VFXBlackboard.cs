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
    class VFXBlackboardRow : BlackboardRow, IControlledElement<VFXParametersController>
    {
        BlackboardField m_Field;

        VisualElement m_Properties;
        public VFXBlackboardRow() : base(new BlackboardField(null, "", "") { name = "vfx-field" }, new VisualElement() { name = "vfx-properties"})
        {
            m_Field = this.Q<BlackboardField>("vfx-field");
            m_Properties = this.Q("vfx-properties");

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

        public VFXBlackboard()
        {
            SetPosition(s_BlackBoardPosition);

            RegisterCallback<ControllerChangedEvent>(OnControllerChanged);
            editTextRequested = OnEditName;
            moveItemRequested = OnMoveItem;

            m_ExposedSection = new BlackboardSection() { title = "exposed"};
            Add(m_ExposedSection);
            m_PrivateSection = new BlackboardSection() { title = "private" };
            Add(m_PrivateSection);
        }

        BlackboardSection m_ExposedSection;
        BlackboardSection m_PrivateSection;

        void OnEditName(Blackboard bb, VisualElement element, string value)
        {
            if (element is BlackboardField)
            {
                VFXBlackboardRow row = element.GetFirstAncestorOfType<VFXBlackboardRow>();
                row.controller.exposedName = value;
            }
        }

        void OnMoveItem(Blackboard bb, int index, VisualElement element)
        {
            if (element is BlackboardField)
            {
                VFXBlackboardRow row = element.GetFirstAncestorOfType<VFXBlackboardRow>();
                if (row != null)
                    controller.SetParametersOrder(row.controller, index);
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

            s_BlackBoardPosition = GetPosition();
        }

        static Rect s_BlackBoardPosition = new Rect(100, 100, 100, 100);
    }
}
