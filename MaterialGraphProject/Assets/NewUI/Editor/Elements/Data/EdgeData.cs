using System;
using UnityEngine;

namespace RMGUI.GraphView
{
	[Serializable]
	internal class EdgeData : GraphElementData
	{
		public IConnectable Left;
		public IConnectable Right;
		public Vector2 candidatePosition;
		public bool candidate;

		protected new void OnEnable()
		{
			capabilities = Capabilities.Selectable;
		}
	}
}
