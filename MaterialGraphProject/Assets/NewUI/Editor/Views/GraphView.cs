using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.RMGUI;
using UnityEngine.RMGUI.StyleSheets;

namespace RMGUI.GraphView
{
	[StyleSheet("Assets/Editor/Views/GraphView.uss")]
	public abstract class GraphView : DataContainer, ISelection
	{
		class ContentiewContainer:VisualContainer
		{
			public override bool Overlaps(Rect r)
			{
				return true;
			}
		}

		public VisualContainer contentViewContainer{ get; private set; }

		readonly ClassList elementsClassList = new ClassList("graphElement");

		public VisualContainer viewport
		{
			get { return this; }
		}

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
		}

		public override void OnDataChanged()
		{
			if (dataProvider == null)
				return;

			// process removals
			var current = contentViewContainer.children.OfType<GraphElement>().ToList();
			current.AddRange(children.OfType<GraphElement>().ToList());
			foreach (var c in current)
			{
				// been removed?
				if (!dataProvider.elements.Contains(c.GetData<GraphElementData>()))
				{
					c.parent.RemoveChild(c);
				}
			}

			// process additions
			var elements = contentViewContainer.children.OfType<GraphElement>().ToList();
			elements.AddRange(children.OfType<GraphElement>().ToList());
			foreach (var elementData in dataProvider.elements)
			{
				// been added?
				bool found = false;

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

		// ISelection implementation
		public List<ISelectable> selection { get; protected set; }

		// functions to ISelection extensions
		public void AddToSelection(ISelectable e)
		{
			GraphElement ce = e as GraphElement;
			if (ce != null && ce.GetData<GraphElementData>() != null)
				ce.GetData<GraphElementData>().selected = true;
			selection.Add(e);
			contentViewContainer.Touch(ChangeType.Repaint);
		}

		public void RemoveFromSelection(ISelectable e)
		{
			GraphElement ce = e as GraphElement;
			if (ce != null && ce.GetData<GraphElementData>() != null)
				ce.GetData<GraphElementData>().selected = false;
			selection.Remove(e);
			contentViewContainer.Touch(ChangeType.Repaint);
		}

		public void ClearSelection()
		{
			foreach (GraphElement e in selection.OfType<GraphElement>())
			{
				if (e.GetData<GraphElementData>() != null)
					e.GetData<GraphElementData>().selected = false;
			}

			selection.Clear();
			contentViewContainer.Touch(ChangeType.Repaint);
		}

		private void InstanciateElement(GraphElementData elementData)
		{
			// call factory
			var newElem = CustomDataView.Create(elementData) as GraphElement;

			if (newElem == null)
			{
				return;
			}

			newElem.SetPosition(elementData.position);
			newElem.classList = elementsClassList;
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
