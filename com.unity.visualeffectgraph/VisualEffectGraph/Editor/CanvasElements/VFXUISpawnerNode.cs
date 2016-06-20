using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental.VFX
{
    internal class VFXUISpawnerNode : VFXEdNodeBase, VFXModelHolder
    {
        public VFXSpawnerNodeModel Model { get { return m_Model; } }
        public VFXElementModel GetAbstractModel() { return Model; }
        private VFXSpawnerNodeModel m_Model;

        public VFXUIPropertySlotField[] Fields { get { return m_Fields; } }
        protected VFXUIPropertySlotField[] m_Fields;

        internal VFXUISpawnerNode(VFXSpawnerNodeModel model, VFXEdDataSource dataSource) 
            : base (model.UIPosition, dataSource)
        {
            m_Model = model;
            scale = new Vector2(VFXEditorMetrics.NodeDefaultWidth, 100);

            m_Inputs.Add(new VFXEdFlowAnchor(0, typeof(float), VFXContextDesc.Type.kTypeNone, m_DataSource, Direction.Input));
            m_Outputs.Add(new VFXEdFlowAnchor(1, typeof(float), VFXContextDesc.Type.kTypeNone, m_DataSource, Direction.Output));

            AddChild(inputs[0]);
            AddChild(outputs[0]);

            int nbSlots = Model.GetNbInputSlots();
            m_Fields = new VFXUIPropertySlotField[nbSlots];
            for (int i = 0; i < nbSlots; ++i)
            {
                m_Fields[i] = new VFXUIPropertySlotField(dataSource, Model.GetInputSlot(i));
                AddChild(m_Fields[i]);
            }

            AddManipulator(new ImguiContainer());

            ZSort();
            Layout();
        }

       /* protected override MiniMenu.MenuSet GetNodeMenu(Vector2 mousePosition)
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
        }*/

        public override void UpdateModel(UpdateType t)
        {
            Model.UpdatePosition(translation);
        }

        public virtual float GetHeight()
        {
            float height = VFXEditorMetrics.NodeBlockHeaderHeight;
            foreach (var field in m_Fields)
            {
                height += field.scale.y + VFXEditorMetrics.NodeBlockParameterSpacingHeight;
            }
            height += VFXEditorMetrics.NodeBlockFooterHeight;
            return height;
        }

        // TODO Put that in VFXEdNodeBase
        public override void Layout()
        {
            const float HEIGHT = 64.0f;

            base.Layout();

            float inputheight = 0.0f;
            if (inputs.Count > 0)
                inputheight = VFXEditorMetrics.FlowAnchorSize.y;
            else
                inputheight = 16.0f;

            float outputheight = 0.0f;
            if (outputs.Count > 0)
                outputheight = VFXEditorMetrics.FlowAnchorSize.y;
            else
                outputheight = 32.0f;

            m_ClientArea = new Rect(0.0f, inputheight, this.scale.x, GetHeight() + VFXEditorMetrics.NodeHeaderHeight);
            m_ClientArea = VFXEditorMetrics.NodeClientAreaOffset.Add(m_ClientArea);

            // Flow Inputs
            for (int i = 0; i < inputs.Count; i++)
            {
                inputs[i].translation = new Vector2((i + 1) * (scale.x / (inputs.Count + 1)) - VFXEditorMetrics.FlowAnchorSize.x / 2, 4.0f);
            }

            // Flow Outputs
            for (int i = 0; i < outputs.Count; i++)
            {
                outputs[i].translation = new Vector2((i + 1) * (scale.x / (outputs.Count + 1)) - VFXEditorMetrics.FlowAnchorSize.x / 2, scale.y - VFXEditorMetrics.FlowAnchorSize.y - 10);
            }

            // Fields
            scale = new Vector2(scale.x, inputheight + outputheight + m_ClientArea.height);
            float curY = inputheight + VFXEditorMetrics.NodeHeaderHeight + VFXEditorMetrics.NodeBlockHeaderHeight;

            foreach (var field in m_Fields)
            {
                field.translation = new Vector2(0.0f, curY);
                curY += field.scale.y + VFXEditorMetrics.NodeBlockParameterSpacingHeight;
            }
        }

        public override void Render(Rect parentRect, Canvas2D canvas)
        {
            Rect r = m_ClientArea;

            GUI.Box(r, "", VFXEditor.styles.Node);
            GUI.Label(new Rect(0, r.y, r.width, 24), VFXSpawnerNodeModel.TypeToName(Model.SpawnerType), VFXEditor.styles.NodeTitle);

            base.Render(parentRect, canvas);


        }
    }
}
