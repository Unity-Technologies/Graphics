using System;
using System.Linq;
using System.Collections.Generic;
using UIElements.GraphView;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.VFX.UI
{
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
            });

            var descriptorsOperator = VFXLibrary.GetOperators().Select(o =>
            {
                return new Descriptor()
                {
                    modelDescriptor = o,
                    category = "Operator/" + o.info.category,
                    name = o.name
                };
            });

            var descriptorParameter = VFXLibrary.GetParameters().Select(o =>
            {
                return new Descriptor()
                {
                    modelDescriptor = o,
                    category = "Parameter/",
                    name = o.name
                };
            });

            return descriptorsContext   .Concat(descriptorsOperator)
                                        .Concat(descriptorParameter);
        }
    }

    //[StyleSheet("Assets/VFXEditorNew/Editor/GraphView/Views/")]
    class VFXView : GraphView
    {
        public VFXView()
        {
            forceNotififcationOnAdd = true;
            SetupZoom(new Vector3(0.125f,0.125f,1),Vector3.one);

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
					{Event.KeyboardEvent("#tab"), FramePrev},
					{Event.KeyboardEvent("tab"), FrameNext},
                    {Event.KeyboardEvent("c"), CloneModels}, // TEST
                    {Event.KeyboardEvent("#r"), Resync},
                    {Event.KeyboardEvent("#d"), OutputToDot},
				}));

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
                else
                {
                    Debug.LogError("Add unknown presenter");
                }
            }), null));

            typeFactory[typeof(VFXParameterPresenter)] = typeof(VFXParameterUI);
            typeFactory[typeof(VFXOperatorPresenter)] = typeof(VFXOperatorUI);
            typeFactory[typeof(VFXContextPresenter)] = typeof(VFXContextUI);
            typeFactory[typeof(VFXFlowEdgePresenter)] = typeof(VFXFlowEdge);
            typeFactory[typeof(VFXDataEdgePresenter)] = typeof(VFXDataEdge);
            typeFactory[typeof(VFXFlowInputAnchorPresenter)] = typeof(VFXFlowAnchor);
            typeFactory[typeof(VFXFlowOutputAnchorPresenter)] = typeof(VFXFlowAnchor);
            typeFactory[typeof(VFXBlockDataInputAnchorPresenter)] = typeof(VFXDataAnchor);
            typeFactory[typeof(VFXBlockDataOutputAnchorPresenter)] = typeof(VFXDataAnchor);
            typeFactory[typeof(VFXInputOperatorAnchorPresenter)] = typeof(VFXDataAnchor);
            typeFactory[typeof(VFXOutputOperatorAnchorPresenter)] = typeof(VFXDataAnchor);

            AddStyleSheetPath("PropertyRM");
            AddStyleSheetPath("VFXView");

            Dirty(ChangeType.Transform);
        }

        void AddVFXContext(Vector2 pos,VFXModelDescriptor<VFXContext> desc)
        {
            GetPresenter<VFXViewPresenter>().AddVFXContext(pos,desc);
        }

        void AddVFXOperator(Vector2 pos, VFXModelDescriptor<VFXOperator> desc)
        {
            GetPresenter<VFXViewPresenter>().AddVFXOperator(pos, desc);
        }

        void AddVFXParameter(Vector2 pos, VFXModelDescriptorParameters desc)
        {
            GetPresenter<VFXViewPresenter>().AddVFXParameter(pos, desc);
        }

        public EventPropagation CloneModels() // TEST clean that
        {
            var contexts = selection.OfType<VFXContextUI>().Select(p => p.GetPresenter<VFXContextPresenter>().context.Clone<VFXContext>());
            foreach (var context in contexts)
            {
                var system = ScriptableObject.CreateInstance<VFXSystem>();
                system.AddChild(context);
                context.position = context.position + new Vector2(50, 50);
                GetPresenter<VFXViewPresenter>().GetGraphAsset().root.AddChild(system);
            }

            var operators = selection.OfType<Node>().Select(p => p.GetPresenter<VFXNodePresenter>().node.Clone<VFXSlotContainerModel<VFXModel, VFXModel>>());
            foreach (var op in operators)
            {
                op.position = op.position + new Vector2(50, 50);
                GetPresenter<VFXViewPresenter>().GetGraphAsset().root.AddChild(op);
            }
            return EventPropagation.Stop;
        }

        public EventPropagation Resync()
        {
            var presenter = GetPresenter<VFXViewPresenter>();
            presenter.SetGraphAsset(presenter.GetGraphAsset(), true);
            return EventPropagation.Stop;
        }

        public EventPropagation OutputToDot()
        {
            DotGraphOutput.DebugExpressionGraph(GetPresenter<VFXViewPresenter>().GetGraphAsset().root);
            return EventPropagation.Stop;
        }

        public IEnumerable<VFXFlowAnchor> GetAllFlowAnchors(bool input, bool output)
        {
            foreach (var layer in GetAllLayers())
            {
                foreach (var element in layer)
                {
                    if (element is VFXContextUI)
                    {
                        var context = element as VFXContextUI;

                        foreach (VFXFlowAnchor anchor in context.GetFlowAnchors(input, output))
                        {
                            yield return anchor;
                        }
                    }
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

                        foreach (VFXBlockUI block in context.GetAllBlocks())
                        {
                            foreach(VFXDataAnchor anchor in block.GetAnchors(input,output))
                                yield return anchor;
                        }
                    }
                    else if( element is VFXNodeUI)
                    {
                        var ope = element as VFXNodeUI;
                        foreach (VFXDataAnchor anchor in ope.GetAnchors(input, output))
                            yield return anchor;
                    }
                }
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
                    if( element is VFXContextUI)
                    {
                        var context = element as VFXContextUI;

                        foreach( var block in context.GetAllBlocks())
                        {
                            yield return block;
                        }
                    }
                }
            }
        }
    }
}
