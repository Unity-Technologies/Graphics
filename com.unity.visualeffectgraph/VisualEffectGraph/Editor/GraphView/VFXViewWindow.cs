using System;
using UnityEditor.Experimental.UIElements;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.Experimental.UIElements;
using UnityEditor.VFX;
using System.Collections.Generic;
using UnityEditor;

namespace  UnityEditor.VFX.UI
{
    [Serializable]
    class VFXViewWindow : GraphViewEditorWindow
    {
        ShortcutHandler m_ShortcutHandler;

        protected void SetupFramingShortcutHandler(VFXView view)
        {
            m_ShortcutHandler = new ShortcutHandler(
                    new Dictionary<Event, ShortcutDelegate>
            {
                { Event.KeyboardEvent("a"), view.FrameAll },
                { Event.KeyboardEvent("f"), view.FrameSelection },
                { Event.KeyboardEvent("o"), view.FrameOrigin },
                { Event.KeyboardEvent("delete"), view.DeleteSelection },
                { Event.KeyboardEvent("^#>"), view.FramePrev },
                { Event.KeyboardEvent("^>"), view.FrameNext },
                {Event.KeyboardEvent("c"), view.CloneModels},         // TEST
                {Event.KeyboardEvent("#r"), view.Resync},
                {Event.KeyboardEvent("#d"), view.OutputToDot},
                {Event.KeyboardEvent("^#d"), view.OutputToDotReduced},
                {Event.KeyboardEvent("#c"), view.OutputToDotConstantFolding},
                {Event.KeyboardEvent("space"), view.ReinitComponents},
            });
        }

        public static VFXViewWindow currentWindow;

        [MenuItem("VFX Editor/Window")]
        public static void ShowWindow()
        {
            GetWindow<VFXViewWindow>();
        }

        public void LoadAsset(VFXAsset asset)
        {
            VFXViewPresenter newPresenter = VFXViewPresenter.Manager.GetPresenter(asset);

            if (presenter != newPresenter)
            {
                if (presenter != null)
                    GetPresenter<VFXGraphViewPresenter>().m_RealPresenter.useCount--;
                presenter = newPresenter.graphViewPresenter;
                newPresenter.useCount++;

                graphView.controller = newPresenter;
            }
        }

        public new VFXView graphView
        {
            get { return base.graphView as VFXView; }
        }

        protected override GraphView BuildView()
        {
            BuildPresenters();

            VFXView view = new VFXView();
            VFXGraphViewPresenter presenter = GetPresenter<VFXGraphViewPresenter>();
            if (presenter != null)
            {
                presenter.m_RealPresenter.useCount++;
            }
            view.controller = presenter.m_RealPresenter;

            SetupFramingShortcutHandler(view);
            return view;
        }

        protected override GraphViewPresenter BuildPresenters()
        {
            var objs = Selection.objects;

            VFXAsset selectedAsset = null;
            if (objs != null && objs.Length == 1 && objs[0] is VFXAsset)
            {
                selectedAsset = objs[0] as VFXAsset;
            }
            else if (!string.IsNullOrEmpty(m_DisplayedAssetPath))
            {
                VFXAsset asset = AssetDatabase.LoadAssetAtPath<VFXAsset>(m_DisplayedAssetPath);

                selectedAsset = asset;
            }
            if (selectedAsset != null)
            {
                if (presenter != null)
                {
                    var viewPresenter = graphView.controller;
                    if (viewPresenter.GetVFXAsset() != selectedAsset)
                        viewPresenter.useCount--;
                }
            }

            if (selectedAsset != null)
            {
                return VFXViewPresenter.Manager.GetPresenter(selectedAsset, false).graphViewPresenter;
            }
            return null;
        }

        protected new void OnEnable()
        {
            base.OnEnable();

            autoCompile = true;


            graphView.RegisterCallback<AttachToPanelEvent>(OnEnterPanel);
            graphView.RegisterCallback<DetachFromPanelEvent>(OnLeavePanel);


            VisualElement rootVisualElement = UIElementsEntryPoint.GetRootVisualContainer(this);
            if (rootVisualElement.panel != null)
            {
                rootVisualElement.parent.AddManipulator(m_ShortcutHandler);
                Debug.Log("View window was already attached to a panel on OnEnable");
            }

            currentWindow = this;
        }

        protected new void OnDisable()
        {
            if (graphView != null)
            {
                graphView.UnregisterCallback<AttachToPanelEvent>(OnEnterPanel);
                graphView.UnregisterCallback<DetachFromPanelEvent>(OnLeavePanel);

                VFXViewPresenter presenter = graphView.controller;
                if (presenter != null)
                {
                    presenter.useCount--;
                    graphView.controller = null;
                }
            }

            base.OnDisable();
            currentWindow = null;
        }

        void OnSelectionChange()
        {
            var objs = Selection.objects;
            if (objs != null && objs.Length == 1 && objs[0] is VFXAsset)
            {
                m_DisplayedAssetPath = AssetDatabase.GetAssetPath(objs[0] as VFXAsset);

                VFXViewPresenter presenter = graphView.controller;

                VFXViewPresenter newPresenter = VFXViewPresenter.Manager.GetPresenter(objs[0] as VFXAsset);

                if (presenter != newPresenter)
                {
                    this.presenter = newPresenter.graphViewPresenter;
                    graphView.controller = newPresenter;
                    newPresenter.useCount++;
                    if (presenter != null)
                        presenter.useCount--;
                }
            }
        }

        void OnEnterPanel(AttachToPanelEvent e)
        {
            VisualElement rootVisualElement = UIElementsEntryPoint.GetRootVisualContainer(this);
            rootVisualElement.parent.AddManipulator(m_ShortcutHandler);

            Debug.Log("VFXViewWindow.OnEnterPanel");
        }

        void OnLeavePanel(DetachFromPanelEvent e)
        {
            VisualElement rootVisualElement = UIElementsEntryPoint.GetRootVisualContainer(this);
            rootVisualElement.parent.RemoveManipulator(m_ShortcutHandler);

            Debug.Log("VFXViewWindow.OnLeavePanel");
        }

        public bool autoCompile {get; set; }

        void Update()
        {
            VFXViewPresenter presenter = graphView.controller;
            if (presenter != null)
            {
                var graph = presenter.GetGraph();
                if (graph != null)
                {
                    var filename = System.IO.Path.GetFileName(m_DisplayedAssetPath);
                    if (!graph.saved)
                    {
                        filename += "*";
                    }
                    titleContent.text = filename;
                    graph.RecompileIfNeeded(!autoCompile);
                }
                presenter.RecompileExpressionGraphIfNeeded();
            }
        }

        [SerializeField]
        private string m_DisplayedAssetPath;
    }
}
