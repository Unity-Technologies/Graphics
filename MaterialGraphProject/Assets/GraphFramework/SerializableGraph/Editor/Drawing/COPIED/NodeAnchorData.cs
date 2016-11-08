using System;
using System.Collections.Generic;
using System.Linq;
using RMGUI.GraphView;
using UnityEngine;

namespace UnityEditor.Graphing.Drawing
{
	[Serializable]
	public class NodeAnchorData : GraphElementData, IConnector
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

		[SerializeField]
		private Direction m_Direction;
		public virtual Direction direction
		{
			// TODO We should not be able to change direction of an existing anchor
			get { return m_Direction; }
			set { m_Direction = value; }
		}

		[SerializeField]
		private Orientation m_Orientation;
		public virtual Orientation orientation
		{
			get { return m_Orientation; } set { m_Orientation = value; }
		}

		[SerializeField]
		private Type m_Type;
		public virtual Type type
		{
			get { return m_Type; }
			set { m_Type = value; }
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
				return (m_Direction == Direction.Output ? m_OutputConnections : m_InputConnections).Count != 0;
			}
		}

		public virtual IEnumerable<IConnection> connections
		{
			get
			{
				return (m_Direction == Direction.Output ? m_OutputConnections : m_InputConnections).Cast<IConnection>();
			}
		}

		// TODO we'll probably want to distinguish between InputAnchorData and OutputAnchorData so as to
		// not burden ourselves with extra data we don't need.
		[SerializeField]
		private List<GraphElementData> m_InputConnections;

		[SerializeField]
		private List<GraphElementData> m_OutputConnections;

		public void Connect(IConnection connection)
		{
			var ged = connection as GraphElementData;
			if (connection != null && ged == null)
			{
				throw new ArgumentException("The value passed to NodeAnchorData.Connect is not a GraphElementData");
			}

			if (m_Direction == Direction.Input)
			{
				if (!m_InputConnections.Contains(ged))
				{
					m_InputConnections.Add(ged);
				}
			}
			else
			{
				if (!m_OutputConnections.Contains(ged))
				{
					m_OutputConnections.Add(ged);
				}
			}
		}

		public void Disconnect(IConnection connection)
		{
			var ged = connection as GraphElementData;
			if (connection != null && ged == null)
			{
				throw new ArgumentException("The value passed to NodeAnchorData.Disconnect is not a GraphElementData");
			}

			if (m_Direction == Direction.Input)
			{
				m_InputConnections.Remove(ged);
			}
			else
			{
				m_OutputConnections.Remove(ged);
			}
		}

		protected new void OnEnable()
		{
			base.OnEnable();
			m_Type = typeof(object);
			m_OutputConnections = new List<GraphElementData>();
			m_InputConnections = new List<GraphElementData>();
		}

		protected NodeAnchorData() {}
	}
}
