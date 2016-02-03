using UnityEditor.Experimental;
using UnityEditor.Graphs;
using UnityEngine;

namespace UnityEditor.MaterialGraph
{
    public class NullInputProxy : CanvasElement
    {
        private Slot m_InputSlot;
        private NodeAnchor m_NodeAnchor;

        private const int kWidth = 180;

        public NullInputProxy(Slot inputSlot, NodeAnchor nodeAnchor)
        {
            m_InputSlot = inputSlot;
            m_NodeAnchor = nodeAnchor;

            var size = m_NodeAnchor.scale;
            size.x = kWidth;
            scale = size;

            nodeAnchor.AddDependency(this);
            UpdateModel(UpdateType.Update);

            var position = m_NodeAnchor.canvasBoundingRect.min;
            position.x -= kWidth;
            translation = position;
            AddManipulator(new ImguiContainer());
        }
        

        public override void Render(Rect parentRect, Canvas2D canvas)
        {
            base.Render(parentRect, canvas);

            var size = m_NodeAnchor.scale;
            size.x = kWidth;
            scale = size;

            var position = m_NodeAnchor.canvasBoundingRect.min;
            position.x -= kWidth;
            translation = position;

            var bmn = (BaseMaterialNode) m_InputSlot.node;

            var rect = new Rect(0, 0, scale.x, scale.y);
            var changed = bmn.DrawSlotDefaultInput(rect, m_InputSlot);
            if (changed)
                DrawableMaterialNode.RepaintDependentNodes(bmn);
        }

        public override void UpdateModel(UpdateType t)
        {
            var size = m_NodeAnchor.scale;
            size.x = kWidth;
            scale = size;

            var position = m_NodeAnchor.canvasBoundingRect.min;
            position.x -= kWidth;
            translation = position;

            base.UpdateModel(t);
        }
    }
}
