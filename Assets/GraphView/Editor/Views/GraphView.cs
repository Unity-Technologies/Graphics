using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.RMGUI;

namespace RMGUI.GraphView
{
	[StyleSheet("Assets/GraphView/Editor/Views/GraphView.uss")]
	public abstract class GraphView : DataWatchContainer, ISelection
	{
		private GraphViewPresenter m_Presenter;

		public T GetPresenter<T>() where T : GraphViewPresenter
		{
			return presenter as T;
		}

		public GraphViewPresenter presenter
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

		class ContentViewContainer : VisualContainer
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

		bool m_FrameAnimate = false;

		public enum FrameType
		{
			All = 0,
			Selection = 1,
			Origin = 2
		}

		protected GraphView()
		{
			selection = new List<ISelectable>();
			clipChildren = true;
			contentViewContainer = new ContentViewContainer
			{
				name = "contentViewContainer",
				clipChildren = false,
				pickingMode = PickingMode.Ignore
			};

			// make it absolute and 0 sized so it acts as a transform to move children to and fro
			AddChild(contentViewContainer);

			dataMapper = new GraphViewDataMapper();
			dataMapper[typeof(EdgePresenter)] = typeof(Edge);
		}

        // thomasi : added a method to be overloaded
        public virtual List<NodeAnchorPresenter> GetCompatibleAnchors(NodeAnchorPresenter startAnchor, NodeAdapter nodeAdapter)
        {
            return allChildren
			.OfType<NodeAnchor>()
			.Select(na => na.GetPresenter<NodeAnchorPresenter>())
			.Where(nap => nap.IsConnectable() &&
							nap.orientation == startAnchor.orientation &&
							nap.direction != startAnchor.direction &&
							nodeAdapter.GetAdapter(nap.source, startAnchor.source) != null)
			.ToList();
        }


		public override void OnDataChanged()
		{
			if (m_Presenter == null)
				return;

			// process removals
			var current = contentViewContainer.children.OfType<GraphElement>().ToList();
			current.AddRange(children.OfType<GraphElement>());
			foreach (var c in current)
			{
				// been removed?
				if (!m_Presenter.elements.Contains(c.presenter))
				{
					c.parent.RemoveChild(c);
					selection.Remove(c);
				}
			}

			// process additions
			var elements = contentViewContainer.children.OfType<GraphElement>().ToList();
			elements.AddRange(children.OfType<GraphElement>().ToList());
			foreach (GraphElementPresenter elementPresenter in m_Presenter.elements)
			{
				// been added?
				var found = false;

				foreach (var dc in elements)
				{
					if (dc != null && dc.presenter == elementPresenter)
					{
						found = true;
						break;
					}
				}

				if (!found)
					InstantiateElement(elementPresenter);
			}
		}

		protected override object toWatch
		{
			get { return presenter; }
		}

		// ISelection implementation
		public List<ISelectable> selection { get; protected set; }

		// functions to ISelection extensions
		public virtual void AddToSelection(ISelectable selectable)
		{
			var graphElement = selectable as GraphElement;
			if (graphElement != null && graphElement.presenter != null)
				graphElement.presenter.selected = true;
			selection.Add(selectable);
			contentViewContainer.Touch(ChangeType.Repaint);
		}

		public virtual void RemoveFromSelection(ISelectable selectable)
		{
			var graphElement = selectable as GraphElement;
			if (graphElement != null && graphElement.presenter != null)
				graphElement.presenter.selected = false;
			selection.Remove(selectable);
			contentViewContainer.Touch(ChangeType.Repaint);
		}

		public virtual void ClearSelection()
		{
			foreach (var graphElement in selection.OfType<GraphElement>())
			{
				if (graphElement.presenter != null)
					graphElement.presenter.selected = false;
			}

			selection.Clear();
			contentViewContainer.Touch(ChangeType.Repaint);
		}

