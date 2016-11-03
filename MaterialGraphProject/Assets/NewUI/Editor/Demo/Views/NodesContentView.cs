using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.RMGUI;

namespace RMGUI.GraphView.Demo
{
	[StyleSheet("Assets/NewUI/Editor/Demo/Views/NodalView.uss")]
	class NodesContentView : SimpleContentView
	{
		private readonly System.Random rnd = new System.Random();

		public NodesContentView()
		{
			// Contextual menu to create new nodes
			AddManipulator(new ContextualMenu((evt, customData) =>
			{
				var menu = new GenericMenu();
				menu.AddItem(new GUIContent("Create Operator"), false,
							 contentView => CreateOperator(),
							 this);
				menu.ShowAsContext();
				return EventPropagation.Continue;
			}));

			dataMapper[typeof(CustomEdgeData)] = typeof(CustomEdge);
			dataMapper[typeof(NodeAnchorData)] = typeof(NodeAnchor);
			dataMapper[typeof(NodeData)] = typeof(Node);
			dataMapper[typeof(VerticalNodeData)] = typeof(Node);
		}

		public void CreateOperator()
		{
			var nodalViewData = dataSource as NodesContentViewData;
			if (nodalViewData != null)
			{
				var x = rnd.Next(0, 600);
				var y = rnd.Next(0, 300);

				nodalViewData.CreateOperator(typeof(Vector3), new Rect(x, y, 200, 176), "Shiny New Operator");
			}
		}
	}
}
