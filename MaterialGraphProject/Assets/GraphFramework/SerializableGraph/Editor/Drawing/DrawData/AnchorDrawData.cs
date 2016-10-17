using System;
using RMGUI.GraphView;
using RMGUI.GraphView.Demo;
using UnityEngine;
using UnityEngine.Graphing;

namespace UnityEditor.Graphing.Drawing
{
    [Serializable]
    public class AnchorDrawData : NodeAnchorData
    {
        protected AnchorDrawData()
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
