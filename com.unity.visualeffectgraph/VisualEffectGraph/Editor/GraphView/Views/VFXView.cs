using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.VFX;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEngine.Profiling;

namespace UnityEditor.VFX.UI
{
    class SelectionSetter : Manipulator
    {
        VFXView m_View;
        public SelectionSetter(VFXView view)
        {
            m_View = view;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
        }

        void OnMouseUp(MouseUpEvent evt)
        {
            if (!VisualEffectEditor.s_IsEditingAsset)
                Selection.activeObject = m_View.controller.model;
        }
    }


    class GroupNodeAdder
    {
    }

    class VFXNodeProvider : VFXAbstractProvider<VFXNodeProvider.Descriptor>
    {
        public class Descriptor
        {
            public object modelDescriptor;
            public string category;
            public string name;
        }

        Func<Descriptor, bool> m_Filter;
        IEnumerable<Type> m_AcceptedTypes;

        public VFXNodeProvider(Action<Descriptor, Vector2> onAddBlock, Func<Descriptor, bool> filter = null, IEnumerable<Type> acceptedTypes = null) : base(onAddBlock)
        {
            m_Filter = filter;
            m_AcceptedTypes = acceptedTypes;
        }

        protected override string GetCategory(Descriptor desc)
        {
            return desc.category;
        }

        protected override string GetName(Descriptor desc)
        {
            return desc.name;
        }

        protected override string title
        {
            get {return "Node"; }
        }

        string ComputeCategory<T>(string type, VFXModelDescriptor<T> model) where T : VFXModel
        {
            if (model.info != null && model.info.category != null)
            {
                if (m_AcceptedTypes != null && m_AcceptedTypes.Count() == 1)
                {
                    return model.info.category;
                }
                else
                {
                    return string.Format("{0}/{1}", type, model.info.category);
                }
            }
            else
            {
                return type;
            }
        }

        protected override IEnumerable<Descriptor> GetDescriptors()
        {
            var systemFiles = System.IO.Directory.GetFiles(VisualEffectAssetEditorUtility.templatePath, "*.vfx").Select(t => t.Replace("\\", "/"));
            var systemDesc = systemFiles.Select(t => new Descriptor() {modelDescriptor = t, category = "System", name = System.IO.Path.GetFileNameWithoutExtension(t)});


            var descriptorsContext = VFXLibrary.GetContexts().Select(o =>
                {
                    return new Descriptor()
                    {
                        modelDescriptor = o,
                        category = ComputeCategory("Context", o),
                        name = o.name
                    };
                }).OrderBy(o => o.category + o.name);

            var descriptorsOperator = VFXLibrary.GetOperators().Select(o =>
                {
                    return new Descriptor()
                    {
                        modelDescriptor = o,
                        category = ComputeCategory("Operator", o),
                        name = o.name
                    };
                }).OrderBy(o => o.category + o.name);

            var descriptorParameter = VFXLibrary.GetParameters().Select(o =>
                {
                    return new Descriptor()
                    {
                        modelDescriptor = o,
                        category = ComputeCategory("Parameter", o),
                        name = o.name
                    };
                }).OrderBy(o => o.category + o.name);

            IEnumerable<Descriptor> descs = Enumerable.Empty<Descriptor>();

            if (m_AcceptedTypes == null || m_AcceptedTypes.Contains(typeof(VFXContext)))
            {
                descs = descs.Concat(descriptorsContext);
            }
            if (m_AcceptedTypes == null || m_AcceptedTypes.Contains(typeof(VFXOperator)))
            {
                descs = descs.Concat(descriptorsOperator);
            }
            if (m_AcceptedTypes == null || m_AcceptedTypes.Contains(typeof(VFXParameter)))
            {
                descs = descs.Concat(descriptorParameter);
            }
            if (m_AcceptedTypes == null)
            {
                descs = descs.Concat(systemDesc);
            }
            var groupNodeDesc = new Descriptor()
            {
                modelDescriptor = new GroupNodeAdder(),
                category = "Misc",
                name = "Group Node"
            };

            descs = descs.Concat(Enumerable.Repeat(groupNodeDesc, 1));

            if (m_Filter == null)
                return descs;
            else
                return descs.Where(t => m_Filter(t));
        }
    }
    class VFXView : GraphView, IDropTarget, IControlledElement<VFXViewController>
    {
        public HashSet<VFXEditableDataAnchor> allDataAnchors = new HashSet<VFXEditableDataAnchor>();

        void OnRecompile(VFXRecompileEvent e)
        {
            foreach (var anchor in allDataAnchors)
            {
                anchor.OnRecompile();
            }
        }

        VisualElement m_NoAssetLabel;

        VFXViewController m_Controller;
        Controller IControlledElement.controller
        {
            get { return m_Controller; }
        }


        void DisconnectController()
        {
            m_Controller.UnregisterHandler(this);
            m_Controller.useCount--;

            serializeGraphElements = null;
            unserializeAndPaste = null;
            deleteSelection = null;
            nodeCreationRequest = null;

            elementAddedToGroup = null;
            elementRemovedFromGroup = null;
            groupTitleChanged = null;

            // Remove all in view now that the controller has been disconnected.
            var graphElements = this.graphElements.ToList();
            foreach (var element in graphElements)
            {
                RemoveElement(element);
            }
        }

        void ConnectController()
        {
            m_Controller.RegisterHandler(this);
            m_Controller.useCount++;

            serializeGraphElements = SerializeElements;
            unserializeAndPaste = UnserializeAndPasteElements;
            deleteSelection = Delete;
            nodeCreationRequest = OnCreateNode;

            elementAddedToGroup = ElementAddedToGroupNode;
            elementRemovedFromGroup = ElementRemovedFromGroupNode;
            groupTitleChanged = GroupNodeTitleChanged;
        }

