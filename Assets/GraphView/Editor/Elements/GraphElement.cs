using System;
using UnityEngine;
using UnityEngine.RMGUI;
using UnityEngine.RMGUI.StyleSheets;

namespace RMGUI.GraphView
{
	public abstract class GraphElement : DataWatchContainer, ISelectable
	{
		GraphElementPresenter m_Presenter;

		Color m_ElementTypeColor;
		public Color elementTypeColor { get { return m_ElementTypeColor; } set { m_ElementTypeColor = value; } }

		static readonly ClassList s_ElementsClassList = new ClassList("graphElement");

		protected GraphElement()
		{
			classList = s_ElementsClassList;
			m_ElementTypeColor = new Color(0.9f, 0.9f, 0.9f, 0.5f);
		}

		public T GetPresenter<T>() where T : GraphElementPresenter
		{
			return presenter as T;
		}

		public GraphElementPresenter presenter
		{
			get { return m_Presenter; }
			set
			{
				if (m_Presenter == value)
					return;
				RemoveWatch();
				m_Presenter = value;
				OnDataChanged();
				AddWatch();
			}
		}

		protected override object toWatch
		{
			get { return presenter; }
		}

		public override void OnDataChanged()
		{
			if (presenter == null)
			{
				return;
			}

			// propagate selection but why?
			foreach (VisualElement visualElement in children)
			{
				var graphElement = visualElement as GraphElement;
				if (graphElement != null)
				{
					GraphElementPresenter childPresenter = graphElement.presenter;
					if (childPresenter != null)
					{
						childPresenter.selected = presenter.selected;
					}
				}
			}

			if (presenter.selected)
			{
				AddToClassList("selected");
			}
			else
			{
				RemoveFromClassList("selected");
			}

			SetPosition(presenter.position);
		}

		public virtual bool IsSelectable()
		{
			return (presenter.capabilities & Capabilities.Selectable) == Capabilities.Selectable;
		}

		public virtual Vector3 GetGlobalCenter()
		{
			var center = position.center;
			var globalCenter = new Vector3(center.x + parent.position.x, center.y + parent.position.y);
			return parent.globalTransform.MultiplyPoint3x4(globalCenter);
		}

		public virtual void SetPosition(Rect newPos)
		{
			// set absolute position from presenter
			position = newPos;
		}

		public virtual EventPropagation Select(VisualContainer selectionContainer, Event evt)
		{
			var selectable = this.GetFirstOfType<ISelectable>();
			if (selectable == null || !selectable.IsSelectable())
			{
				return EventPropagation.Continue;
			}

			var graphView = selectionContainer as GraphView;
			if (graphView != null && parent == graphView.contentViewContainer)
			{
				if (graphView.selection.Contains(selectable))
				{
					if (evt.control)
					{
						graphView.RemoveFromSelection(selectable);
						return EventPropagation.Stop;
					}
					return EventPropagation.Continue;
				}

				if (!evt.control)
					graphView.ClearSelection();
				graphView.AddToSelection(selectable);
			}

			return EventPropagation.Continue;
		}
	}
}
