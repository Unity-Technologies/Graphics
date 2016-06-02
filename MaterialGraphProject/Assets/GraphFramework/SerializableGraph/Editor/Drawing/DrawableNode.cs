using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using UnityEngine;
using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;

namespace UnityEditor.Graphing.Drawing
{
    public interface ICustomNodeUi
    {
        int GetNodeUiHeight(int width);
        GUIModificationType Render(Rect area);
    }

    public class DrawableNode : CanvasElement
    {
        private readonly GraphDataSource m_Data;

        private readonly Rect m_CustomUiRect;
        public readonly INode m_Node;
        private readonly ICustomNodeUi m_Ui;

        public DrawableNode(INode node, ICustomNodeUi ui, GraphDataSource data)
        {
            const int width = 200;
            var drawData = node.drawState;
            translation = drawData.position.min;
            scale = new Vector2(width, width);

            m_Node = node;
            m_Ui = ui;
            m_Data = data;

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

            if (ui != null)
            {
                var customUiHeight = ui.GetNodeUiHeight(width);
                m_CustomUiRect = new Rect(10, pos.y, width - 20, customUiHeight);
                pos.y += customUiHeight;
            }

            /*
            if (node.hasPreview && node.drawMode != DrawMode.Collapsed)
            {
                m_PreviewArea = new Rect(10, pos.y, width - 20, width - 20);
                pos.y += m_PreviewArea.height;
            }*/

            scale = new Vector3(pos.x, pos.y + 10.0f, 0.0f);

            OnWidget += MarkDirtyIfNeedsTime;

            AddManipulator(new ImguiContainer());
            AddManipulator(new Draggable());
        }
        
        private bool MarkDirtyIfNeedsTime(CanvasElement element, Event e, Canvas2D parent)
        {
            var childrenNodes = ListPool<INode>.Get();
            NodeUtils.DepthFirstCollectNodesFromNode(childrenNodes, m_Node);
            if (childrenNodes.Any(x => x is IRequiresTime))
                Invalidate();
            ListPool<INode>.Release(childrenNodes);
            return true;
        }

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

            if (m_Ui != null)
            {
                var modificationType = m_Ui.Render(m_CustomUiRect);
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
            }

            /*  if (m_Node.hasPreview
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
        
        private void RepaintDependentNodes(INode theNode)
        {
            var dependentNodes = new List<INode>();
            NodeUtils.CollectNodesNodeFeedsInto(dependentNodes, theNode);
            foreach (var node in dependentNodes)
            {
                foreach (var drawableNode in m_Data.lastGeneratedNodes.Where(x => x.m_Node == node))
                    drawableNode.Invalidate();
            }
        }

        /*
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

       /* public virtual GUIModificationType NodeUI(Rect drawArea)
        {
            return GUIModificationType.None;
        }

        public virtual bool OnGUI()
        {
            GUILayout.Label("MaterialSlot Defaults", EditorStyles.boldLabel);
            var modified = false;
            foreach (var slot in inputSlots)
            {
                if (!owner.GetEdges(GetSlotReference(slot.name)).Any())
                    modified |= DoSlotUI(this, slot);
            }

            return modified;
        }

        public static bool DoSlotUI(SerializableNode node, ISlot slot)
        {
            GUILayout.BeginHorizontal( /*EditorStyles.inspectorBig*);
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.Label("MaterialSlot " + slot.name, EditorStyles.largeLabel);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            //TODO: fix this
            return false;
            //return slot.OnGUI();
        }*/
    }
}
