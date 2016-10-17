using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.RMGUI;
using UnityEngine.RMGUI.StyleSheets;

namespace RMGUI.GraphView
{
	[StyleSheet("Assets/NewUI/Editor/Views/GraphView.uss")]
	public abstract class GraphView : DataWatchContainer, ISelection
	{
		private IGraphElementDataSource m_DataSource;

		public IGraphElementDataSource dataSource
		{
			get { return m_DataSource; }
			set
			{
 				if (m_DataSource == value)
					return;

				RemoveWatch();
				m_DataSource = value;
				OnDataChanged();
				AddWatch();
			}
		}

		class ContentiewContainer : VisualContainer
		{
			public override bool Overlaps(Rect r)
			{
				return true;
			}
		}

		protected GraphViewDataMapper dataMapper { get; set; }

		public VisualContainer contentViewContainer{ get; private set; }

		public VisualContainer viewport
		{
			get { return this; }
		}

		readonly ClassList m_ElementsClassList = new ClassList("graphElement");

		protected GraphView()
		{
			selection = new List<ISelectable>();
			clipChildren = true;
			contentViewContainer = new ContentiewContainer
			{
				name = "contentViewContainer",
				clipChildren = false,
				position = new Rect(0, 0, 0, 0)
			};
			// make it absolute and 0 sized so it acts as a transform to move children to and fro
			AddChild(contentViewContainer);

			dataMapper = new GraphViewDataMapper();
			dataMapper[typeof(EdgeData)] = typeof(Edge);
		}

		public override void OnDataChanged()
		{
			if (m_DataSource == null)
				return;

			// process removals
			var current = contentViewContainer.children.OfType<GraphElement>().ToList();
			current.AddRange(children.OfType<GraphElement>());
			foreach (var c in current)
			{
				// been removed?
				if (!m_DataSource.elements.Contains(c.dataProvider))
				{
					c.parent.RemoveChild(c);
				}
			}

			// process additions
			var elements = contentViewContainer.children.OfType<GraphElement>().ToList();
			elements.AddRange(children.OfType<GraphElement>().ToList());
			foreach (var elementData in m_DataSource.elements)
			{
				// been added?
				var found = false;

				// TODO what the heck is a "dc" anyway?
				foreach (var dc in elements)
				{
					if (dc != null && dc.dataProvider == elementData)
					{
						found = true;
						break;
					}
				}
				if (!found)
					InstanciateElement(elementData);
			}
		}

		protected override object toWatch
		{
			get { return dataSource; }
		}

		// ISelection implementation
		public List<ISelectable> selection { get; protected set; }

		// functions to ISelection extensions
		public void AddToSelection(ISelectable selectable)
		{
			var graphElement = selectable as GraphElement;
			if (graphElement != null && graphElement.dataProvider != null)
				graphElement.dataProvider.selected = true;
			selection.Add(selectable);
			contentViewContainer.Touch(ChangeType.Repaint);
		}

		public void RemoveFromSelection(ISelectable selectable)
		{
			var graphElement = selectable as GraphElement;
			if (graphElement != null && graphElement.dataProvider != null)
				graphElement.dataProvider.selected = false;
			selection.Remove(selectable);
			contentViewContainer.Touch(ChangeType.Repaint);
		}

		public void ClearSelection()
		{
			foreach (var graphElement in selection.OfType<GraphElement>())
			{
				if (graphElement.dataProvider != null)
					graphElement.dataProvider.selected = false;
			}

			selection.Clear();
			contentViewContainer.Touch(ChangeType.Repaint);
		}

		private void InstanciateElement(GraphElementData elementData)
		{
			// call factory
			GraphElement newElem = dataMapper.Create(elementData);

			if (newElem == null)
			{
				return;
			}

			newElem.SetPosition(elementData.position);
			newElem.classList = m_ElementsClassList;
			newElem.dataProvider = elementData;

			if ((elementData.capabilities & Capabilities.Resizable) != 0)
			{
				var resizable = new Resizer();
				newElem.AddManipulator(resizable);
				newElem.AddDecorator(resizable);
				newElem.borderBottom = 6;
			}

			bool attachToContainer = (elementData.capabilities & Capabilities.Floating) == 0;
			if (attachToContainer)
				contentViewContainer.AddChild(newElem);
			else
				AddChild(newElem);
		}
	}
}
