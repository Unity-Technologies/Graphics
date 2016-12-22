using System;
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
                    selection.Remove(c);
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
		public virtual void AddToSelection(ISelectable selectable)
		{
			var graphElement = selectable as GraphElement;
			if (graphElement != null && graphElement.dataProvider != null)
				graphElement.dataProvider.selected = true;
			selection.Add(selectable);
			contentViewContainer.Touch(ChangeType.Repaint);
		}

		public virtual void RemoveFromSelection(ISelectable selectable)
		{
			var graphElement = selectable as GraphElement;
			if (graphElement != null && graphElement.dataProvider != null)
				graphElement.dataProvider.selected = false;
			selection.Remove(selectable);
			contentViewContainer.Touch(ChangeType.Repaint);
		}

		public virtual void ClearSelection()
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

		protected EventPropagation DeleteSelection()
		{
			// and DeleteSelection would call that method.
			var nodesContentViewData = dataSource as GraphViewDataSource;
			if (nodesContentViewData == null)
				return EventPropagation.Stop;

			var elementsToRemove = new HashSet<GraphElementData>();
			foreach (var selectedElement in selection.Cast<GraphElement>()
													 .Where(e => e != null && e.dataProvider != null))
			{
				if ((selectedElement.dataProvider.capabilities & Capabilities.Deletable) == 0)
					continue;

				elementsToRemove.Add(selectedElement.dataProvider);

				var connectorColl = selectedElement.dataProvider as IConnectorCollection;
				if (connectorColl == null)
					continue;

				elementsToRemove.UnionWith(connectorColl.inputConnectors.SelectMany(c => c.connections)
																		.Cast<GraphElementData>()
																		.Where(d => (d.capabilities & Capabilities.Deletable) != 0));
				elementsToRemove.UnionWith(connectorColl.outputConnectors.SelectMany(c => c.connections)
																		 .Cast<GraphElementData>()
																		 .Where(d => (d.capabilities & Capabilities.Deletable) != 0));
			}

			// Notify the ends of connections that the connection is going way.
			foreach (var connection in elementsToRemove.OfType<IConnection>())
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
				nodesContentViewData.RemoveElement(b);

			return EventPropagation.Stop;
		}

		protected EventPropagation FrameAll()
		{
			return Frame(FrameType.All);
		}

		protected EventPropagation FrameSelection()
		{
			return Frame(FrameType.Selection);
		}

		protected EventPropagation FrameOrigin()
		{
			return Frame(FrameType.Origin);
		}

		EventPropagation Frame(FrameType frameType)
		{
			// Reset container translation, scale and position
			contentViewContainer.transform *= contentViewContainer.transform.inverse;
			Rect p = contentViewContainer.position;
			p.x = 0;
			p.y = 0;
			contentViewContainer.position = p;

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
				bool reachedFirstChild = false;
				foreach (VisualElement child in contentViewContainer.children)
				{
					var graphElement = child as GraphElement;
					if (graphElement == null ||
						(graphElement.dataProvider.capabilities & Capabilities.Floating) != 0 ||
						(graphElement.dataProvider is EdgeData))
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
			}

			Vector3 frameTranslation;
			Vector3 frameScaling;

			CalculateFrameTransform(rectToFit, out frameTranslation, out frameScaling);

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

		void CalculateFrameTransform(Rect rectToFit, out Vector3 frameTranslation, out Vector3 frameScaling)
		{
			// Give it full width/height
			Rect clientRect = position;

			// bring slightly smaller screen rect into GUI space
			var screenRect = new Rect
			{
				xMin = 30,
				xMax = clientRect.width - 30,
				yMin = 30,
				yMax = clientRect.height - 30
			};

			Matrix4x4 m = GUI.matrix;
			GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);
			Rect identity = GUIUtility.ScreenToGUIRect(screenRect);

			// measure zoom level necessary to fit the canvas rect into the screen rect
			float zoomLevel = Math.Min(identity.width / rectToFit.width, identity.height / rectToFit.height);

			// clamp
			zoomLevel = Mathf.Clamp(zoomLevel, 0.08f, 1.0f);

			var cachedScale = new Vector3(transform.GetColumn(0).magnitude,
										  transform.GetColumn(1).magnitude,
										  transform.GetColumn(2).magnitude);
			Vector4 cachedTranslation = transform.GetColumn(3);

			transform = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(zoomLevel, zoomLevel, 1.0f));

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

			transform = Matrix4x4.TRS(cachedTranslation, Quaternion.identity, cachedScale);

			GUI.matrix = m;
		}
	}
}