        public VFXViewController controller
        {
            get { return m_Controller; }
            set
            {
                if (m_Controller != value)
                {
                    if (m_Controller != null)
                    {
                        DisconnectController();
                    }
                    m_Controller = value;
                    if (m_Controller != null)
                    {
                        ConnectController();
                    }
                    NewControllerSet();
                }
            }
        }
        public VFXNodeController AddNode(VFXNodeProvider.Descriptor d, Vector2 mPos)
        {
            List<VisualElement> picked = new List<VisualElement>();
            panel.PickAll(mPos, picked);

            VFXGroupNode groupNode = picked.OfType<VFXGroupNode>().FirstOrDefault();

            mPos = this.ChangeCoordinatesTo(contentViewContainer, mPos);


            if (d.modelDescriptor is string)
            {
                string path = d.modelDescriptor as string;

                CreateTemplateSystem(path, mPos, groupNode);
            }
            else if (d.modelDescriptor is GroupNodeAdder)
            {
                controller.AddGroupNode(mPos);
            }
            else
                return controller.AddNode(mPos, d.modelDescriptor, groupNode != null ? groupNode.controller : null);
            return null;
        }

        protected void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.imguiEvent.Equals(Event.KeyboardEvent("space")))
            {
                OnCreateThing(evt as KeyDownEvent);
            }
        }

        void OnCreateThing(KeyDownEvent evt)
        {
            VisualElement picked = panel.Pick(evt.originalMousePosition);
            VFXContextUI context = picked.GetFirstOfType<VFXContextUI>();

            if (context != null)
            {
                context.OnCreateBlock(evt.originalMousePosition);
            }
            else
            {
                NodeCreationContext ctx = new NodeCreationContext();
                ctx.screenMousePosition = GUIUtility.GUIToScreenPoint(evt.imguiEvent.mousePosition);
                OnCreateNode(ctx);
            }
        }

        VFXNodeProvider m_NodeProvider;

        public VFXView()
        {
            SetupZoom(0.125f, 8);

            //this.AddManipulator(new SelectionSetter(this));
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            this.AddManipulator(new FreehandSelector());

            m_NodeProvider = new VFXNodeProvider((d, mPos) => AddNode(d, mPos));

            AddStyleSheetPath("VFXView");

            Dirty(ChangeType.Transform);

            AddLayer(-1);
            AddLayer(1);
            AddLayer(2);

            focusIndex = 0;

            VisualElement toolbar = new VisualElement();
            toolbar.AddToClassList("toolbar");


            Button button = new Button(() => { Resync(); });
            button.text = "Refresh";
            button.AddToClassList("toolbarItem");
            toolbar.Add(button);

            Button toggleBlackboard = new Button(() => { ToggleBlackboard(); });
            toggleBlackboard.text = "Blackboard";
            toggleBlackboard.AddToClassList("toolbarItem");
            toolbar.Add(toggleBlackboard);


            VisualElement spacer = new VisualElement();
            spacer.style.flex = 1;
            toolbar.Add(spacer);
            m_ToggleDebug = new Toggle(OnToggleDebug);
            m_ToggleDebug.text = "Debug";
            toolbar.Add(m_ToggleDebug);
            m_ToggleDebug.AddToClassList("toolbarItem");

            m_DropDownButtonCullingMode = new Label();

            m_DropDownButtonCullingMode.text = CullingMaskToString(cullingFlags);
            m_DropDownButtonCullingMode.AddManipulator(new DownClickable(() => {
                    var menu = new GenericMenu();
                    foreach (var val in k_CullingOptions)
                    {
                        menu.AddItem(new GUIContent(val.Key), val.Value == cullingFlags, (v) =>
                        {
                            cullingFlags = (VFXCullingFlags)v;
                            m_DropDownButtonCullingMode.text = CullingMaskToString((VFXCullingFlags)v);
                        }, val.Value);
                    }
                    menu.DropDown(m_DropDownButtonCullingMode.worldBound);
                }));
            toolbar.Add(m_DropDownButtonCullingMode);
            m_DropDownButtonCullingMode.AddToClassList("toolbarItem");

            m_ToggleCastShadows = new Toggle(OnToggleCastShadows);
            m_ToggleCastShadows.text = "Cast Shadows";
            m_ToggleCastShadows.on = GetRendererSettings().shadowCastingMode != ShadowCastingMode.Off;
            toolbar.Add(m_ToggleCastShadows);
            m_ToggleCastShadows.AddToClassList("toolbarItem");

            m_ToggleMotionVectors = new Toggle(OnToggleMotionVectors);
            m_ToggleMotionVectors.text = "Use Motion Vectors";
            m_ToggleMotionVectors.on = GetRendererSettings().motionVectorGenerationMode == MotionVectorGenerationMode.Object;
            toolbar.Add(m_ToggleMotionVectors);
            m_ToggleMotionVectors.AddToClassList("toolbarItem");

            Toggle toggleRenderBounds = new Toggle(OnShowBounds);
            toggleRenderBounds.text = "Show Bounds";
            toggleRenderBounds.on = VisualEffect.renderBounds;
            toolbar.Add(toggleRenderBounds);
            toggleRenderBounds.AddToClassList("toolbarItem");

            Toggle toggleAutoCompile = new Toggle(OnToggleCompile);
            toggleAutoCompile.text = "Auto Compile";
            toggleAutoCompile.on = true;
            toolbar.Add(toggleAutoCompile);
            toggleAutoCompile.AddToClassList("toolbarItem");

            button = new Button(OnCompile);
            button.text = "Compile";
            button.AddToClassList("toolbarItem");
            toolbar.Add(button);


            m_NoAssetLabel = new Label("Please Select An Asset");
            m_NoAssetLabel.style.positionType = PositionType.Absolute;
            m_NoAssetLabel.style.positionLeft = 0;
            m_NoAssetLabel.style.positionRight = 0;
            m_NoAssetLabel.style.positionTop = 0;
            m_NoAssetLabel.style.positionBottom = 0;
            m_NoAssetLabel.style.textAlignment = TextAnchor.MiddleCenter;
            m_NoAssetLabel.style.fontSize = 72;
            m_NoAssetLabel.style.textColor = Color.white * 0.75f;

            Add(m_NoAssetLabel);

            vfxGroupNodes = this.Query<VisualElement>().Children<VFXGroupNode>().Build();
            RegisterCallback<ControllerChangedEvent>(OnControllerChanged);
            RegisterCallback<VFXRecompileEvent>(OnRecompile);

            m_Blackboard = new VFXBlackboard();


            bool blackboardVisible = EditorPrefs.GetBool("vfx-blackboard-visible", true);
            if (blackboardVisible)
                Add(m_Blackboard);

            Add(toolbar);

            RegisterCallback<DragUpdatedEvent>(OnDragUpdated);
            RegisterCallback<DragPerformEvent>(OnDragPerform);
            RegisterCallback<KeyDownEvent>(OnKeyDown);
            RegisterCallback<ValidateCommandEvent>(ValidateCommand);
            RegisterCallback<ExecuteCommandEvent>(ExecuteCommand);

            graphViewChanged = VFXGraphViewChanged;

            elementResized = VFXElementResized;

            Undo.undoRedoPerformed = OnUndoPerformed;
        }

        void OnUndoPerformed()
        {
            foreach (var anchor in allDataAnchors)
            {
                anchor.ForceUpdate();
            }
        }

        void ToggleBlackboard()
        {
            if (m_Blackboard.parent == null)
            {
                Insert(childCount - 1, m_Blackboard);
                EditorPrefs.SetBool("vfx-blackboard-visible", true);
            }
            else
            {
                m_Blackboard.RemoveFromHierarchy();
                EditorPrefs.SetBool("vfx-blackboard-visible", false);
            }
        }

        public UQuery.QueryState<VFXGroupNode> vfxGroupNodes { get; private set; }

        Toggle m_ToggleDebug;

        void Delete(string cmd, AskUser askUser)
        {
            controller.Remove(selection.OfType<IControlledElement>().Select(t => t.controller));
        }

        void OnControllerChanged(ControllerChangedEvent e)
        {
            if (e.controller == controller)
            {
                ControllerChanged(e.change);
            }
            else if (e.controller is VFXNodeController)
            {
                UpdateUIBounds();
            }
        }

        bool m_InControllerChanged;

        void ControllerChanged(int change)
        {
            m_InControllerChanged = true;
            Profiler.BeginSample("VFXView.ControllerChanged");
            if (change == VFXViewController.Change.destroy)
            {
                m_Blackboard.controller = null;
                controller = null;
                return;
            }
            if (change == VFXViewController.AnyThing)
            {
                SyncNodes();
            }
            SyncEdges(change);
            SyncGroupNodes();

            if (controller != null)
            {
                if (change == VFXViewController.AnyThing)
                {
                    var settings = GetRendererSettings();

                    m_DropDownButtonCullingMode.text = CullingMaskToString(cullingFlags);

                    m_ToggleCastShadows.on = settings.shadowCastingMode != ShadowCastingMode.Off;
                    m_ToggleCastShadows.SetEnabled(true);

                    m_ToggleMotionVectors.on = settings.motionVectorGenerationMode == MotionVectorGenerationMode.Object;
                    m_ToggleMotionVectors.SetEnabled(true);

                    m_ToggleDebug.on = controller.graph.displaySubAssets;

                    // if the asset dis destroy somehow, fox example if the user delete the asset, delete the controller and update the window.
                    VisualEffectAsset asset = controller.model;
                    if (asset == null)
                    {
                        this.controller = null;
                        return;
                    }
                }
            }
            else
            {
                m_DropDownButtonCullingMode.text = k_CullingOptions[0].Key;
                m_ToggleCastShadows.SetEnabled(false);
                m_ToggleMotionVectors.SetEnabled(false);
            }
            SyncStickyNotes();

            // needed if some or all the selection has been deleted, so we no longer show the deleted object in the inspector.
            SelectionUpdated();
            Profiler.EndSample();
            m_InControllerChanged = false;
            if (m_UpdateUIBounds)
            {
                UpdateUIBounds();
            }
        }

        public override void OnPersistentDataReady()
        {
            // warning : default could messes with view restoration from the VFXViewWindow (TODO : check this)
            base.OnPersistentDataReady();
        }

        void NewControllerSet()
        {
            m_Blackboard.controller = controller;
            if (controller != null)
            {
                m_NoAssetLabel.RemoveFromHierarchy();

                pasteOffset = Vector2.zero; // if we change asset we want to paste exactly at the same place as the original asset the first time.
            }
            else
            {
                if (m_NoAssetLabel.parent == null)
                {
                    Add(m_NoAssetLabel);
                }
            }
        }

        public void FrameNewController()
        {
            if (panel != null)
            {
                (panel as BaseVisualElementPanel).ValidateLayout();
                FrameAll();
            }
            else
            {
                RegisterCallback<AttachToPanelEvent>(OnFrameNewControllerWithPanel);
            }
        }

        void OnFrameNewControllerWithPanel(AttachToPanelEvent e)
        {
            (panel as BaseVisualElementPanel).scheduler.ScheduleOnce(
                t => {
                    if (panel != null)
                    {
                        (panel as BaseVisualElementPanel).ValidateLayout();
                        FrameAll();
                    }
                }
                ,
                10
                );

            UnregisterCallback<AttachToPanelEvent>(OnFrameNewControllerWithPanel);
        }

        Dictionary<VFXNodeController, GraphElement> rootNodes = new Dictionary<VFXNodeController, GraphElement>();


        Dictionary<VFXGroupNodeController, VFXGroupNode> groupNodes
        {
            get
            {
                var dic = new Dictionary<VFXGroupNodeController, VFXGroupNode>();
                foreach (var layer in contentViewContainer.Children())
                {
                    foreach (var graphElement in layer.Children())
                    {
                        if (graphElement is VFXGroupNode)
                        {
                            dic[(graphElement as VFXGroupNode).controller] = graphElement as VFXGroupNode;
                        }
                    }
                }


                return dic;
            }
        }
        Dictionary<VFXStickyNoteController, VFXStickyNote> stickyNotes
        {
            get
            {
                var dic = new Dictionary<VFXStickyNoteController, VFXStickyNote>();
                foreach (var layer in contentViewContainer.Children())
                {
                    foreach (var graphElement in layer.Children())
                    {
                        if (graphElement is VFXStickyNote)
                        {
                            dic[(graphElement as VFXStickyNote).controller] = graphElement as VFXStickyNote;
                        }
                    }
                }
                return dic;
            }
        }


        void SyncNodes()
        {
            Profiler.BeginSample("VFXView.SyncNodes");
            if (controller == null)
            {
                foreach (var element in rootNodes.Values.ToArray())
                {
                    RemoveElement(element);
                }
                rootNodes.Clear();
            }
            else
            {
                elementAddedToGroup = null;
                elementRemovedFromGroup = null;

                var deletedControllers = rootNodes.Keys.Except(controller.nodes).ToArray();
                bool changed = false;

                foreach (var deletedController in deletedControllers)
                {
                    RemoveElement(rootNodes[deletedController]);
                    rootNodes.Remove(deletedController);
                    changed = true;
                }

                foreach (var newController in controller.nodes.Except(rootNodes.Keys).ToArray())
                {
                    GraphElement newElement = null;
                    if (newController is VFXContextController)
                    {
                        newElement = new VFXContextUI();
                    }
                    else if (newController is VFXOperatorController)
                    {
                        newElement = new VFXOperatorUI();
                    }
                    else if (newController is VFXParameterNodeController)
                    {
                        newElement = new VFXParameterUI();
                    }
                    else
                    {
                        throw new InvalidOperationException("Can't find right ui for controller" + newController.GetType().Name);
                    }
                    changed = true;
                    AddElement(newElement);
                    rootNodes[newController] = newElement;
                    (newElement as ISettableControlledElement<VFXNodeController>).controller = newController;
                }


                elementAddedToGroup = ElementAddedToGroupNode;
                elementRemovedFromGroup = ElementRemovedFromGroupNode;
            }

            Profiler.EndSample();
        }

        bool m_UpdateUIBounds = false;
        void UpdateUIBounds()
        {
            if (m_InControllerChanged)
            {
                m_UpdateUIBounds = true;
                return;
            }
            m_UpdateUIBounds = false;

            if (panel != null)
            {
                (panel as BaseVisualElementPanel).ValidateLayout();
                controller.graph.UIInfos.uiBounds = GetElementsBounds(rootNodes.Values);
            }
        }

        void SyncGroupNodes()
        {
            var groupNodes = this.groupNodes;

            if (controller == null)
            {
                foreach (var kv in groupNodes)
                {
                    RemoveElement(kv.Value);
                }
            }
            else
            {
                var deletedControllers = groupNodes.Keys.Except(controller.groupNodes);

                foreach (var deletedController in deletedControllers)
                {
                    RemoveElement(groupNodes[deletedController]);
                }


                bool addNew = false;

                foreach (var newController in controller.groupNodes.Except(groupNodes.Keys))
                {
                    var newElement = new VFXGroupNode();
                    AddElement(newElement);
                    newElement.controller = newController;

                    addNew = true;
                }

                if (addNew && panel != null)
                {
                    (panel as BaseVisualElementPanel).ValidateLayout();
                }
            }
        }

        void SyncStickyNotes()
        {
            var stickyNotes = this.stickyNotes;

            if (controller == null)
            {
                foreach (var kv in stickyNotes)
                {
                    RemoveElement(kv.Value);
                }
            }
            else
            {
                var deletedControllers = stickyNotes.Keys.Except(controller.stickyNotes);

                foreach (var deletedController in deletedControllers)
                {
                    RemoveElement(stickyNotes[deletedController]);
                }

                foreach (var newController in controller.stickyNotes.Except(stickyNotes.Keys))
                {
                    var newElement = new VFXStickyNote();
                    newElement.controller = newController;
                    AddElement(newElement);
                }
            }
        }

        void SyncEdges(int change)
        {
            if (change != VFXViewController.Change.flowEdge)
            {
                var dataEdges = contentViewContainer.Query().Children<VisualElement>().Children<VFXDataEdge>().ToList().ToDictionary(t => t.controller, t => t);
                if (controller == null)
                {
                    foreach (var element in dataEdges.Values)
                    {
                        RemoveElement(element);
                    }
                }
                else
                {
                    var deletedControllers = dataEdges.Keys.Except(controller.dataEdges);

                    foreach (var deletedController in deletedControllers)
                    {
                        var edge = dataEdges[deletedController];
                        if (edge.input != null)
                        {
                            edge.input.Disconnect(edge);
                        }
                        if (edge.output != null)
                        {
                            edge.output.Disconnect(edge);
                        }
                        RemoveElement(edge);
                    }

                    foreach (var newController in controller.dataEdges.Except(dataEdges.Keys))
                    {
                        // SyncEdges could be called before the VFXNodeUI have been created, it that case ignore them and trust that they will be created later when the
                        // nodes arrive.
                        if (GetNodeByController(newController.input.sourceNode) == null || GetNodeByController(newController.output.sourceNode) == null)
                        {
                            if (change != VFXViewController.Change.dataEdge)
                            {
                                Debug.LogError("Can't match nodes for a data edge after nodes should have been updated.");
                            }
                            continue;
                        }

                        var newElement = new VFXDataEdge();
                        AddElement(newElement);
                        newElement.controller = newController;
                        if (newElement.input != null)
                            newElement.input.node.RefreshExpandedState();
                        if (newElement.output != null)
                            newElement.output.node.RefreshExpandedState();
                    }
                }
            }

            if (change != VFXViewController.Change.dataEdge)
            {
                var flowEdges = contentViewContainer.Query().Children<VisualElement>().Children<VFXFlowEdge>().ToList().ToDictionary(t => t.controller, t => t);
                if (controller == null)
                {
                    foreach (var element in flowEdges.Values)
                    {
                        RemoveElement(element);
                    }
                }
                else
                {
                    var deletedControllers = flowEdges.Keys.Except(controller.flowEdges);

                    foreach (var deletedController in deletedControllers)
                    {
                        var edge = flowEdges[deletedController];
                        if (edge.input != null)
                        {
                            edge.input.Disconnect(edge);
                        }
                        if (edge.output != null)
                        {
                            edge.output.Disconnect(edge);
                        }
                        RemoveElement(edge);
                    }

                    foreach (var newController in controller.flowEdges.Except(flowEdges.Keys))
                    {
                        var newElement = new VFXFlowEdge();
                        AddElement(newElement);
                        newElement.controller = newController;
                    }
                }
            }
        }

        void OnCreateNode(NodeCreationContext ctx)
        {
            VFXFilterWindow.Show(VFXViewWindow.currentWindow, GUIUtility.ScreenToGUIPoint(ctx.screenMousePosition), m_NodeProvider);
        }

        VFXRendererSettings GetRendererSettings()
        {
            if (controller != null)
            {
                var asset = controller.model;
                if (asset != null)
                    return asset.rendererSettings;
            }

            return new VFXRendererSettings();
        }

        VFXCullingFlags cullingFlags
        {
            get
            {
                if (controller != null)
                {
                    var asset = controller.model;
                    if (asset != null)
                        return asset.cullingFlags;
                }
                return VFXCullingFlags.CullDefault;
            }

            set
            {
                if (controller != null)
                {
                    var asset = controller.model;
                    if (asset != null)
                        asset.cullingFlags = value;
                }
            }
        }

        public void CreateTemplateSystem(string path, Vector2 tPos, VFXGroupNode groupNode)
        {
            VisualEffectAsset asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(path);
            if (asset != null)
            {
                VFXViewController templateController = VFXViewController.GetController(asset, true);
                templateController.useCount++;


                var data = VFXCopyPaste.SerializeElements(templateController.allChildren, templateController.graph.UIInfos.uiBounds);

                VFXCopyPaste.UnserializeAndPasteElements(controller, tPos, data, this, groupNode != null ? groupNode.controller : null);

                templateController.useCount--;
            }
        }

        void SetRendererSettings(VFXRendererSettings settings)
        {
            if (controller != null)
            {
                var asset = controller.model;
                if (asset != null)
                {
                    asset.rendererSettings = settings;
                    controller.graph.SetExpressionGraphDirty();
                }
            }
        }

        void OnToggleCastShadows()
        {
            var settings = GetRendererSettings();
            if (settings.shadowCastingMode != ShadowCastingMode.Off)
                settings.shadowCastingMode = ShadowCastingMode.Off;
            else
                settings.shadowCastingMode = ShadowCastingMode.On;
            SetRendererSettings(settings);
        }

        void OnToggleMotionVectors()
        {
            var settings = GetRendererSettings();
            if (settings.motionVectorGenerationMode == MotionVectorGenerationMode.Object)
                settings.motionVectorGenerationMode = MotionVectorGenerationMode.Camera;
            else
                settings.motionVectorGenerationMode = MotionVectorGenerationMode.Object;
            SetRendererSettings(settings);
        }

        void OnShowBounds()
        {
            VisualEffect.renderBounds = !VisualEffect.renderBounds;
        }

        void OnToggleCompile()
        {
            VFXViewWindow.currentWindow.autoCompile = !VFXViewWindow.currentWindow.autoCompile;
        }

        void OnCompile()
        {
            var graph = controller.graph;
            graph.SetExpressionGraphDirty();
            graph.RecompileIfNeeded();
        }

        public EventPropagation Compile()
        {
            OnCompile();

            return EventPropagation.Stop;
        }

        VFXContext AddVFXContext(Vector2 pos, VFXModelDescriptor<VFXContext> desc)
        {
            if (controller == null) return null;
            return controller.AddVFXContext(pos, desc);
        }

        VFXOperator AddVFXOperator(Vector2 pos, VFXModelDescriptor<VFXOperator> desc)
        {
            if (controller == null) return null;
            return controller.AddVFXOperator(pos, desc);
        }

        VFXParameter AddVFXParameter(Vector2 pos, VFXModelDescriptorParameters desc)
        {
            if (controller == null) return null;
            return controller.AddVFXParameter(pos, desc);
        }

        void AddVFXParameter(Vector2 pos, VFXParameterController parameterController)
        {
            if (controller == null || parameterController == null) return;

            controller.AddVFXParameter(pos, parameterController);
        }

        public EventPropagation Resync()
        {
            if (controller != null)
                controller.ForceReload();
            return EventPropagation.Stop;
        }

        public EventPropagation OutputToDot()
        {
            if (controller == null) return EventPropagation.Stop;
            DotGraphOutput.DebugExpressionGraph(controller.graph, VFXExpressionContextOption.None);
            return EventPropagation.Stop;
        }

        public EventPropagation OutputToDotReduced()
        {
            if (controller == null) return EventPropagation.Stop;
            DotGraphOutput.DebugExpressionGraph(controller.graph, VFXExpressionContextOption.Reduction);
            return EventPropagation.Stop;
        }

        public EventPropagation OutputToDotConstantFolding()
        {
            if (controller == null) return EventPropagation.Stop;
            DotGraphOutput.DebugExpressionGraph(controller.graph, VFXExpressionContextOption.ConstantFolding);
            return EventPropagation.Stop;
        }

        public EventPropagation ReinitComponents()
        {
            foreach (var component in VFXManager.GetComponents())
                component.Reinit();
            return EventPropagation.Stop;
        }

        public IEnumerable<VFXContextUI> GetAllContexts()
        {
            foreach (var layer in contentViewContainer.Children())
            {
                foreach (var element in layer)
                {
                    if (element is VFXContextUI)
                    {
                        yield return element as VFXContextUI;
                    }
                }
            }
        }

        public IEnumerable<VFXNodeUI> GetAllNodes()
        {
            foreach (var layer in contentViewContainer.Children())
            {
                foreach (var element in layer)
                {
                    if (element is VFXNodeUI)
                    {
                        yield return element as VFXNodeUI;
                    }
                }
            }
        }

        public override List<Port> GetCompatiblePorts(Port startAnchor, NodeAdapter nodeAdapter)
        {
            if (controller == null) return null;


            if (startAnchor is VFXDataAnchor)
            {
                var controllers = controller.GetCompatiblePorts((startAnchor as  VFXDataAnchor).controller, nodeAdapter);
                return controllers.Select(t => (Port)GetDataAnchorByController(t as VFXDataAnchorController)).ToList();
            }
            else
            {
                var controllers = controller.GetCompatiblePorts((startAnchor as VFXFlowAnchor).controller, nodeAdapter);
                return controllers.Select(t => (Port)GetFlowAnchorByController(t as VFXFlowAnchorController)).ToList();
            }
        }

        public IEnumerable<VFXFlowAnchor> GetAllFlowAnchors(bool input, bool output)
        {
            foreach (var context in GetAllContexts())
            {
                foreach (VFXFlowAnchor anchor in context.GetFlowAnchors(input, output))
                {
                    yield return anchor;
                }
            }
        }

        void VFXElementResized(VisualElement element)
        {
            if (element is IVFXResizable)
            {
                (element as IVFXResizable).OnResized();
            }
        }

        GraphViewChange VFXGraphViewChanged(GraphViewChange change)
        {
            if (change.movedElements != null && change.movedElements.Count > 0)
            {
                HashSet<IVFXMovable> movables = new HashSet<IVFXMovable>(change.movedElements.OfType<IVFXMovable>());
                foreach (var groupNode in vfxGroupNodes.ToList())
                {
                    var containedElements = groupNode.containedElements;

                    if (containedElements != null && containedElements.Intersect(change.movedElements).Count() > 0)
                    {
                        groupNode.UpdateGeometryFromContent();
                        movables.Add(groupNode);
                    }
                }

                foreach (var groupNode in change.movedElements.OfType<VFXGroupNode>())
                {
                    var containedElements = groupNode.containedElements;
                    if (containedElements != null)
                    {
                        foreach (var node in containedElements.OfType<IVFXMovable>())
                        {
                            movables.Add(node);
                        }
                    }
                }

                foreach (var movable in movables)
                {
                    movable.OnMoved();
                }
            }
            return change;
        }

        VFXNodeUI GetNodeByController(VFXNodeController controller)
        {
            if (controller is VFXBlockController)
            {
                var blockController = (controller as VFXBlockController);
                VFXContextUI context = GetNodeByController(blockController.contextController) as VFXContextUI;

                return context.GetAllBlocks().FirstOrDefault(t => t.controller == blockController);
            }
            return GetAllNodes().FirstOrDefault(t => t.controller == controller);
        }

        public VFXDataAnchor GetDataAnchorByController(VFXDataAnchorController controller)
        {
            if (controller == null)
                return null;

            VFXNodeUI node = GetNodeByController(controller.sourceNode);
            if (node == null)
            {
                Debug.LogError("Can't find the node for a given node controller");
                return null;
            }

            VFXDataAnchor anchor = node.GetPorts(controller.direction == Direction.Input, controller.direction == Direction.Output).FirstOrDefault(t => t.controller == controller);
            if (anchor == null)
            {
                // Can happen because the order of the DataWatch is not controlled
                node.ForceUpdate();
                anchor = node.GetPorts(controller.direction == Direction.Input, controller.direction == Direction.Output).FirstOrDefault(t => t.controller == controller);
            }
            return anchor;
        }

        public VFXFlowAnchor GetFlowAnchorByController(VFXFlowAnchorController controller)
        {
            if (controller == null)
                return null;
            return GetAllFlowAnchors(controller.direction == Direction.Input, controller.direction == Direction.Output).Where(t => t.controller == controller).FirstOrDefault();
        }

        public IEnumerable<VFXDataAnchor> GetAllDataAnchors(bool input, bool output)
        {
            foreach (var layer in contentViewContainer.Children())
            {
                foreach (var element in layer)
                {
                    if (element is VFXNodeUI)
                    {
                        var ope = element as VFXNodeUI;
                        foreach (VFXDataAnchor anchor in ope.GetPorts(input, output))
                            yield return anchor;
                        if (element is VFXContextUI)
                        {
                            var context = element as VFXContextUI;

                            foreach (VFXBlockUI block in context.GetAllBlocks())
                            {
                                foreach (VFXDataAnchor anchor in block.GetPorts(input, output))
                                    yield return anchor;
                            }
                        }
                    }
                }
            }
        }

        public VFXDataEdge GetDataEdgeByController(VFXDataEdgeController controller)
        {
            foreach (var layer in contentViewContainer.Children())
            {
                foreach (var element in layer)
                {
                    if (element is VFXDataEdge)
                    {
                        VFXDataEdge candidate = element as VFXDataEdge;
                        if (candidate.controller == controller)
                            return candidate;
                    }
                }
            }
            return null;
        }

        public IEnumerable<VFXDataEdge> GetAllDataEdges()
        {
            foreach (var layer in contentViewContainer.Children())
            {
                foreach (var element in layer)
                {
                    if (element is VFXDataEdge)
                    {
                        yield return element as VFXDataEdge;
                    }
                }
            }
        }

        public IEnumerable<Port> GetAllPorts(bool input, bool output)
        {
            foreach (var anchor in GetAllDataAnchors(input, output))
            {
                yield return anchor;
            }
            foreach (var anchor in GetAllFlowAnchors(input, output))
            {
                yield return anchor;
            }
        }

        void SelectionUpdated()
        {
            if (controller == null) return;

            if (!VisualEffectEditor.s_IsEditingAsset)
            {
                var objectSelected = selection.OfType<VFXNodeUI>().Select(t => t.controller.model).Concat(selection.OfType<VFXContextUI>().Select(t => t.controller.model)).Where(t => t != null).ToArray();

                if (objectSelected.Length > 0)
                {
                    Selection.objects = objectSelected;
                    return;
                }

                var blackBoardSelected = selection.OfType<BlackboardField>().Select(t => t.GetFirstAncestorOfType<VFXBlackboardRow>().controller.model).ToArray();

                if (blackBoardSelected.Length > 0)
                {
                    Selection.objects = blackBoardSelected;
                    return;
                }

                if (Selection.activeObject != controller.model)
                {
                    Selection.activeObject = controller.model;
                }
            }
        }

        void ElementAddedToGroupNode(Group groupNode, GraphElement element)
        {
            (groupNode as VFXGroupNode).ElementAddedToGroupNode(element);
        }

        void ElementRemovedFromGroupNode(Group groupNode, GraphElement element)
        {
            (groupNode as VFXGroupNode).ElementRemovedFromGroupNode(element);
        }

        void GroupNodeTitleChanged(Group groupNode, string title)
        {
            (groupNode as VFXGroupNode).GroupNodeTitleChanged(title);
        }

        public override void AddToSelection(ISelectable selectable)
        {
            base.AddToSelection(selectable);
            SelectionUpdated();
        }

        public override void RemoveFromSelection(ISelectable selectable)
        {
            base.RemoveFromSelection(selectable);
            SelectionUpdated();
        }

        public override void ClearSelection()
        {
            bool selectionEmpty = selection.Count() == 0;
            base.ClearSelection();
            if (!selectionEmpty)
                SelectionUpdated();
        }

        VFXBlackboard m_Blackboard;

        private Label m_DropDownButtonCullingMode;
        private Toggle m_ToggleCastShadows;
        private Toggle m_ToggleMotionVectors;

        public readonly Vector2 defaultPasteOffset = new Vector2(100, 100);
        public Vector2 pasteOffset = Vector2.zero;

        public VFXBlackboard blackboard
        {
            get { return m_Blackboard; }
        }

        protected internal override bool canCopySelection
        {
            get { return selection.OfType<VFXNodeUI>().Any() || selection.OfType<Group>().Any() || selection.OfType<VFXContextUI>().Any() || selection.OfType<VFXStickyNote>().Any(); }
        }

        IEnumerable<Controller> ElementsToController(IEnumerable<GraphElement> elements)
        {
            return elements.OfType<IControlledElement>().Select(t => t.controller);
        }

        void CollectElements(IEnumerable<GraphElement> elements, HashSet<GraphElement> elementsToCopySet)
        {
            foreach (var element in elements)
            {
                if (element is Group)
                {
                    CollectElements((element as Group).containedElements, elementsToCopySet);
                    elementsToCopySet.Add(element);
                }
                else if (element is Node || element is VFXContextUI || element is VFXStickyNote)
                {
                    elementsToCopySet.Add(element);
                }
            }
        }

        protected internal override void CollectCopyableGraphElements(IEnumerable<GraphElement> elements, HashSet<GraphElement> elementsToCopySet)
        {
            CollectElements(elements, elementsToCopySet);

            var nodeuis = new HashSet<VFXNodeUI>(elementsToCopySet.SelectMany(t => t.Query().OfType<VFXNodeUI>().ToList()));
            var contextuis = new HashSet<VFXContextUI>(elementsToCopySet.OfType<VFXContextUI>());

            foreach (var edge in edges.ToList())
            {
                if (edge is VFXDataEdge)
                {
                    if (nodeuis.Contains(edge.input.GetFirstAncestorOfType<VFXNodeUI>()) && nodeuis.Contains(edge.output.GetFirstAncestorOfType<VFXNodeUI>()))
                    {
                        elementsToCopySet.Add(edge);
                    }
                }
                else
                {
                    if (contextuis.Contains(edge.input.GetFirstAncestorOfType<VFXContextUI>()) && contextuis.Contains(edge.output.GetFirstAncestorOfType<VFXContextUI>()))
                    {
                        elementsToCopySet.Add(edge);
                    }
                }
            }
        }

        Rect GetElementsBounds(IEnumerable<GraphElement> elements)
        {
            Rect[] elementBounds = elements.Select(t => contentViewContainer.WorldToLocal(t.worldBound)).ToArray();
            if (elementBounds.Length < 1) return Rect.zero;

            Rect bounds = elementBounds[0];

            for (int i = 1; i < elementBounds.Length; ++i)
            {
                bounds = Rect.MinMaxRect(Mathf.Min(elementBounds[i].xMin, bounds.xMin), Mathf.Min(elementBounds[i].yMin, bounds.yMin), Mathf.Max(elementBounds[i].xMax, bounds.xMax), Mathf.Max(elementBounds[i].yMax, bounds.yMax));
            }

            return bounds;
        }

        string SerializeElements(IEnumerable<GraphElement> elements)
        {
            pasteOffset = defaultPasteOffset;

            return VFXCopyPaste.SerializeElements(ElementsToController(elements), GetElementsBounds(elements));
        }

        Vector2 visibleCenter
        {
            get
            {
                Vector2 center = layout.size * 0.5f;

                center = this.ChangeCoordinatesTo(contentViewContainer, center);

                return center;
            }
        }

        void UnserializeAndPasteElements(string operationName, string data)
        {
            VFXCopyPaste.UnserializeAndPasteElements(controller, visibleCenter, data, this);

            pasteOffset += defaultPasteOffset;
        }

        const float k_MarginBetweenContexts = 30;
        public void PushUnderContext(VFXContextUI context, float size)
        {
            if (size < 5) return;

            HashSet<VFXContextUI> contexts = new HashSet<VFXContextUI>();

            contexts.Add(context);

            var flowEdges = edges.ToList().OfType<VFXFlowEdge>().ToList();

            int contextCount = 0;

            while (contextCount < contexts.Count())
            {
                contextCount = contexts.Count();
                foreach (var flowEdge in flowEdges)
                {
                    VFXContextUI topContext = flowEdge.output.GetFirstAncestorOfType<VFXContextUI>();
                    VFXContextUI bottomContext = flowEdge.input.GetFirstAncestorOfType<VFXContextUI>();
                    if (contexts.Contains(topContext)  && !contexts.Contains(bottomContext))
                    {
                        float topContextBottom = topContext.layout.yMax;
                        float newTopContextBottom = topContext.layout.yMax + size;
                        if (topContext == context)
                        {
                            newTopContextBottom -= size;
                            topContextBottom -= size;
                        }
                        float bottomContextTop = bottomContext.layout.yMin;

                        if (topContextBottom < bottomContextTop && newTopContextBottom + k_MarginBetweenContexts > bottomContextTop)
                        {
                            contexts.Add(bottomContext);
                        }
                    }
                }
            }

            contexts.Remove(context);

            foreach (var c in contexts)
            {
                c.controller.position = c.GetPosition().min + new Vector2(0, size);
            }
        }

        void OnToggleDebug()
        {
            if (controller != null)
            {
                controller.graph.displaySubAssets = !controller.graph.displaySubAssets;
            }
        }

        bool canGroupSelection
        {
            get
            {
                return canCopySelection && !selection.Any(t => t is Group);
            }
        }

        public void ValidateCommand(ValidateCommandEvent evt)
        {
            if (evt.commandName == "SelectAll")
            {
                evt.StopPropagation();
                if (evt.imguiEvent != null)
                {
                    evt.imguiEvent.Use();
                }
            }
        }

        public void ExecuteCommand(ExecuteCommandEvent e)
        {
            if (e.commandName == "SelectAll")
            {
                ClearSelection();

                foreach (var element in graphElements.ToList())
                {
                    AddToSelection(element);
                }
                e.StopPropagation();
            }
        }

        void GroupSelection()
        {
            controller.GroupNodes(selection.OfType<ISettableControlledElement<VFXNodeController>>().Select(t => t.controller));
        }

        void AddStickyNote(Vector2 position)
        {
            position = contentViewContainer.WorldToLocal(position);
            controller.AddStickyNote(position);
        }

        void OnCreateNodeInGroupNode(ContextualMenu.MenuAction e)
        {
            Debug.Log("CreateMenuPosition" + e.eventInfo.mousePosition);
            //The targeted groupnode will be determined by a PickAll later
            VFXFilterWindow.Show(VFXViewWindow.currentWindow, e.eventInfo.mousePosition, m_NodeProvider);
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            Vector2 mousePosition = evt.mousePosition;
                evt.menu.AppendAction("Group Selection", (e) => { GroupSelection(); },
                    (e) => { return canGroupSelection ? ContextualMenu.MenuAction.StatusFlags.Normal : ContextualMenu.MenuAction.StatusFlags.Disabled; });
            evt.menu.AppendAction("New Sticky Note", (e) => { AddStickyNote(mousePosition); },
                (e) => { return ContextualMenu.MenuAction.StatusFlags.Normal; });
            evt.menu.AppendSeparator();
            if (evt.target is VFXContextUI)
            {
                evt.menu.AppendAction("Cut", (e) => { CutSelectionCallback(); },
                    (e) => { return canCutSelection ? ContextualMenu.MenuAction.StatusFlags.Normal : ContextualMenu.MenuAction.StatusFlags.Disabled; });
                evt.menu.AppendAction("Copy", (e) => { CopySelectionCallback(); },
                    (e) => { return canCopySelection ? ContextualMenu.MenuAction.StatusFlags.Normal : ContextualMenu.MenuAction.StatusFlags.Disabled; });
            }
            base.BuildContextualMenu(evt);
            /*
            if (evt.target is UIElements.GraphView.GraphView)
            {
                evt.menu.AppendAction("Paste", (e) => { PasteCallback(); },
                    (e) => { return canPaste ? ContextualMenu.MenuAction.StatusFlags.Normal : ContextualMenu.MenuAction.StatusFlags.Disabled; });
            }*/
        }

        private static readonly KeyValuePair<string, VFXCullingFlags>[] k_CullingOptions = new KeyValuePair<string, VFXCullingFlags>[]
        {
            new KeyValuePair<string, VFXCullingFlags>("Cull simulation and bounds", (VFXCullingFlags.CullSimulation | VFXCullingFlags.CullBoundsUpdate)),
            new KeyValuePair<string, VFXCullingFlags>("Cull simulation only", (VFXCullingFlags.CullSimulation)),
            new KeyValuePair<string, VFXCullingFlags>("Disable culling", VFXCullingFlags.CullNone),
        };

        private string CullingMaskToString(VFXCullingFlags flags)
        {
            return k_CullingOptions.First(o => o.Value == flags).Key;
        }

        bool IDropTarget.CanAcceptDrop(List<ISelectable> selection)
        {
            return selection.Any(t => t is BlackboardField && (t as BlackboardField).GetFirstAncestorOfType<VFXBlackboardRow>() != null);
        }

        bool IDropTarget.DragExited()
        {
            return true;
        }

        bool IDropTarget.DragPerform(DragPerformEvent evt, IEnumerable<ISelectable> selection, IDropTarget dropTarget)
        {
            var rows = selection.OfType<BlackboardField>().Select(t => t.GetFirstAncestorOfType<VFXBlackboardRow>()).Where(t => t != null).ToArray();

            Vector2 mousePosition = contentViewContainer.WorldToLocal(evt.mousePosition);
            foreach (var row in rows)
            {
                AddVFXParameter(mousePosition - new Vector2(100, 75), row.controller);
            }

            return true;
        }

        bool IDropTarget.DragUpdated(DragUpdatedEvent evt, IEnumerable<ISelectable> selection, IDropTarget dropTarget)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Link;

            return true;
        }

        void OnDragUpdated(DragUpdatedEvent e)
        {
            if (selection.Any(t => t is BlackboardField && (t as BlackboardField).GetFirstAncestorOfType<VFXBlackboardRow>() != null))
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                e.StopPropagation();
            }
        }

        void OnDragPerform(DragPerformEvent e)
        {
            var rows = selection.OfType<BlackboardField>().Select(t => t.GetFirstAncestorOfType<VFXBlackboardRow>()).Where(t => t != null).ToArray();
            if (rows.Length > 0)
            {
                Vector2 mousePosition = contentViewContainer.WorldToLocal(e.mousePosition);
                foreach (var row in rows)
                {
                    AddVFXParameter(mousePosition - new Vector2(50, 20), row.controller);
                }
            }
        }
    }
}
