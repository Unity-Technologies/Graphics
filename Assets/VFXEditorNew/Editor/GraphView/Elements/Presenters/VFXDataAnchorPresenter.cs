using System;
using System.Collections.Generic;
using UnityEngine;
using RMGUI.GraphView;

namespace UnityEditor.VFX.UI
{
    abstract class VFXDataAnchorPresenter : NodeAnchorPresenter
    {
        [SerializeField]
        private VFXModel m_Owner;
        public VFXModel Owner { get { return m_Owner; } }

        VFXBlockPresenter.PropertyInfo m_PropertyInfo;

        VFXBlockPresenter m_NodePresenter;


        public VFXBlockPresenter.PropertyInfo propertyInfo
        {
            get { return m_PropertyInfo; }
        }

        public void Init(VFXModel owner, VFXBlockPresenter nodePresenter, VFXBlockPresenter.PropertyInfo propertyInfo)
        {
            m_Owner = owner;
            anchorType = propertyInfo.type;
            m_NodePresenter = nodePresenter;
            m_PropertyInfo = propertyInfo;
        }

        public void UpdateInfos(ref VFXBlockPresenter.PropertyInfo propertyInfo)
        {
            m_PropertyInfo = propertyInfo;
        }


        public VFXBlockPresenter nodePresenter
        {
            get { return m_NodePresenter; }
        }


        public void SetPropertyValue(object value)
        {
            m_PropertyInfo.value = value;
            m_NodePresenter.PropertyValueChanged(ref m_PropertyInfo);
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

    class VFXDataInputAnchorPresenter : VFXDataAnchorPresenter
    {
        public override Direction direction
        {
            get
            {
                return Direction.Input;
            }
        }
    }

    class VFXDataOutputAnchorPresenter : VFXDataAnchorPresenter
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
