using System.Collections.Generic;
using UnityEngine;
using UnityEngine.RMGUI;

namespace RMGUI.GraphView
{
	public class MouseManipulator : Manipulator
	{
		public List<ManipActivator> activators { get; private set; }
		private ManipActivator m_currentActivator;

		public MouseManipulator()
		{
			activators = new List<ManipActivator>();
		}

		protected bool CanStartManipulation(Event evt)
		{
			foreach (var activator in activators)
			{
				if (activator.Matches(evt))
				{
					m_currentActivator = activator;
					return true;
				}
			}

			return false;
		}

		protected bool CanStopManipulation(Event evt)
		{
			return ((MouseButton)evt.button == m_currentActivator.button) && this.HasCapture();
		}
	}
}
