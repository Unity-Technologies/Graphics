using System;
using System.Collections.Generic;
using System.Linq;
using UIElements.GraphView;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

using Object = UnityEngine.Object;
namespace UnityEditor.VFX.UI
{
    [Serializable]
    partial class VFXViewPresenter : GraphViewPresenter
    {
        [SerializeField]
        public List<VFXFlowAnchorPresenter> m_FlowAnchorPresenters;

        [SerializeField]
        public Dictionary<Type, List<NodeAnchorPresenter>> m_DataInputAnchorPresenters = new Dictionary<Type, List<NodeAnchorPresenter>>();

        [SerializeField]
        public Dictionary<Type, List<NodeAnchorPresenter>> m_DataOutputAnchorPresenters = new Dictionary<Type, List<NodeAnchorPresenter>>();

        // Model / Presenters synchronization
        private Dictionary<VFXModel, VFXSlotContainerPresenter> m_SyncedModels = new Dictionary<VFXModel, VFXSlotContainerPresenter>();

        private class PresenterFactory : BaseTypeFactory<VFXModel, GraphElementPresenter>
        {
            protected override GraphElementPresenter InternalCreate(Type valueType)
            {
                return (GraphElementPresenter)ScriptableObject.CreateInstance(valueType);
            }
        }
        private PresenterFactory m_PresenterFactory = new PresenterFactory();

        public VFXViewPresenter()
        {
            m_PresenterFactory[typeof(VFXContext)] = typeof(VFXContextPresenter);
            m_PresenterFactory[typeof(VFXOperator)] = typeof(VFXOperatorPresenter);
            m_PresenterFactory[typeof(VFXBuiltInParameter)] = typeof(VFXBuiltInParameterPresenter);
            m_PresenterFactory[typeof(VFXAttributeParameter)] = typeof(VFXAttributeParameterPresenter);
            m_PresenterFactory[typeof(VFXParameter)] = typeof(VFXParameterPresenter);
        }

        protected new void OnEnable()
        {
            base.OnEnable();

            if (m_FlowAnchorPresenters == null)
                m_FlowAnchorPresenters = new List<VFXFlowAnchorPresenter>();

            if (m_DataOutputAnchorPresenters == null)
                m_DataOutputAnchorPresenters = new Dictionary<Type, List<NodeAnchorPresenter>>();

            if (m_DataInputAnchorPresenters == null)
                m_DataInputAnchorPresenters = new Dictionary<Type, List<NodeAnchorPresenter>>();

            SetVFXAsset(m_VFXAsset != null ? m_VFXAsset : new VFXAsset(), true);
        }

        public VFXView View
        {
            get
            {
                // TODO Is that good design?
                if (m_View == null)
                    m_View = new VFXView();
                return m_View;
            }
        }

        public void RecreateNodeEdges()
        {
            HashSet<VFXDataEdgePresenter> unusedEdges = new HashSet<VFXDataEdgePresenter>();
            foreach (var e in m_Elements.OfType<VFXDataEdgePresenter>())
            {
                unusedEdges.Add(e);
            }

            var operatorPresenters = m_Elements.OfType<VFXSlotContainerPresenter>();
            var blockPresenters = (m_Elements.OfType<VFXContextPresenter>().SelectMany(t => t.allChildren.OfType<VFXBlockPresenter>())).Cast<VFXSlotContainerPresenter>();

            var allLinkables = operatorPresenters.Concat(blockPresenters).ToArray();
            foreach (var operatorPresenter in allLinkables)
            {
                var slotContainer = operatorPresenter.slotContainer;
                foreach (var input in slotContainer.inputSlots)
                {
                    RecreateInputSlotEdge(unusedEdges, allLinkables, slotContainer, input);
                }
            }

            foreach (var edge in unusedEdges)
            {
                edge.input = null;
                edge.output = null;
                m_Elements.Remove(edge);
            }
        }

