using System;
using System.Linq;
using System.Collections.Generic;
using UIElements.GraphView;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.VFX.UI
{
    interface IParameterDropTarget
    {
        void OnDragUpdated(IMGUIEvent evt, VFXParameterPresenter parameter);
        void OnDragPerform(IMGUIEvent evt, VFXParameterPresenter parameter);
    }

    class ParameterDropper : Manipulator
    {
        public ParameterDropper()
        {
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<IMGUIEvent>(OnIMGUIEvent);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<IMGUIEvent>(OnIMGUIEvent);
        }

        void OnIMGUIEvent(IMGUIEvent e)
        {
            Event evt = e.imguiEvent;
            if (evt.type != EventType.DragUpdated && evt.type != EventType.DragPerform)
                return;
            var pickElem = target.panel.Pick(target.LocalToGlobal(evt.mousePosition));
            IParameterDropTarget dropTarget = pickElem != null ? pickElem.GetFirstOfType<IParameterDropTarget>() : null;

            if (dropTarget == null)
                return;

            VFXParameterPresenter dragData = DragAndDrop.GetGenericData(VFXAssetEditor.VFXParameterDragging) as VFXParameterPresenter;


            switch (evt.type)
            {
                case EventType.DragUpdated:
                {
                    dropTarget.OnDragUpdated(e, dragData);
                }
                break;
                case EventType.DragPerform:
                {
                    dropTarget.OnDragPerform(e, dragData);
                }
                break;
            }
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
            var descriptorsContext = VFXLibrary.GetContexts().Select(o =>
                {
                    return new Descriptor()
                    {
                        modelDescriptor = o,
                        category = "Context/" + o.info.category,
                        name = o.name
                    };
                }).OrderBy(o => o.name);

            var descriptorsOperator = VFXLibrary.GetOperators().Select(o =>
                {
                    return new Descriptor()
                    {
                        modelDescriptor = o,
                        category = "Operator/" + o.info.category,
                        name = o.name
                    };
                }).OrderBy(o => o.name);

            var descriptorParameter = VFXLibrary.GetParameters().Select(o =>
                {
                    return new Descriptor()
                    {
                        modelDescriptor = o,
                        category = "Parameter/",
                        name = o.name
                    };
                }).OrderBy(o => o.name);

            var descriptorBuiltInParameter = VFXLibrary.GetBuiltInParameters().Select(o =>
                {
                    return new Descriptor()
                    {
                        modelDescriptor = o,
                        category = "BuiltIn/",
                        name = o.name
                    };
                }).OrderBy(o => o.name);

            var descriptorAttributeParameter = VFXLibrary.GetAttributeParameters().Select(o =>
                {
                    return new Descriptor()
                    {
                        modelDescriptor = o,
                        category = "Attribute/",
                        name = o.name
                    };
                }).OrderBy(o => o.name);

            return descriptorsContext.Concat(descriptorsOperator)
                .Concat(descriptorParameter)
                .Concat(descriptorBuiltInParameter)
                .Concat(descriptorAttributeParameter);
        }
    }

    //[StyleSheet("Assets/VFXEditor/Editor/GraphView/Views/")]
    class VFXView : GraphView, IParameterDropTarget
    {
        public VFXView()
        {
            forceNotififcationOnAdd = true;
            SetupZoom(new Vector3(0.125f, 0.125f, 1), new Vector3(8, 8, 1));

            AddManipulator(new ContentDragger());
            AddManipulator(new RectangleSelector());
            AddManipulator(new SelectionDragger());
            AddManipulator(new ClickSelector());
            AddManipulator(new ShortcutHandler(
                    new Dictionary<Event, ShortcutDelegate>
            {
                {Event.KeyboardEvent("a"), FrameAll},
                {Event.KeyboardEvent("f"), FrameSelection},
                {Event.KeyboardEvent("o"), FrameOrigin},
                {Event.KeyboardEvent("delete"), DeleteSelection},
//                  {Event.KeyboardEvent("#tab"), FramePrev},
//                  {Event.KeyboardEvent("tab"), FrameNext},
                {Event.KeyboardEvent("c"), CloneModels},     // TEST
                {Event.KeyboardEvent("#r"), Resync},
                {Event.KeyboardEvent("#d"), OutputToDot},
                {Event.KeyboardEvent("^#d"), OutputToDotReduced},
                {Event.KeyboardEvent("#c"), OutputToDotConstantFolding},
            }));

            AddManipulator(new ParameterDropper());

            var bg = new GridBackground() { name = "VFXBackgroundGrid" };
            InsertChild(0, bg);

            AddManipulator(new FilterPopup(new VFXNodeProvider((d, mPos) =>
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
                    else if (d.modelDescriptor is VFXModelDescriptorAttributeParameters)
                    {
                        AddVFXAttributeParameter(tPos, d.modelDescriptor as VFXModelDescriptorAttributeParameters);
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

            AddStyleSheetPath("PropertyRM");
            AddStyleSheetPath("VFXView");

            Dirty(ChangeType.Transform);
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

        void AddVFXAttributeParameter(Vector2 pos, VFXModelDescriptorAttributeParameters desc)
        {
            GetPresenter<VFXViewPresenter>().AddVFXAttributeParameter(pos, desc);
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
    }
}
