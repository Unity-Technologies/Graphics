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

    // TODO JOCE Use GraphView's Input and Output node anchors instead.

    [Serializable]
    public class AnchorDrawData : NodeAnchorPresenter
    {
        protected AnchorDrawData()
        {}

        public ISlot slot { get; private set; }

        public void Initialize(ISlot slot)
        {
            this.slot = slot;
            name = slot.displayName;
            anchorType = typeof(Vector4);
            m_Direction = slot.isInputSlot ? Direction.Input : Direction.Output;
        }

        private Direction m_Direction;
        public override Direction direction
        {
            get { return m_Direction; }
        }
    }
}
