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
        public VFXViewPresenter viewPresenter { get { return m_ViewPresenter; } }


        public abstract VFXSlotContainerPresenter slotContainerPresenter { get; }

        [SerializeField]
        VFXViewPresenter m_ViewPresenter;

        [SerializeField]
        protected List<VFXDataAnchorPresenter> m_InputPorts = new List<VFXDataAnchorPresenter>();

        [SerializeField]
        protected List<VFXDataAnchorPresenter> m_OutputPorts = new List<VFXDataAnchorPresenter>();

        public ReadOnlyCollection<VFXDataAnchorPresenter> inputPorts
        {
            get { return m_InputPorts.AsReadOnly(); }
        }

        public ReadOnlyCollection<VFXDataAnchorPresenter> outputPorts
        {
            get { return m_OutputPorts.AsReadOnly(); }
        }

        public virtual void Init(VFXModel model, VFXViewPresenter viewPresenter)
        {
            base.Init(model);
            m_ViewPresenter = viewPresenter;
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
        public virtual string title
        {
            get { return model.name; }
        }
    }
}
