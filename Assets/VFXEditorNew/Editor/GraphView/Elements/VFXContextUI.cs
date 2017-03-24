using System;
using System.Collections.Generic;
using System.Linq;
using RMGUI.GraphView;
using UnityEngine;
using UnityEngine.Experimental.RMGUI;
using UnityEngine.Experimental.RMGUI.StyleEnums;
using UnityEngine.Experimental.RMGUI.StyleSheets;

namespace UnityEditor.VFX.UI
{
	class BlockContainer : VisualContainer, ISelection
	{
		// ISelection implementation
		public List<ISelectable> selection { get; private set; }


		public BlockContainer()
		{
			selection = new List<ISelectable>();
            clipChildren = false;
		}

		public EventPropagation SelectAll()
		{
			ClearSelection();

            for(int i = 0; i < childrenCount; ++i)
			{
                var child = GetChildAt(0) as VFXBlockUI;
                if( child != null)
				    AddToSelection(child);
			}

			return EventPropagation.Stop;
		}

		// functions to ISelection extensions
		public virtual void AddToSelection(ISelectable selectable)
		{
			var block = selectable as VFXBlockUI;
			if (block != null && block.presenter != null)
				block.presenter.selected = true;
			selection.Add(selectable);
		}

		public virtual void RemoveFromSelection(ISelectable selectable)
		{
			var block = selectable as VFXBlockUI;
			if (block != null && block.presenter != null)
				block.presenter.selected = false;
			selection.Remove(selectable);
		}

		public virtual void ClearSelection()
		{
			foreach (var block in selection.OfType<VFXBlockUI>())
			{
				if (block.presenter != null)
					block.presenter.selected = false;
			}

			selection.Clear();
		}
	}

	class VFXContextUI : GraphElement, IDropTarget
    {
		// TODO: Unused except for debugging
		const string RectColorProperty = "rect-color";

		VisualElement m_Title;
		VisualContainer m_FlowInputConnectorContainer;
		VisualContainer m_FlowOutputConnectorContainer;
		VisualContainer m_NodeContainer;
		BlockContainer m_BlockContainer;

        VisualElement m_DragDisplay;

		protected GraphViewTypeFactory typeFactory { get; set; }

		public VFXContextUI()
        {
            forceNotififcationOnAdd = true;
            pickingMode = PickingMode.Ignore;
			phaseInterest = EventPhase.BubbleUp;

			m_FlowInputConnectorContainer = new VisualContainer()
			{
				name = "FlowInputs",
				pickingMode = PickingMode.Ignore,
			};
            m_FlowInputConnectorContainer.ClearClassList();
            m_FlowInputConnectorContainer.AddToClassList("FlowContainer");
            m_FlowInputConnectorContainer.AddToClassList("Input");

			m_FlowOutputConnectorContainer = new VisualContainer()
			{
				name = "FlowOutputs",
				pickingMode = PickingMode.Ignore
			};
            m_FlowOutputConnectorContainer.ClearClassList();
            m_FlowOutputConnectorContainer.AddToClassList("FlowContainer");
            m_FlowOutputConnectorContainer.AddToClassList("Output");

            m_NodeContainer = new VisualContainer()
			{
				name = "NodeContents"
			};
            m_NodeContainer.clipChildren = false;

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
			m_Title = new VisualElement() { name = "Title" ,text = "Title" };
			m_NodeContainer.AddChild(m_Title);

			m_BlockContainer = new BlockContainer()
			{
				pickingMode = PickingMode.Ignore
			};

			m_BlockContainer.AddManipulator(new ClickSelector());

			m_NodeContainer.AddChild(m_BlockContainer);
            /*
			m_NodeContainer.AddManipulator(new ContextualMenu((evt, customData) =>
			{
				var menu = new GenericMenu();

				// Needs to have the model here to filter compatible node blocks
				var contextType = GetPresenter<VFXContextPresenter>().Model.ContextType;
				foreach (var desc in VFXLibrary.GetBlocks())
					if ((desc.CompatibleContexts & contextType) != 0)
						menu.AddItem(new GUIContent(desc.Name), false,
									 contentView => AddBlock(-1, desc),
									 this);

				menu.ShowAsContext();
				return EventPropagation.Continue;
			}));
            */
			typeFactory = new GraphViewTypeFactory();
			typeFactory[typeof(VFXBlockPresenter)] = typeof(VFXBlockUI);
            typeFactory[typeof(VFXDataInputAnchorPresenter)] = typeof(VFXDataAnchor);
            typeFactory[typeof(VFXDataOutputAnchorPresenter)] = typeof(VFXDataAnchor);

            ClearClassList();
            AddToClassList("VFXContext");

            m_DragDisplay = new VisualElement();
            m_DragDisplay.AddToClassList("dragdisplay");
        }

