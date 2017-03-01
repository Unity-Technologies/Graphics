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

        [SerializeField]
        VFXBlockPresenter.PropertyInfo m_PropertyInfo;

        VFXBlockPresenter m_NodePresenter;

        [SerializeField]
        bool m_DirtyHack;



        public Type type
        {
            get {return m_PropertyInfo.type;}
        }

        public object value
        {
            get { return m_PropertyInfo.value;}
        }

        public string path
        {
            get { return m_PropertyInfo.path;}
        }

        public int depth
        {
            get { return m_PropertyInfo.depth;}
        }

        public bool expandable
        {
            get { return m_PropertyInfo.expandable;}
        }

        public bool expanded
        {
            get { return m_PropertyInfo.expanded;}
        }


        public void Init(VFXModel owner, VFXBlockPresenter nodePresenter, VFXBlockPresenter.PropertyInfo propertyInfo)
        {
            m_Owner = owner;
            anchorType = propertyInfo.type;
            m_NodePresenter = nodePresenter;
            m_PropertyInfo = propertyInfo;
            name = m_PropertyInfo.name;
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
            m_NodePresenter.PropertyValueChanged(this,value);
            m_DirtyHack = !m_DirtyHack;
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
