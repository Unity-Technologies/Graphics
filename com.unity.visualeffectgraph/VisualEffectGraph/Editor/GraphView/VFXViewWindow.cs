using System;
using UnityEditor.Experimental.UIElements;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Experimental.VFX;
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
                { Event.KeyboardEvent("^#>"), view.FramePrev },
                { Event.KeyboardEvent("^>"), view.FrameNext },
                {Event.KeyboardEvent("c"), view.CloneModels},         // TEST
                {Event.KeyboardEvent("#^r"), view.Resync},
                {Event.KeyboardEvent("F7"), view.Compile},
                {Event.KeyboardEvent("#d"), view.OutputToDot},
                {Event.KeyboardEvent("^#d"), view.OutputToDotReduced},
                {Event.KeyboardEvent("#c"), view.OutputToDotConstantFolding},
                {Event.KeyboardEvent("#r"), view.ReinitComponents},
                {Event.KeyboardEvent("F5"), view.ReinitComponents},
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
            if (graphView.controller == null || graphView.controller.model != asset)
            {
                bool differentAsset = asset != m_DisplayedAsset;

                m_AssetName = asset.name;
                m_DisplayedAsset = asset;
                graphView.controller = VFXViewController.GetController(asset, true);

                if (differentAsset)
                {
                    graphView.FrameNewController();
                }
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
            else if (m_DisplayedAsset != null)
            {
                selectedAsset = m_DisplayedAsset;
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
                LoadAsset(currentAsset);
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

            if (m_ViewScale != Vector3.zero)
            {
                graphView.UpdateViewTransform(m_ViewPosition, m_ViewScale);
            }
        }

        protected void OnDisable()
        {
            if (graphView != null)
            {
                m_ViewScale = graphView.contentViewContainer.transform.scale;
                m_ViewPosition = graphView.contentViewContainer.transform.position;

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
                VFXViewController controller = graphView.controller;

                if (controller == null || controller.model != objs[0] as VFXAsset)
                {
                    LoadAsset(objs[0] as VFXAsset);
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
            if (controller != null && controller.model != null && controller.graph != null)
            {
                var graph = controller.graph;
                var filename = m_AssetName;
                if (!graph.saved)
                {
                    filename += "*";
                }
                titleContent.text = filename;
                graph.RecompileIfNeeded(!autoCompile);
                controller.RecompileExpressionGraphIfNeeded();
            }
        }

        [SerializeField]
        private VFXAsset m_DisplayedAsset;

        [SerializeField]
        Vector3 m_ViewPosition;

        [SerializeField]
        Vector3 m_ViewScale;

        private string m_AssetName;
    }
}
