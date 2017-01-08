using System.Linq;
using UnityEngine;
using UnityEngine.RMGUI;
using UnityEngine.RMGUI.StyleEnums;
using UnityEngine.RMGUI.StyleSheets;

namespace RMGUI.GraphView
{
	public class Node : GraphElement
	{
		protected readonly VisualContainer m_MainContainer;
		protected readonly VisualContainer m_LeftContainer;
		protected readonly VisualContainer m_RightContainer;
		protected readonly VisualContainer m_TitleContainer;
		protected readonly VisualContainer m_InputContainer;
		protected readonly VisualContainer m_OutputContainer;

		protected readonly Label m_TitleLabel;
		protected readonly Button m_CollapseButton;

		public override void SetPosition(Rect newPos)
		{
			if (classList.Contains("vertical"))
			{
				base.SetPosition(newPos);
			}
			else
			{
				positionType = PositionType.Absolute;
				positionLeft = newPos.x;
				positionTop = newPos.y;
			}
		}

		protected virtual void SetLayoutClassLists(NodePresenter nodePresenter)
		{
			if (classList.Contains("vertical") || classList.Contains("horizontal"))
			{
				return;
			}

			if (nodePresenter.orientation == Orientation.Vertical)
			{
				if (m_LeftContainer.children.Contains(m_TitleContainer))
				{
					m_LeftContainer.RemoveChild(m_TitleContainer);
				}
			}
			else
			{
				if (!m_LeftContainer.children.Contains(m_TitleContainer))
				{
					m_LeftContainer.InsertChild(0, m_TitleContainer);
				}
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

			bool hasOutput = false;
			foreach (var anchorPresenter in nodePresenter.outputAnchors)
			{
				m_OutputContainer.AddChild(NodeAnchor.Create<EdgePresenter>(anchorPresenter));
				hasOutput = true;
			}

			// Show output container only if we have one or more child
			if (hasOutput)
			{
				if (!m_MainContainer.children.Contains(m_RightContainer))
				{
					m_MainContainer.InsertChild(0, m_RightContainer);
				}
			}
			else
			{
				if (m_MainContainer.children.Contains(m_RightContainer))
				{
					m_MainContainer.RemoveChild(m_RightContainer);
				}
			}

			m_TitleLabel.content.text = nodePresenter.title;

			SetLayoutClassLists(nodePresenter);
		}

		public Node()
		{
			m_MainContainer = new VisualContainer()
			{
				name = "pane",
				pickingMode = PickingMode.Ignore,
			};
			m_LeftContainer = new VisualContainer
			{
				name = "left",
				pickingMode = PickingMode.Ignore,
			};
			m_RightContainer = new VisualContainer
			{
				name = "right",
				pickingMode = PickingMode.Ignore,
			};
			m_TitleContainer = new VisualContainer
			{
				name = "title",
				pickingMode = PickingMode.Ignore,
			};
			m_InputContainer = new VisualContainer
			{
				name = "input",
				pickingMode = PickingMode.Ignore,
			};
			m_OutputContainer = new VisualContainer
			{
				name = "output",
				pickingMode = PickingMode.Ignore,
			};

			m_TitleLabel = new Label(new GUIContent(""));
			m_CollapseButton = new Button(() => {})
			{
				content = new GUIContent("collapse")
			};

			elementTypeColor = new Color(0.9f, 0.9f, 0.9f, 0.5f);

			AddChild(m_MainContainer);
			m_MainContainer.AddChild(m_LeftContainer);
			m_MainContainer.AddChild(m_RightContainer);

			m_TitleContainer.AddChild(m_TitleLabel);
			m_TitleContainer.AddChild(m_CollapseButton);

			m_LeftContainer.AddChild(m_InputContainer);
			m_RightContainer.AddChild(m_OutputContainer);

			classList = new ClassList("node");
		}
	}
}
