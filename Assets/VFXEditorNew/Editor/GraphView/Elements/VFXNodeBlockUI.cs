using RMGUI.GraphView;
using UnityEngine;
using UnityEngine.RMGUI;
using UnityEngine.RMGUI.StyleSheets;

namespace UnityEditor.VFX.UI
{
    class VFXNodeBlockUI : GraphElement
    {
        static int s_Counter = 0;

	    Label m_Title;
        VisualContainer m_SlotContainer;

		public VFXNodeBlockUI()
        {
            pickingMode = PickingMode.Position;
            classList = ClassList.empty;

            m_SlotContainer = new VisualContainer()
            {
                name = "SlotContainer"
            };

		    s_Counter++;

		    m_Title = new Label(new GUIContent("")) {name = "Title"};
			AddChild(m_Title);
            AddChild(m_SlotContainer);
        }

        public override EventPropagation Select(VisualContainer selectionContainer, Event evt)
        {
            NodeBlockContainer nodeBlockContainer = selectionContainer as NodeBlockContainer;
			if (nodeBlockContainer == null || nodeBlockContainer != parent || !IsSelectable())
				return EventPropagation.Continue;

            // TODO: Get rid of this hack (parent.parent) to reach contextUI
            // Make sure we select the container context node
            var contextUI = nodeBlockContainer.parent.parent as VFXContextUI;
            if (contextUI != null)
            {
                var gView = this.GetFirstAncestorOfType<GraphView>();
                if (gView != null && !gView.selection.Contains(contextUI))
                {
                    gView.ClearSelection();
                    gView.AddToSelection(contextUI);
                }
            }

            if (nodeBlockContainer.selection.Contains(this))
			{
				if (evt.control)
				{
					nodeBlockContainer.RemoveFromSelection(this);
					return EventPropagation.Stop;
				}
			}

			if (!evt.control)
				nodeBlockContainer.ClearSelection();
			nodeBlockContainer.AddToSelection(this);

            // TODO: Reset to EventPropagation.Continue when Drag&Drop is supported
			return EventPropagation.Stop;
        }

        // On purpose -- until we support Drag&Drop I suppose
		public override void SetPosition(Rect newPos)
		{
		}

        public override void OnDataChanged()
        {
			var presenter = GetPresenter<VFXNodeBlockPresenter>();

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

			m_Title.content.text = presenter.Model.Desc.Name + " " + s_Counter;

			SetPosition(presenter.position);
		}

        public override void DoRepaint(IStylePainter painter)
        {
            base.DoRepaint(painter);
        }
    }
}
