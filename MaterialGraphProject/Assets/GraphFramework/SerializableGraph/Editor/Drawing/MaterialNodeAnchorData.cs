using System;
using RMGUI.GraphView;
using RMGUI.GraphView.Demo;
using UnityEngine;
using UnityEngine.Graphing;

namespace UnityEditor.Graphing.Drawing
{
	[Serializable]
	public class MaterialEdgeData : EdgeData
	{
		protected MaterialEdgeData()
		{}

		public UnityEngine.Graphing.IEdge edge { get; private set; }

		public void Initialize(UnityEngine.Graphing.IEdge inEdge)
		{
			edge = inEdge;
		}
	}


    [Serializable]
    public class MaterialNodeAnchorData : NodeAnchorData
    {
        protected MaterialNodeAnchorData()
        {}

        public ISlot slot { get; private set; }

        public void Initialize(ISlot slot)
        {
            this.slot = slot;
            name = slot.displayName;
            type = typeof(Vector4);
            direction = slot.isInputSlot ? Direction.Input : Direction.Output;
        }
    }
}
