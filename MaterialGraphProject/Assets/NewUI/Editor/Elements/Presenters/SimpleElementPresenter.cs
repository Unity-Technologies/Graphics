using System;
using UnityEngine;

namespace RMGUI.GraphView
{
	[Serializable]
	public class SimpleElementPresenter : GraphElementPresenter
	{
		[SerializeField]
		private string m_Title;

		public string title
		{
			get { return m_Title; }
			set { m_Title = value; }
		}

		protected new void OnEnable()
		{
			base.OnEnable();
			title = string.Empty;
		}

		protected SimpleElementPresenter() {}
	}
}
