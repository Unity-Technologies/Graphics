using System;
using UnityEngine;
using UnityEngine.RMGUI;
using UnityEngine.RMGUI.StyleSheets;

namespace RMGUI.GraphView
{
	internal class NodeAnchor : GraphElement
	{
		private EdgeConnector m_EdgeConnector;

		VisualElement m_ConnectorBox;
		VisualElement m_ConnectorText;

		// TODO This is a workaround to avoid having a generic type for the anchor as generic types mess with USS.
		public static NodeAnchor Create<TEdgePresenter>(NodeAnchorPresenter presenter) where TEdgePresenter : EdgePresenter
		{
			var anchor = new NodeAnchor(presenter) {m_EdgeConnector = new EdgeConnector<TEdgePresenter>()};
			anchor.AddManipulator(anchor.m_EdgeConnector);
			return anchor;
		}

		private NodeAnchor(NodeAnchorPresenter presenter)
		{
			// currently we don't want to be styled as .graphElement since we're contained in a Node
			classList = ClassList.empty;

			m_ConnectorBox = new VisualElement
			{
				name = "connector",
				pickingMode = PickingMode.Ignore
			};
			AddChild(m_ConnectorBox);

			m_ConnectorText = new VisualElement
			{
				name = "type",
				content = new GUIContent(),
				pickingMode = PickingMode.Ignore
			};
			AddChild(m_ConnectorText);

			this.presenter = presenter;
		}

		private void UpdateConnector()
		{
			if (m_EdgeConnector == null)
				return;

			var anchorPresenter = GetPresenter<NodeAnchorPresenter>();

			RemoveManipulator(m_EdgeConnector);
			if (!anchorPresenter.connected || anchorPresenter.direction != Direction.Input)
			{
				AddManipulator(m_EdgeConnector);
			}
		}

		public override void OnDataChanged()
		{
			UpdateConnector();

			var anchorPresenter = GetPresenter<NodeAnchorPresenter>();
			Type anchorType = anchorPresenter.anchorType;
			Type genericClass = typeof(PortSource<>);
			Type constructedClass = genericClass.MakeGenericType(anchorType);
			anchorPresenter.source = Activator.CreateInstance(constructedClass);

			if (anchorPresenter.highlight)
			{
				m_ConnectorBox.AddToClassList("anchorHighlight");
			}
			else
			{
				m_ConnectorBox.RemoveFromClassList("anchorHighlight");
			}

			string anchorName = string.IsNullOrEmpty(anchorPresenter.name) ? anchorType.Name : anchorPresenter.name;
			m_ConnectorText.content.text = anchorName;
			anchorPresenter.capabilities &= ~Capabilities.Selectable;
		}

		public override Vector3 GetGlobalCenter()
		{
			var center = m_ConnectorBox.position.center;
			center = m_ConnectorBox.transform.MultiplyPoint3x4(center);
			return this.LocalToGlobal(center);
		}

		public override bool ContainsPoint(Vector2 localPoint)
		{
			// Here local point comes without position offset...
			localPoint -= position.position;
			return m_ConnectorBox.ContainsPoint(m_ConnectorBox.transform.MultiplyPoint3x4(localPoint));
		}
	}
}
