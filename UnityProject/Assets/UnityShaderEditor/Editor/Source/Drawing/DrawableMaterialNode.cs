using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using UnityEngine;

namespace UnityEditor.MaterialGraph
{

    public sealed class DrawableMaterialNode : MoveableBox
    {
        private Type m_OutputType;
        private readonly MaterialGraphDataSource m_Data;
        public BaseMaterialNode m_Node;

        private Rect m_PreviewArea;
        private Rect m_NodeUIRect;

        public DrawableMaterialNode(BaseMaterialNode node, float width, Type outputType, MaterialGraphDataSource data)
            : base(node.position.min, width)
        {
            m_Node = node;
            m_Title = node.name;
            m_OutputType = outputType;
            m_Data = data;
            error = node.hasError;

            const float yStart = 10.0f;
            var vector3 = new Vector3(5.0f, yStart, 0.0f);
            Vector3 pos = vector3;

            // input slots
            foreach (var slot in node.inputSlots)
            {
                pos.y += 22;
                AddChild(new NodeAnchor(pos, typeof (Vector4), slot, data, Direction.eInput));
            }
            var inputYMax = pos.y + 22;

            // output port
            pos.x = width;
            pos.y = yStart;
            foreach (var slot in node.outputSlots)
            {
                pos.y += 22;
                AddChild(new NodeAnchor(pos, typeof (Vector4), slot, data, Direction.eOutput));
            }
            pos.y += 22;

            pos.y = Mathf.Max(pos.y, inputYMax);

            var nodeUIHeight = m_Node.GetNodeUIHeight(width);
            m_NodeUIRect = new Rect(10, pos.y, width - 20, nodeUIHeight);
            pos.y += nodeUIHeight;

            if (node.hasPreview)
            { 
                m_PreviewArea = new Rect(10, pos.y, width - 20, width - 20);
                pos.y += width - 20.0f;
            }
            
            scale = new Vector3(pos.x, pos.y + 10.0f, 0.0f);
            
            KeyDown += OnDeleteNode;
            OnWidget += MarkDirtyIfNeedsTime;
            
            AddManipulator(new IMGUIContainer());
        }

        private bool MarkDirtyIfNeedsTime(CanvasElement element, Event e, Canvas2D parent)
        {
            var childrenNodes = m_Node.CollectChildNodesByExecutionOrder();
            if (childrenNodes.Any(x => x is IRequiresTime))
                Invalidate();
            return true;
        }

        private bool OnDeleteNode(CanvasElement element, Event e, Canvas2D canvas)
        {
            if (e.type == EventType.Used)
                return false;

            if (e.keyCode == KeyCode.Delete)
            {
                m_Data.DeleteElement(this);
                return true;
            }
            return false;
        }
        
        public override void UpdateModel(UpdateType t)
        {
            base.UpdateModel(t);
            var pos = m_Node.position;
            pos.min = translation;
            m_Node.position = pos;
            EditorUtility.SetDirty (m_Node);
        }

        public override void Render(Rect parentRect, Canvas2D canvas)
        {
            base.Render(parentRect, canvas);
            if (m_Node.NodeUI(m_NodeUIRect))
            {
                // if we were changed, we need to redraw all the
                // dependent nodes.
                var dependentNodes = m_Node.CollectDependentNodes();
                foreach (var node in dependentNodes)
                {
                    foreach (var drawNode in m_Data.lastGeneratedNodes)
                    {
                        if (drawNode.m_Node == node)
                            drawNode.Invalidate();
                    }
                }
            }

            if (m_Node.hasPreview)
            {
                GL.sRGBWrite = (QualitySettings.activeColorSpace == ColorSpace.Linear);
                GUI.DrawTexture(m_PreviewArea, m_Node.RenderPreview(new Rect(0, 0, m_PreviewArea.width, m_PreviewArea.height)), ScaleMode.StretchToFill, false);
                GL.sRGBWrite = false;
            }
        }
        
        public static void OnGUI(List<CanvasElement> selection)
        {
            foreach (var selected in selection.Where(x => x is DrawableMaterialNode).Cast<DrawableMaterialNode>())
            {
                selected.m_Node.OnGUI();
                break;
            }
        }
    }
}
