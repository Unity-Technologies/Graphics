using System;
using UnityEngine;

namespace RMGUI.GraphView.Demo
{
	class NodesContentViewPresenter : GraphViewPresenter
	{
		private NodePresenter InitNodePresenter(Type outputType)
		{
			var nodePresenter = CreateInstance<NodePresenter>();
			var inputNodeAnchorPresenter = CreateInstance<InputNodeAnchorPresenter>();
			inputNodeAnchorPresenter.anchorType = typeof(int);
			nodePresenter.inputAnchors.Add(inputNodeAnchorPresenter);

			inputNodeAnchorPresenter = CreateInstance<InputNodeAnchorPresenter>();
			inputNodeAnchorPresenter.anchorType = typeof(float);
			nodePresenter.inputAnchors.Add(inputNodeAnchorPresenter);

			inputNodeAnchorPresenter = CreateInstance<InputNodeAnchorPresenter>();
			inputNodeAnchorPresenter.anchorType = typeof(Vector3);
			nodePresenter.inputAnchors.Add(inputNodeAnchorPresenter);

			inputNodeAnchorPresenter = CreateInstance<InputNodeAnchorPresenter>();
			inputNodeAnchorPresenter.anchorType = typeof(Texture2D);
			nodePresenter.inputAnchors.Add(inputNodeAnchorPresenter);

			inputNodeAnchorPresenter = CreateInstance<InputNodeAnchorPresenter>();
			inputNodeAnchorPresenter.anchorType = typeof(Color);
			nodePresenter.inputAnchors.Add(inputNodeAnchorPresenter);

			var outputNodeAnchorPresenter = CreateInstance<OutputNodeAnchorPresenter>();
			outputNodeAnchorPresenter.anchorType = outputType;
			nodePresenter.outputAnchors.Add(outputNodeAnchorPresenter);

			return nodePresenter;
		}

		private NodePresenter InitVerticalNodePresenter()
		{
			// This is a demo version. We could have a ctor that takes in input and output types, etc.
			var nodePresenter = CreateInstance<VerticalNodePresenter>();

			var inputNodeAnchorPresenter = CreateInstance<InputNodeAnchorPresenter>();
			inputNodeAnchorPresenter.orientation = Orientation.Vertical;
			inputNodeAnchorPresenter.anchorType = typeof(float);
			nodePresenter.inputAnchors.Add(inputNodeAnchorPresenter);

			var outputNodeAnchorPresenter = CreateInstance<OutputNodeAnchorPresenter>();
			outputNodeAnchorPresenter.orientation = Orientation.Vertical;
			outputNodeAnchorPresenter.anchorType = typeof(float);
			nodePresenter.outputAnchors.Add(outputNodeAnchorPresenter);

			return nodePresenter;
		}

		protected new void OnEnable()
		{
			base.OnEnable();

			// Here, we would load the data from a given source and create our presenters based on that.
			// This is a demo, so our data (and thus our presenters) is fixed.

			var containerPresenter = CreateInstance<InvisibleBorderContainerPresenter>();
			containerPresenter.position = new Rect(630.0f, 0.0f, 200.0f, 200.0f);
			AddElement(containerPresenter);

			containerPresenter = CreateInstance<InvisibleBorderContainerPresenter>();
			containerPresenter.position = new Rect(630.0f, 210.0f, 200.0f, 200.0f);
			AddElement(containerPresenter);

			var circlePresenter = CreateInstance<CirclePresenter>();
			circlePresenter.position = new Rect(630, 420, 0, 0);
			circlePresenter.radius = 200;
			AddElement(circlePresenter);

			var nodePresenter = InitNodePresenter(typeof(Vector3));
			nodePresenter.position = new Rect(0, 0, 200, 176);
			nodePresenter.title = "Some Operator";
			AddElement(nodePresenter);

			nodePresenter = InitNodePresenter(typeof(int));
			nodePresenter.position = new Rect(300, 0, 200, 176);
			nodePresenter.title = "Some Nice Operator";
			AddElement(nodePresenter);

			nodePresenter = InitNodePresenter(typeof(Color));
			nodePresenter.position = new Rect(300, 186, 200, 176);
			nodePresenter.title = "Some Other Operator";
			AddElement(nodePresenter);

			nodePresenter = InitNodePresenter(typeof(float));
			nodePresenter.position = new Rect(0, 186, 200, 176);
			nodePresenter.title = "Another Operator";
			AddElement(nodePresenter);

			var verticalNodePresenter = InitVerticalNodePresenter();
			verticalNodePresenter.position = new Rect(240, 420, 100, 100);
			AddElement(verticalNodePresenter);

			verticalNodePresenter = InitVerticalNodePresenter();
			verticalNodePresenter.position = new Rect(350, 420, 100, 100);
			AddElement(verticalNodePresenter);

			verticalNodePresenter = InitVerticalNodePresenter();
			verticalNodePresenter.position = new Rect(460, 420, 100, 100);
			AddElement(verticalNodePresenter);

			var commentPresenter = CreateInstance<CommentPresenter>();
			commentPresenter.position = new Rect(850, 0, 500, 300);
			commentPresenter.color = new Color(0.4f, 0f, 0f, 0.3f);
			commentPresenter.titleBar = "My First Comment";
			commentPresenter.body = "This is my first comment.  It is made of words and a few return carriages.  Nothing more.  I hope we can see this whole line.\n\n" +
									"This is a new paragraph.  Just to test the CommentPresenter.";
			AddElement(commentPresenter);

			var miniMapPresenter = CreateInstance<MiniMapPresenter>();
			miniMapPresenter.position = new Rect(0, 372, 200, 176);
			AddElement(miniMapPresenter);
		}

		public void CreateOperator(Type outputType, Rect pos, string title)
		{
			var nodePresenter = InitNodePresenter(typeof(Color));
			nodePresenter.position = pos;
			nodePresenter.title = title;
			AddElement(nodePresenter);
		}

		public void CreateComment(Rect pos, string title, string body, Color color)
		{
			var commentPresenter = CreateInstance<CommentPresenter>();
			commentPresenter.position = pos;
			commentPresenter.titleBar = title;
			commentPresenter.color = color;
			commentPresenter.body = body;
			AddElement(commentPresenter);
		}

		protected NodesContentViewPresenter() {}
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
