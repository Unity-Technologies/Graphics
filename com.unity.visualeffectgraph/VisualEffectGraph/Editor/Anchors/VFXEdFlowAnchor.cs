using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using UnityEditor.Experimental.Graph.Examples; // TODO Dont use that anymore
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
    internal class VFXEdFlowAnchor : CanvasElement, IConnect
    {
        protected Type m_Type;
        protected object m_Source;
        protected Direction m_Direction;
        private VFXEdDataSource m_DataSource;
        public int m_PortIndex;
        public VFXContextDesc.Type context { get {return m_Context;}}
        private VFXContextDesc.Type m_Context;

        public VFXEdFlowAnchor(int portIndex, Type type, VFXContextDesc.Type context, VFXEdDataSource data, Direction direction)
        {
            m_Type = type;
            scale = new Vector3(64.0f, 32.0f, 1.0f);

            AddManipulator(new FlowEdgeConnector());
            m_Direction = direction;
            m_Context = context;

            Type genericClass = typeof(PortSource<>);
            Type constructedClass = genericClass.MakeGenericType(type);
            m_Source = Activator.CreateInstance(constructedClass);
            m_DataSource = data;
            m_PortIndex = portIndex;
            zIndex = -999;
        }

        public override void Layout()
        {
            scale = new Vector3(64.0f, 32.0f, 1.0f);
            base.Layout();
        }

        public override void Render(Rect parentRect, Canvas2D canvas)
        {
            base.Render(parentRect, canvas);
            GUI.color = VFXEditor.styles.GetContextColor(m_Context);
            switch (m_Direction)
            {
                case Direction.Input:
                    GUI.DrawTexture(GetDrawableRect(), VFXEditor.styles.FlowConnectorIn.normal.background);
                    break;

                case Direction.Output:
                    GUI.DrawTexture(GetDrawableRect(), VFXEditor.styles.FlowConnectorOut.normal.background);
                    break;

                default:
                    break;
            }
            GUI.color = Color.white;
        }

        public void RenderOverlay(Canvas2D canvas)
        {
            RectOffset o;
            GUI.color = VFXEditor.styles.GetContextColor(m_Context)*2;
            switch (m_Direction)
            {
                case Direction.Input:
                    o = VFXEditor.styles.ConnectorOverlay.overflow;
                    
                    GUI.DrawTexture(canvas.CanvasToScreen(o.Add(canvasBoundingRect)), VFXEditor.styles.ConnectorOverlay.normal.background);
                    break;

                case Direction.Output:
                    o = VFXEditor.styles.ConnectorOverlay.overflow;
                    GUI.DrawTexture(canvas.CanvasToScreen(o.Add(canvasBoundingRect)), VFXEditor.styles.ConnectorOverlay.normal.background);

                    break;

                default:
                    break;
            }
            GUI.color = Color.white;
        }

        // IConnect
        public Direction GetDirection()
        {
            return m_Direction;
        }

        public Orientation GetOrientation()
        {
            return Orientation.Vertical;
        }

        public void Highlight(bool highlighted)
        {

        }

        public object Source()
        {
            return m_Source;
        }

        public Vector3 ConnectPosition()
        {
            return GetDrawableRect(true).center;
        }

        public void OnConnect(IConnect other)
        {

            if(other != null)
            {
                VFXEdFlowAnchor otherConnector = other as VFXEdFlowAnchor;
                if (m_DataSource.ConnectFlow(this, otherConnector))
                {
                    ParentCanvas().ReloadData();
                }
            }
            else
            {
                ExposeNodeMenu(Event.current.mousePosition);
            }
        }

        public void ExposeNodeMenu(Vector2 position)
        {

            VFXContextDesc currentContextDesc = (parent as VFXEdContextNode).Model.Desc;
            if(currentContextDesc.m_Type != VFXContextDesc.Type.kTypeOutput)
            {

                bool showInitItems = (currentContextDesc.m_Type != VFXContextDesc.Type.kTypeInit && currentContextDesc.m_Type != VFXContextDesc.Type.kTypeUpdate);
                bool showUpdateItems = (currentContextDesc.m_Type != VFXContextDesc.Type.kTypeUpdate);

                MiniMenu.MenuSet items = new MiniMenu.MenuSet();

                var contexts = VFXEditor.ContextLibrary.GetContexts();

                foreach(VFXContextDesc desc in contexts)
                {
                    if(!showInitItems && desc.m_Type == VFXContextDesc.Type.kTypeInit)
                        continue;
                    if (!showUpdateItems && desc.m_Type == VFXContextDesc.Type.kTypeUpdate)
                        continue;
                    
                    items.AddMenuEntry("Add " + VFXContextDesc.GetTypeName(desc.m_Type)+ " ..." , desc.Name , ExposeNode, new ExposeNodeInfo(desc, this));
                }

                MiniMenu.Show(position, items);
            }
        }


        public void ExposeNode(Vector2 mousePosition, object exposeNodeInfo)
        {
            ExposeNodeInfo info = (ExposeNodeInfo)exposeNodeInfo;
            VFXEdCanvas canvas = (VFXEdCanvas)ParentCanvas();

            Vector2 canvasMousePosition = canvas.MouseToCanvas(mousePosition);
            Vector2 OffsetPosition = canvasMousePosition - new Vector2(VFXEditorMetrics.NodeDefaultWidth/2 , 10);
            VFXContextModel context =  m_DataSource.CreateContext(info.ContextDesc, OffsetPosition);
            canvas.ReloadData();

            VFXEdContextNode node = m_DataSource.GetUI<VFXEdContextNode>(context);

            // Connect
            m_DataSource.ConnectFlow(this, node.inputs[0]);
            ParentCanvas().ReloadData();
            
        }

    internal class ExposeNodeInfo
    {
        public VFXContextDesc ContextDesc;
        public VFXEdFlowAnchor Anchor;

        public ExposeNodeInfo(VFXContextDesc desc, VFXEdFlowAnchor anchor)
        {
            ContextDesc = desc;
            Anchor = anchor;
        }

    }


    }
}
