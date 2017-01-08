using System;
using UnityEditor;
using UnityEngine;

namespace RMGUI.GraphView.Demo
{
	[Serializable]
	public class CommentPresenter : IMGUIPresenter
	{
		[SerializeField]
		private string m_TitleBar;
		public string titleBar
		{
			get { return m_TitleBar; }
			set { m_TitleBar = value; }
		}

		[SerializeField]
		private string m_Body;
		public string body
		{
			get { return m_Body; }
			set { m_Body = value; }
		}

		[SerializeField]
		private Color m_Color;
		public Color color
		{
			get { return m_Color; }
			set { m_Color = value; }
		}

		protected new void OnEnable()
		{
			base.OnEnable();
			capabilities |= Capabilities.Deletable | Capabilities.Resizable;
			title = string.Empty;
		}
	}
}