        public void RecreateInputSlotEdge(HashSet<VFXDataEdgePresenter> unusedEdges, VFXSlotContainerPresenter[] allLinkables, IVFXSlotContainer slotContainer, VFXSlot input)
        {
            if (input.HasLink())
            {
                var operatorPresenterFrom = allLinkables.FirstOrDefault(t => input.refSlot.owner == t.slotContainer);
                var operatorPresenterTo = allLinkables.FirstOrDefault(t => slotContainer == t.slotContainer);

                if (operatorPresenterFrom != null && operatorPresenterTo != null)
                {
                    var anchorFrom = operatorPresenterFrom.outputAnchors.FirstOrDefault(o => (o as VFXDataAnchorPresenter).model == input.refSlot);
                    var anchorTo = operatorPresenterTo.inputAnchors.FirstOrDefault(o => (o as VFXDataAnchorPresenter).model == input);

                    var edgePresenter = m_Elements.OfType<VFXDataEdgePresenter>().FirstOrDefault(t => t.input == anchorTo && t.output == anchorFrom);

                    if (edgePresenter != null)
                    {
                        unusedEdges.Remove(edgePresenter);
                    }
                    else
                    {
                        edgePresenter = CreateInstance<VFXDataEdgePresenter>();

                        if (anchorFrom != null && anchorTo != null)
                        {
                            edgePresenter.output = anchorFrom;
                            edgePresenter.input = anchorTo;
                            base.AddElement(edgePresenter);
                        }
                    }
                }
            }

            foreach (VFXSlot subSlot in input.children)
            {
                RecreateInputSlotEdge(unusedEdges, allLinkables, slotContainer, subSlot);
            }
        }

        public void RecreateFlowEdges()
        {
            HashSet<VFXFlowEdgePresenter> unusedEdges = new HashSet<VFXFlowEdgePresenter>();
            foreach (var e in m_Elements.OfType<VFXFlowEdgePresenter>())
            {
                unusedEdges.Add(e);
            }

            var contextPresenters = m_Elements.OfType<VFXContextPresenter>();
            foreach (var outPresenter in contextPresenters.ToArray())
            {
                var output = outPresenter.context;
                foreach (var input in output.inputContexts)
                {
                    var inPresenter = elements.OfType<VFXContextPresenter>().FirstOrDefault(x => x.model == input);
                    if (inPresenter == null)
                        break;

                    var outputAnchor = inPresenter.flowOutputAnchors.First();
                    var inputAnchor = outPresenter.flowInputAnchors.First();

                    var edgePresenter = m_Elements.OfType<VFXFlowEdgePresenter>().FirstOrDefault(t => t.input == inputAnchor && t.output == outputAnchor);
                    if (edgePresenter != null)
                        unusedEdges.Remove(edgePresenter);
                    else
                    {
                        edgePresenter = ScriptableObject.CreateInstance<VFXFlowEdgePresenter>();
                        edgePresenter.output = inPresenter.flowOutputAnchors.First();
                        edgePresenter.input = outPresenter.flowInputAnchors.First();
                        base.AddElement(edgePresenter);
                    }
                }

                foreach (var edge in unusedEdges)
                {
                    edge.input = null;
                    edge.output = null;
                    m_Elements.Remove(edge);
                }
            }

            foreach (var edge in unusedEdges)
            {
                edge.input = null;
                edge.output = null;
                m_Elements.Remove(edge);
            }
        }

        private enum RecordEvent
        {
            Add,
            Remove
        }

        private void RecordEdgePresenter(VFXDataEdgePresenter dataEdge, RecordEvent e)
        {
            var fromAnchor = dataEdge.output as VFXDataAnchorPresenter;
            var toAnchor = dataEdge.input as VFXDataAnchorPresenter;
            if (fromAnchor == null ||  toAnchor == null) return;  // no need to record invalid edge
            var from = fromAnchor.sourceNode.slotContainer as IVFXSlotContainer;
            var to = toAnchor.sourceNode.slotContainer as IVFXSlotContainer;
            var children = new HashSet<IVFXSlotContainer>();
            CollectChildOperator(from, children);
            CollectChildOperator(to, children);

            var allOperator = children.OfType<Object>().ToArray();
            var allSlot = children.SelectMany(c => c.outputSlots.Concat(c.inputSlots)).OfType<Object>().ToArray();
            Undo.RecordObjects(allOperator.Concat(allSlot).ToArray(), string.Format("{0} Edge", e == RecordEvent.Add ? "Add Edge" : "Remove Edge"));
        }

