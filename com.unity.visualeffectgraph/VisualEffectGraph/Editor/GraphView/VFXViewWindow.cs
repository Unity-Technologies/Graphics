using System;
using System.Linq;
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
                {Event.KeyboardEvent("a"), view.FrameAll },
                {Event.KeyboardEvent("f"), view.FrameSelection },
                {Event.KeyboardEvent("o"), view.FrameOrigin },
                {Event.KeyboardEvent("^#>"), view.FramePrev },
                {Event.KeyboardEvent("^>"), view.FrameNext },
                {Event.KeyboardEvent("#^r"), view.Resync},
                {Event.KeyboardEvent("F7"), view.Compile},
                {Event.KeyboardEvent("#d"), view.OutputToDot},
                {Event.KeyboardEvent("^#d"), view.OutputToDotReduced},
                {Event.KeyboardEvent("#c"), view.OutputToDotConstantFolding},
                {Event.KeyboardEvent("^r"), view.ReinitComponents},
                {Event.KeyboardEvent("F5"), view.ReinitComponents},
            });
        }

        public static VFXViewWindow currentWindow;

        [MenuItem("Window/Visual Effects/Visual Effect Graph", false, 3010)]
        public static void ShowWindow()
        {
            GetWindow<VFXViewWindow>();
        }

        public VFXView graphView
        {
            get; private set;
        }
        public void LoadAsset(VisualEffectAsset asset)
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

        protected VisualEffectAsset GetCurrentAsset()
        {
            var objs = Selection.objects;

            VisualEffectAsset selectedAsset = null;
            if (objs != null && objs.Length == 1 && objs[0] is VisualEffectAsset)
            {
                selectedAsset = objs[0] as VisualEffectAsset;
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


            VisualEffectAsset currentAsset = GetCurrentAsset();
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
            }

            currentWindow = this;

            /*if (m_ViewScale != Vector3.zero)
            {
                graphView.UpdateViewTransform(m_ViewPosition, m_ViewScale);
            }*/
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
        void OnFocus()
        {
            graphView.UpdateGloblalSelection();
        }

        void OnSelectionChange()
        {
            var objs = Selection.objects;
            VFXViewController controller = graphView.controller;

            if (objs != null && objs.Length == 1 && objs[0] is VisualEffectAsset)
            {
                if (controller == null || controller.model != objs[0] as VisualEffectAsset)
                {
                    LoadAsset(objs[0] as VisualEffectAsset);
                }
            }
            /*else if( controller != null && objs.All(t => t is VFXModel && (t as VFXModel).GetGraph() == controller.graph)
            {
                graphView.SelectMo
            }*/
        }

        void OnEnterPanel(AttachToPanelEvent e)
        {
            VisualElement rootVisualElement = UIElementsEntryPoint.GetRootVisualContainer(this);
            rootVisualElement.AddManipulator(m_ShortcutHandler);
        }

        void OnLeavePanel(DetachFromPanelEvent e)
        {
            VisualElement rootVisualElement = UIElementsEntryPoint.GetRootVisualContainer(this);
            rootVisualElement.RemoveManipulator(m_ShortcutHandler);
        }

        public bool autoCompile {get; set; }

        void Update()
        {
            VFXViewController controller = graphView.controller;
            var filename = "No Asset";
            if (controller != null)
            {
                controller.NotifyUpdate();
                if (controller.model != null)
                {
                    var graph = controller.graph;
                    if (graph != null)
                    {
                        filename = controller.model.name;
                        if (!graph.saved)
                        {
                            filename += "*";
                        }
                        graph.RecompileIfNeeded(!autoCompile);
                        controller.RecompileExpressionGraphIfNeeded();
                    }
                }
            }
            titleContent.text = filename;
        }

        [SerializeField]
        private VisualEffectAsset m_DisplayedAsset;

        [SerializeField]
        Vector3 m_ViewPosition;

        [SerializeField]
        Vector3 m_ViewScale;

        private string m_AssetName;
    }
}
