using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.RMGUI;

namespace RMGUI.GraphView
{
	internal class EdgeConnector<TEdgeData> : Manipulator where TEdgeData : EdgeData
	{
		private readonly List<IConnectable> m_CompatibleAnchors = new List<IConnectable>();
		private TEdgeData m_EdgeDataCandidate;

		private IGraphElementDataSource m_DataSource;
		private GraphView m_GraphView;

		public MouseButton activateButton { get; set; }

		public EdgeConnector()
		{
			activateButton = MouseButton.LeftMouse;
		}

		public override EventPropagation HandleEvent(Event evt, VisualElement finalTarget)
		{
			switch (evt.type)
			{
				case EventType.MouseDown:
					if (evt.button != (int)activateButton)
					{
						break;
					}

					IConnectable cnx = null;
					var graphElement = finalTarget as GraphElement;
					if (graphElement != null && graphElement.dataProvider != null)
					{
						GraphElementData data = graphElement.dataProvider;
						cnx = (IConnectable)data;
						m_GraphView = graphElement.GetFirstAncestorOfType<GraphView>();
					}

					if (cnx == null || m_GraphView == null)
					{
						break;
					}

					m_DataSource = m_GraphView.dataSource;
					if (m_DataSource == null)
					{
						break;
					}

					this.TakeCapture();

					m_CompatibleAnchors.Clear();

					var nodeAdapter = new NodeAdapter();

					// get all available connectors
					IEnumerable<IConnectable> visibleAnchors = m_GraphView.allChildren.OfType<GraphElement>()
																					  .Select(e => e.dataProvider)
																					  .OfType<IConnectable>()
																					  .Where(a => a.IsConnectable());

					foreach (var toCnx in visibleAnchors)
					{
						if (cnx.orientation != toCnx.orientation)
							continue;

						bool isBidirectional = ((cnx.direction == Direction.Bidirectional) ||
												(toCnx.direction == Direction.Bidirectional));

						if (cnx.direction != toCnx.direction || isBidirectional)
						{
							if (nodeAdapter.GetAdapter(cnx.source, toCnx.source) != null)
							{
								toCnx.highlight = true;
								m_CompatibleAnchors.Add(toCnx);
							}
						}
					}

					m_EdgeDataCandidate = ScriptableObject.CreateInstance<TEdgeData>();

					m_EdgeDataCandidate.position = new Rect(0, 0, 1, 1);
					m_EdgeDataCandidate.left = graphElement.dataProvider as IConnectable;
					m_EdgeDataCandidate.right = null;
					m_EdgeDataCandidate.candidate = true;
					m_EdgeDataCandidate.candidatePosition = target.LocalToGlobal(evt.mousePosition);

					m_DataSource.AddElement(m_EdgeDataCandidate);

					return EventPropagation.Stop;

				case EventType.MouseDrag:
					if (this.HasCapture())
					{
						m_EdgeDataCandidate.candidatePosition = target.LocalToGlobal(evt.mousePosition);
						return EventPropagation.Stop;
					}
					break;

				case EventType.MouseUp:
					if (this.HasCapture() && evt.button == (int)activateButton)
					{
						this.ReleaseCapture();

						foreach (var compatibleAnchor in m_CompatibleAnchors)
						{
							compatibleAnchor.highlight = false;

							if (m_GraphView != null)
							{
								GraphElement anchorElement = m_GraphView.allElements.OfType<GraphElement>().First(e => e.dataProvider == (Object)compatibleAnchor);
								if (anchorElement != null)
								{
									if (anchorElement.globalBound.Contains(target.LocalToGlobal(evt.mousePosition)))
									{
										m_EdgeDataCandidate.right = compatibleAnchor;
									}
								}
							}
						}
						m_CompatibleAnchors.Clear();

						if (m_EdgeDataCandidate != null && m_DataSource != null)
						{
							// Not a candidate anymore, let's see if we're actually going to add it to parent
							m_EdgeDataCandidate.candidate = false;

							m_DataSource.RemoveElement(m_EdgeDataCandidate);
							
							if (m_EdgeDataCandidate.right != null)
							{
								m_DataSource.AddElement(m_EdgeDataCandidate);
								m_EdgeDataCandidate.left.connected = true;
								m_EdgeDataCandidate.right.connected = true;
							}
						}

						m_EdgeDataCandidate = null;
						m_DataSource = null;

						return EventPropagation.Stop;
					}
					break;
			}

			return this.HasCapture() ? EventPropagation.Stop : EventPropagation.Continue;
		}
	}
}
