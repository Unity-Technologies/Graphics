using System;
using System.Collections.Generic;
using System.Linq;
using RMGUI.GraphView;
using UnityEngine;
using UnityEngine.RMGUI;
using UnityEngine.RMGUI.StyleEnums;
using UnityEngine.RMGUI.StyleSheets;

namespace UnityEditor.VFX.UI
{
	class NodeBlockContainer : VisualContainer
	{
		// ISelection implementation
		public List<ISelectable> selection { get; protected set; }

		public NodeBlockContainer()
		{
			selection = new List<ISelectable>();
		}

	    public EventPropagation SelectAll()
	    {
            ClearSelection();

            var nodeBlocks = children.OfType<VFXNodeBlockUI>().ToList();
	        foreach (var c in nodeBlocks)
	        {
	            AddToSelection(c);
	        }

	        return EventPropagation.Stop;
	    }

	    // functions to ISelection extensions
		public virtual void AddToSelection(ISelectable selectable)
		{
			var nodeBlock = selectable as VFXNodeBlockUI;
			if (nodeBlock != null && nodeBlock.presenter != null)
				nodeBlock.presenter.selected = true;
			selection.Add(selectable);
			this.Touch(ChangeType.Repaint);
		}

		public virtual void RemoveFromSelection(ISelectable selectable)
		{
			var nodeBlock = selectable as VFXNodeBlockUI;
			if (nodeBlock != null && nodeBlock.presenter != null)
				nodeBlock.presenter.selected = false;
			selection.Remove(selectable);
			this.Touch(ChangeType.Repaint);
		}

		public virtual void ClearSelection()
		{
			foreach (var nodeBlock in selection.OfType<VFXNodeBlockUI>())
			{
				if (nodeBlock.presenter != null)
					nodeBlock.presenter.selected = false;
			}

			selection.Clear();
			this.Touch(ChangeType.Repaint);
		}
	}

	class VFXContextUI : GraphElement
	{
		// TODO: Unused except for debugging
		const string RectColorProperty = "rect-color";

		Label m_Title;
		VisualContainer m_FlowInputConnectorContainer;
		VisualContainer m_FlowOutputConnectorContainer;
		VisualContainer m_NodeContainer;
		NodeBlockContainer m_NodeBlockContainer;

		protected GraphViewTypeMapper typeMapper { get; set; }

		public VFXContextUI()
		{
			pickingMode = PickingMode.Ignore;
            phaseInterest = EventPhase.BubbleUp;

			m_FlowInputConnectorContainer = new VisualContainer()
			{
				name = "FlowInputs",
				pickingMode = PickingMode.Ignore,
				classList = new ClassList("FlowContainer")
			};

			m_FlowOutputConnectorContainer = new VisualContainer()
			{
				name = "FlowOutputs",
				pickingMode = PickingMode.Ignore,
				classList = new ClassList("FlowContainer")
			};

			m_NodeContainer = new VisualContainer()
			{
				name = "NodeContents"
			};

            AddManipulator(new ShortcutHandler(
                new Dictionary<Event, ShortcutDelegate>
				{
					{Event.KeyboardEvent("#a"), SelectAll},
					{Event.KeyboardEvent("#n"), ClearSelection},
                    {Event.KeyboardEvent("delete"), DeleteSelection},
				}));

			AddChild(m_FlowInputConnectorContainer);
			AddChild(m_NodeContainer);
			AddChild(m_FlowOutputConnectorContainer);
			m_Title = new Label(new GUIContent("Title")) { name = "Title" };
			m_NodeContainer.AddChild(m_Title);

			m_NodeBlockContainer = new NodeBlockContainer()
			{
				pickingMode = PickingMode.Ignore
			};

			m_NodeBlockContainer.AddManipulator(new ClickSelector());

			m_NodeContainer.AddChild(m_NodeBlockContainer);

			m_NodeContainer.AddManipulator(new ContextualMenu((evt, customData) =>
			{
				var menu = new GenericMenu();

				// Needs to have the model here to filter compatible node blocks
				var contextType = GetPresenter<VFXContextPresenter>().Model.ContextType;
				foreach (var desc in VFXLibrary.GetBlocks())
					if ((desc.CompatibleContexts & contextType) != 0)
						menu.AddItem(new GUIContent(desc.Name), false,
									 contentView => AddNodeBlock(-1, desc),
									 this);

				menu.ShowAsContext();
				return EventPropagation.Continue;
			}));

			typeMapper = new GraphViewTypeMapper();
            typeMapper[typeof(VFXNodeBlockPresenter)] = typeof(VFXNodeBlockUI);

			classList = new ClassList("VFXContext");
		}

	    public EventPropagation SelectAll()
	    {
	        return m_NodeBlockContainer.SelectAll();
	    }

		public EventPropagation ClearSelection()
		{
			m_NodeBlockContainer.ClearSelection();
			return EventPropagation.Stop;
		}

