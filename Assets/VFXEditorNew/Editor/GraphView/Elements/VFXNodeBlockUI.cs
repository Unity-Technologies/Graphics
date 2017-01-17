using System;
using RMGUI.GraphView;
using UnityEngine;
using UnityEngine.RMGUI;
using UnityEngine.RMGUI.StyleEnums;
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

		    m_Title = new Label(new GUIContent("")) {name = "Title"};
			AddChild(m_Title);
            AddChild(m_SlotContainer);
        }

        public override EventPropagation Select(VisualContainer selectionContainer, Event evt)
        {
			NodeBlockContainer nodeBlockContainer = selectionContainer as NodeBlockContainer;
			if (nodeBlockContainer == null || nodeBlockContainer != parent || !IsSelectable())
				return EventPropagation.Continue;

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

			return EventPropagation.Continue;
        }

        // On purpose -- until we support Drag&Drop I suppose
		public override void SetPosition(Rect newPos)
		{
		}

        public override void OnDataChanged()
        {
			var presenter = GetPresenter<VFXNodeBlockPresenter>();

			if (presenter == null)
			{
				return;
			}

			if (presenter.selected)
			{
				AddToClassListConditional("selected");
			}
			else
			{
				RemoveFromClassList("selected");
			}

			m_Title.content.text = presenter.Model.Desc.Name + " " + (s_Counter++);

			SetPosition(presenter.position);
		}

        public override void DoRepaint(IStylePainter painter)
        {
            base.DoRepaint(painter);
        }
    }
}
