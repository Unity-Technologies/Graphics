using System;
using System.Collections.Generic;
using UnityEngine;
using UIElements.GraphView;

namespace UnityEditor.VFX.UI
{
    abstract class VFXDataAnchorPresenter : NodeAnchorPresenter
    {
        [SerializeField]
        protected bool m_DirtyHack;


        [SerializeField]
        VFXModel m_Owner;
        public VFXModel Owner { get { return m_Owner; } }

        [SerializeField]
        private VFXSlot m_Model;
        public VFXSlot model { get { return m_Model; } }

        public void Dirty()
        {
            m_DirtyHack = !m_DirtyHack;
        }

        public void Init(VFXModel owner,VFXSlot model)
        {
            m_Owner = owner;
            m_Model = model;
        }
        public object value
        {
            get { return model.value; }
        }


        public string path
        {
            get { return model.path; }
        }

        public int depth
        {
            get { return model.depth; }
        }

        public bool expanded
        {
            get { return model.expanded; }
        }

        public virtual bool expandable
        {
            get { return false; }
        }
    }

    abstract class VFXBlockDataAnchorPresenter : VFXDataAnchorPresenter
    {

        VFXBlockPresenter m_NodePresenter;

        public bool expandable
        {
            get { return VFXBlockPresenter.IsTypeExpandable(anchorType); }
        }


        public void Init(VFXModel owner, VFXSlot model, VFXBlockPresenter nodePresenter)
        {
            base.Init(owner, model);

            m_NodePresenter = nodePresenter;

            anchorType = model.property.type;
            name = model.property.name;
        }

        public void UpdateInfos()
        {
            anchorType = model.property.type;
        }


        public VFXBlockPresenter blockPresenter
        {
            get { return m_NodePresenter; }
        }


        public void SetPropertyValue(object value)
        {
            m_NodePresenter.PropertyValueChanged(this,value);
        }

		public override void Connect(EdgePresenter edgePresenter)
		{
			if (edgePresenter == null)
			{
				throw new ArgumentException("The value passed to VFXDataAnchorPresenter.Connect is null");
			}

			if (!m_Connections.Contains(edgePresenter))
			{
				m_Connections.Add(edgePresenter);
			}
		}

        public override void Disconnect(EdgePresenter edgePresenter)
		{
			if (edgePresenter == null)
			{
				throw new ArgumentException("The value passed to VFXDataAnchorPresenter.Disconnect is null");
			}

			m_Connections.Remove(edgePresenter);
		}
    }

    class VFXBlockDataInputAnchorPresenter : VFXBlockDataAnchorPresenter
    {
        public override Direction direction
        {
            get
            {
                return Direction.Input;
            }
        }
    }

    class VFXBlockDataOutputAnchorPresenter : VFXBlockDataAnchorPresenter
    {
        public override Direction direction
        {
            get
            {
                return Direction.Output;
            }
        }
    }

}
