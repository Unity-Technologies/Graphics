using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.VFX;
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
            scale = new Vector2(VFXEditorMetrics.NodeDefaultWidth, 100);

            m_Inputs.Add(new VFXEdFlowAnchor(0, typeof(float), VFXContextDesc.Type.kTypeNone, m_DataSource, Direction.Input, "Start"));
            m_Inputs.Add(new VFXEdFlowAnchor(1, typeof(float), VFXContextDesc.Type.kTypeNone, m_DataSource, Direction.Input, "Stop"));

            m_Outputs.Add(new VFXEdFlowAnchor(2, typeof(float), VFXContextDesc.Type.kTypeNone, m_DataSource, Direction.Output));

            AddChild(inputs[0]);
            AddChild(inputs[1]);
            AddChild(outputs[0]);

            ZSort();
            Layout();
        }

        protected override MiniMenu.MenuSet GetNodeMenu(Vector2 mousePosition)
        {
            MiniMenu.MenuSet menu = new MiniMenu.MenuSet();
            menu.AddItem("Add New...", new MiniMenu.CallbackItem("Constant Rate", AddSpawnBlock, VFXSpawnerBlockModel.Type.kConstantRate));
            menu.AddItem("Add New...", new MiniMenu.CallbackItem("Variable Rate", AddSpawnBlock,VFXSpawnerBlockModel.Type.kVariableRate));
            menu.AddItem("Add New...", new MiniMenu.CallbackItem("Simple Burst", AddSpawnBlock,VFXSpawnerBlockModel.Type.kBurst));
            menu.AddItem("Add New...", new MiniMenu.CallbackItem("Periodic Burst", AddSpawnBlock,VFXSpawnerBlockModel.Type.kPeriodicBurst));
            menu.AddItem("Add New...", new MiniMenu.CallbackItem("Custom Callback (WIP)", AddSpawnBlock, VFXSpawnerBlockModel.Type.kCustomCallback));
            return menu;
        }

        private void AddSpawnBlock(Vector2 position, object type)
        {
            VFXSpawnerBlockModel.Type modeltype = (VFXSpawnerBlockModel.Type)type;
            VFXSpawnerBlockModel spawnerBlock = new VFXSpawnerBlockModel(modeltype);
            DataSource.Create(spawnerBlock, Model);
        }

        public override void OnAddNodeBlock(VFXEdNodeBlock nodeblock, int index)
        {
            throw new NotImplementedException();
        }

        public override bool AcceptNodeBlock(VFXEdNodeBlockDraggable block)
        {
            return Model.CanAddChild(block.GetAbstractModel());
        }

        public override void UpdateModel(UpdateType t)
        {
            Model.UpdatePosition(translation);
        }

        public override void Render(Rect parentRect, Canvas2D canvas)
        {
            Rect r = m_ClientArea;

            GUI.Box(r, "", VFXEditor.styles.Node);
            GUI.Label(new Rect(0, r.y, r.width, 24), "Spawner", VFXEditor.styles.NodeTitle);

            base.Render(parentRect, canvas);
        }
    }

    internal class VFXUISpawnerBlock : VFXEdNodeBlockDraggable
    {
        public VFXSpawnerBlockModel Model { get { return m_Model; } }
        public override VFXElementModel GetAbstractModel() { return Model; }
        private VFXSpawnerBlockModel m_Model;

        internal VFXUISpawnerBlock(VFXSpawnerBlockModel model, VFXEdDataSource dataSource)
            : base(dataSource)
        {
            m_Model = model;
            collapsed = Model.UICollapsed;

            int nbSlots = Model.GetNbInputSlots();
            m_Fields = new VFXUIPropertySlotField[nbSlots];
            for (int i = 0; i < nbSlots; ++i)
            {
                m_Fields[i] = new VFXUIPropertySlotField(dataSource, Model.GetInputSlot(i));
                AddChild(m_Fields[i]);
            }

            var header = new VFXEdNodeBlockHeaderSimple(VFXSpawnerBlockModel.TypeToName(model.SpawnerType), null, model.GetNbInputSlots() > 0);
            AddChild(header);
        }

        public override void UpdateModel(UpdateType t)
        {
            Model.UpdateCollapsed(collapsed);
        }

        // TODO Not sure this is needed anymore, remove that ?
        public override VFXPropertySlot GetSlot(string name) { return null; }
        public override void SetSlotValue(string name, VFXValue value) {}
    }
}
