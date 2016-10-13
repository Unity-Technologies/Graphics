using System;
using UnityEngine;

namespace RMGUI.GraphView.Demo
{
	[Serializable]
	public class NodeAnchorData : GraphElementData, IConnectable
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
		public Direction direction
		{
			get { return m_Direction; }
			set { m_Direction = value; }
		}

		[SerializeField]
		private Orientation m_Orientation;
		public Orientation orientation
		{
			get { return m_Orientation; } set { m_Orientation = value; }
		}

		[SerializeField]
		private Type m_Type;
		public Type type
		{
			get { return m_Type; }
			set { m_Type = value; }
		}

		[SerializeField]
		private bool m_Highlight;
		public bool highlight
		{
			get { return m_Highlight; }
			set { m_Highlight = value; }
		}

		[SerializeField]
		private bool m_Connected;
		public bool connected
		{
			get { return m_Connected; }
			set { m_Connected = value; }
		}

		protected new void OnEnable()
		{
			base.OnEnable();
			m_Type = typeof(object);
		}

		protected NodeAnchorData() {}
	}
}
