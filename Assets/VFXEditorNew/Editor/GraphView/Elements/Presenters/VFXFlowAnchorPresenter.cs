using System;
using System.Collections.Generic;
using UnityEngine;
using RMGUI.GraphView;

namespace UnityEditor.VFX.UI
{
    abstract class VFXFlowAnchorPresenter : NodeAnchorPresenter
    {
        [SerializeField]
        private VFXModel m_Owner;
        public VFXModel Owner { get { return m_Owner; } }

        public void Init(VFXModel owner)
        {
            m_Owner = owner;
            anchorType = typeof(int); // We dont care about that atm!
            orientation = Orientation.Vertical;
        }

		public override void Connect(EdgePresenter edgePresenter)
		{
			if (edgePresenter == null)
			{
				throw new ArgumentException("The value passed to VFXFlowAnchorPresenter.Connect is null");
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
				throw new ArgumentException("The value passed to VFXFlowAnchorPresenter.Disconnect is null");
			}

			m_Connections.Remove(edgePresenter);
		}
    }

    class VFXFlowInputAnchorPresenter : VFXFlowAnchorPresenter
    {
        public VFXFlowInputAnchorPresenter()
        {
        }

        public override Direction direction
        {
            get
            {
                return Direction.Input;
            }
        }
    }

    class VFXFlowOutputAnchorPresenter : VFXFlowAnchorPresenter
    {
        public VFXFlowOutputAnchorPresenter()
        {
        }

        public override Direction direction
        {
            get
            {
                return Direction.Output;
            }
        }
    }
}
