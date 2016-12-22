using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RMGUI.GraphView
{
	[Serializable]
	public abstract class GraphViewPresenter : ScriptableObject
	{
		[SerializeField]
		// TODO TEMP protected while upgrading MaterialGraph. Needs to go back private
		protected List<GraphElementPresenter> m_Elements = new List<GraphElementPresenter>();

		[SerializeField]
		private List<GraphElementPresenter> m_TempElements = new List<GraphElementPresenter>();

		public virtual void AddElement(GraphElementPresenter element)
		{
			m_Elements.Add(element);
		}

		// Some usage require a separate handler for edges.
		public virtual void AddElement(EdgePresenter edge)
		{
			AddElement((GraphElementPresenter)edge);
		}

		public virtual void RemoveElement(GraphElementPresenter element)
		{
			m_Elements.RemoveAll(x => x == element);
		}

		protected void OnEnable()
		{
			m_Elements.Clear();
			m_TempElements.Clear();
		}

		public IEnumerable<GraphElementPresenter> elements
		{
			get { return m_Elements.Union(m_TempElements); }
		}

		public void AddTempElement(GraphElementPresenter element)
		{
			m_TempElements.Add(element);
		}

		public void RemoveTempElement(GraphElementPresenter element)
		{
			m_TempElements.Remove(element);
		}

		public void ClearTempElements()
		{
			m_TempElements.Clear();
		}
	}
}
