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

        VisualContainer m_SlotContainer;

        public VFXNodeBlockUI(VFXNodeBlockPresenter presenter)
        {
            pickingMode = PickingMode.Position;
            this.presenter = presenter;
            classList = ClassList.empty;

            m_SlotContainer = new VisualContainer()
            {
                name = "SlotContainer"
            };

            AddChild(new Label(new GUIContent(presenter.Model.Desc.Name+" "+(s_Counter++))));
            AddChild(m_SlotContainer);
        }

        public override EventPropagation Select(VisualContainer selectionContainer, Event evt)
        {
            if (selectionContainer != parent || !IsSelectable())
                return EventPropagation.Continue;

            foreach(var child in selectionContainer.allChildren)
            {
                var block = (child as VFXNodeBlockUI);
                if (block != null)
                    block.presenter.selected = false;
            }

			presenter.selected = true;

            var graphView = selectionContainer.parent as GraphView;
            if(graphView != null)
			    graphView.contentViewContainer.Touch(ChangeType.Repaint);

            return EventPropagation.Stop;
        }

		public override void SetPosition(Rect newPos)
		{
		}

        public override void OnDataChanged()
        {
            base.OnDataChanged();
        }

        public override void DoRepaint(IStylePainter painter)
        {
            base.DoRepaint(painter);
        }
    }
}
