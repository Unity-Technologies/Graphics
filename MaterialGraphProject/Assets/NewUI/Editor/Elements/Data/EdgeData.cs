using System;
using UnityEngine;

namespace RMGUI.GraphView
{
	[Serializable]
	public class EdgeData : GraphElementData, IConnection
	{
		[SerializeField]
		private GraphElementData m_OutputData;

		[SerializeField]
		private GraphElementData m_InputData;

		public virtual IConnector output
		{
			get { return (IConnector)m_OutputData; }
			set
			{
				var ged = value as GraphElementData;
				if (value != null && ged == null)
					throw new ArgumentException("The value passed to EgdeData.output is not a GraphElementData");
				m_OutputData = ged;
			}
		}

		public virtual IConnector input
		{
			get { return (IConnector)m_InputData; }
			set
			{
				var ged = value as GraphElementData;
				if (value != null && ged == null)
					throw new ArgumentException("The value passed to EgdeData.input is not a GraphElementData");
				m_InputData = ged;
			}
		}

		public Vector2 candidatePosition { get; set; }
		public bool candidate { get; set; }

		protected new void OnEnable()
		{
			base.OnEnable();
			capabilities = Capabilities.Deletable;
		}

		protected EdgeData() {}
	}
}