	    public override EventPropagation Select(VisualContainer selectionContainer, Event evt)
	    {
 	        var clearNodeBlockSelection = false;
            var gView = this.GetFirstAncestorOfType<GraphView>();
 	        if (gView != null && gView.selection.Contains(this) && !evt.control)
                clearNodeBlockSelection = true;

            var result = base.Select(selectionContainer, evt);

            if (clearNodeBlockSelection)
                m_NodeBlockContainer.ClearSelection();

	        return result;
	    }

        public EventPropagation DeleteSelection()
	    {
            foreach (var nodeBlock in m_NodeBlockContainer.selection.OfType<VFXNodeBlockUI>().ToList())
	        {
                m_NodeBlockContainer.RemoveFromSelection(nodeBlock);
                m_NodeBlockContainer.RemoveChild(nodeBlock);
                VFXContextPresenter contextPresenter = GetPresenter<VFXContextPresenter>();
	            contextPresenter.nodeBlockPresenters.Remove(nodeBlock.GetPresenter<VFXNodeBlockPresenter>());
                this.Touch(ChangeType.Layout);
	        }

            return EventPropagation.Stop;
	    }

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

		private void AddNodeBlock(int index, VFXBlockDesc desc)
		{
			VFXContextPresenter presenter = GetPresenter<VFXContextPresenter>();
			presenter.AddNodeBlock(index, desc);
		}


		private void InstantiateNodeBlock(VFXNodeBlockPresenter nodeBlockPresenter)
		{
			// call factory
            GraphElement newElem = typeMapper.Create(nodeBlockPresenter);

			if (newElem == null)
			{
				return;
			}

			newElem.SetPosition(nodeBlockPresenter.position);
			newElem.presenter = nodeBlockPresenter;
			m_NodeBlockContainer.AddChild(newElem);

			newElem.presenter.selected = nodeBlockPresenter.selected;
		}

		public void RefreshContext()
		{
			VFXContextPresenter contextPresenter = GetPresenter<VFXContextPresenter>();

            var nodeBlocks = m_NodeBlockContainer.children.OfType<VFXNodeBlockUI>().ToList();

			// Process removals
			foreach (var c in nodeBlocks)
			{
				// been removed?
				var nb = c as VFXNodeBlockUI;
				var nodeBlock = contextPresenter.nodeBlockPresenters.OfType<VFXNodeBlockPresenter>().FirstOrDefault(a => a == nb.GetPresenter<VFXNodeBlockPresenter>());
				if (nodeBlock == null)
				{
					m_NodeBlockContainer.RemoveFromSelection(nb);
					m_NodeBlockContainer.RemoveChild(nb);
				}
			}

			// Process additions
			foreach (var nodeblockPresenter in contextPresenter.nodeBlockPresenters)
			{
 				var nodeBlock = nodeBlocks.OfType<VFXNodeBlockUI>().FirstOrDefault(a => a.GetPresenter<VFXNodeBlockPresenter>() == nodeblockPresenter);
				if (nodeBlock == null)
				{
					InstantiateNodeBlock(nodeblockPresenter);
				}
			}
		}

		public override void OnDataChanged()
		{
			base.OnDataChanged();

			VFXContextPresenter presenter = GetPresenter<VFXContextPresenter>();
		    if (presenter == null || presenter.Model == null)
		        return;

			// Recreate label with good name // Dirty
			m_Title.content.text = presenter.Model.Desc.Name;

            VFXContextType contextType = presenter.Model.ContextType;

			RemoveFromClassList("init", "update", "output");

			switch (contextType)
			{
                case VFXContextType.kInit: AddToClassList("init"); break;
                case VFXContextType.kUpdate: AddToClassList("update"); break;
                case VFXContextType.kOutput: AddToClassList("output"); break;
				default: throw new Exception();
			}

			if (presenter.Model.Position != presenter.position.position)
			{
				presenter.Model.Position = presenter.position.position;
				// Needs to make the model dirty
			}

			m_FlowInputConnectorContainer.ClearChildren();
			m_FlowOutputConnectorContainer.ClearChildren();

			foreach (var inanchorpresenter in presenter.inputAnchors)
			{
				var edge = VFXFlowAnchor.Create<VFXFlowEdgePresenter>(inanchorpresenter);
				m_FlowInputConnectorContainer.AddChild(edge);
			}

			foreach (var outanchorpresenter in presenter.outputAnchors)
			{
				var edge = VFXFlowAnchor.Create<VFXFlowEdgePresenter>(outanchorpresenter);
                m_FlowOutputConnectorContainer.AddChild(edge);
			}

			RefreshContext();
		}

		public override void DoRepaint(IStylePainter painter)
		{
			base.DoRepaint(painter);
		}

		// TODO: Remove, unused except for debugging
		// Declare new USS rect-color and use it
		public override void OnStylesResolved(VisualElementStyles elementStyles)
		{
			base.OnStylesResolved(elementStyles);
			elementStyles.ApplyCustomProperty(RectColorProperty, ref m_RectColor);
		}

		// TODO: Remove, unused except for debugging
		StyleProperty<Color> m_RectColor;
		Color rectColor { get { return m_RectColor.GetOrDefault(Color.magenta); } }
	}
}
