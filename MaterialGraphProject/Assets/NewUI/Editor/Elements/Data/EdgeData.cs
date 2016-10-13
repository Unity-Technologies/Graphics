using System;
using UnityEngine;

namespace RMGUI.GraphView
{
	[Serializable]
	public class EdgeData : GraphElementData
	{
		public IConnectable left;
		public IConnectable right;
		public Vector2 candidatePosition;
		public bool candidate;

		protected new void OnEnable()
		{
			capabilities = Capabilities.Selectable;
		}

		protected EdgeData() {}
	}
}
