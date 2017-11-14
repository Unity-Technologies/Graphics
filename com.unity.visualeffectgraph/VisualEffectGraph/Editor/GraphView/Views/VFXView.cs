using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.VFX;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;


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
            if (!VFXComponentEditor.s_IsEditingAsset)
                Selection.activeObject = m_View.GetPresenter<VFXViewPresenter>().GetVFXAsset();
        }
    }

    class VFXNodeProvider : VFXAbstractProvider<VFXNodeProvider.Descriptor>
    {
        public class Descriptor
        {
            public object modelDescriptor;
            public string category;
            public string name;
        }

        public VFXNodeProvider(Action<Descriptor, Vector2> onAddBlock) : base(onAddBlock)
        {
        }

        protected override string GetCategory(Descriptor desc)
        {
            return desc.category;
        }

        protected override string GetName(Descriptor desc)
        {
            return desc.name;
        }

        protected override IEnumerable<Descriptor> GetDescriptors()
        {
            var systemDesc = new Descriptor()
            {
                modelDescriptor = null,
                category = "System",
                name = "Default System"
            };
            var descriptorsContext = VFXLibrary.GetContexts().Select(o =>
                {
                    return new Descriptor()
                    {
                        modelDescriptor = o,
                        category = "Context/" + o.info.category,
                        name = o.name
                    };
                }).OrderBy(o => o.category + o.name);

            var descriptorsOperator = VFXLibrary.GetOperators().Select(o =>
                {
                    return new Descriptor()
                    {
                        modelDescriptor = o,
                        category = "Operator/" + o.info.category,
                        name = o.name
                    };
                }).OrderBy(o => o.category + o.name);

            var descriptorParameter = VFXLibrary.GetParameters().Select(o =>
                {
                    return new Descriptor()
                    {
                        modelDescriptor = o,
                        category = "Parameter/",
                        name = o.name
                    };
                }).OrderBy(o => o.category + o.name);

            var descriptorBuiltInParameter = VFXLibrary.GetBuiltInParameters().Select(o =>
                {
                    return new Descriptor()
                    {
                        modelDescriptor = o,
                        category = "BuiltIn/",
                        name = o.name
                    };
                }).OrderBy(o => o.category + o.name);

            var descriptorSourceAttributeParameter = VFXLibrary.GetSourceAttributeParameters().Select(o =>
                {
                    return new Descriptor()
                    {
                        modelDescriptor = o,
                        category = "SourceAttribute/",
                        name = o.name
                    };
                }).OrderBy(o => o.name);

            var descriptorCurrentAttributeParameter = VFXLibrary.GetCurrentAttributeParameters().Select(o =>
                {
                    return new Descriptor()
                    {
                        modelDescriptor = o,
                        category = "CurrentAttribute/",
                        name = o.name
                    };
                }).OrderBy(o => o.category + o.name);

            return descriptorsContext.Concat(descriptorsOperator)
                .Concat(descriptorParameter)
                .Concat(descriptorBuiltInParameter)
                .Concat(descriptorSourceAttributeParameter)
                .Concat(descriptorCurrentAttributeParameter)
                .Concat(Enumerable.Repeat(systemDesc, 1));
        }
    }

    //[StyleSheet("Assets/VFXEditor/Editor/GraphView/Views/")]
    class VFXView : GraphView, IParameterDropTarget
    {
        VisualElement m_NoAssetLabel;

        public VFXView()
        {
            forceNotififcationOnAdd = true;
            SetupZoom(new Vector3(0.125f, 0.125f, 1), new Vector3(8, 8, 1));

            //this.AddManipulator(new SelectionSetter(this));
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new RectangleSelector());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new ClickSelector());

            this.AddManipulator(new ParameterDropper());

            var bg = new GridBackground() { name = "VFXBackgroundGrid" };
            Insert(0, bg);

            this.AddManipulator(new FilterPopup(new VFXNodeProvider((d, mPos) =>
                {
                    Vector2 tPos = this.ChangeCoordinatesTo(contentViewContainer, mPos);
                    if (d.modelDescriptor is VFXModelDescriptor<VFXOperator>)
                    {
                        AddVFXOperator(tPos, (d.modelDescriptor as VFXModelDescriptor<VFXOperator>));
                    }
                    else if (d.modelDescriptor is VFXModelDescriptor<VFXContext>)
                    {
                        AddVFXContext(tPos, d.modelDescriptor as VFXModelDescriptor<VFXContext>);
                    }
                    else if (d.modelDescriptor is VFXModelDescriptorParameters)
                    {
                        AddVFXParameter(tPos, d.modelDescriptor as VFXModelDescriptorParameters);
                    }
                    else if (d.modelDescriptor is VFXModelDescriptorBuiltInParameters)
                    {
                        AddVFXBuiltInParameter(tPos, d.modelDescriptor as VFXModelDescriptorBuiltInParameters);
                    }
                    else if (d.modelDescriptor is VFXModelDescriptorCurrentAttributeParameters)
                    {
                        AddVFXCurrentAttributeParameter(tPos, d.modelDescriptor as VFXModelDescriptorCurrentAttributeParameters);
                    }
                    else if (d.modelDescriptor is VFXModelDescriptorSourceAttributeParameters)
                    {
                        AddVFXSourceAttributeParameter(tPos, d.modelDescriptor as VFXModelDescriptorSourceAttributeParameters);
                    }
                    else if (d.modelDescriptor == null)
                    {
                        VFXViewPresenter presenter = GetPresenter<VFXViewPresenter>();
                        if (presenter != null)
                        {
                            var contexts = VFXLibrary.GetContexts().ToArray();
                            var spawnerDesc = contexts.FirstOrDefault(t => t.name == "Spawner");
                            var spawner = presenter.AddVFXContext(tPos, spawnerDesc);
                            var initialize = presenter.AddVFXContext(tPos + new Vector2(0, 200), contexts.FirstOrDefault(t => t.name == "Initialize"));
                            var update = presenter.AddVFXContext(tPos + new Vector2(0, 400), contexts.FirstOrDefault(t => t.name == "Update"));
                            var output = presenter.AddVFXContext(tPos + new Vector2(0, 600), contexts.FirstOrDefault(t => t.name == "Point Output"));

                            spawner.LinkTo(initialize);
                            initialize.LinkTo(update);
                            update.LinkTo(output);
                        }
                    }
                    else
                    {
                        Debug.LogErrorFormat("Add unknown presenter : {0}", d.modelDescriptor.GetType());
                    }
                }), null));

            typeFactory[typeof(VFXAttributeParameterPresenter)] = typeof(VFXAttributeParameterUI);
            typeFactory[typeof(VFXBuiltInParameterPresenter)] = typeof(VFXBuiltInParameterUI);
            typeFactory[typeof(VFXParameterPresenter)] = typeof(VFXParameterUI);
            typeFactory[typeof(VFXOperatorPresenter)] = typeof(VFXOperatorUI);
            typeFactory[typeof(VFXContextPresenter)] = typeof(VFXContextUI);
            typeFactory[typeof(VFXFlowEdgePresenter)] = typeof(VFXFlowEdge);
            typeFactory[typeof(VFXDataEdgePresenter)] = typeof(VFXDataEdge);
            typeFactory[typeof(VFXFlowInputAnchorPresenter)] = typeof(VFXFlowAnchor);
            typeFactory[typeof(VFXFlowOutputAnchorPresenter)] = typeof(VFXFlowAnchor);
            typeFactory[typeof(VFXContextDataInputAnchorPresenter)] = typeof(VFXDataAnchor);
            typeFactory[typeof(VFXContextDataOutputAnchorPresenter)] = typeof(VFXDataAnchor);
            typeFactory[typeof(VFXInputOperatorAnchorPresenter)] = typeof(VFXDataAnchor);
            typeFactory[typeof(VFXOutputOperatorAnchorPresenter)] = typeof(VFXDataAnchor);
            typeFactory[typeof(Preview3DPresenter)] = typeof(Preview3D);

            AddStyleSheetPath("PropertyRM");
            AddStyleSheetPath("VFXContext");
            AddStyleSheetPath("VFXFlow");
            AddStyleSheetPath("VFXBlock");
            AddStyleSheetPath("VFXNode");
            AddStyleSheetPath("VFXDataAnchor");
            AddStyleSheetPath("VFXTypeColor");
            AddStyleSheetPath("VFXView");

            Dirty(ChangeType.Transform);

            AddLayer(-1);
            AddLayer(1);
            AddLayer(2);

            focusIndex = 0;

            VisualElement toolbar = new VisualElement();
            toolbar.AddToClassList("toolbar");
            Add(toolbar);


            Button button = new Button(() => { Resync(); });
            button.text = "Refresh";
            button.AddToClassList("toolbarItem");
            toolbar.Add(button);


            VisualElement spacer = new VisualElement();
            spacer.style.flex = 1;
            toolbar.Add(spacer);

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
            toggleRenderBounds.on = VFXComponent.renderBounds;
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

            this.serializeGraphElements = SerializeElements;
            this.unserializeAndPaste = UnserializeAndPasteElements;
        }

        VFXRendererSettings GetRendererSettings()
        {
            var presenter = GetPresenter<VFXViewPresenter>();
            if (presenter != null)
            {
                var asset = presenter.GetVFXAsset();
                if (asset != null)
                    return asset.rendererSettings;
            }

            return new VFXRendererSettings();
        }

        void SetRendererSettings(VFXRendererSettings settings)
        {
            var presenter = GetPresenter<VFXViewPresenter>();
            if (presenter != null)
            {
                var asset = presenter.GetVFXAsset();
                if (asset != null)
                {
                    asset.rendererSettings = settings;
                    presenter.GetGraph().SetExpressionGraphDirty();
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
            VFXComponent.renderBounds = !VFXComponent.renderBounds;
        }

        void OnToggleCompile()
        {
            VFXViewWindow.currentWindow.autoCompile = !VFXViewWindow.currentWindow.autoCompile;
        }

        void OnCompile()
        {
            var graph = GetPresenter<VFXViewPresenter>().GetGraph();
            graph.SetExpressionGraphDirty();
            graph.RecompileIfNeeded();
        }

        void AddVFXContext(Vector2 pos, VFXModelDescriptor<VFXContext> desc)
        {
            if (presenter == null) return;
            GetPresenter<VFXViewPresenter>().AddVFXContext(pos, desc);
        }

        void AddVFXOperator(Vector2 pos, VFXModelDescriptor<VFXOperator> desc)
        {
            if (presenter == null) return;
            GetPresenter<VFXViewPresenter>().AddVFXOperator(pos, desc);
        }

        void AddVFXParameter(Vector2 pos, VFXModelDescriptorParameters desc)
        {
            if (presenter == null) return;
            GetPresenter<VFXViewPresenter>().AddVFXParameter(pos, desc);
        }

        void AddVFXBuiltInParameter(Vector2 pos, VFXModelDescriptorBuiltInParameters desc)
        {
            if (presenter == null) return;
            GetPresenter<VFXViewPresenter>().AddVFXBuiltInParameter(pos, desc);
        }

        void AddVFXCurrentAttributeParameter(Vector2 pos, VFXModelDescriptorCurrentAttributeParameters desc)
        {
            if (presenter == null) return;
            GetPresenter<VFXViewPresenter>().AddVFXCurrentAttributeParameter(pos, desc);
        }

        void AddVFXSourceAttributeParameter(Vector2 pos, VFXModelDescriptorSourceAttributeParameters desc)
        {
            if (presenter == null) return;
            GetPresenter<VFXViewPresenter>().AddVFXSourceAttributeParameter(pos, desc);
        }

        public EventPropagation CloneModels() // TEST clean that
        {
            var contexts = selection.OfType<VFXContextUI>().Select(p => p.GetPresenter<VFXContextPresenter>().context.Clone<VFXContext>());
            foreach (var context in contexts)
            {
                context.position = context.position + new Vector2(50, 50);
                GetPresenter<VFXViewPresenter>().GetGraph().AddChild(context);
            }

            var operators = selection.OfType<Node>().Select(p => p.GetPresenter<VFXSlotContainerPresenter>().model.Clone<VFXSlotContainerModel<VFXModel, VFXModel>>());
            foreach (var op in operators)
            {
                op.position = op.position + new Vector2(50, 50);
                GetPresenter<VFXViewPresenter>().GetGraph().AddChild(op);
            }
            return EventPropagation.Stop;
        }

        public EventPropagation Resync()
        {
            var presenter = GetPresenter<VFXViewPresenter>();
            if (presenter != null)
                presenter.ForceReload();
            return EventPropagation.Stop;
        }

        public EventPropagation OutputToDot()
        {
            if (presenter == null) return EventPropagation.Stop;
            DotGraphOutput.DebugExpressionGraph(GetPresenter<VFXViewPresenter>().GetGraph(), VFXExpressionContextOption.None);
            return EventPropagation.Stop;
        }

        public EventPropagation OutputToDotReduced()
        {
            if (presenter == null) return EventPropagation.Stop;
            DotGraphOutput.DebugExpressionGraph(GetPresenter<VFXViewPresenter>().GetGraph(), VFXExpressionContextOption.Reduction);
            return EventPropagation.Stop;
        }

        public EventPropagation OutputToDotConstantFolding()
        {
            if (presenter == null) return EventPropagation.Stop;
            DotGraphOutput.DebugExpressionGraph(GetPresenter<VFXViewPresenter>().GetGraph(), VFXExpressionContextOption.ConstantFolding);
            return EventPropagation.Stop;
        }

        public EventPropagation ReinitComponents()
        {
            foreach (var component in VFXComponent.GetAllActive())
                component.Reinit();
            return EventPropagation.Stop;
        }

        public IEnumerable<VFXContextUI> GetAllContexts()
        {
            foreach (var layer in GetAllLayers())
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

        public override List<Port> GetCompatiblePorts(Port startAnchor, NodeAdapter nodeAdapter)
        {
            VFXViewPresenter presenter = GetPresenter<VFXViewPresenter>();
            if (presenter == null) return null;

            var presenters = presenter.GetCompatiblePorts(startAnchor.GetPresenter<PortPresenter>(), nodeAdapter);

            if (startAnchor is VFXDataAnchor)
            {
                return presenters.Select(t => (Port)GetDataAnchorByPresenter(t as VFXDataAnchorPresenter)).ToList();
            }
            else
            {
                return presenters.Select(t => (Port)GetFlowAnchorByPresenter(t as VFXFlowAnchorPresenter)).ToList();
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

        VFXViewPresenter m_OldPresenter;

        public override void OnDataChanged()
        {
            base.OnDataChanged();
            VFXViewPresenter presenter = GetPresenter<VFXViewPresenter>();

            if (presenter != null)
            {
                var settings = GetRendererSettings();

                m_ToggleCastShadows.on = settings.shadowCastingMode != ShadowCastingMode.Off;
                m_ToggleCastShadows.SetEnabled(true);

                m_ToggleMotionVectors.on = settings.motionVectorGenerationMode == MotionVectorGenerationMode.Object;
                m_ToggleMotionVectors.SetEnabled(true);

                // if the asset dis destroy somehow, fox example if the user delete the asset, delete the presenter and update the window.
                VFXAsset asset = presenter.GetVFXAsset();
                if (asset == null)
                {
                    VFXViewPresenter.Manager.RemovePresenter(presenter);

                    VFXViewWindow.currentWindow.presenter = null;
                    this.presenter = null; // this recall OnDataChanged recursively;
                    return;
                }
            }
            else
            {
                m_ToggleCastShadows.SetEnabled(false);
                m_ToggleMotionVectors.SetEnabled(false);
            }

            if (presenter != null)
            {
                m_NoAssetLabel.RemoveFromHierarchy();
            }
            else
            {
                if (m_NoAssetLabel.parent == null)
                {
                    Add(m_NoAssetLabel);
                }
            }

            if (m_OldPresenter != presenter && panel != null)
            {
                BaseVisualElementPanel panel = this.panel as BaseVisualElementPanel;


                panel.scheduler.ScheduleOnce(t => { panel.ValidateLayout(); FrameAll(); }, 100);

                m_OldPresenter = presenter;
            }
        }

        public VFXDataAnchor GetDataAnchorByPresenter(VFXDataAnchorPresenter presenter)
        {
            if (presenter == null)
                return null;
            return GetAllDataAnchors(presenter.direction == Direction.Input, presenter.direction == Direction.Output).Where(t => t.presenter == presenter).FirstOrDefault();
        }

        public VFXFlowAnchor GetFlowAnchorByPresenter(VFXFlowAnchorPresenter presenter)
        {
            if (presenter == null)
                return null;
            return GetAllFlowAnchors(presenter.direction == Direction.Input, presenter.direction == Direction.Output).Where(t => t.presenter == presenter).FirstOrDefault();
        }

        public IEnumerable<VFXDataAnchor> GetAllDataAnchors(bool input, bool output)
        {
            foreach (var layer in GetAllLayers())
            {
                foreach (var element in layer)
                {
                    if (element is VFXContextUI)
                    {
                        var context = element as VFXContextUI;


                        foreach (VFXDataAnchor anchor in context.ownData.GetPorts(input, output))
                            yield return anchor;

                        foreach (VFXBlockUI block in context.GetAllBlocks())
                        {
                            foreach (VFXDataAnchor anchor in block.GetPorts(input, output))
                                yield return anchor;
                        }
                    }
                    else if (element is VFXNodeUI)
                    {
                        var ope = element as VFXNodeUI;
                        foreach (VFXDataAnchor anchor in ope.GetPorts(input, output))
                            yield return anchor;
                    }
                }
            }
        }

        public VFXDataEdge GetDataEdgeByPresenter(VFXDataEdgePresenter presenter)
        {
            foreach (var layer in GetAllLayers())
            {
                foreach (var element in layer)
                {
                    if (element is VFXDataEdge)
                    {
                        VFXDataEdge candidate = element as VFXDataEdge;
                        if (candidate.presenter == presenter)
                            return candidate;
                    }
                }
            }
            return null;
        }

        public IEnumerable<VFXDataEdge> GetAllDataEdges()
        {
            foreach (var layer in GetAllLayers())
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

        public override IEnumerable<Port> GetAllPorts(bool input, bool output)
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

        public override IEnumerable<Node> GetAllNodes()
        {
            foreach (var node in base.GetAllNodes())
            {
                yield return node;
            }

            foreach (var layer in GetAllLayers())
            {
                foreach (var element in layer)
                {
                    if (element is VFXContextUI)
                    {
                        var context = element as VFXContextUI;

                        foreach (var block in context.GetAllBlocks())
                        {
                            yield return block;
                        }
                    }
                }
            }
        }

        void IParameterDropTarget.OnDragUpdated(IMGUIEvent evt, VFXParameterPresenter parameter)
        {
            //TODO : show that we accept the drag
            DragAndDrop.visualMode = DragAndDropVisualMode.Link;
        }

        void IParameterDropTarget.OnDragPerform(IMGUIEvent evt, VFXParameterPresenter parameter)
        {
            VFXViewPresenter presenter = GetPresenter<VFXViewPresenter>();
            if (presenter == null) return;

            VFXParameter newParameter = presenter.AddVFXParameter(contentViewContainer.GlobalToBound(evt.imguiEvent.mousePosition), VFXLibrary.GetParameters().FirstOrDefault(t => t.name == parameter.portType.UserFriendlyName()));

            newParameter.exposedName = parameter.exposedName;
            newParameter.exposed = true;
        }

        void SelectionUpdated()
        {
            if (!VFXComponentEditor.s_IsEditingAsset)
            {
                var contextSelected = selection.OfType<VFXContextUI>();

                if (presenter == null) return;

                if (contextSelected.Count() > 0)
                {
                    Selection.objects = contextSelected.Select(t => t.GetPresenter<VFXContextPresenter>().model).ToArray();
                }
                else if (Selection.activeObject != GetPresenter<VFXViewPresenter>().GetVFXAsset())
                {
                    Selection.activeObject = GetPresenter<VFXViewPresenter>().GetVFXAsset();
                }
            }
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

        private Toggle m_ToggleCastShadows;
        private Toggle m_ToggleMotionVectors;


        [System.Serializable]
        struct CopyPasteDataAnchor
        {
            public int targetIndex;
            public int[] slotPath;
        }

        [System.Serializable]
        struct CopyPasteDataEdge
        {
            public bool inputContext;
            public int inputBlockIndex;
            public CopyPasteDataAnchor input;
            public CopyPasteDataAnchor output;
        }

        [System.Serializable]
        struct CopyPasteFlowAnchor
        {
            public int contextIndex;
            public int flowIndex;
        }


        [System.Serializable]
        struct CopyPasteFlowEdge
        {
            public CopyPasteFlowAnchor input;
            public CopyPasteFlowAnchor output;
        }

        [System.Serializable]
        struct CopyPasteStruct
        {
            public VFXContext[] contexts;
            public VFXModel[] slotContainers;
            public CopyPasteDataEdge[] dataEdges;
            public CopyPasteFlowEdge[] flowEdges;
        }


        public readonly Vector2 defaultPasteOffset = new Vector2(100, 100);
        public Vector2 pasteOffset = Vector2.zero;

        string SerializeElements(IEnumerable<GraphElement> elements)
        {
            IEnumerable<VFXContextUI> contexts = elements.OfType<VFXContextUI>();
            IEnumerable<VFXStandaloneSlotContainerUI> slotContainers = elements.OfType<VFXStandaloneSlotContainerUI>();

            IEnumerable<VFXSlotContainerUI> dataEdgeTargets = slotContainers.Cast<VFXSlotContainerUI>().Concat(contexts.Select(t => t.ownData as VFXSlotContainerUI)).Concat(contexts.SelectMany(t => t.GetAllBlocks()).Cast<VFXSlotContainerUI>());

            // consider only edges contained in the selection


            IEnumerable<VFXDataEdge> dataEdges = elements.OfType<VFXDataEdge>().Where(t => dataEdgeTargets.Contains(t.input.GetFirstAncestorOfType<VFXSlotContainerUI>()) && dataEdgeTargets.Contains(t.output.GetFirstAncestorOfType<VFXSlotContainerUI>()));
            IEnumerable<VFXFlowEdge> flowEdges = elements.OfType<VFXFlowEdge>().Where(t => contexts.Contains(t.input.GetFirstAncestorOfType<VFXContextUI>()) && contexts.Contains(t.output.GetFirstAncestorOfType<VFXContextUI>()));
            CopyPasteStruct copyData = new CopyPasteStruct();


            VFXContext[] copiedContexts = contexts.Select(t => t.GetPresenter<VFXContextPresenter>().context).ToArray();
            copyData.contexts = copiedContexts.Select(t => t.Clone<VFXContext>()).ToArray();
            VFXModel[] copiedSlotContainers = slotContainers.Select(t => t.GetPresenter<VFXSlotContainerPresenter>().model).ToArray();
            copyData.slotContainers = copiedSlotContainers.Select(t => t.Clone<VFXModel>()).ToArray();


            copyData.dataEdges = new CopyPasteDataEdge[dataEdges.Count()];
            int cpt = 0;
            foreach (var edge in dataEdges)
            {
                CopyPasteDataEdge copyPasteEdge = new CopyPasteDataEdge();

                var edgePresenter = edge.GetPresenter<VFXDataEdgePresenter>();

                var inputPresenter = edgePresenter.input as VFXDataAnchorPresenter;
                var outputPresenter = edgePresenter.output as VFXDataAnchorPresenter;


                copyPasteEdge.input.slotPath = MakeSlotPath(inputPresenter.model, true);

                if (inputPresenter.model.owner is VFXContext)
                {
                    VFXContext context = inputPresenter.model.owner as VFXContext;
                    copyPasteEdge.inputContext = true;
                    copyPasteEdge.input.targetIndex = System.Array.IndexOf(copiedContexts, context);
                    copyPasteEdge.inputBlockIndex = -1;
                }
                else if (inputPresenter.model.owner is VFXBlock)
                {
                    VFXBlock block = inputPresenter.model.owner as VFXBlock;
                    copyPasteEdge.inputContext = true;
                    copyPasteEdge.input.targetIndex = System.Array.IndexOf(copiedContexts, block.GetParent());
                    copyPasteEdge.inputBlockIndex = block.GetParent().GetIndex(block);
                }
                else
                {
                    copyPasteEdge.inputContext = false;
                    copyPasteEdge.input.targetIndex = System.Array.IndexOf(copiedSlotContainers, inputPresenter.model.owner as VFXModel);
                    copyPasteEdge.inputBlockIndex = -1;
                }


                copyPasteEdge.output.slotPath = MakeSlotPath(outputPresenter.model, false);
                copyPasteEdge.output.targetIndex = System.Array.IndexOf(copiedSlotContainers, outputPresenter.model.owner as VFXModel);

                copyData.dataEdges[cpt++] = copyPasteEdge;
            }


            copyData.flowEdges = new CopyPasteFlowEdge[flowEdges.Count()];
            cpt = 0;
            foreach (var edge in flowEdges)
            {
                CopyPasteFlowEdge copyPasteEdge = new CopyPasteFlowEdge();

                var edgePresenter = edge.GetPresenter<VFXFlowEdgePresenter>();

                var inputPresenter = edgePresenter.input as VFXFlowAnchorPresenter;
                var outputPresenter = edgePresenter.output as VFXFlowAnchorPresenter;

                copyPasteEdge.input.contextIndex = System.Array.IndexOf(copiedContexts, inputPresenter.Owner);
                copyPasteEdge.input.flowIndex = inputPresenter.slotIndex;
                copyPasteEdge.output.contextIndex = System.Array.IndexOf(copiedContexts, outputPresenter.Owner);
                copyPasteEdge.output.flowIndex = outputPresenter.slotIndex;

                copyData.flowEdges[cpt++] = copyPasteEdge;
            }


            pasteOffset = defaultPasteOffset;
            return JsonUtility.ToJson(copyData);
        }

        int[] MakeSlotPath(VFXSlot slot, bool input)
        {
            List<int> slotPath = new List<int>(slot.depth + 1);
            while (slot.GetParent() != null)
            {
                slotPath.Add(slot.GetParent().GetIndex(slot));
                slot = slot.GetParent();
            }
            slotPath.Add((input ? (slot.owner as IVFXSlotContainer).inputSlots : (slot.owner as IVFXSlotContainer).outputSlots).IndexOf(slot));

            return slotPath.ToArray();
        }

        VFXSlot FetchSlot(IVFXSlotContainer container, int[] slotPath, bool input)
        {
            int containerSlotIndex = slotPath[slotPath.Length - 1];

            VFXSlot slot = null;
            if (input)
            {
                if (container.GetNbInputSlots() > containerSlotIndex)
                {
                    slot = container.GetInputSlot(slotPath[slotPath.Length - 1]);
                }
            }
            else
            {
                if (container.GetNbOutputSlots() > containerSlotIndex)
                {
                    slot = container.GetOutputSlot(slotPath[slotPath.Length - 1]);
                }
            }
            if (slot == null)
            {
                return null;
            }

            for (int i = slotPath.Length - 2; i >= 0; --i)
            {
                if (slot.GetNbChildren() > slotPath[i])
                {
                    slot = slot[slotPath[i]];
                }
                else
                {
                    return null;
                }
            }

            return slot;
        }

        void UnserializeAndPasteElements(string operationName, string data)
        {
            CopyPasteStruct copyData = (CopyPasteStruct)JsonUtility.FromJson<CopyPasteStruct>(data);

            var graph = GetPresenter<VFXViewPresenter>().GetGraph();

            List<VFXContext> newContexts = new List<VFXContext>(copyData.contexts.Length);

            foreach (var slotContainer in copyData.contexts)
            {
                var newContext = slotContainer.Clone<VFXContext>();
                newContext.position += pasteOffset;
                newContexts.Add(newContext);
                graph.AddChild(newContext);
            }

            List<VFXModel> newSlotContainers = new List<VFXModel>(copyData.slotContainers.Length);

            foreach (var slotContainer in copyData.slotContainers)
            {
                var newSlotContainer = slotContainer.Clone<VFXModel>();
                newSlotContainer.position += pasteOffset;
                newSlotContainers.Add(newSlotContainer);
                graph.AddChild(newSlotContainer);
            }

            foreach (var dataEdge in copyData.dataEdges)
            {
                VFXSlot inputSlot = null;
                if (dataEdge.inputContext)
                {
                    VFXContext targetContext = newContexts[dataEdge.input.targetIndex];
                    if (dataEdge.inputBlockIndex == -1)
                    {
                        inputSlot = FetchSlot(targetContext, dataEdge.input.slotPath, true);
                    }
                    else
                    {
                        inputSlot = FetchSlot(targetContext[dataEdge.inputBlockIndex], dataEdge.input.slotPath, true);
                    }
                }
                else
                {
                    VFXModel model = newSlotContainers[dataEdge.input.targetIndex];
                    inputSlot = FetchSlot(model as IVFXSlotContainer, dataEdge.input.slotPath, true);
                }

                VFXSlot outputSlot = FetchSlot(newSlotContainers[dataEdge.output.targetIndex] as IVFXSlotContainer, dataEdge.output.slotPath, false);

                if (inputSlot != null && outputSlot != null)
                    inputSlot.Link(outputSlot);
            }


            foreach (var flowEdge in copyData.flowEdges)
            {
                VFXContext inputContext = newContexts[flowEdge.input.contextIndex];
                VFXContext outputContext = newContexts[flowEdge.output.contextIndex];

                inputContext.LinkFrom(outputContext, flowEdge.input.flowIndex, flowEdge.output.flowIndex);
            }

            // Create all ui based on model
            OnDataChanged();

            ClearSelection();

            var elements = graphElements.ToList();


            List<VFXSlotContainerUI> newSlotContainerUIs = new List<VFXSlotContainerUI>();
            List<VFXContextUI> newContainerUIs = new List<VFXContextUI>();

            foreach (var slotContainer in newContexts)
            {
                VFXContextUI contextUI = elements.OfType<VFXContextUI>().FirstOrDefault(t => t.GetPresenter<VFXContextPresenter>().model == slotContainer);
                if (contextUI != null)
                {
                    newSlotContainerUIs.Add(contextUI.ownData);
                    newSlotContainerUIs.AddRange(contextUI.GetAllBlocks().Cast<VFXSlotContainerUI>());
                    newContainerUIs.Add(contextUI);
                    AddToSelection(contextUI);
                }
            }
            foreach (var slotContainer in newSlotContainers)
            {
                VFXStandaloneSlotContainerUI slotContainerUI = elements.OfType<VFXStandaloneSlotContainerUI>().FirstOrDefault(t => t.GetPresenter<VFXSlotContainerPresenter>().model == slotContainer);
                if (slotContainerUI != null)
                {
                    newSlotContainerUIs.Add(slotContainerUI);
                    AddToSelection(slotContainerUI);
                }
            }

            // Simply selected all data edge with the context or slot container, they can be no other than the copied ones
            foreach (var dataEdge in elements.OfType<VFXDataEdge>())
            {
                if (newSlotContainerUIs.Contains(dataEdge.input.GetFirstAncestorOfType<VFXSlotContainerUI>()))
                {
                    AddToSelection(dataEdge);
                }
            }
            // Simply selected all data edge with the context or slot container, they can be no other than the copied ones
            foreach (var flowEdge in elements.OfType<VFXFlowEdge>())
            {
                if (newContainerUIs.Contains(flowEdge.input.GetFirstAncestorOfType<VFXContextUI>()))
                {
                    AddToSelection(flowEdge);
                }
            }


            pasteOffset += defaultPasteOffset;
        }
    }
}
