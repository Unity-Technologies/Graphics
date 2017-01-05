using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.RMGUI;

namespace RMGUI.GraphView.Demo
{
	class NodesContentView : SimpleContentView
	{
		private readonly System.Random m_Rnd = new System.Random();
		private int m_CommentIndex;

		public NodesContentView()
		{
			// Contextual menu to create new nodes
			AddManipulator(new ContextualMenu((evt, customData) =>
			{
				var menu = new GenericMenu();
				menu.AddItem(new GUIContent("Create Operator"), false,
							 contentView => CreateOperator(),
							 this);
				menu.AddItem(new GUIContent("Create Comment"), false,
							 contentView => CreateComment(),
							 this);
				menu.ShowAsContext();
				return EventPropagation.Continue;
			}));

			dataMapper[typeof(CustomEdgePresenter)] = typeof(CustomEdge);
			dataMapper[typeof(NodeAnchorPresenter)] = typeof(NodeAnchor);
			dataMapper[typeof(NodePresenter)] = typeof(Node);
			dataMapper[typeof(VerticalNodePresenter)] = typeof(Node);
		}

		public void CreateOperator()
		{
			var contentViewPresenter = GetPresenter<NodesContentViewPresenter>();
			int x = m_Rnd.Next(0, 600);
			int y = m_Rnd.Next(0, 300);

			contentViewPresenter.CreateOperator(typeof(Vector3), new Rect(x, y, 200, 176), "Shiny New Operator");
		}

		public void CreateComment()
		{
			var contentViewPresenter = ((GraphView) this).presenter as NodesContentViewPresenter;
			if (contentViewPresenter != null)
			{
				int x = m_Rnd.Next(0, 600);
				int y = m_Rnd.Next(0, 500);
				var color = new Color(m_Rnd.Next(0, 255) / 255f, m_Rnd.Next(0, 255) / 255f, m_Rnd.Next(0, 255) / 255f);
				var title = String.Format("Comment {0}", (++m_CommentIndex));
				var body = "This is another comment.  It is made of words and a few return carriages.  Nothing more.  I hope we can see this whole line.\n\n" +
						   "This is a new paragraph.  Just to test the CommentPresenter.";
				contentViewPresenter.CreateComment(new Rect(x, y, 500, 300), title, body, color);
			}
		}
	}
}
