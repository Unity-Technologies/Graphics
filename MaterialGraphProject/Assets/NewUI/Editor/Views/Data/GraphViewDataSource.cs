using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RMGUI.GraphView
{
	public interface IDataSource
	{
		IEnumerable<GraphElementData> elements { get; }
		void AddElement(GraphElementData element);
		void RemoveElement(GraphElementData element);
	}

	[Serializable]
	public abstract class GraphViewDataSource : ScriptableObject, IDataSource
	{
		[SerializeField]
		private List<GraphElementData> m_Elements = new List<GraphElementData>();

		public IEnumerable<GraphElementData> elements
		{
			get { return m_Elements.OfType<GraphElementData>(); }
		}

		public void AddElement(GraphElementData element)
		{
			m_Elements.Add(element);
		}

		public void RemoveElement(GraphElementData element)
		{
			m_Elements.RemoveAll(x => x == element);
		}
	}
}
