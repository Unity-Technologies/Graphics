#define DEBUG_MAT_GEN

using UnityEngine;
using System.IO;

namespace UnityEditor.Graphs.Material
{
	class MaterialWindow : EditorWindow
	{
		[MenuItem("Window/Material")]
		public static void OpenMenu ()
		{
			GetWindow<MaterialWindow>();
		}

		[MenuItem("Assets/Create/Shader Graph", false, 208)]
		public static void CreateMaterialGraph()
		{
			ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, DoCreateShaderGraphAsset, "New Shader Graph.ShaderGraph", null, null);
		}

		[MenuItem("Assets/Create/Shader Sub-Graph", false, 209)]
		public static void CreateMaterialSubGraph()
		{
			ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, DoCreateShaderSubGraphAsset, "New Shader SubGraph.ShaderSubGraph", null, null);
		}

		private static void DoCreateShaderGraphAsset (int instanceId, string pathName, string resourceFile)
		{
			var graph = CreateInstance<MaterialGraph>();
			graph.name = Path.GetFileName (pathName);
			AssetDatabase.CreateAsset (graph, pathName);
			graph.CreateSubAssets ();
		}

		private static void DoCreateShaderSubGraphAsset(int instanceId, string pathName, string resourceFile)
		{
			var graph = CreateInstance<MaterialSubGraph>();
			graph.name = Path.GetFileName(pathName);
			AssetDatabase.CreateAsset(graph, pathName);
			graph.CreateSubAssets();
		}

		private MaterialGraph m_MaterialGraph;
		private MaterialGraphGUI m_MaterialGraphGUI;
		private MaterialSubGraph m_MaterialSubGraph;
		private MaterialSubGraphGUI m_MaterialSubGraphGUI;
		
		private BaseMaterialGraphGUI m_ActiveGUI;
		private ScriptableObject m_ActiveGraph;
	
		private bool shouldRepaint
		{
			get
			{
				return m_MaterialGraph != null && m_MaterialGraph.currentGraph != null && m_MaterialGraph.currentGraph.requiresRepaint
				       || m_MaterialSubGraph != null && m_MaterialSubGraph.requiresRepaint;
			}
		}

		void Update ()
		{
			if (shouldRepaint)
				Repaint();
		}

		void OnSelectionChange()
		{
			MaterialWindow.DebugMaterialGraph ( "Got OnSelection Change: " + Selection.activeObject);

			if (Selection.activeObject == null || !EditorUtility.IsPersistent(Selection.activeObject))
				return;

			if (Selection.activeObject is MaterialSubGraph)
			{
				var selection = Selection.activeObject as MaterialSubGraph;
				if (selection != m_MaterialSubGraph)
				{
					m_MaterialSubGraph = selection;
					m_MaterialSubGraphGUI = null;
					m_MaterialGraph = null;
					m_MaterialSubGraph.GeneratePreviewShaders ();
				}

				if (m_MaterialSubGraphGUI == null)
				{
					m_MaterialSubGraphGUI = CreateInstance<MaterialSubGraphGUI>();
					m_MaterialSubGraphGUI.hideFlags = HideFlags.HideAndDontSave;
					m_MaterialSubGraphGUI.graph = m_MaterialSubGraph;
				}
				
				m_ActiveGUI = m_MaterialSubGraphGUI;
				m_ActiveGraph = m_MaterialSubGraph;
			}
			else if(Selection.activeObject is MaterialGraph)
			{
				var selection = Selection.activeObject as MaterialGraph;
				MaterialWindow.DebugMaterialGraph ("Selection: " + selection);
				MaterialWindow.DebugMaterialGraph("Existing: " + m_MaterialGraph);
				if (selection != m_MaterialGraph)
				{
					m_MaterialGraph = selection;
					m_MaterialSubGraph = null;
					m_MaterialGraphGUI = null;
					m_MaterialGraph.currentGraph.GeneratePreviewShaders();

				}

				if (m_MaterialGraphGUI == null)
				{
					m_MaterialGraphGUI = CreateInstance<MaterialGraphGUI>();
					m_MaterialGraphGUI.hideFlags = HideFlags.HideAndDontSave;
					m_MaterialGraphGUI.graph = m_MaterialGraph.currentGraph;
					m_MaterialGraphGUI.materialGraph = m_MaterialGraph;
				}

				m_ActiveGUI = m_MaterialGraphGUI;
				m_ActiveGraph = m_MaterialGraph;
			}

			Repaint();
		}

		void OnGUI ()
		{
			if (m_ActiveGraph == null || m_ActiveGUI == null)
			{
				GUILayout.Label("No Graph selected");
				return;
			}

			if(m_ActiveGUI == m_MaterialGraphGUI)
			{
				m_MaterialGraphGUI.graph = m_MaterialGraph.currentGraph;
				m_MaterialGraphGUI.materialGraph = m_MaterialGraph;
			}
			else if(m_ActiveGUI == m_MaterialSubGraphGUI)
			{
				m_MaterialSubGraphGUI.graph = m_MaterialSubGraph;
			}

			m_ActiveGUI.BeginGraphGUI(this, new Rect(0, 0, position.width - 300, position.height));
			m_ActiveGUI.OnGraphGUI();
			m_ActiveGUI.EndGraphGUI();
			if (m_ActiveGUI == m_MaterialGraphGUI)
				m_MaterialGraphGUI.RenderOptions(new Rect(position.width - 300, 0, 300, position.height), m_MaterialGraph);
		}

		public static void DebugMaterialGraph(string s)
		{
#if DEBUG_MAT_GEN
			Debug.Log (s);
#endif
		}
	}
}
