using System;
using System.Collections.Generic;
using UnityEngine;
using RMGUI.GraphView;

namespace UnityEditor.VFX.UI
{
    public abstract class VFXFlowAnchorPresenter : NodeAnchorPresenter
    {
        // thomasi : need to override
		public virtual void Connect(EdgePresenter edgePresenter)
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

        // thomasi : need to override
		public virtual void Disconnect(EdgePresenter edgePresenter)
		{
			if (edgePresenter == null)
			{
				throw new ArgumentException("The value passed to VFXFlowAnchorPresenter.Disconnect is null");
			}

			m_Connections.Remove(edgePresenter);
		}
    }

    public class VFXFlowInputAnchorPresenter : VFXFlowAnchorPresenter
    {
        public override Direction direction
        {
            get
            {
                return Direction.Input;
            }
        }
    }

    public class VFXFlowOutputAnchorPresenter : VFXFlowAnchorPresenter
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
