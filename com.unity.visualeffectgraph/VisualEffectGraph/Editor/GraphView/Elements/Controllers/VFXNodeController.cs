using System;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

using Object = UnityEngine.Object;
using System.Collections.ObjectModel;

namespace UnityEditor.VFX.UI
{
    abstract class VFXNodeController : Controller<VFXModel>
    {
        public VFXViewController viewController { get { return m_ViewController; } }


        public abstract VFXSlotContainerController slotContainerController { get; }

        [SerializeField]
        VFXViewController m_ViewController;

        [SerializeField]
        protected List<VFXDataAnchorController> m_InputPorts = new List<VFXDataAnchorController>();

        [SerializeField]
        protected List<VFXDataAnchorController> m_OutputPorts = new List<VFXDataAnchorController>();

        public ReadOnlyCollection<VFXDataAnchorController> inputPorts
        {
            get { return m_InputPorts.AsReadOnly(); }
        }

        public ReadOnlyCollection<VFXDataAnchorController> outputPorts
        {
            get { return m_OutputPorts.AsReadOnly(); }
        }

        public VFXNodeController(VFXModel model, VFXViewController viewController) : base(model)
        {
            m_ViewController = viewController;
        }

        public virtual void ForceUpdate()
        {
            ModelChanged(model);
        }

        protected override void ModelChanged(UnityEngine.Object obj)
        {
            NotifyChange(AnyThing);
        }

        public Vector2 position
        {
            get
            {
                return model.position;
            }
            set
            {
                model.position = value;
            }
        }
        public bool expanded
        {
            get
            {
                return !model.collapsed;
            }
            set
            {
                model.collapsed = !value;
            }
        }
        public bool superCollapsed
        {
            get
            {
                return model.superCollapsed;
            }
            set
            {
                model.superCollapsed = value;
                if (model.superCollapsed)
                {
                    model.collapsed = false;
                }
            }
        }
        public virtual string title
        {
            get { return model.name; }
        }

        public override IEnumerable<Controller> allChildren
        {
            get { return inputPorts.Cast<Controller>().Concat(outputPorts.Cast<Controller>()); }
        }
    }
}