        private void RecordFlowEdgePresenter(VFXFlowEdgePresenter flowEdge, RecordEvent e)
        {
            var context0 = ((VFXFlowAnchorPresenter)flowEdge.output).Owner;
            var context1 = ((VFXFlowAnchorPresenter)flowEdge.input).Owner;
            var objects = new Object[] { context0, context1, context0.GetParent(), context1.GetParent() }.Where(o => o != null).ToArray();
            var eventName = string.Format("{0} FlowEdge", e == RecordEvent.Add ? "Add" : "Remove");
            Undo.RecordObjects(objects, eventName);

            Undo.RegisterFullObjectHierarchyUndo(m_Graph, eventName); //Hotfix : VFXContext issue, refactor in progress
        }

        private void RecordAll(string modelName, RecordEvent e)
        {
            Undo.RegisterFullObjectHierarchyUndo(m_Graph, string.Format("{0} {1}", e == RecordEvent.Add ? "Add" : "Remove", modelName)); //Full hierarchy for VFXContext, refactor in progress (hotfix)
        }

        public override void AddElement(EdgePresenter edge)
        {
            if (edge is VFXFlowEdgePresenter)
            {
                var flowEdge = (VFXFlowEdgePresenter)edge;
                RecordFlowEdgePresenter(flowEdge, RecordEvent.Add);

                var context0 = ((VFXFlowAnchorPresenter)flowEdge.output).Owner;
                var context1 = ((VFXFlowAnchorPresenter)flowEdge.input).Owner;

                context0.LinkTo(context1);

                // disconnect this edge as it will not be added by add element
                edge.input = null;
                edge.output = null;
            }
            else if (edge is VFXDataEdgePresenter)
            {
                var flowEdge = edge as VFXDataEdgePresenter;
                RecordEdgePresenter(flowEdge, RecordEvent.Add);
                var fromAnchor = flowEdge.output as VFXDataAnchorPresenter;
                var toAnchor = flowEdge.input as VFXDataAnchorPresenter;

                //Update connection
                var slotInput = toAnchor ? toAnchor.model : null;
                var slotOuput = fromAnchor ? fromAnchor.model : null;
                if (slotInput && slotOuput)
                {
                    //Save concerned object
                    slotInput.Link(slotOuput);
                    RecreateNodeEdges();
                }

                // disconnect this edge as it will not be added by add element
                edge.input = null;
                edge.output = null;
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public override void RemoveElement(GraphElementPresenter element)
        {
            base.RemoveElement(element);

            if (element is VFXContextPresenter)
            {
                VFXContext context = ((VFXContextPresenter)element).context;
                RecordAll(context.name, RecordEvent.Remove);

                // Remove connections from context
                foreach (var slot in context.inputSlots)
                    slot.UnlinkAll();
                foreach (var slot in context.outputSlots)
                    slot.UnlinkAll();

                // Remove connections from blocks
                foreach (VFXBlockPresenter blockPres in (element as VFXContextPresenter).blockPresenters)
                {
                    foreach (var slot in blockPres.slotContainer.outputSlots)
                    {
                        slot.UnlinkAll();
                    }
                    foreach (var slot in blockPres.slotContainer.inputSlots)
                    {
                        slot.UnlinkAll();
                    }
                }

                // remove flow connections from context
                // TODO update data types
                context.UnlinkAll();
                // Detach from graph
                context.Detach();
            }
            else if (element is VFXSlotContainerPresenter)
            {
                var operatorPresenter = element as VFXSlotContainerPresenter;
                RecordAll(operatorPresenter.model.name, RecordEvent.Remove);
                VFXSlot slotToClean = null;
                do
                {
                    slotToClean = operatorPresenter.slotContainer.inputSlots.Concat(operatorPresenter.slotContainer.outputSlots)
                        .FirstOrDefault(o => o.HasLink());
                    if (slotToClean)
                    {
                        slotToClean.UnlinkAll();
                    }
                }
                while (slotToClean != null);

                m_Graph.RemoveChild(operatorPresenter.model);
                Undo.DestroyObjectImmediate(operatorPresenter.model);
                RecreateNodeEdges();
            }
            else if (element is VFXFlowEdgePresenter)
            {
                var flowEdge = element as VFXFlowEdgePresenter;
                RecordFlowEdgePresenter(flowEdge, RecordEvent.Add);

                var context0 = ((VFXFlowAnchorPresenter)(flowEdge.input)).Owner as VFXContext;
                var context1 = ((VFXFlowAnchorPresenter)(flowEdge.output)).Owner as VFXContext;

                context0.Unlink(context1);
            }
            else if (element is VFXDataEdgePresenter)
            {
                var edge = element as VFXDataEdgePresenter;
                var to = edge.input as VFXDataAnchorPresenter;

                RecordEdgePresenter(edge, RecordEvent.Remove);
                if (to != null)
                {
                    var slot = to.model;
                    if (slot != null)
                    {
                        slot.UnlinkAll();
                    }
                }
            }
            else if (element is VFXBlockPresenter)
            {
                var block = element as VFXBlockPresenter;

                block.contextPresenter.RemoveBlock(block.block);
            }
            else
            {
                Debug.LogErrorFormat("Unexpected type : {0}", element.GetType().FullName);
            }
        }

        public void RegisterFlowAnchorPresenter(VFXFlowAnchorPresenter presenter)
        {
            if (!m_FlowAnchorPresenters.Contains(presenter))
                m_FlowAnchorPresenters.Add(presenter);
        }

        public void UnregisterFlowAnchorPresenter(VFXFlowAnchorPresenter presenter)
        {
            m_FlowAnchorPresenters.Remove(presenter);
        }

        public void RegisterDataAnchorPresenter(VFXDataAnchorPresenter presenter)
        {
            List<NodeAnchorPresenter> list;

            Dictionary<Type, List<NodeAnchorPresenter>> dict = presenter.direction == Direction.Input ? m_DataInputAnchorPresenters : m_DataOutputAnchorPresenters;

            if (!dict.TryGetValue(presenter.anchorType, out list))
            {
                list = new List<NodeAnchorPresenter>();
                dict[presenter.anchorType] = list;
            }
            if (!list.Contains(presenter))
                list.Add(presenter);
        }

        public void UnregisterDataAnchorPresenter(VFXDataAnchorPresenter presenter)
        {
            Dictionary<Type, List<NodeAnchorPresenter>> dict = presenter.direction == Direction.Input ? m_DataInputAnchorPresenters : m_DataOutputAnchorPresenters;

            dict[presenter.anchorType].Remove(presenter);
        }

        private static void CollectParentOperator(IVFXSlotContainer operatorInput, HashSet<IVFXSlotContainer> listParent)
        {
            listParent.Add(operatorInput);
            foreach (var input in operatorInput.inputSlots)
            {
                if (input.HasLink())
                {
                    CollectParentOperator(input.refSlot.owner as IVFXSlotContainer, listParent);
                }
            }
        }

        private static void CollectChildOperator(IVFXSlotContainer operatorInput, HashSet<IVFXSlotContainer> hashChildren)
        {
            hashChildren.Add(operatorInput);

            var all = operatorInput.outputSlots.SelectMany(s => s.LinkedSlots.Select(
                        o => o.owner
                        ));
            var children = all.Cast<IVFXSlotContainer>();
            foreach (var child in children)
            {
                CollectChildOperator(child, hashChildren);
            }
        }

        public override List<NodeAnchorPresenter> GetCompatibleAnchors(NodeAnchorPresenter startAnchorPresenter, NodeAdapter nodeAdapter)
        {
            if (startAnchorPresenter is VFXDataAnchorPresenter)
            {
                var allOperatorPresenter = elements.OfType<VFXSlotContainerPresenter>();

                IEnumerable<NodeAnchorPresenter> allCandidates = Enumerable.Empty<NodeAnchorPresenter>();

                if (startAnchorPresenter.direction == Direction.Input)
                {
                    var startAnchorOperatorPresenter = (startAnchorPresenter as VFXDataAnchorPresenter);
                    if (startAnchorOperatorPresenter != null) // is is an input from another operator
                    {
                        var currentOperator = startAnchorOperatorPresenter.sourceNode.slotContainer;
                        var childrenOperators = new HashSet<IVFXSlotContainer>();
                        CollectChildOperator(currentOperator, childrenOperators);
                        allOperatorPresenter = allOperatorPresenter.Where(o => !childrenOperators.Contains(o.slotContainer));
                        var toSlot = startAnchorOperatorPresenter.model;
                        allCandidates = allOperatorPresenter.SelectMany(o => o.outputAnchors).Where(o =>
                            {
                                var candidate = o as VFXDataAnchorPresenter;
                                return toSlot.CanLink(candidate.model);
                            }).ToList();
                    }
                    else
                    {
                    }
                }
                else
                {
                    var startAnchorOperatorPresenter = (startAnchorPresenter as VFXDataAnchorPresenter);
                    var currentOperator = startAnchorOperatorPresenter.sourceNode.slotContainer;
                    var parentOperators = new HashSet<IVFXSlotContainer>();
                    CollectParentOperator(currentOperator, parentOperators);
                    allOperatorPresenter = allOperatorPresenter.Where(o => !parentOperators.Contains(o.slotContainer));
                    allCandidates = allOperatorPresenter.SelectMany(o => o.inputAnchors).Where(o =>
                        {
                            var candidate = o as VFXOperatorAnchorPresenter;
                            var toSlot = candidate.model;
                            return toSlot.CanLink(startAnchorOperatorPresenter.model);
                        }).ToList();

                    // For edge starting with an output, we must add all data anchors from all blocks
                    List<NodeAnchorPresenter> presenters;
                    /*if (!m_DataInputAnchorPresenters.TryGetValue(startAnchorPresenter.anchorType, out presenters))
                    {
                        presenters = new List<NodeAnchorPresenter>();
                        m_DataInputAnchorPresenters[startAnchorPresenter.anchorType] = presenters;
                    }
                    else
                    {
                        presenters = m_DataInputAnchorPresenters[startAnchorPresenter.anchorType];
                    }
                    */
                    presenters = new List<NodeAnchorPresenter>();

                    allCandidates = allCandidates.Concat(presenters);
                }

                return allCandidates.ToList();
            }
            else
            {
                var res = new List<NodeAnchorPresenter>();

                if (!(startAnchorPresenter is VFXFlowAnchorPresenter))
                    return res;

                var startFlowAnchorPresenter = (VFXFlowAnchorPresenter)startAnchorPresenter;
                foreach (var anchorPresenter in m_FlowAnchorPresenters)
                {
                    VFXContext owner = anchorPresenter.Owner;
                    if (owner == null ||
                        startAnchorPresenter == anchorPresenter ||
                        !anchorPresenter.IsConnectable() ||
                        startAnchorPresenter.direction == anchorPresenter.direction ||
                        owner == startFlowAnchorPresenter.Owner)
                        continue;

                    var from = startFlowAnchorPresenter.Owner;
                    var to = owner;
                    if (startAnchorPresenter.direction == Direction.Input)
                    {
                        from = owner;
                        to = startFlowAnchorPresenter.Owner;
                    }

                    if (VFXContext.CanLink(from, to))
                        res.Add(anchorPresenter);
                }
                return res;
            }
        }

        private void AddVFXModel(Vector2 pos, VFXModel model)
        {
            RecordAll(model.name, RecordEvent.Add);
            model.position = pos;
            m_Graph.AddChild(model);
        }

        public VFXContext AddVFXContext(Vector2 pos, VFXModelDescriptor<VFXContext> desc)
        {
            VFXContext model = desc.CreateInstance();
            AddVFXModel(pos, model);
            return model;
        }

        public VFXOperator AddVFXOperator(Vector2 pos, VFXModelDescriptor<VFXOperator> desc)
        {
            var model = desc.CreateInstance();
            AddVFXModel(pos, model);
            return model;
        }

        public VFXBuiltInParameter AddVFXBuiltInParameter(Vector2 pos, VFXModelDescriptorBuiltInParameters desc)
        {
            var model = desc.CreateInstance();
            AddVFXModel(pos, model);
            return model;
        }

        public VFXAttributeParameter AddVFXAttributeParameter(Vector2 pos, VFXModelDescriptorAttributeParameters desc)
        {
            var model = desc.CreateInstance();
            AddVFXModel(pos, model);
            return model;
        }

        public VFXParameter AddVFXParameter(Vector2 pos, VFXModelDescriptorParameters desc)
        {
            var model = desc.CreateInstance();
            AddVFXModel(pos, model);
            return model;
        }

        public VFXAsset GetVFXAsset()
        {
            return m_VFXAsset;
        }

        public VFXGraph GetGraph()
        {
            return m_Graph;
        }

        public void Clear()
        {
            m_Elements.Clear();
            ClearTempElements();

            m_FlowAnchorPresenters.Clear();
            m_DataInputAnchorPresenters.Clear();
            m_DataOutputAnchorPresenters.Clear();

            //m_SyncedContexts.Clear();
            m_SyncedModels.Clear();
        }

        public void SetVFXAsset(VFXAsset vfx, bool force)
        {
            if (m_VFXAsset != vfx || force)
            {
                // Do we have a leak without this line ?
                /*if (m_VFXAsset != null && !EditorUtility.IsPersistent(m_VFXAsset))
                    DestroyImmediate(m_VFXAsset);*/

                Clear();
                //Debug.Log(string.Format("SET GRAPH ASSET new:{0} old:{1} force:{2}", vfx, m_VFXAsset, force));

                if (m_Graph != null)
                {
                    m_Graph.onInvalidateDelegate -= SyncPresentersFromModel;
                    m_Graph.onInvalidateDelegate -= RecomputeExpressionGraph;
                }

                m_VFXAsset = vfx == null ? new VFXAsset() : vfx;
                m_Graph = m_VFXAsset.GetOrCreateGraph();

                m_Graph.onInvalidateDelegate += SyncPresentersFromModel;
                m_Graph.onInvalidateDelegate += RecomputeExpressionGraph;

                // First trigger
                RecomputeExpressionGraph(m_Graph, VFXModel.InvalidationCause.kStructureChanged);
                SyncPresentersFromModel(m_Graph, VFXModel.InvalidationCause.kStructureChanged);


                // Doesn't work for some reason
                //View.FrameAll();
            }
        }

        public void SyncPresentersFromModel(VFXModel model, VFXModel.InvalidationCause cause)
        {
            switch (cause)
            {
                case VFXModel.InvalidationCause.kStructureChanged:
                {
                    Dictionary<VFXModel, VFXSlotContainerPresenter> syncedModels = null;
                    if (model is VFXGraph)
                        syncedModels = m_SyncedModels;

                    if (syncedModels != null)
                    {
                        var toRemove = syncedModels.Keys.Except(model.children).ToList();
                        foreach (var m in toRemove)
                            RemovePresentersFromModel(m, syncedModels);

                        var toAdd = model.children.Except(syncedModels.Keys).ToList();
                        foreach (var m in toAdd)
                            AddPresentersFromModel(m, syncedModels);
                    }

                    RecreateNodeEdges();
                    RecreateFlowEdges();

                    break;
                }
                case VFXModel.InvalidationCause.kConnectionChanged:
                    RecreateNodeEdges();
                    break;
            }

            //Debug.Log("Invalidate Model: " + model + " Cause: " + cause + " nbElements:" + m_Elements.Count);
        }

        private void AddPresentersFromModel(VFXModel model, Dictionary<VFXModel, VFXSlotContainerPresenter> syncedModels)
        {
            VFXSlotContainerPresenter newPresenter = m_PresenterFactory.Create(model) as VFXSlotContainerPresenter;

            syncedModels[model] = newPresenter;
            if (newPresenter != null)
            {
                var presenter = (GraphElementPresenter)newPresenter;
                newPresenter.Init(model, this);
                AddElement(presenter);
            }
            RecreateNodeEdges();
            RecreateFlowEdges();
        }

        private void RemovePresentersFromModel(VFXModel model, Dictionary<VFXModel, VFXSlotContainerPresenter> syncedModels)
        {
            var presenter = syncedModels[model];
            syncedModels.Remove(model);

            if (presenter != null)
                m_Elements.RemoveAll(x => x as VFXSlotContainerPresenter == presenter); // We don't call RemoveElement as it modifies the model...
        }

        [SerializeField]
        private VFXAsset m_VFXAsset;
        [SerializeField]
        private VFXGraph m_Graph;

        private VFXView m_View; // Don't call directly as it is lazy initialized
    }
}
