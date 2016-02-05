using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental;
using System.Collections;
using System.IO;

namespace UnityEditor.Experimental
{

	public class VFXEditor : EditorWindow
	{

		[MenuItem("VFXEditor/Export Skin")]
		public static void ExportSkin()
		{
			VFXEditor.styles.ExportGUISkin();
		}

		[MenuItem("Assets/Create/VFX Asset")]
		public static void CreateVFXAsset()
		{
			VFXAsset asset = ScriptableObject.CreateInstance<VFXAsset>();

			string path = AssetDatabase.GetAssetPath(Selection.activeObject);
			if(path == "")
			{
				path = "Assets";
			}
			else if (Path.GetExtension(path) != "")
			{
				path = path.Replace(Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)),"");
			}

			string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + "/New VFX Asset.asset");

			AssetDatabase.CreateAsset(asset, assetPathAndName);

			AssetDatabase.SaveAssets();
			EditorUtility.FocusProjectWindow();
			Selection.activeObject = asset;

		}

		[MenuItem("Window/VFX Editor %R")]
		public static void ShowWindow()
		{
			EditorWindow.GetWindow(typeof(VFXEditor));
		}

		/* Singletons */
		public static VFXEditorMetrics metrics
		{
			get
			{
				if (s_Metrics == null) s_Metrics = new VFXEditorMetrics();
				return s_Metrics;
			}
		}
		public static VFXEditorStyles styles
		{
			get
			{
				if (s_Styles == null) s_Styles = new VFXEditorStyles();
				return s_Styles;
			}
		}
		private static VFXEditorMetrics s_Metrics;
		private static VFXEditorStyles s_Styles;
		/* end Singletons */

		private VFXEdCanvas m_Canvas = null;
		private EditorWindow m_HostWindow = null;
		private Texture m_Icon = null;
		private GenericMenu m_Menu = null;
		private Rect m_LibraryRect;
		private Rect m_PreviewRect;
		private VFXEdDataSource m_DataSource;

		private bool m_bShowPreview = false;
		private bool m_bShowLibrary = false;
		private bool m_bShowDebugInfo = true;
		private bool m_CannotPreview = true;
		private VFXAsset m_CurrentAsset;
		private void InitializeCanvas()
		{
			if (m_Canvas == null)
			{
				m_DataSource = ScriptableObject.CreateInstance<VFXEdDataSource>();
				m_Canvas = new VFXEdCanvas(this, m_HostWindow, m_DataSource);
				// m_Canvas.showQuadTree = true;

				// draggable manipulator allows to move the canvas around. Note that individual elements can have the draggable manipulator on themselves
				m_Canvas.AddManipulator(new Draggable(2, EventModifiers.None));
				m_Canvas.AddManipulator(new Draggable(0, EventModifiers.Alt));

				// make the canvas zoomable
				m_Canvas.AddManipulator(new Zoomable(Zoomable.ZoomType.AroundMouse));

				// allow framing the selection when hitting "F" (frame) or "A" (all). Basically shows how to trap a key and work with the canvas selection
				m_Canvas.AddManipulator(new Frame(Frame.FrameType.All));
				m_Canvas.AddManipulator(new Frame(Frame.FrameType.Selection));

				// The following manipulator show how to work with canvas2d overlay and background rendering
				m_Canvas.AddManipulator(new RectangleSelect());
				m_Canvas.AddManipulator(new ScreenSpaceGrid());
			}

			if (m_Icon == null) m_Icon = EditorGUIUtility.Load("edicon.psd") as Texture;

			Undo.undoRedoPerformed += OnUndoRedo;

			Rebuild();
		}

		void OnUndoRedo()
		{
			Debug.Log("OnUndoRedo");
			m_Canvas.ReloadData();
			m_Canvas.Repaint();
		}

		void OnSelectionChange()
		{
			if(Selection.activeObject != null)
				if(Selection.activeObject.GetType() == typeof(VFXAsset))
					Debug.Log("Selection Changed : " + Selection.activeObject);
		}


		private void InitializeMenu(Event e)
		{
			m_Menu = new GenericMenu();
			m_Menu.AddItem(new GUIContent("Add New Node"), false, AddGenericNode, e);
			m_Menu.AddSeparator("");
			m_Menu.AddItem(new GUIContent("NodeBlocks/Test1"), false, null, "Item1");
			m_Menu.AddItem(new GUIContent("NodeBlocks/Test2"), false, null, "Item2");

		}

		private void Rebuild()
		{
			if (m_Canvas == null)
				return;

			m_Canvas.Clear();
			m_Canvas.ReloadData();
			m_Canvas.ZSort();
		}

		void OnGUI()
		{
			Event currentEvent = Event.current;

			m_HostWindow = this;
			if (m_Canvas == null)
			{
				InitializeCanvas();
			}

			if (m_Menu == null)
			{
				InitializeMenu(currentEvent);
			}
			titleContent = new GUIContent("VFX Editor", m_Icon);
			//GUI.Toolbar(new Rect(0, 0, position.width, 24),0);
			DrawToolbar(new Rect(0, 0, position.width, EditorStyles.toolbar.fixedHeight));
			Rect canvasRect = new Rect(0, EditorStyles.toolbar.fixedHeight, position.width, position.height);
			m_Canvas.OnGUI(this, canvasRect);
			DrawWindows(canvasRect);
			
			if (currentEvent.type == EventType.ContextClick)
			{
				Vector2 mousePos = currentEvent.mousePosition;
				if (canvasRect.Contains(mousePos))
				{
					m_Menu.ShowAsContext();
				}
			}
		}

		void AddGenericNode(object o)
		{
			Event e = o as Event;
			m_DataSource.AddNode(new VFXEdNode(m_Canvas.MouseToCanvas(e.mousePosition), new Vector2(220, 180), m_DataSource));
			m_Canvas.ReloadData();
		}

		void DrawToolbar(Rect rect)
		{
			GUI.BeginGroup(rect);
			GUILayout.BeginHorizontal(EditorStyles.toolbar);
			if (GUILayout.Button("Create...", EditorStyles.toolbarButton))
			{
			   
			}
			GUILayout.FlexibleSpace();
			m_Canvas.showQuadTree = GUILayout.Toggle(m_Canvas.showQuadTree, "Canvas2D Debug Info", EditorStyles.toolbarButton);
			m_bShowDebugInfo = GUILayout.Toggle(m_bShowDebugInfo, "VFXEditor Debug Info", EditorStyles.toolbarButton);
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("Reset", EditorStyles.toolbarButton))
			{
				m_Canvas = null;
				InitializeCanvas();
			}
			if (GUILayout.Button("Tools", EditorStyles.toolbarDropDown))
			{
				GenericMenu toolsMenu = new GenericMenu();
				if (Selection.activeGameObject != null)
					toolsMenu.AddItem(new GUIContent("Optimize Selected"), false, null);
				else
					toolsMenu.AddDisabledItem(new GUIContent("Optimize Selected"));
				toolsMenu.AddSeparator("");
				toolsMenu.AddItem(new GUIContent("Help..."), false, null);
				// Offset menu from right of editor window
				toolsMenu.DropDown(new Rect(Screen.width - 216 - 40, 0, 0, 16));
				EditorGUIUtility.ExitGUI();
			}

			m_bShowLibrary = GUILayout.Toggle(m_bShowLibrary, "NodeBlock Library",EditorStyles.toolbarButton);
			m_bShowPreview = GUILayout.Toggle(m_bShowPreview, "Preview", EditorStyles.toolbarButton);

			GUILayout.EndHorizontal();
			GUI.EndGroup();

		}

		#region TOOL WINDOWS
		void DrawWindows(Rect canvasRect)
		{
			// Calculate Rect's
			m_LibraryRect = new Rect(
												canvasRect.xMax - (VFXEditorMetrics.LibraryWindowWidth + 2 * VFXEditorMetrics.WindowPadding),
												canvasRect.yMin + VFXEditorMetrics.WindowPadding,
												VFXEditorMetrics.LibraryWindowWidth,
												canvasRect.height - (4 * VFXEditorMetrics.WindowPadding)
											);

			m_PreviewRect = new Rect(
											canvasRect.xMax - (VFXEditorMetrics.PreviewWindowWidth + 2 * VFXEditorMetrics.WindowPadding),
											canvasRect.yMax - (VFXEditorMetrics.PreviewWindowHeight + 2 * VFXEditorMetrics.WindowPadding),
											VFXEditorMetrics.PreviewWindowWidth,
											VFXEditorMetrics.PreviewWindowHeight
									   );

			
			if(m_bShowPreview)
			{
				m_LibraryRect.height = canvasRect.height - VFXEditorMetrics.PreviewWindowHeight - ( 5* VFXEditorMetrics.WindowPadding);
			}

			BeginWindows();
				if (m_bShowLibrary) GUI.Window(0, m_LibraryRect, DrawLibraryWindowContent, "NodeBlock Library");
				if (m_bShowPreview) GUI.Window(1, m_PreviewRect, DrawPreviewWindowContent, "Preview");
			EndWindows();
		}
		void DrawLibraryWindowContent(int windowID)
		{
			GUILayout.BeginScrollView(Vector2.zero);

			for(int i=0;i<10;i++)
			{
				GUILayout.BeginHorizontal(EditorStyles.toolbar);
				GUILayout.Box("Test Item " + i.ToString(), EditorStyles.toolbarButton);
				GUILayout.EndHorizontal();
			}
			GUILayout.EndScrollView();
		}


		void DrawPreviewWindowContent(int windowID)
		{
			if (m_CannotPreview)
			{
				GUILayout.BeginHorizontal();
					GUILayout.FlexibleSpace();
						GUILayout.BeginVertical();
							GUILayout.FlexibleSpace();
								EditorGUILayout.HelpBox("  No Preview Available    ", MessageType.Error);
							GUILayout.FlexibleSpace();
						GUILayout.EndVertical();
					GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();
			}
		}
		#endregion
	}


	public class VFXEditorMetrics
	{
		public static int WindowPadding = 16;
		public static int LibraryWindowWidth = 320;
		public static int NodeWidth = 320;
		public static int PreviewWindowWidth = 480;
		public static int PreviewWindowHeight = 320;
		
	}

	public class VFXEditorStyles
	{
		public GUIStyle Node;
		public GUIStyle NodeSelected;
		public GUIStyle NodeTitle;
		public GUIStyle NodeInfoText;


		public GUIStyle NodeBlock;
		public GUIStyle NodeBlockSelected;
		public GUIStyle NodeBlockTitle;

		public GUIStyle ConnectorLeft;
		public GUIStyle ConnectorRight;

		public GUIStyle FlowConnectorIn;
		public GUIStyle FlowConnectorOut;

		public GUIStyle Foldout;

		public Texture2D FlowEdgeOpacity;

		public VFXEditorStyles()
		{
			Node = new GUIStyle();
			Node.name = "Node";
			Node.normal.background = EditorGUIUtility.Load("NodeBase.psd") as Texture2D;
			Node.border = new RectOffset(9, 36, 41, 13);

			NodeSelected = new GUIStyle(Node);
			NodeSelected.name = "NodeSelected";
			NodeSelected.normal.background = EditorGUIUtility.Load("NodeBase_Selected.psd") as Texture2D;

			NodeTitle = new GUIStyle();
			NodeTitle.fontSize = 12;
			NodeTitle.fontStyle = FontStyle.Bold;
			NodeTitle.padding = new RectOffset(32, 32, 12, 0);
			NodeTitle.alignment = TextAnchor.MiddleCenter;
			NodeTitle.normal.textColor = Color.white;

			NodeInfoText = new GUIStyle();
			NodeInfoText.fontSize = 12;
			NodeInfoText.fontStyle = FontStyle.Italic;
			NodeInfoText.padding = new RectOffset(12, 12, 12, 12);
			NodeInfoText.alignment = TextAnchor.MiddleCenter;
			NodeInfoText.normal.textColor = Color.white;

			NodeBlock = new GUIStyle();
			NodeBlock.name = "NodeBlock";
			NodeBlock.normal.background = EditorGUIUtility.Load("NodeBlock_Flow_Unselected.psd") as Texture2D;
			NodeBlock.border = new RectOffset(4, 26, 12, 4);

			NodeBlockSelected = new GUIStyle();
			NodeBlockSelected.name = "NodeBlockSelected";
			NodeBlockSelected.normal.background = EditorGUIUtility.Load("NodeBlock_Flow_Selected.psd") as Texture2D;
			NodeBlockSelected.border = new RectOffset(4, 26, 12, 4);

			NodeBlockTitle = new GUIStyle();
			NodeBlockTitle.fontSize = 12;
			NodeBlockTitle.padding = new RectOffset(4, 4, 4, 4);
			NodeBlockTitle.alignment = TextAnchor.MiddleLeft;
			NodeBlockTitle.normal.textColor = Color.white;

			ConnectorLeft = new GUIStyle();
			ConnectorLeft.name = "ConnectorLeft";
			ConnectorLeft.normal.background = EditorGUIUtility.Load("Connector_Left.psd") as Texture2D;
			ConnectorLeft.border = new RectOffset(16, 0, 16, 0);

			ConnectorRight = new GUIStyle();
			ConnectorRight.name = "ConnectorRight";
			ConnectorRight.normal.background = EditorGUIUtility.Load("Connector_Right.psd") as Texture2D;
			ConnectorRight.border = new RectOffset(0,16, 16, 0);

			FlowConnectorIn = new GUIStyle();
			FlowConnectorIn.name = "FlowConnectorIn";
			FlowConnectorIn.normal.background = EditorGUIUtility.Load("LayoutFlow_In.psd") as Texture2D;
			FlowConnectorIn.active.background = EditorGUIUtility.Load("LayoutFlow_In_Glow.psd") as Texture2D;
			FlowConnectorIn.overflow = new RectOffset(15, 15, 12, 16);

			FlowConnectorOut = new GUIStyle();
			FlowConnectorOut.name = "FlowConnectorOut";
			FlowConnectorOut.normal.background = EditorGUIUtility.Load("LayoutFlow_Out.psd") as Texture2D;
			FlowConnectorOut.active.background = EditorGUIUtility.Load("LayoutFlow_Out_Glow.psd") as Texture2D;
			FlowConnectorOut.overflow = new RectOffset(15, 15, 15, 15);

			Foldout = "IN Foldout";

			FlowEdgeOpacity = EditorGUIUtility.Load("FlowEdge.psd") as Texture2D;

		}

		public void ExportGUISkin()
		{
			GUISkin s = ScriptableObject.CreateInstance<GUISkin>();
			s.customStyles = new GUIStyle[7];
			s.customStyles[0] = Node;
			s.customStyles[1] = NodeTitle;
			s.customStyles[2] = NodeBlock;
			s.customStyles[3] = ConnectorLeft;
			s.customStyles[4] = ConnectorRight;
			s.customStyles[5] = FlowConnectorIn;
			s.customStyles[6] = FlowConnectorOut;

			AssetDatabase.CreateAsset(s, "Assets/VFXEditor/VFXEditor.guiskin");
		}
	}
	

}
