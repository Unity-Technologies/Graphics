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

        public void Init(VFXModel owner,Type type)
        {
            m_Owner = owner;
            anchorType = type;
            anchorType = typeof(int); // We dont care about that atm!
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
