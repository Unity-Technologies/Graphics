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

        private VFXEdDataSource m_DataSource;
        private VFXEdNodeClientArea m_NodeClientArea;
        private List<VFXEdFlowAnchor> m_Inputs;
        private List<VFXEdFlowAnchor> m_Outputs;



        public VFXEdNode(Vector2 canvasposition, Vector2 size, VFXEdDataSource dataSource)
        {
            m_DataSource = dataSource;
            translation = canvasposition;
            m_Title = "(Generic Node)";
            scale = new Vector2(size.x, size.y + 46);

            m_Inputs = new List<VFXEdFlowAnchor>();
            m_Outputs = new List<VFXEdFlowAnchor>();

            m_NodeClientArea = new VFXEdNodeClientArea(Vector2.zero, size, dataSource, title);
            m_Inputs.Add(new VFXEdFlowAnchor(1, typeof(float), this, m_DataSource, Direction.Input));
            m_Outputs.Add(new VFXEdFlowAnchor(2, typeof(float), this, m_DataSource, Direction.Output));

            AddChild(inputs[0]);
            AddChild(outputs[0]);
            AddChild(m_NodeClientArea);

            AddManipulator(new Draggable(m_NodeClientArea.elementRect, false));
            AddManipulator(new NodeDelete());

            AllEvents += ManageSelection;

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

            scale = new Vector2(scale.x, m_NodeClientArea.scale.y + 50);
            //Inputs
            for (int i = 0; i < inputs.Count; i++)
            {
                inputs[i].translation = new Vector2((i + 1) * (scale.x / (inputs.Count + 1)) - 32, 0.0f);
            }

            //Outputs
            for (int i = 0; i < outputs.Count; i++)
            {
                outputs[i].translation = new Vector2((i + 1) * (scale.x / (outputs.Count + 1)) - 32, m_NodeClientArea.scale.y + 12);
            }


        }

        public override void Render(Rect parentRect, Canvas2D canvas)
        {
            base.Render(parentRect, canvas);
        }


    }
}