		public EventPropagation SelectAll()
		{
			return m_BlockContainer.SelectAll();
		}

		public EventPropagation ClearSelection()
		{
			m_BlockContainer.ClearSelection();
			return EventPropagation.Stop;
		}


        public bool CanDrop(IEnumerable<VFXBlockUI> blocks,VFXBlockUI target)
        {
            bool accept = true;
            foreach (var block in blocks)
            {
                if( block == target )
                {
                    accept = false;
                    break;
                }
                if (!GetPresenter<VFXContextPresenter>().model.AcceptChild(block.GetPresenter<VFXBlockPresenter>().Model))
                {
                    accept = false;
                    break;
                }
            }
            return accept;
        }

        public void DraggingBlocks( IEnumerable<VFXBlockUI> blocks,VFXBlockUI target, bool after)
        {
            DragFinished();
            if( ! CanDrop(blocks,target) )
            {
                return;
            }

            Vector2 position;
            if (target != null)
            {
                position = target.position.position;
                if (after)
                {
                    position.y += target.position.height;
                }
            }
            else
            {
                if( after)
                {
                    position.y = m_BlockContainer.position.height;
                }
                else
                {
                    position.y = 0;
                }
            }
            m_DragDisplay.positionTop = position.y;

            if( m_DragDisplay.parent == null)
            {
                m_BlockContainer.AddChild(m_DragDisplay);
            }
        }

        public void DragFinished()
        {
            if (m_DragDisplay.parent != null)
                m_BlockContainer.RemoveChild(m_DragDisplay);
        }


        bool m_DragStarted;



        public bool CanAcceptDrop(List<ISelectable> selection)
        {
            IEnumerable<VFXBlockUI> blocksUI = selection.Select(t => t as VFXBlockUI).Where(t => t != null);

            return CanDrop(blocksUI,null);
        }

        EventPropagation IDropTarget.DragUpdated(Event evt, IEnumerable<ISelectable> selection, IDropTarget dropTarget)
        {
            IEnumerable<VFXBlockUI> blocksUI = selection.Select(t => t as VFXBlockUI).Where(t => t != null);

            DraggingBlocks(blocksUI, null, true);
            if (!m_DragStarted)
            {
                // TODO: Do something on first DragUpdated event (initiate drag)
                m_DragStarted = true;
                AddToClassList("dropping");
            }
            else
            {
                // TODO: Do something on subsequent DragUpdated events
            }

            return EventPropagation.Stop;
        }

        EventPropagation IDropTarget.DragPerform(Event evt, IEnumerable<ISelectable> selection, IDropTarget dropTarget)
        {
            DragFinished();
            IEnumerable<VFXBlockUI> blocksUI = selection.Select(t => t as VFXBlockUI).Where(t => t != null);
            if (!CanDrop(blocksUI, null))
                return EventPropagation.Stop;

            VFXContextPresenter presenter = GetPresenter<VFXContextPresenter>();

            foreach(var blockui in blocksUI)
            {
                VFXBlockPresenter blockPres = blockui.GetPresenter<VFXBlockPresenter>();   
                presenter.AddBlock(-1,blockPres.Model);
            }

            m_DragStarted = false;
            RemoveFromClassList("dropping");

            return EventPropagation.Stop;
        }

