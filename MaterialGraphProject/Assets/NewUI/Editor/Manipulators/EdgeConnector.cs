using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.RMGUI;

namespace RMGUI.GraphView
{
	internal class EdgeConnector<TEdgeData> : MouseManipulator where TEdgeData : EdgeData
	{
		private readonly List<IConnector> m_CompatibleAnchors = new List<IConnector>();
		private TEdgeData m_EdgeDataCandidate;

		private IGraphElementDataSource m_DataSource;
		private GraphView m_GraphView;

		public EdgeConnector()
		{
			activateButton = MouseButton.LeftMouse;
		}

		public override EventPropagation HandleEvent(Event evt, VisualElement finalTarget)
		{
			switch (evt.type)
			{
				case EventType.MouseDown:
					if (!CanStartManipulation(evt))
					{
						break;
					}

					IConnector cnx = null;
					var graphElement = finalTarget as GraphElement;
					if (graphElement != null && graphElement.dataProvider != null)
					{
						GraphElementData data = graphElement.dataProvider;
						cnx = (IConnector)data;
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
					foreach (var toCnx in m_GraphView.allChildren.OfType<GraphElement>()
																 .Select(e => e.dataProvider)
																 .OfType<IConnector>()
																 .Where(c => c.IsConnectable() &&
																			 c.orientation == cnx.orientation &&
																			 c.direction != cnx.direction &&
																			 nodeAdapter.GetAdapter(c.source, cnx.source) != null))
					{
						toCnx.highlight = true;
						m_CompatibleAnchors.Add(toCnx);
					}

					m_EdgeDataCandidate = ScriptableObject.CreateInstance<TEdgeData>();

					m_EdgeDataCandidate.position = new Rect(0, 0, 1, 1);

					bool startFromOutput = (cnx.direction == Direction.Output);
					if (startFromOutput)
					{
						m_EdgeDataCandidate.output = graphElement.dataProvider as IConnector;
						m_EdgeDataCandidate.input = null;
					}
					else
					{
						m_EdgeDataCandidate.output = null;
						m_EdgeDataCandidate.input = graphElement.dataProvider as IConnector;
					}
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
					if (CanStopManipulation(evt))
					{
						this.ReleaseCapture();
						IConnector endConnector = null;

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
										endConnector = compatibleAnchor;
									}
								}
							}
						}
						m_CompatibleAnchors.Clear();

						if (m_EdgeDataCandidate != null && m_DataSource != null)
						{
							if (endConnector == null)
							{
								m_DataSource.RemoveElement(m_EdgeDataCandidate);
							}
							else
							{
								if (m_EdgeDataCandidate.output == null)
								{
									m_EdgeDataCandidate.output = endConnector;
								}
								else
								{
									m_EdgeDataCandidate.input = endConnector;
								}
								m_EdgeDataCandidate.output.Connect(m_EdgeDataCandidate);
								m_EdgeDataCandidate.input.Connect(m_EdgeDataCandidate);
							}

							m_EdgeDataCandidate.candidate = false;
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
