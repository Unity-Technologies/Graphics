using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.RMGUI;

namespace RMGUI.GraphView
{
	internal abstract class EdgeConnector : MouseManipulator
	{ }

	internal class EdgeConnector<TEdgePresenter> : EdgeConnector where TEdgePresenter : EdgePresenter
	{
		private List<NodeAnchorPresenter> m_CompatibleAnchors;
		private TEdgePresenter m_EdgePresenterCandidate;

		private GraphViewPresenter m_GraphViewPresenter;
		private GraphView m_GraphView;

		private static NodeAdapter s_nodeAdapter = new NodeAdapter();

		public EdgeConnector()
		{
			activateButtons[(int)MouseButton.LeftMouse] = true;
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

					var graphElement = finalTarget as NodeAnchor;
					if (graphElement == null)
					{
						break;
					}

					NodeAnchorPresenter startAnchor = graphElement.GetPresenter<NodeAnchorPresenter>();
					m_GraphView = graphElement.GetFirstAncestorOfType<GraphView>();

					if (startAnchor == null || m_GraphView == null)
					{
						break;
					}

					m_GraphViewPresenter = m_GraphView.presenter;
					if (m_GraphViewPresenter == null)
					{
						break;
					}

					this.TakeCapture();

					// get all available connectors
					m_CompatibleAnchors = m_GraphView.allChildren
						.OfType<NodeAnchor>()
						.Select(na => na.GetPresenter<NodeAnchorPresenter>())
						.Where(nap => nap.IsConnectable() &&
									  nap.orientation == startAnchor.orientation &&
									  nap.direction != startAnchor.direction &&
									  s_nodeAdapter.GetAdapter(nap.source, startAnchor.source) != null)
						.ToList();

					foreach (var compatibleAnchor in m_CompatibleAnchors)
					{
						compatibleAnchor.highlight = true;
					}

					m_EdgePresenterCandidate = ScriptableObject.CreateInstance<TEdgePresenter>();

					m_EdgePresenterCandidate.position = new Rect(0, 0, 1, 1);

					bool startFromOutput = (startAnchor.direction == Direction.Output);
					if (startFromOutput)
					{
						m_EdgePresenterCandidate.output = graphElement.GetPresenter<NodeAnchorPresenter>();
						m_EdgePresenterCandidate.input = null;
					}
					else
					{
						m_EdgePresenterCandidate.output = null;
						m_EdgePresenterCandidate.input = graphElement.GetPresenter<NodeAnchorPresenter>();
					}
					m_EdgePresenterCandidate.candidate = true;
					m_EdgePresenterCandidate.candidatePosition = target.LocalToGlobal(evt.mousePosition);

					m_GraphViewPresenter.AddTempElement(m_EdgePresenterCandidate);

					return EventPropagation.Stop;

				case EventType.MouseDrag:
					if (this.HasCapture())
					{
						m_EdgePresenterCandidate.candidatePosition = target.LocalToGlobal(evt.mousePosition);
						return EventPropagation.Stop;
					}
					break;

				case EventType.MouseUp:
					if (CanStopManipulation(evt))
					{
						this.ReleaseCapture();
						NodeAnchorPresenter endAnchor = null;

						foreach (var compatibleAnchor in m_CompatibleAnchors)
						{
							compatibleAnchor.highlight = false;

							if (m_GraphView != null)
							{
								NodeAnchor anchorElement = m_GraphView.allElements.OfType<NodeAnchor>().First(e => e.GetPresenter<NodeAnchorPresenter>() == compatibleAnchor);
								if (anchorElement != null)
								{
									if (anchorElement.globalBound.Contains(target.LocalToGlobal(evt.mousePosition)))
									{
										endAnchor = compatibleAnchor;
									}
								}
							}
						}

						m_GraphViewPresenter.RemoveTempElement(m_EdgePresenterCandidate);
						if (m_EdgePresenterCandidate != null && m_GraphViewPresenter != null)
						{
							if (endAnchor != null)
							{
								if (m_EdgePresenterCandidate.output == null)
								{
									m_EdgePresenterCandidate.output = endAnchor;
								}
								else
								{
									m_EdgePresenterCandidate.input = endAnchor;
								}
								m_EdgePresenterCandidate.output.Connect(m_EdgePresenterCandidate);
								m_EdgePresenterCandidate.input.Connect(m_EdgePresenterCandidate);

								m_GraphViewPresenter.AddElement(m_EdgePresenterCandidate);
							}

							m_EdgePresenterCandidate.candidate = false;
						}

						m_EdgePresenterCandidate = null;
						m_GraphViewPresenter = null;

						return EventPropagation.Stop;
					}
					break;
			}

			return this.HasCapture() ? EventPropagation.Stop : EventPropagation.Continue;
		}
	}
}