		private void InstantiateElement(GraphElementPresenter elementPresenter)
		{
			// call factory
			GraphElement newElem = dataMapper.Create(elementPresenter);

			if (newElem == null)
			{
				return;
			}

			newElem.SetPosition(elementPresenter.position);
			newElem.presenter = elementPresenter;

			if ((elementPresenter.capabilities & Capabilities.Resizable) != 0)
			{
				newElem.AddChild(new Resizer());
				newElem.borderBottom = 6;
			}

			bool attachToContainer = (elementPresenter.capabilities & Capabilities.Floating) == 0;
			if (attachToContainer)
				contentViewContainer.AddChild(newElem);
			else
				AddChild(newElem);
		}

		protected EventPropagation DeleteSelection()
		{
			// and DeleteSelection would call that method.
			if (presenter == null)
				return EventPropagation.Stop;

			var elementsToRemove = new HashSet<GraphElementPresenter>();
			foreach (var selectedElement in selection.Cast<GraphElement>()
													 .Where(e => e != null && e.presenter != null))
			{
				if ((selectedElement.presenter.capabilities & Capabilities.Deletable) == 0)
					continue;

				elementsToRemove.Add(selectedElement.presenter);

				var connectorColl = selectedElement.GetPresenter<NodePresenter>();
				if (connectorColl == null)
					continue;

				elementsToRemove.UnionWith(connectorColl.inputAnchors.SelectMany(c => c.connections)
																	 .Where(d => (d.capabilities & Capabilities.Deletable) != 0)
																	 .Cast<GraphElementPresenter>());
				elementsToRemove.UnionWith(connectorColl.outputAnchors.SelectMany(c => c.connections)
																	  .Where(d => (d.capabilities & Capabilities.Deletable) != 0)
																	  .Cast<GraphElementPresenter>());
			}

			// Notify the ends of connections that the connection is going way.
			foreach (var connection in elementsToRemove.OfType<EdgePresenter>())
			{
				if (connection.output != null)
				{
					connection.output.Disconnect(connection);
				}

				if (connection.input != null)
				{
					connection.input.Disconnect(connection);
				}
			}

			foreach (var b in elementsToRemove)
				presenter.RemoveElement(b);

			return EventPropagation.Stop;
		}

		public EventPropagation FrameAll()
		{
			return Frame(FrameType.All);
		}

		public EventPropagation FrameSelection()
		{
			return Frame(FrameType.Selection);
		}

		protected EventPropagation FrameOrigin()
		{
			return Frame(FrameType.Origin);
		}

		protected EventPropagation FramePrev()
		{
			if (contentViewContainer.childrenCount == 0)
				return EventPropagation.Continue;

			var childrenList = contentViewContainer.children.ToList();
			childrenList.Reverse();
			return FramePrevNext(childrenList.GetEnumerator());
		}

		protected EventPropagation FrameNext()
		{
			if (contentViewContainer.childrenCount == 0)
				return EventPropagation.Continue;
			return FramePrevNext(contentViewContainer.GetChildren());
		}

		// TODO: Do we limit to GraphElements or can we tab through ISelectable's?
		EventPropagation FramePrevNext(List<VisualElement>.Enumerator childrenEnum)
		{
			GraphElement graphElement = null;

			// Start from current selection, if any
			if (selection.Count != 0)
				graphElement = selection[0] as GraphElement;

			var it = childrenEnum;
			while (it.MoveNext())
			{
				if (graphElement == null)
				{
					// Select first item we encounter
					graphElement = it.Current as GraphElement;
					break;
				}

				if (graphElement == it.Current as GraphElement)
					graphElement = null; // I.e. select next item in line
			}

			if (graphElement == null)
			{
				// It is possible we exhausted the list, so go back to the start
				it = childrenEnum;
				it.MoveNext();
				graphElement = it.Current as GraphElement;
			}

			if (graphElement == null)
				return EventPropagation.Continue;

			// New selection...
			ClearSelection();
			AddToSelection(graphElement);

			// ...and frame this new selection
			return Frame(FrameType.Selection);
		}

