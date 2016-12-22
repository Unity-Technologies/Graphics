using System;
using UnityEngine;

namespace RMGUI.GraphView.Demo
{
	[Serializable]
	public class CirclePresenter : GraphElementPresenter
	{
		[SerializeField]
		private float m_Radius;
		public float radius
		{
			get { return m_Radius; }
			set
			{
				if (m_Radius == value) return;
				m_Radius = value;

				// Now update the position
				Rect newPos = position;
				newPos.width = m_Radius;
				newPos.height = m_Radius;
				position = newPos;
			}
		}

		protected new void OnEnable()
		{
			base.OnEnable();
			capabilities |= Capabilities.Deletable;
		}

		protected CirclePresenter() {}
	}
}
