using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.RMGUI;
using UnityEngine.RMGUI.StyleEnums;
using UnityEngine.RMGUI.StyleSheets;

namespace RMGUI.GraphView
{
	public class Node : GraphElement
	{
		protected virtual VisualContainer mainContainer { get; private set; }
		protected virtual VisualContainer leftContainer { get; private set; }
		protected virtual VisualContainer rightContainer { get; private set; }
		protected virtual VisualContainer titleContainer { get; private set; }
		protected virtual VisualContainer inputContainer { get; private set; }
		protected virtual VisualContainer outputContainer { get; private set; }

		private readonly Label m_TitleLabel;
		private readonly Button m_CollapseButton;

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
				if (leftContainer.children.Contains(titleContainer))
				{
					leftContainer.RemoveChild(titleContainer);
				}
			}
			else
			{
				if (!leftContainer.children.Contains(titleContainer))
				{
					leftContainer.InsertChild(0, titleContainer);
				}
			}

			AddToClassList(nodePresenter.orientation == Orientation.Vertical ? "vertical" : "horizontal");
		}

		public void RefreshAnchors()
		{
			var nodePresenter = GetPresenter<NodePresenter>();

			var currentInputs = inputContainer.allChildren.OfType<NodeAnchor>().ToList();
			var currentOutputs = outputContainer.allChildren.OfType<NodeAnchor>().ToList();

			outputContainer.ClearChildren();
			inputContainer.ClearChildren();

			foreach (var anchorPresenter in nodePresenter.inputAnchors)
			{
				var anchor = currentInputs.FirstOrDefault(a => a.GetPresenter<NodeAnchorPresenter>() == anchorPresenter);
				if (anchor == null)
				{
					anchor = NodeAnchor.Create<EdgePresenter>(anchorPresenter);
				}
				inputContainer.AddChild(anchor);
				if (nodePresenter.expanded || anchorPresenter.connected)
				{
					anchor.paintFlags &= ~PaintFlags.Invisible;
					anchor.RemoveFromClassList("hidden");
				}
				else
				{
					anchor.paintFlags |= PaintFlags.Invisible;
					anchor.AddToClassList("hidden");
				}
			}

			bool hasOutput = false;
			foreach (var anchorPresenter in nodePresenter.outputAnchors)
			{
				var anchor = currentOutputs.FirstOrDefault(a => a.GetPresenter<NodeAnchorPresenter>() == anchorPresenter);
				if (anchor == null)
				{
					anchor = NodeAnchor.Create<EdgePresenter>(anchorPresenter);
				}
				outputContainer.AddChild(anchor);
				if (nodePresenter.expanded || anchorPresenter.connected)
				{
					anchor.paintFlags &= ~PaintFlags.Invisible;
					anchor.RemoveFromClassList("hidden");
					hasOutput = true;
				}
				else
				{
					anchor.paintFlags |= PaintFlags.Invisible;
					anchor.AddToClassList("hidden");
				}
			}

			// Show output container only if we have one or more child
			if (hasOutput)
			{
				if (!mainContainer.children.Contains(rightContainer))
				{
					mainContainer.AddChild(rightContainer);
				}
			}
			else
			{
				if (mainContainer.children.Contains(rightContainer))
				{
					mainContainer.RemoveChild(rightContainer);
				}
			}
		}

		public override void OnDataChanged()
		{
			base.OnDataChanged();

			var nodePresenter = GetPresenter<NodePresenter>();

			RefreshAnchors();

			m_TitleLabel.content.text = nodePresenter.title;

			m_CollapseButton.content.text = nodePresenter.expanded ? "collapse" : "expand";

			SetLayoutClassLists(nodePresenter);
		}

		protected virtual void ToggleCollapse()
		{
			var nodePresenter = GetPresenter<NodePresenter>();
			nodePresenter.expanded = !nodePresenter.expanded;
		}

		public Node()
		{
			mainContainer = new VisualContainer()
			{
				name = "pane",
				pickingMode = PickingMode.Ignore,
			};
			leftContainer = new VisualContainer
			{
				name = "left",
				pickingMode = PickingMode.Ignore,
			};
			rightContainer = new VisualContainer
			{
				name = "right",
				pickingMode = PickingMode.Ignore,
			};
			titleContainer = new VisualContainer
			{
				name = "title",
				pickingMode = PickingMode.Ignore,
			};
			inputContainer = new VisualContainer
			{
				name = "input",
				pickingMode = PickingMode.Ignore,
			};
			outputContainer = new VisualContainer
			{
				name = "output",
				pickingMode = PickingMode.Ignore,
			};

			m_TitleLabel = new Label(new GUIContent(""));
			m_CollapseButton = new Button(ToggleCollapse)
			{
				content = new GUIContent("collapse")
			};

			elementTypeColor = new Color(0.9f, 0.9f, 0.9f, 0.5f);

			AddChild(mainContainer);
			mainContainer.AddChild(leftContainer);
			mainContainer.AddChild(rightContainer);

			titleContainer.AddChild(m_TitleLabel);
			titleContainer.AddChild(m_CollapseButton);

			leftContainer.AddChild(inputContainer);
			rightContainer.AddChild(outputContainer);

			classList = new ClassList("node");
		}
	}
}
