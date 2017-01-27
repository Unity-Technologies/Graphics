using System.Security.Cryptography.X509Certificates;
using UnityEditor;
using UnityEngine;
using UnityEngine.RMGUI;
using UnityEngine.RMGUI.StyleSheets;

namespace RMGUI.GraphView.Demo
{
	// TODO: Temporary class to use IMGUI's TextArea instead of RMGUI.TextField (unavailable for now)
	public class Comment : IMGUIElement
	{
		bool m_ShowBackgroundColor;

		// TODO: Get style from USS when switching to RMGUI.TextField
		GUIStyle m_TitleStyle;
		GUIStyle m_BodyStyle;

		public override void OnGUIHandler()
		{
			var commentPresenter = GetPresenter<CommentPresenter>();
			if (commentPresenter == null)
				return;

			Color oldBackgroundColor = GUI.backgroundColor;
			GUI.backgroundColor = backgroundColor;

			EditorGUILayout.BeginVertical();
			commentPresenter.titleBar = EditorGUILayout.TextArea(commentPresenter.titleBar, /*m_MaxTitleLength,*/ m_TitleStyle);
			if (m_ShowBackgroundColor)
				commentPresenter.color = EditorGUILayout.ColorField(commentPresenter.color);
			commentPresenter.body = EditorGUILayout.TextArea(commentPresenter.body, m_BodyStyle);
			EditorGUILayout.EndVertical();

			GUI.backgroundColor = oldBackgroundColor;
		}

		public Comment()
		{
			clipChildren = true;
			elementTypeColor = new Color(0.8f, 0.8f, 0.8f, 0.5f);
			backgroundColor = elementTypeColor;

			m_Container.positionTop = 0;

			m_TitleStyle = new GUIStyle();
			m_TitleStyle.name = "commentTitle";
			m_TitleStyle.font = EditorGUIUtility.Load("Assets/Resources/BebasNeue.otf") as Font;
			m_TitleStyle.fontSize = 64;
			m_TitleStyle.alignment = TextAnchor.UpperLeft;
			m_TitleStyle.normal.textColor = new Color(0.75f, 0.75f, 0.75f);
			m_TitleStyle.border.left = 3;
			m_TitleStyle.border.top = 3;
			m_TitleStyle.border.right = 3;
			m_TitleStyle.clipping = TextClipping.Clip;
			m_TitleStyle.wordWrap = true;
			m_TitleStyle.padding.top = 8;
			m_TitleStyle.padding.left = 8;
			m_TitleStyle.padding.bottom = 8;
			m_TitleStyle.padding.right = 8;
			m_TitleStyle.margin.top = 8;
			m_TitleStyle.margin.bottom = 8;
			m_TitleStyle.margin.left = 8;
			m_TitleStyle.margin.right = 8;

			m_BodyStyle = new GUIStyle();
			m_BodyStyle.name = "commentBody";
			m_BodyStyle.font = EditorGUIUtility.Load("Assets/Resources/Roboto/Roboto-Regular.ttf") as Font;
			m_BodyStyle.fontSize = 12;
			m_BodyStyle.alignment = TextAnchor.UpperLeft;
			m_BodyStyle.normal.textColor = new Color(0.75f, 0.75f, 0.75f);
			m_BodyStyle.clipping = TextClipping.Clip;
			m_BodyStyle.wordWrap = true;
			m_BodyStyle.padding.top = 8;
			m_BodyStyle.padding.left = 8;
			m_BodyStyle.padding.bottom = 8;
			m_BodyStyle.padding.right = 8;
			m_BodyStyle.margin.top = 8;
			m_BodyStyle.margin.bottom = 8;
			m_BodyStyle.margin.left = 8;
			m_BodyStyle.margin.right = 8;

 			AddManipulator(new ContextualMenu((evt, customData) =>
 			{
 				var menu = new GenericMenu();
 				menu.AddItem(new GUIContent("Toggle Background Color Control"), false,
 							 contentView => ToggleBackgroundColor(),
 							 this);
 				menu.ShowAsContext();
 				return EventPropagation.Continue;
 			}));

		}

		private void ToggleBackgroundColor()
		{
			m_ShowBackgroundColor = !m_ShowBackgroundColor;
		}

		public override void OnDataChanged()
		{
			base.OnDataChanged();

			var commentPresenter = GetPresenter<CommentPresenter>();
			if (commentPresenter == null)
				return;

			backgroundColor = new Color(commentPresenter.color.r, commentPresenter.color.g, commentPresenter.color.b, 0.5f);

			this.Touch(ChangeType.Layout);
		}
	}

//	// Comment is, for the moment, made of a title bar and a comment body (both IMGUI), until we have RMGUI.TextField
// 	public class Comment : IMGUIElement
// 	{
// 		TextField m_TitleBar;
// 		TextField m_Body;
//
// 		public Comment()
// 		{
// 			clipChildren = true;
// 			elementTypeColor = new Color(0.8f, 0.8f, 0.8f, 0.5f);
//
// 			m_TitleBar = new TextField()
// 			{
// 				name = "commentTitle",
// 				flex = 1
// 			};
// 			AddChild(m_Title);
//
// 			m_Body = new TextField()
// 			{
// 				name = "commentBody",
// 				flex = 1
// 			};
// 			AddChild(m_Body);
// 		}
//
// 		public override void OnDataChanged()
// 		{
// 			base.OnDataChanged();
//
// 			var commentPresenter = GetPresenter<CommentPresenter>();
// 			if (commentPresenter == null)
// 				return;
//
// 			m_TitleBar.text = commentPresenter.titleBar;
// 			m_Body.text = commentPresenter.body;
//
// 			this.Touch(ChangeType.Layout);
// 		}
// 	}
}
