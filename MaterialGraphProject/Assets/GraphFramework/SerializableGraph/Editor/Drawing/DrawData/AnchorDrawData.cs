using System;
using RMGUI.GraphView;
using UnityEngine;
using UnityEngine.Graphing;

namespace UnityEditor.Graphing.Drawing
{
    internal static class MyNodeAdapters
    {
        internal static bool Adapt(this NodeAdapter value, PortSource<Vector4> a, PortSource<Vector4> b)
        {
            return true;
        }
    }

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
