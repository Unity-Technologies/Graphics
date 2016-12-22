using System;
using UnityEngine;

namespace RMGUI.GraphView
{
	[Serializable]
	public class EdgePresenter : GraphElementPresenter
	{
		[SerializeField]
		private NodeAnchorPresenter m_OutputPresenter;

		[SerializeField]
		private NodeAnchorPresenter m_InputPresenter;

		public virtual NodeAnchorPresenter output
		{
			get { return m_OutputPresenter; }
			set { m_OutputPresenter = value; }
		}

		public virtual NodeAnchorPresenter input
		{
			get { return m_InputPresenter; }
			set { m_InputPresenter = value; }
		}

		public Vector2 candidatePosition { get; set; }
		public bool candidate { get; set; }

		protected new void OnEnable()
		{
			base.OnEnable();
			capabilities = Capabilities.Deletable | Capabilities.Selectable;
		}

		protected EdgePresenter() {}
	}
}
