using System;
using UnityEngine;

namespace RMGUI.GraphView.Demo
{
	[Serializable]
	public class MiniMapPresenter : GraphElementPresenter
	{
		public float maxHeight;
		public float maxWidth;

		[SerializeField]
		private bool m_Anchored;

		public bool anchored
		{
			get { return m_Anchored; }
			set { m_Anchored = value; }
		}

		protected new void OnEnable()
		{
			base.OnEnable();
			capabilities = Capabilities.Floating | Capabilities.Movable;
		}

		protected MiniMapPresenter()
		{
			maxWidth = 200;
			maxHeight = 180;
		}
	}
}
