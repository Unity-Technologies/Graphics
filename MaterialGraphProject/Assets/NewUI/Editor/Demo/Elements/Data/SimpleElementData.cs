using System;
using UnityEngine;

namespace RMGUI.GraphView.Demo
{
	[Serializable]
	public class SimpleElementData : GraphElementData
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

		protected SimpleElementData() {}
	}
}
