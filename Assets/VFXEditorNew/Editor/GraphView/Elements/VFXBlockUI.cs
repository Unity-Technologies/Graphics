using System.Collections.Generic;
using RMGUI.GraphView;
using UnityEngine;
using UnityEngine.RMGUI;
using UnityEngine.RMGUI.StyleSheets;
using System.Reflection;
using UnityEngine.RMGUI.StyleEnums;

namespace UnityEditor.VFX.UI
{
	class VFXBlockUI : Node
	{
        public GraphViewTypeFactory typeFactory { get; set; }

        public VFXBlockUI()
        {
            forceNotififcationOnAdd = true;
            pickingMode = PickingMode.Position;

			AddManipulator(new SelectionDropper(HandleDropEvent));
            clipChildren = false;
            leftContainer.alignContent = Align.Stretch;

            leftContainer.clipChildren = false;
            leftContainer.parent.clipChildren = false;
            inputContainer.clipChildren = false;
            clipChildren = false;
        }

		// This function is a placeholder for common stuff to do before we delegate the action to the drop target
		private EventPropagation HandleDropEvent(Event evt, List<ISelectable> selection, IDropTarget dropTarget)
		{
			if (dropTarget == null)
				return EventPropagation.Continue;

			switch (evt.type)
			{
				case EventType.DragUpdated:
					return dropTarget.DragUpdated(evt, selection, dropTarget);
				case EventType.DragExited:
					return dropTarget.DragExited();
				case EventType.DragPerform:
					return dropTarget.DragPerform(evt, selection, dropTarget);
			}

			return EventPropagation.Stop;
        }
        public override NodeAnchor InstantiateNodeAnchor(NodeAnchorPresenter presenter)
        {
            return VFXDataAnchor.Create<VFXDataEdgePresenter>(presenter as VFXDataAnchorPresenter);
        }

        public override EventPropagation Select(VisualContainer selectionContainer, Event evt)
		{
			BlockContainer blockContainer = selectionContainer as BlockContainer;
			if (blockContainer == null || blockContainer != parent || !IsSelectable())
				return EventPropagation.Continue;

			// TODO: Get rid of this hack (parent.parent) to reach contextUI
			// Make sure we select the container context node
			var contextUI = blockContainer.parent.parent as VFXContextUI;
			if (contextUI != null)
			{
				var gView = this.GetFirstAncestorOfType<GraphView>();
				if (gView != null && !gView.selection.Contains(contextUI))
				{
					gView.ClearSelection();
					gView.AddToSelection(contextUI);
				}
			}

			if (blockContainer.selection.Contains(this))
			{
				if (evt.control)
				{
					blockContainer.RemoveFromSelection(this);
					return EventPropagation.Stop;
				}
				return EventPropagation.Continue;
			}

			if (!evt.control)
				blockContainer.ClearSelection();
			blockContainer.AddToSelection(this);

			// TODO: Reset to EventPropagation.Continue when Drag&Drop is supported
			return EventPropagation.Continue;
		}

		// On purpose -- until we support Drag&Drop I suppose
		public override void SetPosition(Rect newPos)
		{
        }


        public override void OnDataChanged()
		{
            base.OnDataChanged();
			var presenter = GetPresenter<VFXBlockPresenter>();

			if (presenter == null)
				return;

			if (presenter.selected)
			{
				AddToClassList("selected");
			}
			else
			{
				RemoveFromClassList("selected");
			}

			SetPosition(presenter.position);
            presenter.Model.collapsed = !presenter.expanded;
        }


        public override void DoRepaint(IStylePainter painter)
		{
			base.DoRepaint(painter);
		}
	}
}
