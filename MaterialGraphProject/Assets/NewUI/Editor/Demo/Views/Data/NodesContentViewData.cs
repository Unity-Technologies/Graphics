using UnityEngine;

namespace RMGUI.GraphView.Demo
{
	class NodesContentViewData : GraphViewDataSource
	{
		protected new void OnEnable()
		{
			base.OnEnable();

			var containerData = CreateInstance<InvisibleBorderContainerData>();
			containerData.position = new Rect(630.0f, 0.0f, 200.0f, 200.0f);
			AddElement(containerData);

			containerData = CreateInstance<InvisibleBorderContainerData>();
			containerData.position = new Rect(630.0f, 210.0f, 200.0f, 200.0f);
			AddElement(containerData);

			var circleData = CreateInstance<CircleData>();
			circleData.position = new Rect(630, 420, 0, 0);
			circleData.radius = 200;
			AddElement(circleData);

			var nodeData = CreateInstance<NodeData>();
			nodeData.outputAnchor.type = typeof(Vector3);
			nodeData.position = new Rect(0, 0, 200, 176);
			nodeData.title = "Some Operator";
			AddElement(nodeData);

			nodeData = CreateInstance<NodeData>();
			nodeData.outputAnchor.type = typeof(int);
			nodeData.position = new Rect(210, 0, 200, 176);
			nodeData.title = "Some Nice Operator";
			AddElement(nodeData);

			nodeData = CreateInstance<NodeData>();
			nodeData.outputAnchor.type = typeof(Color);
			nodeData.position = new Rect(420, 0, 200, 176);
			nodeData.title = "Some Other Operator";
			AddElement(nodeData);

			nodeData = CreateInstance<NodeData>();
			nodeData.outputAnchor.type = typeof(float);
			nodeData.position = new Rect(0, 186, 200, 176);
			nodeData.title = "Another Operator";
			AddElement(nodeData);

			var miniMapData = CreateInstance<MiniMapData>();
			miniMapData.position = new Rect(210, 186, 200, 176);
			AddElement(miniMapData);

			var verticalNodeData = CreateInstance<VerticalNodeData>();
			verticalNodeData.position = new Rect(210, 420, 100, 100);
			AddElement(verticalNodeData);

			verticalNodeData = CreateInstance<VerticalNodeData>();
			verticalNodeData.position = new Rect(320, 420, 100, 100);
			AddElement(verticalNodeData);

			verticalNodeData = CreateInstance<VerticalNodeData>();
			verticalNodeData.position = new Rect(430, 420, 100, 100);
			AddElement(verticalNodeData);
		}

		public void CreateOperator(System.Type outputType, Rect pos, string title)
		{
			var nodeData = CreateInstance<NodeData>();
			nodeData.outputAnchor.type = typeof(Color);
			nodeData.position = pos;
			nodeData.title = title;
			AddElement(nodeData);
		}

		protected NodesContentViewData() {}
	}

	internal static class MyNodeAdapters
	{
		internal static bool Adapt(this NodeAdapter value, PortSource<int> a, PortSource<int> b)
		{
			// run adapt code for int to int connections
			return true;
		}

		internal static bool Adapt(this NodeAdapter value, PortSource<float> a, PortSource<float> b)
		{
			// run adapt code for float to float connections
			return true;
		}

		internal static bool Adapt(this NodeAdapter value, PortSource<int> a, PortSource<float> b)
		{
			// run adapt code for int to float connections, perhaps by insertion a conversion node
			return true;
		}

		internal static bool Adapt(this NodeAdapter value, PortSource<Vector3> a, PortSource<Vector3> b)
		{
			// run adapt code for vec3 to vec3 connections
			return true;
		}

		internal static bool Adapt(this NodeAdapter value, PortSource<Color> a, PortSource<Color> b)
		{
			// run adapt code for Color to Color connections
			return true;
		}

		internal static bool Adapt(this NodeAdapter value, PortSource<Vector3> a, PortSource<Color> b)
		{
			// run adapt code for vec3 to Color connections
			return true;
		}

		internal static bool Adapt(this NodeAdapter value, PortSource<Color> a, PortSource<Vector3> b)
		{
			// run adapt code for Color to vec3 connections
			return true;
		}
	}
}
