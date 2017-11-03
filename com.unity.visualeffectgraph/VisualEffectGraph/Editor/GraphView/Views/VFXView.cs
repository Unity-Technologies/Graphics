using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.VFX;

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
                        var spawner = GetPresenter<VFXViewPresenter>().AddVFXContext(tPos, VFXLibrary.GetContexts().FirstOrDefault(t => t.name == "Spawner"));
                        var initialize = GetPresenter<VFXViewPresenter>().AddVFXContext(tPos + new Vector2(0, 200), VFXLibrary.GetContexts().FirstOrDefault(t => t.name == "Initialize"));
                        var update = GetPresenter<VFXViewPresenter>().AddVFXContext(tPos + new Vector2(0, 400), VFXLibrary.GetContexts().FirstOrDefault(t => t.name == "Update"));
                        var output = GetPresenter<VFXViewPresenter>().AddVFXContext(tPos + new Vector2(0, 600), VFXLibrary.GetContexts().FirstOrDefault(t => t.name == "Point Output"));

                        spawner.LinkTo(initialize);
                        initialize.LinkTo(update);
                        update.LinkTo(output);
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
            AddStyleSheetPath("VFXView");

            Dirty(ChangeType.Transform);

            AddLayer(-1);
            AddLayer(1);
            AddLayer(2);

            focusIndex = 0;

            VisualElement toolbar = new VisualElement();
            toolbar.AddToClassList("toolbar");
            Add(toolbar);


            Button button = new Button(() => {Resync(); });
            button.text = "Refresh";
            button.AddToClassList("toolbarItem");
            toolbar.Add(button);


            VisualElement spacer = new VisualElement();
            spacer.style.flex = 1;
            toolbar.Add(spacer);

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
                    VFXViewPresenter.viewPresenter.GetGraph().SetExpressionGraphDirty();
                }
            }
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
            var graph = VFXViewPresenter.viewPresenter.GetGraph();
            graph.SetExpressionGraphDirty();
            graph.RecompileIfNeeded();
        }

        public override void OnDataChanged()
        {
            base.OnDataChanged();
            if (presenter != null)
            {
                m_ToggleMotionVectors.on = GetRendererSettings().motionVectorGenerationMode == MotionVectorGenerationMode.Object;
                m_ToggleMotionVectors.SetEnabled(true);
            }
            else
                m_ToggleMotionVectors.SetEnabled(false);
        }

        void AddVFXContext(Vector2 pos, VFXModelDescriptor<VFXContext> desc)
        {
            GetPresenter<VFXViewPresenter>().AddVFXContext(pos, desc);
        }

        void AddVFXOperator(Vector2 pos, VFXModelDescriptor<VFXOperator> desc)
        {
            GetPresenter<VFXViewPresenter>().AddVFXOperator(pos, desc);
        }

        void AddVFXParameter(Vector2 pos, VFXModelDescriptorParameters desc)
        {
            GetPresenter<VFXViewPresenter>().AddVFXParameter(pos, desc);
        }

        void AddVFXBuiltInParameter(Vector2 pos, VFXModelDescriptorBuiltInParameters desc)
        {
            GetPresenter<VFXViewPresenter>().AddVFXBuiltInParameter(pos, desc);
        }

        void AddVFXCurrentAttributeParameter(Vector2 pos, VFXModelDescriptorCurrentAttributeParameters desc)
        {
            GetPresenter<VFXViewPresenter>().AddVFXCurrentAttributeParameter(pos, desc);
        }

        void AddVFXSourceAttributeParameter(Vector2 pos, VFXModelDescriptorSourceAttributeParameters desc)
        {
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
            presenter.SetVFXAsset(presenter.GetVFXAsset(), true);
            return EventPropagation.Stop;
        }

        public EventPropagation OutputToDot()
        {
            DotGraphOutput.DebugExpressionGraph(GetPresenter<VFXViewPresenter>().GetGraph(), VFXExpressionContextOption.None);
            return EventPropagation.Stop;
        }

        public EventPropagation OutputToDotReduced()
        {
            DotGraphOutput.DebugExpressionGraph(GetPresenter<VFXViewPresenter>().GetGraph(), VFXExpressionContextOption.Reduction);
            return EventPropagation.Stop;
        }

        public EventPropagation OutputToDotConstantFolding()
        {
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

        public override List<NodeAnchor> GetCompatibleAnchors(NodeAnchor startAnchor, NodeAdapter nodeAdapter)
        {
            VFXViewPresenter presenter = GetPresenter<VFXViewPresenter>();

            var presenters = presenter.GetCompatibleAnchors(startAnchor.GetPresenter<NodeAnchorPresenter>(), nodeAdapter);

            if (startAnchor is VFXDataAnchor)
            {
                return presenters.Select(t => (NodeAnchor)GetDataAnchorByPresenter(t as VFXDataAnchorPresenter)).ToList();
            }
            else
            {
                return presenters.Select(t => (NodeAnchor)GetFlowAnchorByPresenter(t as VFXFlowAnchorPresenter)).ToList();
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


                        foreach (VFXDataAnchor anchor in context.ownData.GetAnchors(input, output))
                            yield return anchor;

                        foreach (VFXBlockUI block in context.GetAllBlocks())
                        {
                            foreach (VFXDataAnchor anchor in block.GetAnchors(input, output))
                                yield return anchor;
                        }
                    }
                    else if (element is VFXNodeUI)
                    {
                        var ope = element as VFXNodeUI;
                        foreach (VFXDataAnchor anchor in ope.GetAnchors(input, output))
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

        public override IEnumerable<NodeAnchor> GetAllAnchors(bool input, bool output)
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

            VFXParameter newParameter = presenter.AddVFXParameter(contentViewContainer.GlobalToBound(evt.imguiEvent.mousePosition), VFXLibrary.GetParameters().FirstOrDefault(t => t.name == parameter.anchorType.UserFriendlyName()));

            newParameter.exposedName = parameter.exposedName;
            newParameter.exposed = true;
        }

        void SelectionUpdated()
        {
            if (!VFXComponentEditor.s_IsEditingAsset)
            {
                var contextSelected = selection.OfType<VFXContextUI>();

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

        private Toggle m_ToggleMotionVectors;
    }
}
