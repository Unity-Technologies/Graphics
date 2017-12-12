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
    class VFXViewWindow : EditorWindow
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
                //{ Event.KeyboardEvent("delete"), view.DeleteSelection },
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

        public VFXView graphView
        {
            get; private set;
        }
        public void LoadAsset(VFXAsset asset)
        {
            if (graphView.controller == null || graphView.controller.GetVFXAsset() != asset)
            {
                graphView.controller = VFXViewController.Manager.GetController(asset, true);
            }
        }

        protected VFXAsset GetCurrentAsset()
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
            return selectedAsset;
        }

        protected void OnEnable()
        {
            graphView = new VFXView();
            graphView.StretchToParentSize();
            SetupFramingShortcutHandler(graphView);

            this.GetRootVisualContainer().Add(graphView);


            VFXAsset currentAsset = GetCurrentAsset();
            if (currentAsset != null)
            {
                graphView.controller = VFXViewController.Manager.GetController(currentAsset, true);
            }

            autoCompile = true;


            graphView.RegisterCallback<AttachToPanelEvent>(OnEnterPanel);
            graphView.RegisterCallback<DetachFromPanelEvent>(OnLeavePanel);


            VisualElement rootVisualElement = this.GetRootVisualContainer();
            if (rootVisualElement.panel != null)
            {
                rootVisualElement.AddManipulator(m_ShortcutHandler);
                Debug.Log("View window was already attached to a panel on OnEnable");
            }

            currentWindow = this;
        }

        protected void OnDisable()
        {
            if (graphView != null)
            {
                graphView.UnregisterCallback<AttachToPanelEvent>(OnEnterPanel);
                graphView.UnregisterCallback<DetachFromPanelEvent>(OnLeavePanel);
                graphView.controller = null;
            }
            currentWindow = null;
        }

        void OnSelectionChange()
        {
            var objs = Selection.objects;
            if (objs != null && objs.Length == 1 && objs[0] is VFXAsset)
            {
                m_DisplayedAssetPath = AssetDatabase.GetAssetPath(objs[0] as VFXAsset);

                VFXViewController controller = graphView.controller;

                if (controller == null || controller.GetVFXAsset() != objs[0] as VFXAsset)
                {
                    graphView.controller = VFXViewController.Manager.GetController(objs[0] as VFXAsset);
                }
            }
        }

        void OnEnterPanel(AttachToPanelEvent e)
        {
            VisualElement rootVisualElement = UIElementsEntryPoint.GetRootVisualContainer(this);
            rootVisualElement.AddManipulator(m_ShortcutHandler);

            Debug.Log("VFXViewWindow.OnEnterPanel");
        }

        void OnLeavePanel(DetachFromPanelEvent e)
        {
            VisualElement rootVisualElement = UIElementsEntryPoint.GetRootVisualContainer(this);
            rootVisualElement.RemoveManipulator(m_ShortcutHandler);

            Debug.Log("VFXViewWindow.OnLeavePanel");
        }

        public bool autoCompile {get; set; }

        void Update()
        {
            VFXViewController controller = graphView.controller;
            if (controller != null)
            {
                var graph = controller.model;
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
                controller.RecompileExpressionGraphIfNeeded();
            }
        }

        [SerializeField]
        private string m_DisplayedAssetPath;
    }
}
