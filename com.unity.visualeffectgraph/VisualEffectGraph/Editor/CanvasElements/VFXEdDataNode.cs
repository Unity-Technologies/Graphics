using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
    internal class VFXEdDataNode : VFXEdNode
    {
        public bool exposed { get { return m_ExposeOption.Enabled; } }
        protected VFXEdExposeDataNodeOption m_ExposeOption;
        internal VFXEdDataNode(Vector2 canvasposition, VFXEdDataSource dataSource) 
            : base (canvasposition, dataSource)
        {
            m_Title = "Data Node";
            m_ExposeOption = new VFXEdExposeDataNodeOption();
            this.AddChild(m_ExposeOption);
            Layout();
        }

        public override void Layout()
        {
            base.Layout();
            m_ExposeOption.translation = m_ClientArea.position + new Vector2(8.0f,-4.0f);
        }

        protected override void ShowNodeBlockMenu(Vector2 canvasClickPosition)
        {
           GenericMenu menu = new GenericMenu();

                List<VFXDataBlock> blocks = VFXEditor.DataBlockLibrary.GetBlocks();

                foreach (VFXDataBlock block in blocks)
                {
                    menu.AddItem(new GUIContent(block.path), false, AddNodeBlock, new VFXEdDataNodeBlock(block));
                }

            menu.ShowAsContext();
        }

        public override void OnAddNodeBlock(VFXEdNodeBlock nodeblock, int index)
        {
            
        }

        public override bool AcceptNodeBlock(VFXEdNodeBlock block)
        {
            return block is VFXEdDataNodeBlock;
        }

        public override void Render(Rect parentRect, Canvas2D canvas)
        {
            Rect r = m_ClientArea;

            if(exposed)
            {
                GUI.Box(r, "", VFXEditor.styles.NodeParameters);
                GUI.Label(new Rect(r.x, r.y, r.width, 24), "Parameter Interface", VFXEditor.styles.NodeParametersTitle);
            }
            else
            {
                GUI.Box(r, "", VFXEditor.styles.NodeData);
                GUI.Label(new Rect(r.x, r.y, r.width, 24), "Local Constants", VFXEditor.styles.NodeParametersTitle);
            }  

            base.Render(parentRect, canvas);
        }
    }
}
