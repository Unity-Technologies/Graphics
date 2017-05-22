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
        private Dictionary<VFXModel, IVFXPresenter> m_SyncedModels = new Dictionary<VFXModel, IVFXPresenter>();
        // As systems are flattened within the view presenter atm, we must keep a list of synced contexts per system
        private Dictionary<VFXModel, Dictionary<VFXModel, IVFXPresenter>> m_SyncedContexts = new Dictionary<VFXModel, Dictionary<VFXModel, IVFXPresenter>>();

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

            var operatorPresenters = m_Elements.OfType<VFXNodePresenter>().Cast<VFXLinkablePresenter>();
            var blockPresenters = (m_Elements.OfType<VFXContextPresenter>().SelectMany(t => t.allChildren.OfType<VFXBlockPresenter>())).Cast<VFXLinkablePresenter>();
            var contextSlotPresenters = m_Elements.OfType<VFXContextPresenter>().Select(t => t.slotContainerPresenter).Cast<VFXLinkablePresenter>();

            var allLinkables = operatorPresenters.Concat(blockPresenters).Concat(contextSlotPresenters).ToArray();
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

        public void RecreateInputSlotEdge(HashSet<VFXDataEdgePresenter> unusedEdges, VFXLinkablePresenter[] allLinkables, IVFXSlotContainer slotContainer, VFXSlot input)
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
            var from = fromAnchor.Owner as IVFXSlotContainer;
            var to = toAnchor.Owner as IVFXSlotContainer;
            var children = new HashSet<IVFXSlotContainer>();
            CollectChildOperator(from, children);
            CollectChildOperator(to, children);

            var allOperator = children.OfType<Object>().ToArray();
            var allSlot = children.SelectMany(c => c.outputSlots.Concat(c.inputSlots)).OfType<Object>().ToArray();
            Undo.RecordObjects(allOperator.Concat(allSlot).ToArray(), string.Format("{0} Edge", e == RecordEvent.Add ? "Add Edge" : "Remove Edge"));
        }

        private void RecordFlowEdgePresenter(VFXFlowEdgePresenter flowEdge, RecordEvent e)
        {
            var context0 = ((VFXFlowAnchorPresenter)flowEdge.output).Owner as VFXContext;
            var context1 = ((VFXFlowAnchorPresenter)flowEdge.input).Owner as VFXContext;
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

                var context0 = ((VFXFlowAnchorPresenter)flowEdge.output).Owner as VFXContext;
                var context1 = ((VFXFlowAnchorPresenter)flowEdge.input).Owner as VFXContext;

                VFXSystem.ConnectContexts(context0, context1, m_Graph);

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

                // First we need to disconnect context if needed
                VFXSystem.DisconnectContext(context, m_Graph);
                var system = context.GetParent();
                var index = system.GetIndex(context);
                if (index < system.GetNbChildren() - 1)
                    VFXSystem.DisconnectContext(system.GetChild(index + 1), m_Graph);

                // now context should be in its own system
                var newSystem = context.GetParent();
                m_Graph.RemoveChild(newSystem);
                Undo.DestroyObjectImmediate(newSystem);
                context.Detach();
            }
            else if (element is VFXNodePresenter)
            {
                var operatorPresenter = element as VFXNodePresenter;
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

                var anchorPresenter = flowEdge.input;
                var context = ((VFXFlowAnchorPresenter)anchorPresenter).Owner as VFXContext;
                if (context != null)
                    VFXSystem.DisconnectContext(context, m_Graph);
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

        public void RegisterDataAnchorPresenter(VFXContextDataInputAnchorPresenter presenter)
        {
            List<NodeAnchorPresenter> list;
            if (!m_DataInputAnchorPresenters.TryGetValue(presenter.anchorType, out list))
            {
                list = new List<NodeAnchorPresenter>();
                m_DataInputAnchorPresenters[presenter.anchorType] = list;
            }
            if (!list.Contains(presenter))
                list.Add(presenter);
        }

        public void UnregisterDataAnchorPresenter(VFXContextDataInputAnchorPresenter presenter)
        {
            List<NodeAnchorPresenter> list;
            if (m_DataInputAnchorPresenters.TryGetValue(presenter.anchorType, out list))
            {
                list.Remove(presenter);
            }
        }

        public void RegisterDataAnchorPresenter(VFXContextDataOutputAnchorPresenter presenter)
        {
            List<NodeAnchorPresenter> list;
            if (!m_DataOutputAnchorPresenters.TryGetValue(presenter.anchorType, out list))
            {
                list = new List<NodeAnchorPresenter>();
                m_DataOutputAnchorPresenters[presenter.anchorType] = list;
            }
            if (!list.Contains(presenter))
                list.Add(presenter);
        }

        public void UnregisterDataAnchorPresenter(VFXContextDataOutputAnchorPresenter presenter)
        {
            List<NodeAnchorPresenter> list;
            if (m_DataOutputAnchorPresenters.TryGetValue(presenter.anchorType, out list))
            {
                list.Remove(presenter);
            }
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
                var allOperatorPresenter = elements.OfType<VFXNodePresenter>();

                IEnumerable<NodeAnchorPresenter> allCandidates = Enumerable.Empty<NodeAnchorPresenter>();

                if (startAnchorPresenter.direction == Direction.Input)
                {
                    var startAnchorOperatorPresenter = (startAnchorPresenter as VFXDataAnchorPresenter);
                    if (startAnchorOperatorPresenter != null) // is is an input from another operator
                    {
                        var currentOperator = startAnchorOperatorPresenter.sourceNode.slotContainer;
                        var childrenOperators = new HashSet<IVFXSlotContainer>();
                        CollectChildOperator(currentOperator, childrenOperators);
                        allOperatorPresenter = allOperatorPresenter.Where(o => !childrenOperators.Contains(o.node));
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
                    allOperatorPresenter = allOperatorPresenter.Where(o => !parentOperators.Contains(o.node));
                    allCandidates = allOperatorPresenter.SelectMany(o => o.inputAnchors).Where(o =>
                        {
                            var candidate = o as VFXOperatorAnchorPresenter;
                            var toSlot = candidate.model;
                            return toSlot.CanLink(startAnchorOperatorPresenter.model);
                        }).ToList();

                    // For edge starting with an output, we must add all data anchors from all blocks
                    List<NodeAnchorPresenter> presenters;
                    if (!m_DataInputAnchorPresenters.TryGetValue(startAnchorPresenter.anchorType, out presenters))
                    {
                        presenters = new List<NodeAnchorPresenter>();
                        m_DataInputAnchorPresenters[startAnchorPresenter.anchorType] = presenters;
                    }
                    else
                    {
                        presenters = m_DataInputAnchorPresenters[startAnchorPresenter.anchorType];
                    }

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
                    VFXModel owner = anchorPresenter.Owner;
                    if (owner == null ||
                        startAnchorPresenter == anchorPresenter ||
                        !anchorPresenter.IsConnectable() ||
                        startAnchorPresenter.direction == anchorPresenter.direction ||
                        owner == startFlowAnchorPresenter.Owner)
                        continue;

                    if (owner is VFXContext)
                    {
                        VFXSystem system = ((VFXContext)owner).GetParent();
                        if (system == null)
                            continue;

                        int indexOffset = startAnchorPresenter.direction == Direction.Output ? 0 : 1;
                        if (system.AcceptChild(startFlowAnchorPresenter.Owner, system.GetIndex(owner) + indexOffset))
                            res.Add(anchorPresenter);
                    }
                }
                return res;
            }
        }

        public VFXContext AddVFXContext(Vector2 pos, VFXModelDescriptor<VFXContext> desc)
        {
            VFXContext newContext = desc.CreateInstance();
            RecordAll(newContext.name, RecordEvent.Add);

            newContext.position = pos;

            // needs to create a temp system to hold the context
            var system = CreateInstance<VFXSystem>();
            system.AddChild(newContext);
            m_Graph.AddChild(system);

            return newContext;
        }

        private void AddVFXModel(Vector2 pos, VFXModel model)
        {
            model.position = pos;
            RecordAll(model.name, RecordEvent.Add);
            m_Graph.AddChild(model);
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

        private void CreateFlowEdges(VFXSystem system)
        {
            if (elements.Count() == 0)
                return;

            for (int i = 0; i < system.GetNbChildren() - 1; ++i)
            {
                var inModel = system.GetChild(i);
                var outModel = system.GetChild(i + 1);
                var inPresenter = elements.OfType<VFXContextPresenter>().FirstOrDefault(x => x.model == inModel);
                var outPresenter = elements.OfType<VFXContextPresenter>().FirstOrDefault(x => x.model == outModel);

                if (inPresenter == null || outPresenter == null)
                    break;

                var edgePresenter = ScriptableObject.CreateInstance<VFXFlowEdgePresenter>();
                edgePresenter.output = inPresenter.outputAnchors[0];
                edgePresenter.input = outPresenter.inputAnchors[0];
                base.AddElement(edgePresenter);


                //Debug.Log("Create Edge: " + edgePresenter.GetHashCode());
            }
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
            m_DataInputAnchorPresenters.Clear();

            m_SyncedContexts.Clear();
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
                Debug.Log(string.Format("SET GRAPH ASSET new:{0} old:{1} force:{2}", vfx, m_VFXAsset, force));

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
                SyncPresentersFromModel(m_Graph, VFXModel.InvalidationCause.kStructureChanged);
                RecomputeExpressionGraph(m_Graph, VFXModel.InvalidationCause.kStructureChanged);

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
                    Dictionary<VFXModel, IVFXPresenter> syncedModels = null;
                    if (model is VFXGraph)
                        syncedModels = m_SyncedModels;
                    else if (model is VFXSystem)
                        syncedModels = m_SyncedContexts[model];

                    // TODO Temp We remove previous flow edges
                    if (model is VFXSystem)
                        RemoveFlowEdges(syncedModels.Values.OfType<VFXContextPresenter>());

                    if (syncedModels != null)
                    {
                        var toRemove = syncedModels.Keys.Except(model.children).ToList();
                        foreach (var m in toRemove)
                            RemovePresentersFromModel(m, syncedModels);

                        var toAdd = model.children.Except(syncedModels.Keys).ToList();
                        foreach (var m in toAdd)
                            AddPresentersFromModel(m, syncedModels);
                    }

                    // TODO Temp We recreate flow edges
                    if (model is VFXSystem)
                        CreateFlowEdges((VFXSystem)model);
                    else
                        RecreateNodeEdges();

                    break;
                }
                case VFXModel.InvalidationCause.kConnectionChanged:
                    RecreateNodeEdges();
                    break;
            }

            //Debug.Log("Invalidate Model: " + model + " Cause: " + cause + " nbElements:" + m_Elements.Count);
        }

        private void AddPresentersFromModel(VFXModel model, Dictionary<VFXModel, IVFXPresenter> syncedModels)
        {
            IVFXPresenter newPresenter = null;
            if (model is VFXSystem)
            {
                VFXSystem system = (VFXSystem)model;

                var syncContexts = new Dictionary<VFXModel, IVFXPresenter>();
                foreach (var context in system.GetChildren())
                    AddPresentersFromModel(context, syncContexts);
                m_SyncedContexts[model] = syncContexts;

                CreateFlowEdges((VFXSystem)model);
            }
            else
            {
                GraphElementPresenter presenter = m_PresenterFactory.Create(model);
                newPresenter = presenter as IVFXPresenter;
            }

            syncedModels[model] = newPresenter;
            if (newPresenter != null)
            {
                var presenter = (GraphElementPresenter)newPresenter;
                newPresenter.Init(model, this);
                AddElement(presenter);
            }
            RecreateNodeEdges();
        }

        private void RemovePresentersFromModel(VFXModel model, Dictionary<VFXModel, IVFXPresenter> syncedModels)
        {
            var presenter = syncedModels[model];
            syncedModels.Remove(model);

            if (model is VFXSystem)
            {
                RemoveFlowEdges(m_SyncedContexts[model].Values.OfType<VFXContextPresenter>());

                foreach (var context in m_SyncedContexts[model].Keys.ToList())
                    RemovePresentersFromModel(context, m_SyncedContexts[model]);
                m_SyncedContexts.Remove(model);
            }

            if (presenter != null)
                m_Elements.RemoveAll(x => x as IVFXPresenter == presenter); // We don't call RemoveElement as it modifies the model...
        }

        private void RemoveFlowEdges(IEnumerable<VFXContextPresenter> presenters)
        {
            foreach (var p in presenters)
            {
                m_Elements.RemoveAll(e =>
                    {
                        if (e is VFXFlowEdgePresenter)
                        {
                            var edge = (VFXFlowEdgePresenter)e;
                            if (p.outputAnchors.FirstOrDefault(a => a == edge.output) != null ||
                                p.inputAnchors.FirstOrDefault(a => a == edge.input) != null)
                            {
                                //Debug.Log("Remove Edge: " + edge.GetHashCode());
                                return true;
                            }
                        }
                        return false;
                    });
            }
        }

        [SerializeField]
        private VFXAsset m_VFXAsset;
        [SerializeField]
        private VFXGraph m_Graph;

        private VFXView m_View; // Don't call directly as it is lazy initialized
    }
}
