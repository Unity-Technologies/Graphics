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


        private VFXLinkablePresenter m_SourceNode;

        public VFXLinkablePresenter sourceNode
        {
            get
            {
                return m_SourceNode;
            }
        }

        public void Dirty()
        {
            m_DirtyHack = !m_DirtyHack;
        }

        public void Init(VFXModel owner,VFXSlot model, VFXLinkablePresenter nodePresenter)
        {
            m_Owner = owner;
            m_Model = model;
            m_SourceNode = nodePresenter;
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
        public override bool expandable
        {
            get { return VFXBlockPresenter.IsTypeExpandable(anchorType); }
        }


        public void Init(VFXModel owner, VFXSlot model, VFXBlockPresenter nodePresenter)
        {
            base.Init(owner, model, nodePresenter);

            anchorType = model.property.type;
            name = model.property.name;
        }

        public void UpdateInfos()
        {
            anchorType = model.property.type;
        }


        public VFXBlockPresenter blockPresenter
        {
            get { return sourceNode as VFXBlockPresenter; }
        }


        public void SetPropertyValue(object value)
        {
            blockPresenter.PropertyValueChanged(this,value);
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
