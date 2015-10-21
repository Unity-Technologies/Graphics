using System;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using UnityEngine;

namespace UnityEditor.MaterialGraph
{
    internal class PortSource<T>
    {
    }

    public class DrawableMaterialNode : MoveableBox
    {
        private Type m_OutputType;

        public DrawableMaterialNode(BaseMaterialNode node, float size, Type outputType, MaterialGraphDataSource data)
            : base(node.position.min, size)
        {
            m_Title = node.name;
            m_OutputType = outputType;

            Vector3 pos = new Vector3(5.0f, 32.0f, 0.0f);

            AddChild(new NodeAnchor(0, pos, typeof (int), this, data));
            pos.y += 22;

            AddChild(new NodeAnchor(1, pos, typeof (float), this, data));
            pos.y += 22;

            AddChild(new NodeAnchor(2, pos, typeof (Vector3), this, data));
            pos.y += 22;

            AddChild(new NodeAnchor(3, pos, typeof (Texture2D), this, data));
            pos.y += 22;

            AddChild(new NodeAnchor(4, pos, typeof (Color), this, data));
            pos.y += 22;

            // output port
            pos.x = size - 20.0f;
            AddChild(new NodeOutputAnchor(pos, m_OutputType, this, data));
            pos.y += 22;
            scale = new Vector3(scale.x, pos.y + 12.0f, 0.0f);
        }
    }

    public class NodeAnchor : CanvasElement, IConnect
    {
        protected Type m_Type;
        protected object m_Source;
        protected Direction m_Direction;
        private MaterialGraphDataSource m_Data;
        public DrawableMaterialNode m_Node;
        public int m_PortIndex;

        public NodeAnchor(int portIndex, Vector3 position, Type type, DrawableMaterialNode node, MaterialGraphDataSource data)
        {
            m_Type = type;
            scale = new Vector3(15.0f, 15.0f, 1.0f);
            translation = position;
            AddManipulator(new EdgeConnector<NodeAnchor>());
            m_Direction = Direction.eInput;

            Type genericClass = typeof (PortSource<>);
            Type constructedClass = genericClass.MakeGenericType(type);
            m_Source = Activator.CreateInstance(constructedClass);
            m_Data = data;
            m_Node = node;
            m_PortIndex = portIndex;
        }

        public override void Render(Rect parentRect, Canvas2D canvas)
        {
            var anchorColor = Color.yellow;
            anchorColor.a = 0.7f;
            base.Render(parentRect, canvas);
            EditorGUI.DrawRect(new Rect(translation.x, translation.y, scale.x, scale.y), anchorColor);
            Rect labelRect = new Rect(translation.x + scale.x + 10.0f, translation.y, parentRect.width, 20.0f);
            GUI.Label(labelRect, m_Type.Name);
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

    public class NodeOutputAnchor : NodeAnchor
    {
        public NodeOutputAnchor(Vector3 position, Type type, DrawableMaterialNode node, MaterialGraphDataSource data)
            : base(-1, position, type, node, data)
        {
            m_Direction = Direction.eOutput;
        }

        public override void Render(Rect parentRect, Canvas2D canvas)
        {
            var anchorColor = Color.yellow;
            anchorColor.a = 0.7f;
            base.Render(parentRect, canvas);
            EditorGUI.DrawRect(new Rect(translation.x, translation.y, scale.x, scale.y), anchorColor);
            Vector2 sizeOfText = GUIStyle.none.CalcSize(new GUIContent(m_Type.Name));

            Rect labelRect = new Rect(translation.x - sizeOfText.x - 4.0f, translation.y, sizeOfText.x + 4.0f, sizeOfText.y + 4.0f);
            GUI.Label(labelRect, m_Type.Name);
        }

    };

    internal static class MyNodeAdapters
    {
        internal static bool Adapt(this NodeAdapter value, PortSource<int> a, PortSource<int> b)
        {
            // run adapt code for int to int connections
            return true;
        }

        internal static bool Adapt(this NodeAdapter value, PortSource<float> a, PortSource<float> b)
        {
            // run adapt code for float to float connections
            return true;
        }

        internal static bool Adapt(this NodeAdapter value, PortSource<int> a, PortSource<float> b)
        {
            // run adapt code for int to float connections, perhaps by insertion a conversion node
            return true;
        }

        internal static bool Adapt(this NodeAdapter value, PortSource<Vector3> a, PortSource<Vector3> b)
        {
            // run adapt code for vec3 to vec3 connections
            return true;
        }

        internal static bool Adapt(this NodeAdapter value, PortSource<Color> a, PortSource<Color> b)
        {
            // run adapt code for Color to Color connections
            return true;
        }

        internal static bool Adapt(this NodeAdapter value, PortSource<Vector3> a, PortSource<Color> b)
        {
            // run adapt code for vec3 to Color connections
            return true;
        }

        internal static bool Adapt(this NodeAdapter value, PortSource<Color> a, PortSource<Vector3> b)
        {
            // run adapt code for Color to vec3 connections
            return true;
        }
    }
}
