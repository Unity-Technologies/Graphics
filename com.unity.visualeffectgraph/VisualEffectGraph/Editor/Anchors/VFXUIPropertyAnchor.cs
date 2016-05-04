using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
    internal struct VFXSemanticsSource
    {
        public VFXSemanticsSource(VFXPropertyTypeSemantics semantics,Direction direction)
        {
            m_Semantics = semantics;
            m_Direction = direction;
        }

        public bool CanLink(VFXSemanticsSource other)
        {
            // As VFXPropertyTypeSemantics.CanLink is not commutative
            if (m_Direction == Direction.Input)
                return m_Semantics.CanLink(other.m_Semantics);
            else
                return other.m_Semantics.CanLink(m_Semantics);
        }

        private VFXPropertyTypeSemantics m_Semantics;
        private Direction m_Direction;
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
        public VFXUIPropertySlotField Owner         { get { return m_Owner; } }
        public VFXPropertySlot Slot                 { get { return Owner.Slot; } }
        public VFXPropertyTypeSemantics Semantics   { get { return Slot.Semantics; } }
        public VFXValueType ValueType               { get { return Slot.ValueType; } }

        private VFXUIPropertySlotField m_Owner;
        private Direction m_Direction;
        private VFXEdDataSource m_DataSource;
        private VFXSemanticsSource m_Source; 

        public VFXUIPropertyAnchor(VFXUIPropertySlotField owner, VFXEdDataSource dataSource, Vector3 position, Direction direction)
        {
            m_Owner = owner;
            m_Direction = direction;
            m_DataSource = dataSource;
            m_Source = new VFXSemanticsSource(Semantics,direction);

            scale = VFXEditorMetrics.DataAnchorSize;
            translation = position;

            AddManipulator(new VFXPropertyEdgeConnector());
            AddManipulator(new TooltipManipulator(GetAnchorToolTip));
            zIndex = -998; // ?
        }

        public List<string> GetAnchorToolTip()
        {
            List<string> lines = new List<string>();
            lines.Add( Owner.Name + " : " + ValueType );
            return lines;
        }

        public override void Layout()
        {
            scale = VFXEditorMetrics.DataAnchorSize;
            base.Layout();
        }

        public override void Render(Rect parentRect, Canvas2D canvas)
        {
            if (!collapsed && !Owner.Collapsed())
            {
                Rect r = GetDrawableRect();
                Color typeColor = VFXEditor.styles.GetTypeColor(Owner.ValueType);
                Rect colorzone = r;
                Texture2D texture = null;

                switch (m_Direction)
                {
                    case Direction.Input:
                        colorzone = VFXEditorMetrics.DataAnchorLeftColorZone.Remove(r);
                        texture = VFXEditor.styles.ConnectorLeft.normal.background;
                        break;

                    case Direction.Output:
                        colorzone = VFXEditorMetrics.DataAnchorRightColorZone.Remove(r);
                        texture = VFXEditor.styles.ConnectorRight.normal.background;
                        break;
                }

                if (Owner.Slot.IsLinked())
                    GUI.color = Color.Lerp(typeColor, Color.gray, 0.5f);
                else
                    GUI.color = Color.gray;
                
                GUI.DrawTexture(r, texture);
                GUI.color = Color.white;

                EditorGUI.DrawRect(colorzone, typeColor);
            }
        }

        // IConnect implementation
        public Direction GetDirection()         { return m_Direction; }
        public Orientation GetOrientation()     { return Orientation.Horizontal; }
        public object Source()                  { return m_Source; }
        public Vector3 ConnectPosition()        { return GetDrawableRect(true).center; }

        public void Highlight(bool highlighted)
        {

        }

        public void RenderOverlay(Canvas2D canvas)
        {
            Rect thisRect = canvasBoundingRect;
            thisRect = canvas.CanvasToScreen(thisRect);

            // TODO : Find out why theres a -2,5 offset in C2D overlays then remove this crap
            // For undocked windows, offsets should be : (-0,2)
            thisRect.x -= 2;
            thisRect.y += 5;
            GUI.color = VFXEditor.styles.GetTypeColor(ValueType);
            if (!collapsed && !Owner.Collapsed())
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
}
