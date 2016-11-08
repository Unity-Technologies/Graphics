using System;
using System.Collections.Generic;
using UnityEngine;

namespace RMGUI.GraphView
{
	[Serializable]
	public abstract class GraphViewDataSource : ScriptableObject, IGraphElementDataSource
	{
		[SerializeField]
		private List<GraphElementData> m_Elements = new List<GraphElementData>();

		public IEnumerable<GraphElementData> elements
		{
			get { return m_Elements; }
		}

		public void AddElement(GraphElementData element)
		{
			m_Elements.Add(element);
		}

		public void RemoveElement(GraphElementData element)
		{
			m_Elements.RemoveAll(x => x == element);
		}

		protected void OnEnable()
		{
		}
	}
}
