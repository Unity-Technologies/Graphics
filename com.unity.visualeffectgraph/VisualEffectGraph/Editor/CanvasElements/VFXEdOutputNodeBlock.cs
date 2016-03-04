using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{

    internal class VFXEdOutputNodeBlockPoint : VFXEdOutputNodeBlock
    {
        public VFXEdOutputNodeBlockPoint(VFXEdDataSource dataSource) : base(dataSource,0) { }
    }

    internal class VFXEdOutputNodeBlockBillboard : VFXEdOutputNodeBlock
    {
        public VFXEdOutputNodeBlockBillboard(VFXEdDataSource dataSource) : base(dataSource,1)
        {
            m_Fields = new VFXEdNodeBlockParameterField[3];
            m_Fields[0] = new VFXEdNodeBlockParameterField(dataSource, "Texture", "_MainTex", new VFXParamValueTexture2D(), false, Direction.Input, 0);
            m_Fields[1] = new VFXEdNodeBlockParameterField(dataSource, "U Orientation", "Orientation", new VFXParamValueInt(), false, Direction.Input, 1);
            m_Fields[2] = new VFXEdNodeBlockParameterField(dataSource, "V Orientation", "Orientation", new VFXParamValueInt(), false, Direction.Input, 2);
            AddChild(m_Fields[0]);
            AddChild(m_Fields[1]);
            AddChild(m_Fields[2]);
            Header.Collapseable = true;
        }
    }

    internal class VFXEdOutputNodeBlockVelocity : VFXEdOutputNodeBlock
    {
        public VFXEdOutputNodeBlockVelocity(VFXEdDataSource dataSource) : base(dataSource,2)
        {
            m_Fields = new VFXEdNodeBlockParameterField[3];
            m_Fields[0] = new VFXEdNodeBlockParameterField(dataSource, "Texture", "_MainTex", new VFXParamValueTexture2D(), false, Direction.Input, 0);
            m_Fields[1] = new VFXEdNodeBlockParameterField(dataSource, "U Orientation", "Orientation", new VFXParamValueInt(), false, Direction.Input, 1);
            m_Fields[2] = new VFXEdNodeBlockParameterField(dataSource, "V Orientation", "Orientation", new VFXParamValueInt(), false, Direction.Input, 2);
            AddChild(m_Fields[0]);
            AddChild(m_Fields[1]);
            AddChild(m_Fields[2]);

            Header.Collapseable = true;
        }
    }

    internal class VFXEdOutputNodeBlock : CanvasElement
    {
        public string Name
        {
            get {
               switch(m_renderType)
                {
                    case 0: return "Point"; 
                    case 1: return "Billboard"; 
                    case 2: return "Velocity Quad"; 
                    default: return "UNKNOWN";
                }
            }
        }

        public VFXEdNodeBlockHeader Header { get { return m_Header; } }
        public int RenderType { get { return m_renderType; } }
        protected VFXEdNodeBlockParameterField[] m_Fields;

        protected VFXEdDataSource m_DataSource;
        private NodeBlockManipulator m_NodeBlockManipulator;
        private int m_renderType;
        private VFXEdNodeBlockHeader m_Header;

        public VFXEdOutputNodeBlock(VFXEdDataSource dataSource, int renderType)
        {
            m_DataSource = dataSource;
            translation = Vector3.zero; 
            m_Caps = Capabilities.Normal;
            m_renderType = renderType;
            m_Fields = new VFXEdNodeBlockParameterField[0];
            m_Header = new VFXEdNodeBlockHeader(Name, VFXEditor.styles.GetIcon("Box"), false);
            AddChild(m_Header);
        }

        public void BindTo(VFXEdOutputNode node)
        {
            VFXEditor.AssetModel.OutputType = m_renderType;
        }


        // Retrieve the full height of the block
        public virtual float GetHeight()
        {
            float height = VFXEditorMetrics.NodeBlockHeaderHeight;
            foreach(VFXEdNodeBlockParameterField field in m_Fields) {
                height += field.scale.y + VFXEditorMetrics.NodeBlockParameterSpacingHeight;
            }
            height += VFXEditorMetrics.NodeBlockFooterHeight;
            return height;
        }

        public override void Layout()
        {
            base.Layout();

            if (collapsed)
            {
                scale = new Vector2(scale.x, VFXEditorMetrics.NodeBlockHeaderHeight);

                // if collapsed, rejoin all connectors on the middle of the header
                foreach(VFXEdNodeBlockParameterField field in m_Fields)
                {
                    field.translation = new Vector2(0.0f, (VFXEditorMetrics.NodeBlockHeaderHeight-VFXEditorMetrics.DataAnchorSize.y)/2);
                }
            }
            else
            {
                scale = new Vector2(scale.x, GetHeight());
                float curY = VFXEditorMetrics.NodeBlockHeaderHeight;

                foreach(VFXEdNodeBlockParameterField field in m_Fields)
                {
                    field.translation = new Vector2(0.0f, curY);
                    curY += field.scale.y + VFXEditorMetrics.NodeBlockParameterSpacingHeight;
                }

            }
        }

        public override void Render(Rect parentRect, Canvas2D canvas)
        {
            Rect r = GetDrawableRect();
            GUI.color = new Color(0.75f,0.75f,0.75f,1.0f);
            GUI.Box(r, "", VFXEditor.styles.DataNodeBlock);
            GUI.color = Color.white;
            base.Render(parentRect, canvas);
        }
    }
}