        EventPropagation IDropTarget.DragExited()
        {
            // TODO: Do something when current drag is canceled
            DragFinished();
            m_DragStarted = false;

            return EventPropagation.Stop;
        }


        public override EventPropagation Select(VisualContainer selectionContainer, Event evt)
		{
			var clearBlockSelection = false;
			var gView = this.GetFirstAncestorOfType<GraphView>();
			if (gView != null && gView.selection.Contains(this) && !evt.control)
				clearBlockSelection = true;

			var result = base.Select(selectionContainer, evt);

			if (clearBlockSelection)
				m_BlockContainer.ClearSelection();

			return result;
		}

		public EventPropagation DeleteSelection()
		{
			var elementsToRemove = m_BlockContainer.selection.OfType<VFXBlockUI>().ToList();
			foreach (var block in elementsToRemove)
			{
				RemoveBlock(block as VFXBlockUI);
			}

			return (elementsToRemove.Count > 0) ? EventPropagation.Stop : EventPropagation.Continue;
		}

		public override void SetPosition(Rect newPos)
		{
			//if (classList.Contains("vertical"))
			/*{
				base.SetPosition(newPos);
			}
			else*/
			{
				positionType = PositionType.Absolute;
				positionLeft = newPos.x;
				positionTop = newPos.y;
			}
		}

		public void RemoveBlock(VFXBlockUI block)
		{
			if (block == null)
				return;

			VFXContextPresenter contextPresenter = GetPresenter<VFXContextPresenter>();
			contextPresenter.RemoveBlock(block.GetPresenter<VFXBlockPresenter>().Model);
		}

		private void InstantiateBlock(VFXBlockPresenter blockPresenter)
		{
			// call factory
			GraphElement newElem = typeFactory.Create(blockPresenter);

			if (newElem == null)
			{
				return;
			}

			newElem.SetPosition(blockPresenter.position);
			newElem.presenter = blockPresenter;
			m_BlockContainer.AddChild(newElem);

			newElem.presenter.selected = blockPresenter.selected;
		}

		public void RefreshContext()
		{
			VFXContextPresenter contextPresenter = GetPresenter<VFXContextPresenter>();
            var blockPresenters = contextPresenter.blockPresenters;

            // recreate the children list based on the presenter list to keep the order.

            var blocksUIs = new Dictionary<VFXBlockPresenter, VFXBlockUI>();
            for(int i = 0; i < m_BlockContainer.childrenCount; ++i)
            {
                var child = m_BlockContainer.GetChildAt(i) as VFXBlockUI;
                if(child != null)
                    blocksUIs.Add( child.GetPresenter<VFXBlockPresenter>(),child);
            }

            foreach(var kv in blocksUIs)
            {
                m_BlockContainer.RemoveChild(kv.Value);
            }
            foreach(var blockPresenter in blockPresenters)
            {
                VFXBlockUI blockUI;
                if( blocksUIs.TryGetValue(blockPresenter,out blockUI))
                {
                    m_BlockContainer.AddChild(blockUI);
                }
                else
                {
                    InstantiateBlock(blockPresenter);
                }
            }


            

            // Does not guarantee correct ordering
			/*var blocks = m_BlockContainer.children.OfType<VFXBlockUI>().ToList();

			// Process removals
			foreach (var c in blocks)
			{
				// been removed?
				var nb = c as VFXBlockUI;
				var block = contextPresenter.blockPresenters.OfType<VFXBlockPresenter>().FirstOrDefault(a => a == nb.GetPresenter<VFXBlockPresenter>());
				if (block == null)
				{
					m_BlockContainer.RemoveFromSelection(nb);
					m_BlockContainer.RemoveChild(nb);
				}
			}

			// Process additions
			foreach (var blockPresenter in contextPresenter.blockPresenters)
			{
                var block = blocks.OfType<VFXBlockUI>().FirstOrDefault(a => a.GetPresenter<VFXBlockPresenter>() == blockPresenter);
				if (block == null)
				{
                    InstantiateBlock(blockPresenter);
				}
			}*/
		}

