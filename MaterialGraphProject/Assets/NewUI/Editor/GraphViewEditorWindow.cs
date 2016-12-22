using UnityEngine;
using UnityEditor;
using UnityEngine.RMGUI;

namespace RMGUI.GraphView
{
	public abstract class GraphViewEditorWindow : EditorWindow
	{
		public GraphView graphView { get; private set; }
		GraphViewPresenter m_Presenter;

		// we watch the data source for destruction and re-create it
		IDataWatchHandle handle;

		void OnEnable()
		{
			m_Presenter = BuildPresenters();
			graphView = BuildView();
			graphView.name = "theView";
			graphView.presenter = m_Presenter;
			graphView.StretchToParentSize();
			graphView.onEnter += OnEnterPanel;
			graphView.onLeave += OnLeavePanel;
			rootVisualContainer.AddChild(graphView);
		}

		void OnDisable()
		{
			rootVisualContainer.RemoveChild(graphView);
		}

		// Override these methods to properly support domain reload & enter/exit playmode
		protected abstract GraphView BuildView();
		protected abstract GraphViewPresenter BuildPresenters();

		void OnEnterPanel()
		{
			if (m_Presenter == null)
			{
				m_Presenter = BuildPresenters();
				graphView.presenter = m_Presenter;
			}
			handle = graphView.panel.dataWatch.AddWatch(graphView, m_Presenter, OnChanged);
		}

		void OnLeavePanel()
		{
			if (handle != null)
			{
				handle.Dispose();
				handle = null;
			}
			else
			{
				Debug.LogError("No active handle to remove");
			}
		}

		void OnChanged()
		{
			// If data was destroyed, remove the watch and try to re-create it
			if (m_Presenter == null && graphView.panel != null)
			{
				if (handle != null)
				{
					handle.Dispose();
				}

				m_Presenter = BuildPresenters();
				graphView.presenter = m_Presenter;
				handle = graphView.panel.dataWatch.AddWatch(graphView, m_Presenter, OnChanged);
			}
		}
	}
}
