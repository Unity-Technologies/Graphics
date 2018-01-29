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


        void OnControllerChanged(ControllerChangedEvent e)
        {
            m_Field.text = controller.exposedName;
            m_Field.typeText = controller.portType.UserFriendlyName();
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
        }

        void OnEditName(Blackboard bb, VisualElement element, string value)
        {
            if (element is BlackboardField)
            {
                VFXBlackboardRow row = element.GetFirstAncestorOfType<VFXBlackboardRow>();
                row.controller.model.SetSettingValue("m_exposedName", value);
            }
        }

        Dictionary<VFXParametersController, VFXBlackboardRow> m_Parameters = new Dictionary<VFXParametersController, VFXBlackboardRow>();

        void OnControllerChanged(ControllerChangedEvent e)
        {
            if (e.controller == controller)
            {
                HashSet<VFXParametersController> actualControllers = new HashSet<VFXParametersController>(controller.parametersController);
                foreach (var removedControllers in m_Parameters.Where(t => !actualControllers.Contains(t.Key)).ToArray())
                {
                    removedControllers.Value.RemoveFromHierarchy();
                    m_Parameters.Remove(removedControllers.Key);
                }

                foreach (var addedController in actualControllers.Where(t => !m_Parameters.ContainsKey(t)).ToArray())
                {
                    VFXBlackboardRow row = new VFXBlackboardRow();

                    contentContainer.Add(row);

                    row.controller = addedController;

                    m_Parameters[addedController] = row;
                }

                var orderedParameters = m_Parameters.OrderBy(t => t.Key.order).Select(t => t.Value).ToArray();

                if (contentContainer.ElementAt(0) != orderedParameters[0])
                {
                    orderedParameters[0].SendToBack();
                }

                for (int i = 1; i < orderedParameters.Length; ++i)
                {
                    if (contentContainer.ElementAt(i) != orderedParameters[i])
                    {
                        orderedParameters[i].PlaceInFront(orderedParameters[i - 1]);
                    }
                }
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
