using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using UnityEditor.Experimental.Graph.Examples; // TODO Dont use that anymore
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
    internal struct VFXSemanticsSource
    {
        public VFXSemanticsSource(VFXPropertyTypeSemantics semantics)
        {
            m_Semantics = semantics;
        }

        public bool CanLink(VFXSemanticsSource other)
        {
            return m_Semantics.CanLink(other.m_Semantics);
        }

        private VFXPropertyTypeSemantics m_Semantics;
    }

    internal static class VFXSemanticsNodeAdapter
    {
        internal static bool Adapt(this NodeAdapter value, VFXSemanticsSource a, VFXSemanticsSource b)
        {
            return b.CanLink(a);
        }
    }

    internal class VFXPropertyEdgeConnector : EdgeConnector<VFXUIPropertyAnchor>
    {
        public VFXPropertyEdgeConnector()
            : base(VFXUIPropertyEdge.DrawDataEdgeConnector)
        {}
    }

    internal class VFXUIPropertyAnchor : CanvasElement, IConnect
    {
        public VFXPropertySlot Slot                 { get { return m_Slot; } }
        public VFXPropertyTypeSemantics Semantics   { get { return Slot.Semantics; } }
        public VFXValueType ValueType               { get { return Slot.ValueType; } }

        private VFXPropertySlot m_Slot;
        private Direction m_Direction;
        private VFXEdDataSource m_DataSource;
        private VFXSemanticsSource m_Source;

        public VFXUIPropertyAnchor(VFXEdDataSource dataSource, Vector3 position, VFXPropertySlot slot, Direction direction)
        {
            m_Slot = slot;
            m_Direction = direction;
            m_DataSource = dataSource;
            m_Source = new VFXSemanticsSource(Semantics);

            scale = new Vector3(15.0f, 15.0f, 1.0f);
            translation = position;

            AddManipulator(new VFXPropertyEdgeConnector());

            zIndex = -998; // ?
        }

        public override void Layout()
        {
            scale = new Vector3(16.0f, 16.0f, 1.0f);
            base.Layout();
        }

        public override void Render(Rect parentRect, Canvas2D canvas)
        {
            if (!collapsed)
            {
                Rect r = GetDrawableRect();
                switch (m_Direction)
                {
                    case Direction.Input:
                        GUI.DrawTexture(r, VFXEditor.styles.ConnectorLeft.normal.background);
                        break;

                    case Direction.Output:
                        GUI.DrawTexture(r, VFXEditor.styles.ConnectorRight.normal.background);
                        break;
                }
            }
        }

        // IConnect implementation
        public Direction GetDirection()         { return m_Direction; }
        public Orientation GetOrientation()     { return Orientation.Horizontal; }
        public object Source()                  { return m_Source; }
        public Vector3 ConnectPosition()        { return canvasBoundingRect.center; }

        public void Highlight(bool highlighted)
        {

        }

        public void RenderOverlay(Canvas2D canvas)
        {
            Rect thisRect = canvasBoundingRect;
            thisRect = canvas.CanvasToScreen(thisRect);

            // TODO : Find out why theres a -2,5 offset in C2D overlays then remove this crap
            thisRect.x -= 2;
            thisRect.y += 5;
            GUI.color = VFXEditor.styles.GetTypeColor(ValueType);
            if (!collapsed)
            {
                switch (m_Direction)
                {
                    case Direction.Input:
                        GUI.DrawTexture(thisRect, VFXEditor.styles.ConnectorLeft.normal.background);
                        GUI.DrawTexture(canvas.CanvasToScreen(VFXEditor.styles.ConnectorOverlay.overflow.Add(canvasBoundingRect)), VFXEditor.styles.ConnectorOverlay.normal.background);
                        break;

                    case Direction.Output:
                        GUI.DrawTexture(thisRect, VFXEditor.styles.ConnectorRight.normal.background);
                        GUI.DrawTexture(canvas.CanvasToScreen(VFXEditor.styles.ConnectorOverlay.overflow.Add(canvasBoundingRect)), VFXEditor.styles.ConnectorOverlay.normal.background);
                        break;
                }
            }
            GUI.color = Color.white;
        }

        public void OnConnect(IConnect other)
        {
            VFXUIPropertyAnchor otherConnector = other as VFXUIPropertyAnchor;
            if (otherConnector != null)
            {
                m_DataSource.ConnectData(this,otherConnector);
                ParentCanvas().ReloadData();
            }
        }
    }

    [Obsolete]
    internal class VFXEdDataAnchor : CanvasElement, IConnect
    {
        public VFXValueType ValueType { get { return m_ValueType; } }
        protected VFXValueType m_ValueType;
        protected Type m_Type;
        protected object m_Source;
        protected Direction m_Direction;
        private VFXEdDataSource m_Data;
        private int m_ParamIndex;
        public int Index { get { return m_ParamIndex; } }

        public VFXEdDataAnchor(Vector3 position, VFXValueType type, VFXEdDataSource data, Direction direction, int index)
        {
            m_ValueType = type;
            m_Type = GetValueType(type);
            scale = new Vector3(15.0f, 15.0f, 1.0f);
            translation = position;
            AddManipulator(new DataEdgeConnector());
            m_Direction = direction;
            m_ParamIndex = index;

            Type genericClass = typeof(PortSource<>);
            Type constructedClass = genericClass.MakeGenericType(m_Type);
            m_Source = Activator.CreateInstance(constructedClass);
            
            m_Data = data;
            zIndex = -998;
        }

        public VFXEdNodeBlockParameterField GetAnchorField()
        {
            return parent as VFXEdNodeBlockParameterField;
        }

        private static Type GetValueType(VFXValueType type)
        {
            switch(type)
            {
                case VFXValueType.kInt: return typeof(int);
                case VFXValueType.kUint: return typeof(uint);
                case VFXValueType.kFloat: return typeof(float);
                case VFXValueType.kFloat2: return typeof(Vector2);
                case VFXValueType.kFloat3: return typeof(Vector3);
                case VFXValueType.kFloat4: return typeof(Vector4);
                case VFXValueType.kTexture2D: return typeof(Texture2D);
                case VFXValueType.kTexture3D: return typeof(Texture3D);
                case VFXValueType.kNone:
                default: return typeof(void);
            }
        }

        public override void Layout()
        {
            scale = new Vector3(16.0f, 16.0f, 1.0f);
            base.Layout();
        }

        public override void Render(Rect parentRect, Canvas2D canvas)
        {
            if(!collapsed)
            {
                Rect r = GetDrawableRect();
                switch (m_Direction)
                {
                    case Direction.Input:
                        GUI.DrawTexture(r, VFXEditor.styles.ConnectorLeft.normal.background);
                        break;

                    case Direction.Output:
                        GUI.DrawTexture(r, VFXEditor.styles.ConnectorRight.normal.background);
                        break;

                    default:
                        break;
                }
            }
            
        }

        // IConnect
        public Direction GetDirection()
        {
            return m_Direction;
        }

        public Orientation GetOrientation()
        {
            return Orientation.Horizontal;
        }

        public void Highlight(bool highlighted)
        {

        }

        public void RenderOverlay(Canvas2D canvas)
        {
            Rect thisRect = canvasBoundingRect;
            thisRect = canvas.CanvasToScreen(thisRect);

            // TODO : Find out why theres a -2,5 offset in C2D overlays then remove this crap
            thisRect.x -= 2;
            thisRect.y += 5;
            GUI.color = VFXEditor.styles.GetTypeColor(ValueType);
            if (!collapsed)
            {
                switch (m_Direction)
                {
                    case Direction.Input:
                        GUI.DrawTexture(thisRect, VFXEditor.styles.ConnectorLeft.normal.background);
                        GUI.DrawTexture(canvas.CanvasToScreen(VFXEditor.styles.ConnectorOverlay.overflow.Add(canvasBoundingRect)), VFXEditor.styles.ConnectorOverlay.normal.background);

                        break;

                    case Direction.Output:
                        GUI.DrawTexture(thisRect, VFXEditor.styles.ConnectorRight.normal.background);
                        GUI.DrawTexture(canvas.CanvasToScreen(VFXEditor.styles.ConnectorOverlay.overflow.Add(canvasBoundingRect)), VFXEditor.styles.ConnectorOverlay.normal.background);

                        break;

                    default:
                        break;
                }
            }
            GUI.color = Color.white;
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

            VFXEdDataAnchor otherConnector = other as VFXEdDataAnchor;

            if(otherConnector !=  null)
            {
                m_Data.ConnectData(this, otherConnector);
                ParentCanvas().ReloadData();
            }
        }

    }

}
