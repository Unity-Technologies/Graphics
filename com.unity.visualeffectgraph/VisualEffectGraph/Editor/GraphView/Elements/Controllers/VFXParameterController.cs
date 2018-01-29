using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using UnityEditor.Experimental.UIElements.GraphView;

using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEngine.Experimental.UIElements;


namespace UnityEditor.VFX.UI
{
    class VFXParameterOutputDataAnchorController : VFXDataAnchorController
    {
        public VFXParameterOutputDataAnchorController(VFXSlot model, VFXSlotContainerController sourceNode, bool hidden) : base(model, sourceNode, hidden)
        {
        }

        public override Direction direction
        { get { return Direction.Output; } }
        public override string name
        {
            get
            {
                if (model.IsMasterSlot())
                {
                    return model.property.type.UserFriendlyName();
                }
                return base.name;
            }
        }

        public new VFXParameterController sourceNode
        {
            get { return base.sourceNode as VFXParameterController; }
        }
    }

    class VFXParameterController : VFXSlotContainerController, IPropertyRMProvider, IValueController
    {
        VFXParametersController m_ParentController;

        VFXParameter.ParamInfo m_Infos;

        IDataWatchHandle m_SlotHandle;

        public VFXParameterController(VFXParametersController controller, VFXParameter.ParamInfo infos, VFXViewController viewController) : base(controller.model, viewController)
        {
            m_ParentController = controller;
            m_Infos = infos;

            m_SlotHandle = DataWatchService.sharedInstance.AddWatch(m_ParentController.parameter.outputSlots[0], OnSlotChanged);
        }

        void OnSlotChanged(UnityEngine.Object obj)
        {
            NotifyChange(AnyThing);
        }

        public VFXParameter.ParamInfo infos
        {
            get { return m_Infos; }
        }

        protected override VFXDataAnchorController AddDataAnchor(VFXSlot slot, bool input, bool hidden)
        {
            var anchor = new VFXParameterOutputDataAnchorController(slot, this, hidden);
            anchor.portType = slot.property.type;
            return anchor;
        }

        public override string title
        {
            get { return m_ParentController.parameter.outputSlots[0].property.type.UserFriendlyName(); }
        }

        public string exposedName
        {
            get { return m_ParentController.parameter.exposedName; }
        }
        public bool exposed
        {
            get { return m_ParentController.parameter.exposed; }
        }

        public int order
        {
            get { return m_ParentController.parameter.order; }
        }

        bool IPropertyRMProvider.expanded
        {
            get
            {
                return false;
            }
        }
        bool IPropertyRMProvider.editable
        {
            get { return true; }
        }

        bool IPropertyRMProvider.expandable { get { return false; } }


        public object value
        {
            get
            {
                return m_ParentController.value;
            }
            set
            {
                m_ParentController.value = value;
            }
        }

        string IPropertyRMProvider.name { get { return "Value"; } }

        object[] IPropertyRMProvider.customAttributes { get { return new object[] {}; } }

        VFXPropertyAttribute[] IPropertyRMProvider.attributes { get { return new VFXPropertyAttribute[] {}; } }

        public Type portType
        {
            get
            {
                return m_ParentController.parameter.GetOutputSlot(0).property.type;
            }
        }

        int IPropertyRMProvider.depth { get { return 0; } }

        void IPropertyRMProvider.ExpandPath()
        {
            throw new NotImplementedException();
        }

        void IPropertyRMProvider.RetractPath()
        {
            throw new NotImplementedException();
        }

        public override void DrawGizmos(VFXComponent component)
        {
            VFXValueGizmo.Draw(this, component);

            m_ParentController.DrawGizmos(component);
        }

        public override int id
        {
            get { return m_Infos.id; }
        }

        public override Vector2 position
        {
            get
            {
                return m_Infos.position;
            }
            set
            {
                m_Infos.position = value;
            }
        }

        public VFXParametersController parentController
        {
            get { return m_ParentController; }
        }
    }
}
