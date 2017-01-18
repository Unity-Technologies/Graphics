using System;
using System.Collections.Generic;
using UnityEngine;

namespace RMGUI.GraphView
{
	[Serializable]
	public abstract class NodeAnchorPresenter : GraphElementPresenter
	{
		protected object m_Source;
		public object source
		{
			get { return m_Source;}
			set
			{
				if (m_Source == value) return;
				m_Source = value;
			}
		}

		public abstract Direction direction { get; }

		[SerializeField]
		private Orientation m_Orientation;
		public virtual Orientation orientation
		{
			get { return m_Orientation; }
			set { m_Orientation = value; }
		}

		[SerializeField]
		private Type m_AnchorType;
		public virtual Type anchorType
		{
			get { return m_AnchorType; }
			set { m_AnchorType = value; }
		}

		[SerializeField]
		private bool m_Highlight;
		public virtual bool highlight
		{
			get { return m_Highlight; }
			set { m_Highlight = value; }
		}

		public virtual bool connected
		{
			get
			{
				return m_Connections.Count != 0;
			}
		}

		public virtual IEnumerable<EdgePresenter> connections
		{
			get
			{
				return m_Connections;
			}
		}

		[SerializeField]
		protected List<EdgePresenter> m_Connections;

		public virtual void Connect(EdgePresenter edgePresenter)
		{
			if (edgePresenter == null)
			{
				throw new ArgumentException("The value passed to NodeAnchorPresenter.Connect is null");
			}

			if (!m_Connections.Contains(edgePresenter))
			{
				m_Connections.Add(edgePresenter);
			}
		}

		public virtual void Disconnect(EdgePresenter edgePresenter)
		{
			if (edgePresenter == null)
			{
				throw new ArgumentException("The value passed to NodeAnchorPresenter.Disconnect is null");
			}

			m_Connections.Remove(edgePresenter);
		}

		public bool IsConnectable()
		{
			return true;
		}

		protected new void OnEnable()
		{
			base.OnEnable();
			m_AnchorType = typeof(object);
			m_Connections = new List<EdgePresenter>();
		}
	}
}
