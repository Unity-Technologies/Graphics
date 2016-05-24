using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEditor.Experimental;
using UnityEditor.Experimental.VFX;
using UnityEditor.Experimental.Graph;

using Object = UnityEngine.Object;
using VFXBLKLibrary = UnityEditor.VFXBlockLibrary;

namespace UnityEditor.Experimental
{
    internal class VFXEdContextNode : VFXEdNode, VFXModelHolder
    {

        public VFXEdContextNodeBlock ContextNodeBlock
        {
            get { return m_ContextNodeBlock; }
            set
            {
                if (m_ContextNodeBlock != null)
                    RemoveChild(m_ContextNodeBlock);
                m_ContextNodeBlock = value;
                if (m_ContextNodeBlock != null)
                    AddChild(m_ContextNodeBlock);
            }
        }
        private VFXEdContextNodeBlock m_ContextNodeBlock;

        public VFXContextModel Model    { get { return m_Model; } }
        public VFXContextDesc Desc      { get { return Model.Desc; } }
        public VFXContextDesc.Type Context     { get { return Desc.m_Type; } }

        public VFXElementModel GetAbstractModel() { return Model; }

	protected VFXContextModel m_Model;

        internal VFXEdContextNode(VFXContextModel model, VFXEdDataSource dataSource) 
            : base(model.UIPosition,dataSource)
        {
            m_Model = model;
            collapsed = model.UICollapsed;

            m_Title = VFXContextDesc.GetTypeName(Context);
            target = ScriptableObject.CreateInstance<VFXEdContextNodeTarget>();
            (target as VFXEdContextNodeTarget).targetNode = this;

            SetContext(Desc);

            m_Inputs.Add(new VFXEdFlowAnchor(1, typeof(float), Context, m_DataSource, Direction.Input));
            m_Outputs.Add(new VFXEdFlowAnchor(2, typeof(float), Context, m_DataSource, Direction.Output));

            AddChild(inputs[0]);
            AddChild(outputs[0]);

            AddManipulator(new TooltipManipulator(GetTooltipText));

            ZSort();
            Layout();
        }

        protected virtual List<string> GetTooltipText()
        {
            List<string> lines = new List<string>();
            lines = VFXModelDebugInfoProvider.GetInfo(lines, Model, VFXModelDebugInfoProvider.InfoFlag.kDefault);
            return lines;
        }

        public void SetSlotValue(string name, VFXValue value)
        {
            VFXContextModel model = ContextNodeBlock.Model;
            for(int i = 0; i < model.GetNbSlots(); i++)
            {
                if (model.Desc.m_Properties[i].m_Name == name)
                {
                    model.GetSlot(i).Value = value; 
                }
            }
        }

        private static string FormatMenuString(VFXBlockDesc block)
        {
            return block.Category + (block.Category.Length != 0 ? "/" : "") + block.Name;
        }

        protected override GenericMenu GetNodeMenu(Vector2 canvasClickPosition)
        {
            GenericMenu menu = new GenericMenu();

            // Use an additional list to sort blocks in menu
            var blocks = new List<VFXBlockDesc>(VFXEditor.BlockLibrary.GetBlocks());
            blocks.Sort((blockA, blockB) => {
                int res = blockA.Category.CompareTo(blockB.Category);
                return res != 0 ? res : blockA.Name.CompareTo(blockB.Name);
            });
                
            // Add New...
            foreach (VFXBlockDesc block in blocks)
            {
                // TODO : Only add item if block is compatible with current context.
                menu.AddItem(new GUIContent("Add New/" + FormatMenuString(block)), false, AddNodeBlock, new VFXEdProcessingNodeBlockSpawner(canvasClickPosition,block, this, m_DataSource));
            }
            

            // Replace Current...
            if (OwnsBlock((ParentCanvas() as VFXEdCanvas).SelectedNodeBlock))
            {
                menu.AddSeparator("");
                foreach (VFXBlockDesc block in blocks)
                {
                    // TODO : Only add item if block is compatible with current context.
                    menu.AddItem(new GUIContent("Replace By/"+ FormatMenuString(block)), false, ReplaceNodeBlock, new VFXEdProcessingNodeBlockSpawner(canvasClickPosition,block, this, m_DataSource));
                }
            }

            // Switch Context Types

            ReadOnlyCollection<VFXContextDesc> contexts = VFXEditor.ContextLibrary.GetContexts();

            foreach(VFXContextDesc context in contexts)
            {
                if(context.m_Type == Model.Desc.m_Type && context.Name != Model.Desc.Name)
                {
                    menu.AddItem(new GUIContent("Switch "+ VFXContextDesc.GetTypeName(Model.Desc.m_Type) + " Type/" + context.Name), false, MenuSwitchContext, context);
                }
            }


            // TODO : Layout Functions
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Layout/Blocks/Collapse UnConnected"), false, CollapseUnconnected);
            menu.AddItem(new GUIContent("Layout/Blocks/Collapse Connected"), false, CollapseConnected);
            menu.AddItem(new GUIContent("Layout/Blocks/Collapse All"), false, CollapseAll );
            menu.AddItem(new GUIContent("Layout/Blocks/Expand All"), false, ExpandAll);

            return menu;
        }

        public void MenuSwitchContext(object o)
        {
            SetContext(o as VFXContextDesc);
        }

        public void SetContext(VFXContextDesc context)
        {
            // TODO Do we need that ?
            //for(int i = 0; i < Model.GetNbSlots(); i++)
            //    Model.GetSlot(i).Unlink();

            Model.Desc = context;
            if (m_Model.Desc.ShowBlock)
                ContextNodeBlock = new VFXEdContextNodeBlock(m_DataSource, m_Model);
            else
            {
                if (ContextNodeBlock != null)
                {
                    ContextNodeBlock = null;
                    Layout();
                }               
            }

            Invalidate();

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
            //Model.AddChild((nodeblock as VFXEdProcessingNodeBlock).Model,index);
        }

        public override bool AcceptNodeBlock(VFXEdNodeBlock block)
        {
            return block is VFXEdProcessingNodeBlock;
        }

        public override void Layout()
        {
            if (m_ContextNodeBlock != null)
                m_HeaderOffset = m_ContextNodeBlock.GetHeight();
            else
                m_HeaderOffset = 0.0f;

            base.Layout();

            if (m_ContextNodeBlock != null)
            {
                m_ContextNodeBlock.translation = m_ClientArea.position + VFXEditorMetrics.NodeBlockContainerPosition;
                m_ContextNodeBlock.scale = new Vector2(m_NodeBlockContainer.scale.x, m_ContextNodeBlock.GetHeight());
            }
        }

        public override void Render(Rect parentRect, Canvas2D canvas)
        {
            Rect r = m_ClientArea;

            if(parent is VFXEdCanvas) {

                Color c =  VFXEditor.styles.GetContextColor(Context);
                float a = 0.7f;
                GUI.color = new Color(c.r/a, c.g/a, c.b/a, a);
                GUI.Box(VFXEditorMetrics.NodeImplicitContextOffset.Add(new Rect(0, 0, scale.x, scale.y)), "", VFXEditor.styles.Context);
                GUI.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
            }
           
            GUI.Box(r, "", VFXEditor.styles.Node);
            GUI.Label(new Rect(0, r.y, r.width, 24), title, VFXEditor.styles.NodeTitle);

            base.Render(parentRect, canvas);
        }

        public override void UpdateModel(UpdateType t)
        {
            Model.UpdateCollapsed(collapsed);
            Model.UpdatePosition(translation);
        }
    }
}
