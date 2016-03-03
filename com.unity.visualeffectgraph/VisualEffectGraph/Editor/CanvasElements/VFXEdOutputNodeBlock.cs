using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
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
            m_Header = new VFXEdNodeBlockHeader(Name, VFXEditor.styles.GetIcon("Box"), false);
            AddChild(m_Header);
        }

        public void BindTo(VFXEdOutputNode node)
        {
            VFXEditor.AssetModel.OutputType = m_renderType;
        }

        public float GetHeight()
        {
            return m_Header.scale.y;
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
