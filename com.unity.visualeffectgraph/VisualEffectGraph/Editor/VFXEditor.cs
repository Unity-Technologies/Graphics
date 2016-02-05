using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace UnityEditor.Experimental
{
	public class VFXEditor : EditorWindow
	{

		[MenuItem("VFXEditor/Export Skin")]
		public static void ExportSkin()
		{
			VFXEditor.Styles.ExportGUISkin();
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
		public static VFXEditorMetrics Metrics
		{
			get
			{
				if (m_Metrics == null) m_Metrics = new VFXEditorMetrics();
				return m_Metrics;
			}
		}
		
		public static VFXEditorStyles Styles
		{
			get
			{
				if (m_Styles == null) m_Styles = new VFXEditorStyles();
				return m_Styles;
			}
		}

		public static VFXBlockLibraryCollection BlockLibrary
		{
			get
			{
				InitializeBlockLibrary();
				return s_BlockLibrary;
			}
		}

		private static VFXEditorMetrics m_Metrics;
		private static VFXEditorStyles m_Styles;
		private static VFXBlockLibraryCollection s_BlockLibrary;
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
		private Vector2 m_DebugInfoScroll = Vector2.zero;
		private bool m_CannotPreview = true;
		private VFXAsset m_CurrentAsset;

		private VFXBlockLibraryCollection m_BlockLibrary;

		private static void InitializeBlockLibrary()
		{
			if (s_BlockLibrary == null)
			{
				s_BlockLibrary = new VFXBlockLibraryCollection();
				s_BlockLibrary.Load();
			}
		}

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

			if (this.m_Icon == null) this.m_Icon = EditorGUIUtility.Load("edicon.psd") as Texture;

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
			this.m_Menu = new GenericMenu();
			m_Menu.AddItem(new GUIContent("Add New Node"), false, AddGenericNode, e);
			m_Menu.AddSeparator("");
			m_Menu.AddItem(new GUIContent("MenuItem"), false, null, "Item1");
			m_Menu.AddItem(new GUIContent("MenuItem1"), false, null, "Item1");

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

			if (s_BlockLibrary == null)
				InitializeBlockLibrary();

			if (m_Canvas == null)
			{
				InitializeCanvas();
			}

			if (m_Menu == null)
			{
				InitializeMenu(currentEvent);
			}
			this.titleContent = new GUIContent("VFX Editor", this.m_Icon);
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
			this.m_DataSource.AddNode(new VFXEdNode(this.m_Canvas.MouseToCanvas(e.mousePosition), new Vector2(220, 180), this.m_DataSource));
			this.m_Canvas.ReloadData();
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
				s_BlockLibrary = null;
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
				if (m_bShowDebugInfo) GUI.Window(2, m_LibraryRect, DrawDebugWindowContent, "VFX Editor DebugInfo");
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


		void DrawDebugWindowContent(int windowID)
		{
			GUILayout.BeginScrollView(m_DebugInfoScroll, EditorStyles.label);
			GUILayout.Label(m_Canvas.ShowDebug());
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

	public class VFXBlockLibraryCollection
	{
		private List<VFXBlock> m_Blocks;

		public VFXBlockLibraryCollection()
		{
			m_Blocks = new List<VFXBlock>();
		}

		public void Load()
		{
			m_Blocks.Clear();

			string[] guids = AssetDatabase.FindAssets("t:VFXBlockLibrary");
			VFXBlockLibrary[] blockLibraries = new VFXBlockLibrary[guids.Length];

			for (int i = 0; i < guids.Length; ++i)
			{
				blockLibraries[i] = AssetDatabase.LoadAssetAtPath<VFXBlockLibrary>(AssetDatabase.GUIDToAssetPath(guids[i]));
				for (int j = 0; j < blockLibraries[i].GetNbBlocks(); ++j)
				{
					VFXBlock block = blockLibraries[i].GetBlock(j);
					Debug.Log("Found block: " + block.m_Name + " " + block.m_Params.Length);
					for (int k = 0; k < block.m_Params.Length; ++k)
					{
						Debug.Log("\t" + block.m_Params[k]);
						Debug.Log("\t" + block.m_Params[k].m_Name);
					}

					m_Blocks.Add(block);
				}
			}
		}

		// Just for test
		public VFXBlock GetRandomBlock()
		{
			int index = Random.Range(0, m_Blocks.Count);
			return m_Blocks[index];
		}

		public ReadOnlyCollection<VFXBlock> GetBlocks()
		{
			return new ReadOnlyCollection<VFXBlock>(m_Blocks);
		}
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
			this.Node = new GUIStyle();
			this.Node.name = "Node";
			this.Node.normal.background = EditorGUIUtility.Load("NodeBase.psd") as Texture2D;
			this.Node.border = new RectOffset(9, 36, 41, 13);

			this.NodeSelected = new GUIStyle(this.Node);
			this.NodeSelected.name = "NodeSelected";
			this.NodeSelected.normal.background = EditorGUIUtility.Load("NodeBase_Selected.psd") as Texture2D;

			this.NodeTitle = new GUIStyle();
			this.NodeTitle.fontSize = 12;
			this.NodeTitle.fontStyle = FontStyle.Bold;
			this.NodeTitle.padding = new RectOffset(32, 32, 12, 0);
			this.NodeTitle.alignment = TextAnchor.MiddleCenter;
			this.NodeTitle.normal.textColor = Color.white;

			this.NodeInfoText = new GUIStyle();
			this.NodeInfoText.fontSize = 12;
			this.NodeInfoText.fontStyle = FontStyle.Italic;
			this.NodeInfoText.padding = new RectOffset(12, 12, 12, 12);
			this.NodeInfoText.alignment = TextAnchor.MiddleCenter;
			this.NodeInfoText.normal.textColor = Color.white;

			this.NodeBlock = new GUIStyle();
			this.NodeBlock.name = "NodeBlock";
			this.NodeBlock.normal.background = EditorGUIUtility.Load("NodeBlock_Flow_Unselected.psd") as Texture2D;
			this.NodeBlock.border = new RectOffset(4, 26, 12, 4);

			this.NodeBlockSelected = new GUIStyle();
			this.NodeBlockSelected.name = "NodeBlockSelected";
			this.NodeBlockSelected.normal.background = EditorGUIUtility.Load("NodeBlock_Flow_Selected.psd") as Texture2D;
			this.NodeBlockSelected.border = new RectOffset(4, 26, 12, 4);

			this.NodeBlockTitle = new GUIStyle();
			this.NodeBlockTitle.fontSize = 12;
			this.NodeBlockTitle.padding = new RectOffset(4, 4, 4, 4);
			this.NodeBlockTitle.alignment = TextAnchor.MiddleLeft;
			this.NodeBlockTitle.normal.textColor = Color.white;

			this.ConnectorLeft = new GUIStyle();
			this.ConnectorLeft.name = "ConnectorLeft";
			this.ConnectorLeft.normal.background = EditorGUIUtility.Load("Connector_Left.psd") as Texture2D;
			this.ConnectorLeft.border = new RectOffset(16, 0, 16, 0);

			this.ConnectorRight = new GUIStyle();
			this.ConnectorRight.name = "ConnectorRight";
			this.ConnectorRight.normal.background = EditorGUIUtility.Load("Connector_Right.psd") as Texture2D;
			this.ConnectorRight.border = new RectOffset(0,16, 16, 0);

			this.FlowConnectorIn = new GUIStyle();
			this.FlowConnectorIn.name = "FlowConnectorIn";
			this.FlowConnectorIn.normal.background = EditorGUIUtility.Load("LayoutFlow_In.psd") as Texture2D;
			this.FlowConnectorIn.active.background = EditorGUIUtility.Load("LayoutFlow_In_Glow.psd") as Texture2D;
			this.FlowConnectorIn.overflow = new RectOffset(15, 15, 12, 16);

			this.FlowConnectorOut = new GUIStyle();
			this.FlowConnectorOut.name = "FlowConnectorOut";
			this.FlowConnectorOut.normal.background = EditorGUIUtility.Load("LayoutFlow_Out.psd") as Texture2D;
			this.FlowConnectorOut.active.background = EditorGUIUtility.Load("LayoutFlow_Out_Glow.psd") as Texture2D;
			this.FlowConnectorOut.overflow = new RectOffset(15, 15, 15, 15);

			this.Foldout = "IN Foldout";

			this.FlowEdgeOpacity = EditorGUIUtility.Load("FlowEdge.psd") as Texture2D;

		}

		public void ExportGUISkin()
		{
			GUISkin s = ScriptableObject.CreateInstance<GUISkin>();
			s.customStyles = new GUIStyle[7];
			s.customStyles[0] = this.Node;
			s.customStyles[1] = this.NodeTitle;
			s.customStyles[2] = this.NodeBlock;
			s.customStyles[3] = this.ConnectorLeft;
			s.customStyles[4] = this.ConnectorRight;
			s.customStyles[5] = this.FlowConnectorIn;
			s.customStyles[6] = this.FlowConnectorOut;

			AssetDatabase.CreateAsset(s, "Assets/VFXEditor/VFXEditor.guiskin");
		}
	}
	

}
