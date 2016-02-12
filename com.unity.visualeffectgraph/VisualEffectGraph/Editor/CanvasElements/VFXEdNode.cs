using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
    internal class VFXEdNode : CanvasElement
    {

        public string title
        {
            get { return m_Title; }
        }

        private string m_Title;

        internal List<VFXEdFlowAnchor> inputs
        {
            get { return m_Inputs; }
        }
        internal List<VFXEdFlowAnchor> outputs
        {
            get { return m_Outputs; }
        }

        public VFXEdNodeClientArea ClientArea
        { get { return m_NodeClientArea; } }
        public VFXEdNodeBlockContainer NodeBlockContainer {
            get { return m_NodeClientArea.NodeBlockContainer; }
        }


        private VFXEdDataSource m_DataSource;
        private VFXEdNodeClientArea m_NodeClientArea;
        private List<VFXEdFlowAnchor> m_Inputs;
        private List<VFXEdFlowAnchor> m_Outputs;
        private VFXEdContext m_Context;


        public VFXEdNode(Vector2 canvasposition, VFXEdContext context, VFXEdDataSource dataSource)
        {
            m_DataSource = dataSource;
            translation = canvasposition;
            m_Title = context.ToString();
            scale = new Vector2(VFXEditorMetrics.NodeDefaultWidth, 100);
            m_Context = context;
            m_Inputs = new List<VFXEdFlowAnchor>();
            m_Outputs = new List<VFXEdFlowAnchor>();

            m_NodeClientArea = new VFXEdNodeClientArea(VFXEditorMetrics.NodeClientAreaOffset.Add(new Rect(Vector2.zero, scale)), dataSource, title);
            m_Inputs.Add(new VFXEdFlowAnchor(1, typeof(float), this,m_Context, m_DataSource, Direction.Input));
            m_Outputs.Add(new VFXEdFlowAnchor(2, typeof(float), this,m_Context, m_DataSource, Direction.Output));

            AddChild(inputs[0]);
            AddChild(outputs[0]);
            AddChild(m_NodeClientArea);

            AddManipulator(new Draggable(m_NodeClientArea.elementRect, false));
            AddManipulator(new NodeDelete());

            AllEvents += ManageSelection;

            Layout();

        }

        public void MenuAddNodeBlock(object o) {

            VFXEdSpawnData data = o as VFXEdSpawnData;
            VFXEdNodeBlock block = new VFXEdNodeBlock(VFXEditor.BlockLibrary.GetBlock(data.libraryName), m_DataSource);
            AddNodeBlock(VFXEditor.BlockLibrary.GetBlock(data.libraryName));
            data.targetCanvas.ReloadData();
            Layout();
        }

        public void AddNodeBlock(VFXBlock block) {
            NodeBlockContainer.AddNodeBlock(new VFXEdNodeBlock(block, m_DataSource));
        }

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

            scale = new Vector2(scale.x, m_NodeClientArea.scale.y + VFXEditorMetrics.NodeHeightOffset);

            

            // Flow Inputs
            for (int i = 0; i < inputs.Count; i++)
            {
                inputs[i].translation = new Vector2((i + 1) * (scale.x / (inputs.Count + 1)) - VFXEditorMetrics.FlowAnchorSize.y, 0.0f);
            }

            // Flow Outputs
            for (int i = 0; i < outputs.Count; i++)
            {
                outputs[i].translation = new Vector2((i + 1) * (scale.x / (outputs.Count + 1)) - VFXEditorMetrics.FlowAnchorSize.y, m_NodeClientArea.scale.y + (VFXEditorMetrics.FlowAnchorSize.y-20));
            }


        }

        public override void Render(Rect parentRect, Canvas2D canvas)
        {
            if(parent is VFXEdCanvas) {

                Color c =  VFXEditor.styles.GetContextColor(m_Context);
                float a = 0.7f;
                GUI.color = new Color(c.r/a, c.g/a, c.b/a, a);
                GUI.Box(VFXEditorMetrics.NodeImplicitContextOffset.Add(new Rect(0, 0, scale.x, scale.y)), "", VFXEditor.styles.Context);
                GUI.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
            }

            base.Render(parentRect, canvas);
        }


    }
}

