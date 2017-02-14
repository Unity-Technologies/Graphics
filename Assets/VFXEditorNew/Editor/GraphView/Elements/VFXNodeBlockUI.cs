using System.Collections.Generic;
using RMGUI.GraphView;
using UnityEngine;
using UnityEngine.RMGUI;
using UnityEngine.RMGUI.StyleSheets;
using System.Reflection;
using UnityEngine.RMGUI.StyleEnums;

namespace UnityEditor.VFX.UI
{
	class VFXNodeBlockUI : GraphElement
	{
		static int s_Counter = 0;

		Label m_Title;

		private int m_Index; // tmp

        List<VFXPropertyUI> m_PropertiesUI = new List<VFXPropertyUI>();

        public GraphViewTypeFactory typeFactory { get; set; }

        public VFXNodeBlockUI()
        {
            forceNotififcationOnAdd = true;
            pickingMode = PickingMode.Position;
            classList = ClassList.empty;

			m_Index = s_Counter++;

			m_Title = new Label(new GUIContent("")) {name = "Title"};
			InsertChild(0,m_Title);

			AddManipulator(new SelectionDropper(HandleDropEvent));
            clipChildren = false;

            typeFactory = new GraphViewTypeFactory();

            typeFactory[typeof(VFXDataInputAnchorPresenter)] = typeof(VFXDataAnchor);
            typeFactory[typeof(VFXDataOutputAnchorPresenter)] = typeof(VFXDataAnchor);
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
				return EventPropagation.Continue;
			}

			if (!evt.control)
				nodeBlockContainer.ClearSelection();
			nodeBlockContainer.AddToSelection(this);

			// TODO: Reset to EventPropagation.Continue when Drag&Drop is supported
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
				return;

			if (presenter.selected)
			{
				AddToClassList("selected");
			}
			else
			{
				RemoveFromClassList("selected");
			}

			m_Title.content.text = presenter.Model.Desc.Name + " " + m_Index;

			SetPosition(presenter.position);



            int cpt = 0;
            foreach (VFXNodeBlockPresenter.PropertyInfo propertyInfo in presenter.GetProperties())
            {
                if( m_PropertiesUI.Count <= cpt)
                {
                    VFXPropertyUI propertyUI = new VFXPropertyUI();
                    m_PropertiesUI.Add(propertyUI);
                    AddChild(propertyUI);
                }
                m_PropertiesUI[cpt++].DataChanged(this,propertyInfo);

            }

            while (cpt < m_PropertiesUI.Count)
            {
                RemoveChild(m_PropertiesUI[m_PropertiesUI.Count - 1]);
                m_PropertiesUI.RemoveAt(m_PropertiesUI.Count - 1);
            }

        }


        public override void DoRepaint(IStylePainter painter)
		{
			base.DoRepaint(painter);
		}
	}
}
