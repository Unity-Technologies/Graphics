using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.RMGUI;

namespace RMGUI.GraphView.Demo
{
	[StyleSheet("Assets/Editor/Demo/NodalView.uss")]
	class NodesContentView : SimpleContentView
	{
		System.Random rnd = new System.Random();

		public NodesContentView()
		{
			// Shortcut handler to delete elements
			var dictionary = new Dictionary<Event, ShortcutDelegate>();
			dictionary[Event.KeyboardEvent("delete")] = DeleteSelection;
			contentViewContainer.AddManipulator(new ShortcutHandler(dictionary));

			// Contextual menu to create new nodes
			contentViewContainer.AddManipulator(new ContextualMenu((evt, customData) =>
			{
				var menu = new GenericMenu();
				menu.AddItem(new GUIContent("Create Operator"), false,
							 contentView => CreateOperator(),
							 this);
				menu.ShowAsContext();
				return EventPropagation.Continue;
			}));
		}

		public void CreateOperator()
		{
			NodalViewData nodalViewData = dataProvider as NodalViewData;
			if (nodalViewData != null)
			{
				var x = rnd.Next(0, 600);
				var y = rnd.Next(0, 300);

				nodalViewData.CreateOperator(typeof(Vector3), new Rect(x, y, 200, 176), "Shiny New Operator");
			}
		}

		// TODO: this has data model knowledge, move this to GraphData
		private EventPropagation DeleteSelection()
		{
			// and DeleteSelection would call that method.
			var nodalViewData = dataProvider as NodalViewData;
			if (nodalViewData == null)
				return EventPropagation.Stop;

			// TODO We will want to move this up to GraphView
			var elementsToRemove = new List<GraphElementData>();
			foreach (var selectedElement in selection.Cast<GraphElement>()
													 .Where(e => e != null && e.dataProvider != null))
			{
				var nodeData = selectedElement.dataProvider as NodeData;
				if (nodeData != null)
				{
					// If there are connected edges, disconnect first (if such functionality is available) and delete
					foreach (var element in allElements.OfType<GraphElement>())
					{
						var edge = element as Edge;
						if (edge == null) continue;

						var edgeData = edge.dataProvider as EdgeData;
						if (edgeData == null) continue;

						// Try output anchor first
						if ((edgeData.Left != null && edgeData.Left == (IConnectable)nodeData.outputAnchor) ||
							(edgeData.Right != null && edgeData.Right == (IConnectable)nodeData.outputAnchor))
						{
							elementsToRemove.Add(edgeData);
							continue;
						}

						// Check each input anchor
						if (nodeData.anchors.Any(a => (edgeData.Left != null && edgeData.Left == (IConnectable)a) ||
													  (edgeData.Right != null && edgeData.Right == (IConnectable)a)))
						{
							elementsToRemove.Add(edgeData);
						}
					}
				}

				elementsToRemove.Add(selectedElement.GetData<GraphElementData>());
			}

			// Notify node anchors of deconnection
			foreach (var edgeData in elementsToRemove.OfType<EdgeData>())
			{
				if (edgeData.Left != null)
				{
					edgeData.Left.connected = false;
				}

				if (edgeData.Right != null)
				{
					edgeData.Right.connected = false;
				}
			}

			foreach (var b  in elementsToRemove.OfType<GraphElementData>())
				nodalViewData.RemoveElement(b);

			return EventPropagation.Stop;
		}
	}

	class NodalView : EditorWindow
	{
		[MenuItem("Window/GraphView Demo/Nodal UI")]
		public static void ShowWindow()
		{
			GetWindow<NodalView>();
		}

		void OnEnable()
		{
			var zeView = new NodesContentView
			{
				name = "theView",
				dataProvider = CreateInstance<NodalViewData>()
			};
			zeView.StretchToParentSize();

			windowRoot.AddChild(zeView);
		}

		void OnDisable()
		{
			windowRoot.ClearChildren();
		}
	}
}
