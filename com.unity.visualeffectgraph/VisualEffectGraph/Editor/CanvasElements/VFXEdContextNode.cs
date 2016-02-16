using System;
using System.Collections.Generic;
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
