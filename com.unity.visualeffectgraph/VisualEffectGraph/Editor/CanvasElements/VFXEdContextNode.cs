using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
    internal class VFXEdContextNode : VFXEdNode
    {

        public VFXContextModel Model
		{
			get { return m_Model; }
		}

        public VFXEdContext context
        {
			get { return m_Context; }
        }

		protected VFXContextModel m_Model;
        protected VFXEdContext m_Context;


        internal VFXEdContextNode(Vector2 canvasPosition, VFXEdContext context, VFXEdDataSource dataSource) 
            : base (canvasPosition, dataSource)
        {
            // TODO Use only one enum
            VFXContextModel.Type type;
            switch (context)
            {
                case VFXEdContext.Initialize:
                    type = VFXContextModel.Type.kTypeInit;
                    break;
                case VFXEdContext.Update:
                    type = VFXContextModel.Type.kTypeUpdate;
                    break;
                case VFXEdContext.Output:
                    type = VFXContextModel.Type.kTypeOutput;
                    break;
                default:
                    type = VFXContextModel.Type.kTypeNone;
                    break;
            }
            m_Model = new VFXContextModel(type);
            m_Title = context.ToString();
            target = new VFXEdContextNodeTarget();
            (target as VFXEdContextNodeTarget).targetNode = this;
            m_Context = context;

            // Create a dummy System to hold the newly created context
            VFXSystemModel systemModel = new VFXSystemModel();
            systemModel.AddChild(m_Model);
            VFXEditor.AssetModel.AddChild(systemModel);

            m_Inputs.Add(new VFXEdFlowAnchor(1, typeof(float), m_Context, m_DataSource, Direction.Input));
            m_Outputs.Add(new VFXEdFlowAnchor(2, typeof(float), m_Context, m_DataSource, Direction.Output));

            AddChild(inputs[0]);
            AddChild(outputs[0]);
            ZSort();
            Layout();

        }

        public override void OnRemove()
        {
            base.OnRemove();

            VFXSystemModel owner = Model.GetOwner();
            if (owner != null)
            {
                int nbChildren = owner.GetNbChildren();
                int index = owner.GetIndex(Model);

                Model.Detach();
                if (index != 0 && index != nbChildren - 1)
                {
                    // if the node is in the middle of a system, we need to create a new system
                    VFXSystemModel newSystem = new VFXSystemModel();
                    while (owner.GetNbChildren() > index)
                        owner.GetChild(index).Attach(newSystem);
                    newSystem.Attach(VFXEditor.AssetModel);
                }
            }

        }

        protected override void ShowNodeBlockMenu(Vector2 canvasClickPosition)
        {
            GenericMenu menu = new GenericMenu();

            ReadOnlyCollection<VFXBlock> blocks = VFXEditor.BlockLibrary.GetBlocks();
                
            // Add New...
            foreach (VFXBlock block in blocks)
            {
                // TODO : Only add item if block is compatible with current context.
                menu.AddItem(new GUIContent("Add New/"+block.m_Category + block.m_Name), false, AddNodeBlock, new VFXEdProcessingNodeBlockSpawner(canvasClickPosition,block, this, m_DataSource));
            }
            

            // Replace Current...
            if (OwnsBlock((ParentCanvas() as VFXEdCanvas).SelectedNodeBlock))
            {
                menu.AddSeparator("");
                foreach (VFXBlock block in blocks)
                {
                    // TODO : Only add item if block is compatible with current context.
                    menu.AddItem(new GUIContent("Replace By/"+block.m_Category + block.m_Name), false, ReplaceNodeBlock, new VFXEdProcessingNodeBlockSpawner(canvasClickPosition,block, this, m_DataSource));
                }
            }

            // TODO : Layout Functions
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Layout/Blocks/Collapse UnConnected"), false, CollapseUnconnected);
            menu.AddItem(new GUIContent("Layout/Blocks/Collapse Connected"), false, CollapseConnected);
            menu.AddItem(new GUIContent("Layout/Blocks/Collapse All"), false, CollapseAll );
            menu.AddItem(new GUIContent("Layout/Blocks/Expand All"), false, ExpandAll);
            menu.AddItem(new GUIContent("Layout/Node/Layout Neighbors"), false, null);
            menu.AddItem(new GUIContent("Layout/Node/Align with Previous"), false, null);
            menu.AddItem(new GUIContent("Layout/Node/Align with Next"), false, null);

            menu.ShowAsContext();
        }


        public void CollapseUnconnected()
        {
            foreach(VFXEdNodeBlock block in NodeBlockContainer.nodeBlocks)
            {
                block.collapsed = !block.IsConnected();
            }
            Layout();
        }

        public void CollapseConnected()
        {
            foreach(VFXEdNodeBlock block in NodeBlockContainer.nodeBlocks)
            {
                block.collapsed = block.IsConnected();
            }
            Layout();
        }


        public void CollapseAll()
        {
            foreach(VFXEdNodeBlock block in NodeBlockContainer.nodeBlocks)
            {
                block.collapsed = true;
            }
            Layout();
        }

        public void ExpandAll()
        {
            foreach(VFXEdNodeBlock block in NodeBlockContainer.nodeBlocks)
            {
                block.collapsed = false;
            }
            Layout();
        }


        public override void OnAddNodeBlock(VFXEdNodeBlock nodeblock, int index)
        {
            Model.AddChild((nodeblock as VFXEdProcessingNodeBlock).Model,index);
        }

        public override bool AcceptNodeBlock(VFXEdNodeBlock block)
        {
            return block is VFXEdProcessingNodeBlock;
        }

        public override void Render(Rect parentRect, Canvas2D canvas)
        {
            Rect r = m_ClientArea;

            if(parent is VFXEdCanvas) {

                Color c =  VFXEditor.styles.GetContextColor(m_Context);
                float a = 0.7f;
                GUI.color = new Color(c.r/a, c.g/a, c.b/a, a);
                GUI.Box(VFXEditorMetrics.NodeImplicitContextOffset.Add(new Rect(0, 0, scale.x, scale.y)), "", VFXEditor.styles.Context);
                GUI.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
            }
           

            GUI.Box(r, "", VFXEditor.styles.Node);
            GUI.Label(new Rect(0, r.y, r.width, 24), title, VFXEditor.styles.NodeTitle);



            base.Render(parentRect, canvas);


        }
    }
}
