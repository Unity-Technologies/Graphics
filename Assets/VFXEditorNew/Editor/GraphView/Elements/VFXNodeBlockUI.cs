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
        IMGUIContainer m_Container;

		private int m_Index; // tmp

		public VFXNodeBlockUI()
        {
            forceNotififcationOnAdd = true;
            pickingMode = PickingMode.Position;
            classList = ClassList.empty;
            /*
			m_SlotContainer = new VisualContainer()
			{
				name = "SlotContainer"
			};*/

			m_Index = s_Counter++;

			m_Title = new Label(new GUIContent("")) {name = "Title"};
			InsertChild(0,m_Title);
			//AddChild(m_SlotContainer);

			AddManipulator(new SelectionDropper(HandleDropEvent));


            m_Container = new IMGUIContainer()
            {
                OnGUIHandler = OnGUIHandler
            };
            AddChild(m_Container);
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

        public void OnGUIHandler()
        {
            if (m_Elements == null) return;
            var presenter = GetPresenter<VFXNodeBlockPresenter>();

            int cpt = 0;
            foreach (VFXPropertyIM elem in m_Elements)
            {
                elem.OnGUI(presenter, cpt++);
            }

            if (Event.current.type != EventType.Layout && Event.current.type != EventType.Used)
            {
                if (m_Elements.Length > 0)
                {
                    Rect r = GUILayoutUtility.GetLastRect();
                    m_Container.height = r.yMax;

                    //Rect pos = position;

                    //pos.height = minHeight;

                   // position = pos;
                }
                else
                {
                    height = 0;
                }
            }
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


            System.Type propertyType = presenter.GetPropertiesType();

            if( propertyType != null)
            {
                FieldInfo[] fields = propertyType.GetFields();

                m_Elements = new VFXPropertyIM[fields.Length];

                for(int i = 0; i < fields.Length; ++i)
                {
                    m_Elements[i] = VFXPropertyIM.Create(presenter, i);
                }
            }
            else
            {
                m_Elements = null;
            }

            

        }

        VFXPropertyIM[] m_Elements;


        public override void DoRepaint(IStylePainter painter)
		{
			base.DoRepaint(painter);
		}
	}
}
