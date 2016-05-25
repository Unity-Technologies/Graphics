using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEditor.Experimental;
//using UnityEditor.Experimental.Graph;
using UnityEditor.Experimental.VFX;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
    internal class VFXEdDataNode : VFXEdNode, VFXModelHolder
    {
        public bool exposed { get { return m_ExposeOption.Enabled; } set { m_ExposeOption.Enabled = value; } }
        public VFXDataNodeModel Model { get { return m_Model; } }
        
        protected VFXEdExposeDataNodeOption m_ExposeOption;

        private VFXDataNodeModel m_Model;

        public VFXElementModel GetAbstractModel() { return Model; }

        internal VFXEdDataNode(VFXDataNodeModel model, VFXEdDataSource dataSource) 
            : base (model.UIPosition, dataSource)
        {
            m_Model = model;
            m_Title = "Data Node";
            m_ExposeOption = new VFXEdExposeDataNodeOption(Model);
            AddChild(m_ExposeOption);
            Layout();
        }

        public override void Layout()
        {
            base.Layout();
            m_ExposeOption.translation = m_ClientArea.position + new Vector2(8.0f,-4.0f);
        }

        public override void UpdateModel(UpdateType t)
        {
            Model.UpdatePosition(translation);
        }

        protected override MiniMenu.MenuSet GetNodeMenu(Vector2 mousePosition)
        {
            MiniMenu.MenuSet menu = new MiniMenu.MenuSet();
            menu.AddMenuEntry("Parameters", "Add...", AddParameterBlock, null);
            return menu;
        }

        public void AddParameterBlock(Vector2 mousePosition, object o)
        {
            VFXFilterPopup.ShowNewDataBlockPopup(this, mousePosition, ParentCanvas(), true);
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
