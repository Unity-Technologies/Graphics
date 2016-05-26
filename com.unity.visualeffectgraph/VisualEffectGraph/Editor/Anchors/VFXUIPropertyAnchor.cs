using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEditor.Experimental.VFX;
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
        public VFXPropertySlot Slot                 { get { return m_Slot; } }
        public VFXPropertyTypeSemantics Semantics   { get { return Slot.Semantics; } }
        public VFXValueType ValueType               { get { return Slot.ValueType; } }

        private VFXUIPropertySlotField m_Owner;
        private VFXPropertySlot m_Slot;
        private Direction m_Direction;
        private VFXEdDataSource m_DataSource;
        private VFXSemanticsSource m_Source; 

        public VFXUIPropertyAnchor(VFXUIPropertySlotField owner, VFXPropertySlot slot,VFXEdDataSource dataSource, Vector3 position, Direction direction)
        {
            m_Owner = owner;
            m_Slot = slot;
            m_Direction = direction;
            m_DataSource = dataSource;
            m_Source = new VFXSemanticsSource(Semantics,direction);

            scale = VFXEditorMetrics.DataAnchorSize;
            translation = position;

            dataSource.Register(slot, this);

            AddManipulator(new VFXPropertyEdgeConnector());
            AddManipulator(new TooltipManipulator(GetAnchorToolTip));
            zIndex = -998; // ?
        }

        public List<string> GetAnchorToolTip()
        {
            List<string> lines = new List<string>();
            lines = VFXModelDebugInfoProvider.GetInfo(lines, Slot, VFXModelDebugInfoProvider.InfoFlag.kNone);
            return lines;
        }

        public override void Layout()
        {
            scale = VFXEditorMetrics.DataAnchorSize;
            base.Layout();
        }

        public override void Render(Rect parentRect, Canvas2D canvas)
        {
            if (!collapsed && (Owner == null || !Owner.Collapsed()))
            {
                Rect r = GetDrawableRect();
                Color typeColor = VFXEditor.styles.GetTypeColor(ValueType);
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

                if (Slot.IsLinked())
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
            if (!collapsed && (Owner == null || !Owner.Collapsed()))
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
            ParentCanvas().ClearSelection();
            VFXUIPropertyAnchor otherConnector = other as VFXUIPropertyAnchor;
            if (otherConnector != null)
            {
                m_DataSource.ConnectData(this,otherConnector);
                ParentCanvas().ReloadData();
            }
            else
            {
                if (m_Direction == Direction.Input)
                {
                    ExposePropertyMenu(Event.current.mousePosition);
                }
            }
        }

        public void ExposePropertyMenu(Vector2 position)
        {
            ExposePropertyInfo info = new ExposePropertyInfo(m_Slot.Semantics.GetType().FullName, this);

            List<MiniMenu.Item> items =  new List<MiniMenu.Item>();

            items.Add(new MiniMenu.HeaderItem("Create Parameter?"));
            items.Add(new MiniMenu.CallbackItem("Ok", ExposeProperty, info));

            MiniMenu.Show(Event.current.mousePosition, items);
        }

        public void ExposeProperty(Vector2 mousePosition, object propertyInfo)
        {

            ExposePropertyInfo info = (ExposePropertyInfo)propertyInfo;
            VFXDataBlockDesc desc = VFXEditor.BlockLibrary.GetDataBlock(info.DataBlockDescID);

            Vector2 canvasMousePosition = ParentCanvas().MouseToCanvas(mousePosition);

            // Find Underlying Node if present;
            VFXEdCanvas canvas = (VFXEdCanvas)ParentCanvas();

            VFXEdDataNode target = null;

            foreach(CanvasElement e in canvas.Children())
            {
                if(e is VFXEdDataNode)
                {
                    if(e.canvasBoundingRect.Contains(canvasMousePosition))
                    {
                        target = (VFXEdDataNode)e;
                    }
                }
            }

            // Add Blocks, and optionally Node if not present.
            if(target == null)
            {
                // Offset new node so it's more natural for dropping on mouse pointer.
                Vector2 OffsetPosition = canvasMousePosition - new Vector2(VFXEditorMetrics.NodeDefaultWidth - 40, 80);
                VFXDataNodeModel model =  m_DataSource.CreateDataNode(OffsetPosition);
                target = m_DataSource.GetUI<VFXEdDataNode>(model);
            }

            var blockModel = new VFXDataBlockModel(desc);
            m_DataSource.Create(blockModel, target.Model);

            // Copy values to new exposed parameter.
            newblock.Slot.CopyValuesFrom(m_Slot);

            // Connect
            m_DataSource.ConnectData(blockModel.GetOutputSlot(0),(VFXInputSlot)Slot);
            ParentCanvas().ReloadData(); 
            
        }
    }

    internal class ExposePropertyInfo
    {
        public string DataBlockDescID;
        public VFXUIPropertyAnchor Anchor;

        public ExposePropertyInfo(string id, VFXUIPropertyAnchor anchor)
        {
            DataBlockDescID = id;
            Anchor = anchor;
        }

    }
}