		public override void OnDataChanged()
		{
			base.OnDataChanged();

			VFXContextPresenter presenter = GetPresenter<VFXContextPresenter>();
			if (presenter == null || presenter.context == null)
				return;

			// Recreate label with good name // Dirty
            m_Title.text = presenter.context.name;

            VFXContextType contextType = presenter.context.contextType;

            RemoveFromClassList("init");
            RemoveFromClassList("update");
            RemoveFromClassList("output");

			switch (contextType)
			{
				case VFXContextType.kInit: AddToClassList("init"); break;
				case VFXContextType.kUpdate: AddToClassList("update"); break;
				case VFXContextType.kOutput: AddToClassList("output"); break;
				default: throw new Exception();
			}

            presenter.context.position = presenter.position.position;


            HashSet<VisualElement> newInAnchors = new HashSet<VisualElement>();

            foreach (var inanchorpresenter in presenter.inputAnchors)
            {
                var existing = m_FlowInputConnectorContainer.Select(t => t as VFXFlowAnchor).FirstOrDefault(t => t.presenter == inanchorpresenter);
                if (existing == null)
                {
                    var anchor = VFXFlowAnchor.Create<VFXFlowEdgePresenter>(inanchorpresenter);
                    m_FlowInputConnectorContainer.AddChild(anchor);
                    newInAnchors.Add(anchor);
                }
                else
                {
                    newInAnchors.Add(existing);
                }
            }

            foreach(var nonLongerExistingAnchor in m_FlowInputConnectorContainer.Where(t=>!newInAnchors.Contains(t)).ToList()) // ToList to make a copy because the enumerable will change when we delete
            {
                m_FlowInputConnectorContainer.RemoveChild(nonLongerExistingAnchor);
            }



            HashSet<VisualElement> newOutAnchors = new HashSet<VisualElement>();

            foreach (var outanchorpresenter in presenter.outputAnchors)
			{
                var existing = m_FlowOutputConnectorContainer.Select(t => t as VFXFlowAnchor).FirstOrDefault(t => t.presenter == outanchorpresenter);
                if (existing == null)
                {
                    var anchor = VFXFlowAnchor.Create<VFXFlowEdgePresenter>(outanchorpresenter);
                    m_FlowOutputConnectorContainer.AddChild(anchor);
                    newOutAnchors.Add(anchor);
                }
                else
                {
                    newOutAnchors.Add(existing);
                }
            }

            foreach (var nonLongerExistingAnchor in m_FlowOutputConnectorContainer.Where(t => !newOutAnchors.Contains(t)).ToList()) // ToList to make a copy because the enumerable will change when we delete
            {
                m_FlowOutputConnectorContainer.RemoveChild(nonLongerExistingAnchor);
            }


            RefreshContext();


            if (m_PopupManipulator != null)
            {
                m_NodeContainer.RemoveManipulator(m_PopupManipulator);
            }
            m_PopupManipulator = new FilterPopup(new VFXBlockProvider(presenter, (d, mPos) =>
            {
                GetPresenter<VFXContextPresenter>().AddBlock(-1, d.CreateInstance());
            }));
            m_NodeContainer.AddManipulator(m_PopupManipulator);

        }

        FilterPopup m_PopupManipulator;

        public override void DoRepaint(IStylePainter painter)
		{
			base.DoRepaint(painter);
		}

        // TODO: Remove, unused except for debugging
        // Declare new USS rect-color and use it
        public override void OnStylesResolved(ICustomStyles styles)
        {
            base.OnStylesResolved(styles);
            styles.ApplyCustomProperty(RectColorProperty, ref m_RectColor);
		}

		// TODO: Remove, unused except for debugging
		Style<Color> m_RectColor;
		Color rectColor { get { return m_RectColor.GetSpecifiedValueOrDefault(Color.magenta); } }
	}
}
