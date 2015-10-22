using System;
using UnityEditor.Experimental;
using UnityEngine;

namespace UnityEditor.MaterialGraph
{

    public sealed class DrawableMaterialNode : MoveableBox
    {
        private Type m_OutputType;
        private readonly MaterialGraphDataSource m_Data;
        public BaseMaterialNode m_Node;

        private Rect m_PreviewArea;

        public DrawableMaterialNode(BaseMaterialNode node, float width, Type outputType, MaterialGraphDataSource data)
            : base(node.position.min, width)
        {
            m_Node = node;
            m_Title = node.name;
            m_OutputType = outputType;
            m_Data = data;

            Vector3 pos = new Vector3(5.0f, 10.0f, 0.0f);

            // input slots
            foreach (var slot in node.inputSlots)
            {
                pos.y += 22;
                AddChild(new NodeAnchor(pos, typeof (Vector4), slot, data));
            }
            
            // output port
            pos.x = width;
            foreach (var slot in node.outputSlots)
            {
                pos.y += 22;
                AddChild(new NodeOutputAnchor(pos, typeof (Vector4), slot, data));
            }
            pos.y += 22;

            if (node.hasPreview)
            { 
                m_PreviewArea = new Rect(10, pos.y, width - 20, width - 20);
                pos.y += width;
            }

            scale = new Vector3(pos.x, pos.y, 0.0f);
            
            KeyDown += OnDeleteNode;
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
            if (m_Node.hasPreview)
            {
                GL.sRGBWrite = (QualitySettings.activeColorSpace == ColorSpace.Linear);
                GUI.DrawTexture(m_PreviewArea, m_Node.RenderPreview(new Rect(0, 0, m_PreviewArea.width, m_PreviewArea.height)), ScaleMode.StretchToFill, false);
                GL.sRGBWrite = false;
            }
        }
    }
}
