using UnityEngine;
using UnityEngine.RMGUI;

namespace RMGUI.GraphView
{
	public delegate EventPropagation ContextualMenuDelegate(Event evt, Object customData);

	public class ContextualMenu : Manipulator
	{
		private ContextualMenuDelegate m_Callback;
		private Object m_CustomData;

		public ContextualMenu(ContextualMenuDelegate callback)
		{
			m_Callback = callback;
			m_CustomData = null;
		}

		public ContextualMenu(ContextualMenuDelegate callback, Object customData)
		{
			m_Callback = callback;
			m_CustomData = customData;
		}

		public override EventPropagation HandleEvent(Event evt, VisualElement finalTarget)
		{
			switch (evt.type)
			{
				case EventType.ContextClick:
					if (m_Callback != null)
						return m_Callback(evt, m_CustomData);
					break;
			}

			return EventPropagation.Continue;
		}
	}
}
