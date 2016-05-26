using System.Linq;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using UnityEngine;

namespace UnityEditor.Graphing.Drawing
{
    public sealed class DrawableNode : CanvasElement
    {
        protected int previewWidth
        {
            get { return kPreviewWidth; }
        }

        protected int previewHeight
        {
            get { return kPreviewHeight; }
        }
        public delegate void NeedsRepaint();
        public NeedsRepaint onNeedsRepaint;

        private const int kPreviewWidth = 64;
        private const int kPreviewHeight = 64;

        private readonly GraphDataSource m_Data;
        public INode m_Node;

        private Rect m_PreviewArea;
        private Rect m_NodeUIRect;

        public DrawableNode(INode node, float width, GraphDataSource data)
        {
            var drawData = node.drawState;
            translation = drawData.position.min;
            scale = new Vector2(width, width);

            m_Node = node;
            m_Data = data;
            //m_Node.onNeedsRepaint += Invalidate;

            const float yStart = 10.0f;
            var vector3 = new Vector3(5.0f, yStart, 0.0f);
            Vector3 pos = vector3;

            // input slots
            foreach (var slot in node.inputSlots)
            {
                pos.y += 22;
                AddChild(new NodeAnchor(pos, typeof(Vector4), node, slot, data, Direction.Input));
            }
            var inputYMax = pos.y + 22;

            // output port
            pos.x = width;
            pos.y = yStart;
            foreach (var slot in node.outputSlots)
            {
                var edges = node.owner.GetEdges(node.GetSlotReference(slot.name));
                // don't show empty output slots in collapsed mode
                if (!node.drawState.expanded && !edges.Any())
                    continue;

                pos.y += 22;
                AddChild(new NodeAnchor(pos, typeof(Vector4), node, slot, data, Direction.Output));
            }
            pos.y += 22;

            pos.y = Mathf.Max(pos.y, inputYMax);

            /*
            var nodeUIHeight = m_Node.GetNodeUIHeight(width);
            m_NodeUIRect = new Rect(10, pos.y, width - 20, nodeUIHeight);
            pos.y += nodeUIHeight;

            if (node.hasPreview && node.drawMode != DrawMode.Collapsed)
            {
                m_PreviewArea = new Rect(10, pos.y, width - 20, width - 20);
                pos.y += m_PreviewArea.height;
            }*/

            scale = new Vector3(pos.x, pos.y + 10.0f, 0.0f);
            //OnWidget += MarkDirtyIfNeedsTime;

            AddManipulator(new ImguiContainer());
            AddManipulator(new Draggable());
        }

        /*
        private bool MarkDirtyIfNeedsTime(CanvasElement element, Event e, Canvas2D parent)
        {
            var childrenNodes = ListPool<INode>.Get();
            NodeUtils.DepthFirstCollectNodesFromNode(childrenNodes, m_Node);
            if (childrenNodes.Any(x => x is IRequiresTime))
                Invalidate();
            ListPool<INode>.Release(childrenNodes);
            return true;
        }*/

        public override void UpdateModel(UpdateType t)
        {
            base.UpdateModel(t);
            var drawState = m_Node.drawState;
            var pos = drawState.position;
            pos.min = translation;
            drawState.position = pos;
            m_Node.drawState = drawState;
        }

        public override void Render(Rect parentRect, Canvas2D canvas)
        {
            Color backgroundColor = new Color(0.0f, 0.0f, 0.0f, 0.7f);
            Color selectedColor = new Color(1.0f, 0.7f, 0.0f, 0.7f);
            EditorGUI.DrawRect(new Rect(0, 0, scale.x, scale.y), selected ? selectedColor : backgroundColor);
            GUI.Label(new Rect(0, 0, scale.x, 26f), GUIContent.none, new GUIStyle("preToolbar"));
            GUI.Label(new Rect(10, 2, scale.x - 20.0f, 16.0f), m_Node.name, EditorStyles.toolbarTextField);
            var drawState = m_Node.drawState;
            if (GUI.Button(new Rect(scale.x - 20f, 3f, 14f, 14f), drawState.expanded ? "-" : "+"))
            {
                drawState.expanded = !drawState.expanded;
                m_Node.drawState = drawState;
                ParentCanvas().ReloadData();
                ParentCanvas().Repaint();
                return;
            }

            /*var modificationType = m_Node.NodeUI(m_NodeUIRect);
            if (modificationType == GUIModificationType.Repaint)
            {
                // if we were changed, we need to redraw all the
                // dependent nodes.
                RepaintDependentNodes(m_Node);
            }
            else if (modificationType == GUIModificationType.ModelChanged)
            {
                ParentCanvas().ReloadData();
                ParentCanvas().Repaint();
                return;
            }

            if (m_Node.hasPreview
                && m_Node.drawMode != DrawMode.Collapsed
                && m_PreviewArea.width > 0
                && m_PreviewArea.height > 0)
            {
                GL.sRGBWrite = (QualitySettings.activeColorSpace == ColorSpace.Linear);
                GUI.DrawTexture(m_PreviewArea, m_Node.RenderPreview(new Rect(0, 0, m_PreviewArea.width, m_PreviewArea.height)), ScaleMode.StretchToFill, false);
                GL.sRGBWrite = false;
            }*/

            base.Render(parentRect, canvas);
        }

        /*
        public static void RepaintDependentNodes(AbstractMaterialNode bmn)
        {
            var dependentNodes = new List<INode>();
            NodeUtils.CollectNodesNodeFeedsInto(dependentNodes, bmn);
            foreach (var node in dependentNodes.OfType<SerializableNode>())
                node.onNeedsRepaint();
        }

        public static void OnGUI(List<CanvasElement> selection)
        {
            var drawableMaterialNode = selection.OfType<DrawableMaterialNode>().FirstOrDefault();
            if (drawableMaterialNode != null && drawableMaterialNode.m_Node.OnGUI())
            {
                // if we were changed, we need to redraw all the
                // dependent nodes.
                RepaintDependentNodes(drawableMaterialNode.m_Node);
            }
        }*/
    }
}
