using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental.VFX
{
    internal class VFXUISpawnerNode : VFXEdNode, VFXModelHolder
    {
        public VFXSpawnerNodeModel Model { get { return m_Model; } }

        public VFXElementModel GetAbstractModel() { return Model; }

        private VFXSpawnerNodeModel m_Model;

        internal VFXUISpawnerNode(VFXSpawnerNodeModel model, VFXEdDataSource dataSource) 
            : base (model.UIPosition, dataSource)
        {
            m_Model = model;
            m_Title = "Trigger";

            m_Inputs.Add(new VFXEdFlowAnchor(0, typeof(float), VFXContextDesc.Type.kTypeNone, m_DataSource, Direction.Input));
            m_Outputs.Add(new VFXEdFlowAnchor(1, typeof(float), VFXContextDesc.Type.kTypeNone, m_DataSource, Direction.Output));

            AddChild(inputs[0]);
            AddChild(outputs[0]);
            ZSort();
            Layout();

        }

        protected override MiniMenu.MenuSet GetNodeMenu(Vector2 mousePosition)
        {
            MiniMenu.MenuSet menu = new MiniMenu.MenuSet();
            menu.AddItem("Not Implemented", new MiniMenu.HeaderItem("Check Back Later!"));
            return menu;
        }

        public override void OnAddNodeBlock(VFXEdNodeBlock nodeblock, int index)
        {
            throw new NotImplementedException();
        }

        public override bool AcceptNodeBlock(VFXEdNodeBlockDraggable block)
        {
            return Model.CanAddChild(block.GetAbstractModel());
        }

        public override void Render(Rect parentRect, Canvas2D canvas)
        {
            Rect r = m_ClientArea;

            GUI.Box(r, "", VFXEditor.styles.Node);
            GUI.Label(new Rect(0, r.y, r.width, 24), title, VFXEditor.styles.NodeTitle);

            base.Render(parentRect, canvas);


        }
    }
}
