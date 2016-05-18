using System;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using UnityEditor.Graphs;
using UnityEngine;

namespace UnityEditor.MaterialGraph
{
    public class NodeAnchor : CanvasElement, IConnect
    {
        protected Type m_Type;
        protected object m_Source;
        protected Direction m_Direction;
        private MaterialGraphDataSource m_Data;
        public Slot m_Slot;
        public BaseMaterialNode m_Node;

        public NodeAnchor(Vector3 position, Type type, BaseMaterialNode node, Slot slot, MaterialGraphDataSource data, Direction direction)
        {
            m_Type = type;
            scale = new Vector3(15.0f, 15.0f, 1.0f);
            translation = position;
            AddManipulator(new EdgeConnector<NodeAnchor>());
            m_Direction = direction;

            Type genericClass = typeof (PortSource<>);
            Type constructedClass = genericClass.MakeGenericType(type);
            m_Source = Activator.CreateInstance(constructedClass);
            m_Data = data;
            m_Node = node;
            m_Slot = slot;
        }

        public Orientation GetOrientation()
        {
            return Orientation.Horizontal;
        }

        private static string ConcreteSlotValueTypeAsString(ConcreteSlotValueType type)
        {
            switch (type)
            {
                case ConcreteSlotValueType.Vector1:
                    return "(1)";
                case ConcreteSlotValueType.Vector2:
                    return "(2)";
                case ConcreteSlotValueType.Vector3:
                    return "(3)";
                case ConcreteSlotValueType.Vector4:
                    return "(4)";
                default:
                    return "(E)";

            }
        }

        public override void Render(Rect parentRect, Canvas2D canvas)
        {
            var anchorColor = Color.yellow;
            anchorColor.a = 0.7f;
            base.Render(parentRect, canvas);
            EditorGUI.DrawRect(new Rect(translation.x, translation.y, scale.x, scale.y), anchorColor);
            string text = m_Slot.name;
            Rect labelRect;
            if (m_Direction == Direction.Input)
            {
                text += " " + ConcreteSlotValueTypeAsString(m_Node.GetConcreteInputSlotValueType(m_Slot));
                labelRect = new Rect(translation.x + scale.x + 10.0f, translation.y, parentRect.width, 20.0f);
            }
            else
            {
                text += " " + ConcreteSlotValueTypeAsString(m_Node.GetConcreteOutputSlotValueType(m_Slot));
                Vector2 sizeOfText = GUIStyle.none.CalcSize(new GUIContent(text));
                labelRect = new Rect(translation.x - sizeOfText.x - 4.0f, translation.y, sizeOfText.x + 4.0f, sizeOfText.y + 4.0f);
            }
            GUI.Label(labelRect, text);
        }

        // IConnect
        public Direction GetDirection()
        {
            return m_Direction;
        }

        public void Highlight(bool highlighted)
        {
        }

        public void RenderOverlay(Canvas2D canvas)
        {
            Rect thisRect = canvasBoundingRect;
            thisRect.x += 4;
            thisRect.y += 4;
            thisRect.width -= 8;
            thisRect.height -= 8;
            thisRect = canvas.CanvasToScreen(thisRect);
            EditorGUI.DrawRect(thisRect, new Color(0.0f, 0.0f, 0.8f));
        }

        public object Source()
        {
            return m_Source;
        }

        public Vector3 ConnectPosition()
        {
            return canvasBoundingRect.center;
        }

        public void OnConnect(IConnect other)
        {
            if (other == null)
                return;

            NodeAnchor otherAnchor = other as NodeAnchor;
            m_Data.Connect(this, otherAnchor);

            ParentCanvas().ReloadData();
        }

    };
}