		EventPropagation Frame(FrameType frameType)
		{
			// Reset container translation, scale and position
			contentViewContainer.transform = Matrix4x4.identity;
			// TODO remove once we clarify Touch()
			contentViewContainer.Touch(ChangeType.Repaint);

			if (frameType == FrameType.Origin)
			{
				return EventPropagation.Stop;
			}

			Rect rectToFit = contentViewContainer.position;
			if (frameType == FrameType.Selection)
			{
				// Now calculate rectangle to fit all selected elements
				if (selection.Count == 0)
				{
					return EventPropagation.Continue;
				}

				var graphElement = selection[0] as GraphElement;
				if (graphElement != null)
				{
					rectToFit = graphElement.localBound;
				}

				rectToFit = selection.OfType<GraphElement>()
									 .Aggregate(rectToFit, (current, e) => RectUtils.Encompass(current, e.localBound));
			}
			else /*if (frameType == FrameType.All)*/
			{
				rectToFit = CalculateRectToFitAll(contentViewContainer);
			}

			Vector3 frameTranslation;
			Vector3 frameScaling;
			int frameBorder = 30;

			CalculateFrameTransform(rectToFit, position, frameBorder, out frameTranslation, out frameScaling);

			if (m_FrameAnimate)
			{
				// TODO Animate framing
				// RMAnimation animation = new RMAnimation();
				// parent.Animate(parent)
				//       .Lerp(new string[] {"m_Scale", "m_Translation"},
				//             new object[] {parent.scale, parent.translation},
				//             new object[] {frameScaling, frameTranslation}, 0.08f);
			}
			else
			{
				Matrix4x4 t = Matrix4x4.identity;
				t *= Matrix4x4.TRS(frameTranslation, Quaternion.identity, frameScaling);
				contentViewContainer.transform = t;
			}

			contentViewContainer.Touch(ChangeType.Repaint);

			return EventPropagation.Stop;
		}

		public static Rect CalculateRectToFitAll(VisualContainer contentViewContainer)
		{
			Rect rectToFit = contentViewContainer.position;
			bool reachedFirstChild = false;
			var child = contentViewContainer.GetChildren();
			while (child.MoveNext())
			{
				var graphElement = child.Current as GraphElement;
				var elementPresenter = (graphElement != null) ? graphElement.GetPresenter<GraphElementPresenter>() : null;
				if (elementPresenter == null ||
					(elementPresenter.capabilities & Capabilities.Floating) != 0 ||
					(elementPresenter is EdgePresenter))
				{
					continue;
				}

				if (!reachedFirstChild)
				{
					rectToFit = graphElement.localBound;
					reachedFirstChild = true;
				}
				else
				{
					rectToFit = RectUtils.Encompass(rectToFit, graphElement.localBound);
				}
			}

			return rectToFit;
		}

		public static void CalculateFrameTransform(Rect rectToFit, Rect clientRect, int border, out Vector3 frameTranslation, out Vector3 frameScaling)
		{
			// bring slightly smaller screen rect into GUI space
			var screenRect = new Rect
			{
				xMin = border,
				xMax = clientRect.width - border,
				yMin = border,
				yMax = clientRect.height - border
			};

			Matrix4x4 m = GUI.matrix;
			GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);
			Rect identity = GUIUtility.ScreenToGUIRect(screenRect);

			// measure zoom level necessary to fit the canvas rect into the screen rect
			float zoomLevel = Math.Min(identity.width / rectToFit.width, identity.height / rectToFit.height);

			// clamp
			zoomLevel = Mathf.Clamp(zoomLevel, ContentZoomer.DefaultMinScale.y, 1.0f);

			var transform = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(zoomLevel, zoomLevel, 1.0f));

			var edge = new Vector2(clientRect.width, clientRect.height);
			var origin = new Vector2(0, 0);

			var r = new Rect
			{
				min = origin,
				max = edge
			};

			var parentScale = new Vector3(transform.GetColumn(0).magnitude,
										  transform.GetColumn(1).magnitude,
										  transform.GetColumn(2).magnitude);
			Vector2 offset = r.center - (rectToFit.center * parentScale.x);

			// Update output values before leaving
			frameTranslation = new Vector3(offset.x, offset.y, 0.0f);
			frameScaling = parentScale;

			GUI.matrix = m;
		}
	}
}
