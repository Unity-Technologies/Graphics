using System;
using UnityEngine;

namespace UnityEditor.Experimental.Graph.Examples
{
    class PortSource<T>
    {
    }

  
    class Node : MoveableBox
    {
        Type m_OutputType;
        public Node(Vector2 position, float size, Type outputType, NodalDataSource data)
            : base(position, size)
        {
            m_Title = "Some Operator";
            m_OutputType = outputType;

            Vector3 pos = new Vector3(5.0f, 32.0f, 0.0f);

            AddChild(new NodeAnchor(0, pos, typeof(int), data, null));
            pos.y += 22;

            AddChild(new NodeAnchor(1, pos, typeof(float), data, null));
            pos.y += 22;

            AddChild(new NodeAnchor(2, pos, typeof(Vector3), data, null));
            pos.y += 22;

            AddChild(new NodeAnchor(3, pos, typeof(Texture2D), data, null));
            pos.y += 22;

            AddChild(new NodeAnchor(4, pos, typeof(Color), data, null));
            pos.y += 22;

            // output port
            pos.x = size - 20.0f;
            AddChild(new NodeOutputAnchor(pos, m_OutputType, data));
            pos.y += 22;
            scale = new Vector3(scale.x, pos.y + 12.0f, 0.0f);
        }

        public override void Render(Rect parentRect, Canvas2D canvas)
        {
            base.Render(parentRect, canvas);
        }
    }

    internal class NodeAnchor : CanvasElement, IConnect
    {
        protected Type m_Type;
        protected object m_Source;
        protected Direction m_Direction;
        private NodalDataSource m_Data;
        protected Orientation m_Orientation = Orientation.Horizontal;
        public int m_PortIndex;
        static public float kNodeSize = 15.0f;
        
        public NodeAnchor(int portIndex, Vector3 position, Type type, NodalDataSource data, EdgeRenderMethod customEdgeDrawMethod)
        {
            m_Type = type;
            scale = new Vector3(kNodeSize, kNodeSize, 1.0f);
            translation = position;
            AddManipulator(new EdgeConnector<NodeAnchor>(customEdgeDrawMethod));
            m_Direction = Direction.Input;

            Type genericClass = typeof(PortSource<>);
            Type constructedClass = genericClass.MakeGenericType(type);
            m_Source = Activator.CreateInstance(constructedClass);
            m_Data = data;
            m_PortIndex = portIndex;
        }

        public override void Render(Rect parentRect, Canvas2D canvas)
        {
            var anchorColor = Color.yellow;
            anchorColor.a = 0.7f;
            base.Render(parentRect, canvas);
            EditorGUI.DrawRect(new Rect(translation.x, translation.y, scale.x, scale.y), anchorColor);
            
            Rect labelRect = new Rect(translation.x + scale.x + 10.0f, translation.y, parentRect.width, 20.0f);
            if (m_Orientation == Orientation.Vertical)
            {
                float yPosition = translation.y + 20.0f;
                if ((yPosition) > parent.boundingRect.height)
                {
                    yPosition = translation.y - 20.0f;
                }
                labelRect = new Rect(translation.x, yPosition, 200.0f, 20.0f);
            }
            GUI.Label(labelRect, m_Type.Name);
        }

        // IConnect
        public Direction GetDirection()
        {
            return m_Direction;
        }

        public Orientation GetOrientation()
        {
            return m_Orientation;
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
    internal class NodeOutputAnchor : NodeAnchor
    {
        public NodeOutputAnchor(Vector3 position, Type type, NodalDataSource data)
            : base(-1, position, type, data, null)
        {
            m_Direction = Direction.Output;
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
