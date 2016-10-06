using System;
using System.Collections.Generic;
using UnityEngine;

namespace RMGUI.GraphView
{
	[Serializable]
	public abstract class GraphElementData : ScriptableObject, IDataSource
	{
		[SerializeField]
		private bool m_Selected;

		[SerializeField]
		private Rect m_Position;

		[SerializeField]
		private Capabilities m_Capabilities;
		
		public Rect position
		{
			get { return m_Position; }
			set { m_Position = value; }
		}

		public bool selected
		{
			get { return m_Selected; }
			set
			{
				// Set new value (toggle old value)
				if ((capabilities & Capabilities.Selectable) == Capabilities.Selectable)
				{
					m_Selected = value;
				}
			}
		}

		protected void OnEnable()
		{
			capabilities = Capabilities.Normal | Capabilities.Movable | Capabilities.Selectable;
		}

		public virtual IEnumerable<GraphElementData> elements
		{
			get { return new GraphElementData[0]; }
		}

		public virtual void AddElement(GraphElementData element)
		{}

		public virtual void RemoveElement(GraphElementData element)
		{}

		public Capabilities capabilities
		{
			get { return m_Capabilities; }
			set { m_Capabilities = value; }
		}
	}
}
