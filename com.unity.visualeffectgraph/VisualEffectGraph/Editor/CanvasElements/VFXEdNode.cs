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
		public string Title;

		internal List<VFXEdFlowAnchor> Inputs
		{
			get { return m_Inputs; }
		}

		internal List<VFXEdFlowAnchor> Outputs
		{
			get { return m_Outputs; }
		}

		private VFXEdDataSource m_DataSource;
		private VFXEdNodeClientArea m_NodeClientArea;
		private List<VFXEdFlowAnchor> m_Inputs;
		private List<VFXEdFlowAnchor> m_Outputs;



		public VFXEdNode (Vector2 canvasposition, Vector2 size, VFXEdDataSource dataSource)
		{
			this.m_DataSource = dataSource;
			this.translation = canvasposition;
			this.Title = "(Generic Node)";
			this.scale = new Vector2(size.x, size.y+46);

			this.m_Inputs = new List<VFXEdFlowAnchor>();
			this.m_Outputs = new List<VFXEdFlowAnchor>();

			this.m_NodeClientArea = new VFXEdNodeClientArea(Vector2.zero, size, dataSource, this.Title);
			this.m_Inputs.Add(new VFXEdFlowAnchor(1, typeof(float), this, m_DataSource, Direction.Input));
			this.m_Outputs.Add(new VFXEdFlowAnchor(2, typeof(float), this, m_DataSource, Direction.Output));

			this.AddChild(this.Inputs[0]);
			this.AddChild(this.Outputs[0]);
			this.AddChild(m_NodeClientArea);

			this.AddManipulator(new Draggable());
			this.AddManipulator(new NodeDelete());

			this.AllEvents += ManageSelection;

		}

		private bool ManageSelection(CanvasElement element, Event e, Canvas2D parent)
		{
			if (selected)
			{
				foreach(CanvasElement ce in this.m_NodeClientArea.NodeBlockContainer.Children())
				{
					if(ce.GetType()== typeof(VFXEdNodeBlock))
					{
						(ce as VFXEdNodeBlock).DisableDrag();
					}
				}
			}
			else
			{
				foreach (CanvasElement ce in this.m_NodeClientArea.NodeBlockContainer.Children())
				{
					if (ce.GetType() == typeof(VFXEdNodeBlock))
					{
						(ce as VFXEdNodeBlock).EnableDrag();
					}
				}
			}

			return false;
		}

		public override void Layout()
		{
			base.Layout();

			this.scale = new Vector2(this.scale.x, m_NodeClientArea.scale.y + 50);
			//Inputs
			for (int i = 0 ; i < this.Inputs.Count ; i++)
			{
				Inputs[i].translation = new Vector2((i+1)* (this.scale.x / (this.Inputs.Count + 1))-32,0.0f);
			}
			
			//Outputs
			for (int i = 0 ; i < this.Outputs.Count; i++)
			{
				Outputs[i].translation = new Vector2((i + 1) * (this.scale.x / (this.Outputs.Count + 1))-32, m_NodeClientArea.scale.y+12);
			}


		}

		public override void Render(Rect parentRect, Canvas2D canvas)
		{
			base.Render(parentRect, canvas);
		}


	}
}

