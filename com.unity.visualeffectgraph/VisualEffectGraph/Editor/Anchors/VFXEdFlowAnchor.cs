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
                GenericMenu menu = new GenericMenu();
                VFXEditor.ContextLibrary.GetContexts();

                bool showInitItems = (currentContextDesc.m_Type != VFXContextDesc.Type.kTypeInit && currentContextDesc.m_Type != VFXContextDesc.Type.kTypeUpdate);
                bool showUpdateItems = (currentContextDesc.m_Type != VFXContextDesc.Type.kTypeUpdate);

                foreach(VFXContextDesc desc in VFXEditor.ContextLibrary.GetContexts())
                {
                    if(!showInitItems && desc.m_Type == VFXContextDesc.Type.kTypeInit)
                        continue;
                    if (!showUpdateItems && desc.m_Type == VFXContextDesc.Type.kTypeUpdate)
                        continue;
                    menu.AddItem(new GUIContent(VFXContextDesc.GetFriendlyName(desc.m_Type) + "/" + desc.Name), false, ExposeNode, new ExposeNodeInfo(position, desc.Name , this));
                }
            
                menu.ShowAsContext();
            }
        }

        public void ExposeNode(object exposeNodeInfo)
        {
            ExposeNodeInfo info = (ExposeNodeInfo)exposeNodeInfo;
            VFXContextDesc desc = VFXEditor.ContextLibrary.GetContext(info.ContextName);
            VFXEdCanvas canvas = (VFXEdCanvas)ParentCanvas();

            VFXEdContextNodeSpawner spawner = new VFXEdContextNodeSpawner(m_DataSource, canvas, info.Position, desc);
            spawner.Spawn();
            VFXEdContextNode node = spawner.SpawnedNode;

            // Connect
            m_DataSource.ConnectFlow(this, node.inputs[0]);
            ParentCanvas().ReloadData();
            
        }

    internal class ExposeNodeInfo
    {
        public Vector2 Position;
        public string ContextName;
        public VFXEdFlowAnchor Anchor;

        public ExposeNodeInfo(Vector2 canvasPosition, string contextName, VFXEdFlowAnchor anchor)
        {
            Position = canvasPosition;
            ContextName = contextName;
            Anchor = anchor;
        }

    }


    }
}
