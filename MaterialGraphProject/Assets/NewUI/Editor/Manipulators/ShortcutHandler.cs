using System.Collections.Generic;
using UnityEngine;
using UnityEngine.RMGUI;

namespace RMGUI.GraphView
{
	public delegate EventPropagation ShortcutDelegate();

	public class ShortcutHandler : Manipulator
	{
		Dictionary<Event, ShortcutDelegate> m_Dictionary;

		public ShortcutHandler(Dictionary<Event, ShortcutDelegate> dictionary)
		{
			m_Dictionary = dictionary;
		}

		public override EventPropagation HandleEvent(Event evt, VisualElement finalTarget)
		{
			switch (evt.type)
			{
				case EventType.KeyDown:
					if (m_Dictionary.ContainsKey(evt))
						return m_Dictionary[evt]();
					break;
			}
			return EventPropagation.Continue;
		}
	}
}
