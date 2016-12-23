using UnityEngine;
using UnityEngine.RMGUI;

namespace RMGUI.GraphView
{
	class Node : SimpleElement
	{
		readonly VisualContainer m_InputContainer;
		readonly VisualContainer m_OutputContainer;

		protected virtual void SetLayoutClassLists(NodePresenter nodePresenter)
		{
			if (classList.Contains("vertical") || classList.Contains("horizontal"))
			{
				return;
			}

			AddToClassList(nodePresenter.orientation == Orientation.Vertical ? "vertical" : "horizontal");
		}

		public override void OnDataChanged()
		{
			base.OnDataChanged();

			m_OutputContainer.ClearChildren();
			m_InputContainer.ClearChildren();

			var nodePresenter = GetPresenter<NodePresenter>();

			foreach (var anchorPresenter in nodePresenter.inputAnchors)
			{
				m_InputContainer.AddChild(NodeAnchor.Create<EdgePresenter>(anchorPresenter));
			}

			foreach (var anchorPresenter in nodePresenter.outputAnchors)
			{
				m_OutputContainer.AddChild(NodeAnchor.Create<EdgePresenter>(anchorPresenter));
			}

			SetLayoutClassLists(nodePresenter);
		}

		public Node()
		{
 			var mainContainer = new VisualContainer()
 			{
 				name = "nodeMain", // for USS&Flexbox
 				pickingMode = PickingMode.Ignore,
 			};
			m_InputContainer = new VisualContainer
			{
				name = "input", // for USS&Flexbox
				pickingMode = PickingMode.Ignore,
			};
			m_OutputContainer = new VisualContainer
			{
				name = "output", // for USS&Flexbox
				pickingMode = PickingMode.Ignore,
			};

			elementTypeColor = new Color(0.9f, 0.9f, 0.9f, 0.5f);

			AddChild(mainContainer);
			mainContainer.AddChild(m_InputContainer);
			mainContainer.AddChild(m_OutputContainer);
		}
	}
}
