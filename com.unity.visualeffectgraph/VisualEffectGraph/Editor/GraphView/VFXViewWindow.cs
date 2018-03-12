using System;
using System.Linq;
using UnityEditor.Experimental.UIElements;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEditor.Experimental.VFX;
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
        public void LoadAsset(VisualEffectAsset asset)
        {
            string assetPath = AssetDatabase.GetAssetPath(asset);

            VisualEffectResource resource = VisualEffectResource.GetResourceAtPath(assetPath);

            //Transitionning code
            if (resource == null)
            {
                resource = new VisualEffectResource();
                resource.SetAssetPath(AssetDatabase.GetAssetPath(asset));
            }

            LoadResource(resource);
        }

        public void LoadResource(VisualEffectResource resource)
        {
            if (graphView.controller == null || graphView.controller.model != resource)
            {
                bool differentAsset = resource != m_DisplayedAsset;

                m_AssetName = resource.name;
                m_DisplayedAsset = resource;
                graphView.controller = VFXViewController.GetController(resource, true);

                if (differentAsset)
                {
                    graphView.FrameNewController();
                }
            }
        }

        protected VisualEffectResource GetCurrentAsset()
        {
            var objs = Selection.objects;

            VisualEffectResource selectedAsset = null;
            if (objs != null && objs.Length == 1)
            {
                if (objs[0] is VisualEffectAsset)
                {
                    VisualEffectAsset asset = objs[0] as VisualEffectAsset;

                    selectedAsset = asset.GetResource();
                    //Transitionning code
                    if (selectedAsset == null)
                    {
                        string assetPath = AssetDatabase.GetAssetPath(asset);
                        selectedAsset = VisualEffectResource.GetResourceAtPath(assetPath);
                        if (selectedAsset == null)
                        {
                            selectedAsset = new VisualEffectResource();
                            selectedAsset.SetAssetPath(assetPath);
                        }
                    }
                }
                else if (objs[0] is VisualEffectResource)
                {
                    selectedAsset = objs[0] as VisualEffectResource;
                }
                else if (objs[0] is VFXModel)
                {
                    string assetPath = AssetDatabase.GetAssetPath(objs[0]);

                    selectedAsset = VisualEffectResource.GetResourceAtPath(assetPath);

                    if (selectedAsset == null)
                    {
                        selectedAsset = new VisualEffectResource();
                        selectedAsset.SetAssetPath(assetPath);
                    }
                }
            }
            if (selectedAsset == null)
            {
                int instanceID = Selection.activeInstanceID;

                if (instanceID != 0)
                {
                    string path = AssetDatabase.GetAssetPath(instanceID);
                    if (path.EndsWith(".vfx"))
                    {
                        selectedAsset = VisualEffectResource.GetResourceAtPath(path);
                    }
                }
            }
            if (selectedAsset == null && m_DisplayedAsset != null)
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


            var currentAsset = GetCurrentAsset();
            if (currentAsset != null)
            {
                LoadResource(currentAsset);
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
            if (objs != null && objs.Length == 1 && objs[0] is VisualEffectAsset)
            {
                VFXViewController controller = graphView.controller;

                if (controller == null || controller.model != objs[0] as VisualEffectAsset)
                {
                    LoadAsset(objs[0] as VisualEffectAsset);
                }
            }
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
        private VisualEffectResource m_DisplayedAsset;

        [SerializeField]
        Vector3 m_ViewPosition;

        [SerializeField]
        Vector3 m_ViewScale;

        private string m_AssetName;
    }
}
