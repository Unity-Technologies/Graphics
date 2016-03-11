using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
    internal abstract class VFXEdNode : VFXEdNodeBase 
    {

        public string title
        {
            get { return m_Title; }
        }


        public VFXEdNodeBlockContainer NodeBlockContainer
        {
            get { return m_NodeBlockContainer; }
        }

        protected string m_Title;


        protected VFXEdNodeBlockContainer m_NodeBlockContainer;
        protected float m_HeaderOffset = 0.0f;
        public VFXEdNode(Vector2 canvasposition, VFXEdDataSource dataSource) : base(canvasposition, dataSource)
        {
            m_DataSource = dataSource;

            scale = new Vector2(VFXEditorMetrics.NodeDefaultWidth, 100);

            m_Inputs = new List<VFXEdFlowAnchor>();
            m_Outputs = new List<VFXEdFlowAnchor>();

            m_NodeBlockContainer = new VFXEdNodeBlockContainer(this.scale);
            AddChild(m_NodeBlockContainer);

            MouseDown += ManageSelection;
            this.ContextClick += ManageRightClick;

        }

        protected abstract GenericMenu GetNodeMenu(Vector2 canvasClickPosition);

        private bool ManageRightClick(CanvasElement element, Event e, Canvas2D parent)
        {
            if (e.type == EventType.Used)
                return false;

            GenericMenu m = GetNodeMenu(parent.MouseToCanvas(e.mousePosition));
            m.ShowAsContext();
            e.Use();
            return true;
        }

        public bool OwnsBlock(VFXEdNodeBlockDraggable block)
        {
            return m_NodeBlockContainer.OwnsBlock(block);
        }

        public void AddNodeBlock(object o)
        {
            VFXEdSpawner spawner = o as VFXEdSpawner;
            if(spawner != null)
            {
                spawner.Spawn();
            }
            (ParentCanvas() as VFXEdCanvas).SelectedNodeBlock = null;
            Layout();
        }

        public void ReplaceNodeBlock(object o)
        {
            VFXEdSpawner spawner = o as VFXEdSpawner;
            if(spawner != null)
            {
                spawner.Spawn();
            }
            NodeBlockContainer.RemoveNodeBlock((ParentCanvas() as VFXEdCanvas).SelectedNodeBlock);
            (ParentCanvas() as VFXEdCanvas).SelectedNodeBlock = null;
            Layout();
        }

        public override void OnRemove()
        {
            NodeBlockContainer.ClearNodeBlocks();
        }

        public abstract bool AcceptNodeBlock(VFXEdNodeBlock block);

        public abstract void OnAddNodeBlock(VFXEdNodeBlock nodeblock, int index);



        private bool ManageSelection(CanvasElement element, Event e, Canvas2D parent)
        {

            if (selected)
            {
                (parent as VFXEdCanvas).SetSelectedNodeBlock(null);
            }

            return false;
        }

        public override void Layout()
        {
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

            m_ClientArea = new Rect(0.0f, inputheight, this.scale.x, m_NodeBlockContainer.scale.y+ VFXEditorMetrics.NodeHeaderHeight + m_HeaderOffset);
            m_ClientArea = VFXEditorMetrics.NodeClientAreaOffset.Add(m_ClientArea);

            m_NodeBlockContainer.translation = m_ClientArea.position + VFXEditorMetrics.NodeBlockContainerPosition + new Vector2(0.0f,m_HeaderOffset);
            m_NodeBlockContainer.scale = new Vector2(m_ClientArea.width, m_NodeBlockContainer.scale.y) + VFXEditorMetrics.NodeBlockContainerSizeOffset;

            scale = new Vector3(scale.x, inputheight + outputheight + m_ClientArea.height);
            
            // Flow Inputs
            for (int i = 0; i < inputs.Count; i++)
            {
                inputs[i].translation = new Vector2((i + 1) * (scale.x / (inputs.Count + 1)) - VFXEditorMetrics.FlowAnchorSize.x/2, 4.0f);
            }

            // Flow Outputs
            for (int i = 0; i < outputs.Count; i++)
            {
                outputs[i].translation = new Vector2((i + 1) * (scale.x / (outputs.Count + 1)) - VFXEditorMetrics.FlowAnchorSize.x/2, scale.y - VFXEditorMetrics.FlowAnchorSize.y-10);
            }

        }

        public override void Render(Rect parentRect, Canvas2D canvas)
        {
            if (selected)
                    GUI.Box(m_ClientArea, "", VFXEditor.styles.NodeSelected);
            base.Render(parentRect, canvas);
        }


    }
}

