using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.Experimental.UIElements;

using Object = UnityEngine.Object;
namespace UnityEditor.VFX.UI
{
    [Serializable]
    internal partial class VFXViewPresenter : GraphViewPresenter
    {
        [SerializeField]
        public List<VFXFlowAnchorPresenter> m_FlowAnchorPresenters;

        [SerializeField]
        public Dictionary<Type, List<NodeAnchorPresenter>> m_DataInputAnchorPresenters = new Dictionary<Type, List<NodeAnchorPresenter>>();

        [SerializeField]
        public Dictionary<Type, List<NodeAnchorPresenter>> m_DataOutputAnchorPresenters = new Dictionary<Type, List<NodeAnchorPresenter>>();

        // Model / Presenters synchronization
        private Dictionary<VFXModel, VFXNodePresenter> m_SyncedModels = new Dictionary<VFXModel, VFXNodePresenter>();

        private class PresenterFactory : BaseTypeFactory<VFXModel, GraphElementPresenter>
        {
            protected override GraphElementPresenter InternalCreate(Type valueType)
            {
                return (GraphElementPresenter)ScriptableObject.CreateInstance(valueType);
            }
        }
        private PresenterFactory m_PresenterFactory = new PresenterFactory();

        public Preview3DPresenter presenter { get; set; }

        public VFXViewPresenter()
        {
        }

        static public VFXViewPresenter viewPresenter
        {
            get
            {
                if (s_ViewPresenter == null)
                {
                    VFXViewPresenter[] objects = FindObjectsOfType<VFXViewPresenter>();
                    if (objects.Length == 0)
                    {
                        Debug.Log("Before CreateInstance<VFXViewPresenter> ");
                        s_ViewPresenter = CreateInstance<VFXViewPresenter>();
                        Debug.Log("After CreateInstance<VFXViewPresenter>");
                    }
                    else
                    {
                        if (objects.Length != 1)
                        {
                            Debug.LogError("Only one instance of VFXViewPresenter should exist");
                        }
                        s_ViewPresenter = objects[0];
                    }
                }
                return s_ViewPresenter;
            }
        }

        static VFXViewPresenter s_ViewPresenter;

        protected void OnEnable()
        {
            Debug.Log("OnEnable of VFXViewPresenter with instanceID:" + this.GetInstanceID());

            base.OnEnable();

            m_PresenterFactory[typeof(VFXContext)] = typeof(VFXContextPresenter);
            m_PresenterFactory[typeof(VFXOperator)] = typeof(VFXOperatorPresenter);
            m_PresenterFactory[typeof(VFXBuiltInParameter)] = typeof(VFXBuiltInParameterPresenter);
            m_PresenterFactory[typeof(VFXAttributeParameter)] = typeof(VFXAttributeParameterPresenter);
            m_PresenterFactory[typeof(VFXParameter)] = typeof(VFXParameterPresenter);

            if (m_FlowAnchorPresenters == null)
                m_FlowAnchorPresenters = new List<VFXFlowAnchorPresenter>();

            if (m_DataOutputAnchorPresenters == null)
                m_DataOutputAnchorPresenters = new Dictionary<Type, List<NodeAnchorPresenter>>();

            if (m_DataInputAnchorPresenters == null)
                m_DataInputAnchorPresenters = new Dictionary<Type, List<NodeAnchorPresenter>>();

            SetVFXAsset(m_VFXAsset, true);
            InitializeUndoStack();
            Undo.undoRedoPerformed += SynchronizeUndoRedoState;
            Undo.willFlushUndoRecord += WillFlushUndoRecord;
        }

        protected void OnDisable()
        {
            Debug.Log("OnDisable of VFXViewPresenter with instanceID :" + this.GetInstanceID());
            ReleaseUndoStack();
            Undo.undoRedoPerformed -= SynchronizeUndoRedoState;
            Undo.willFlushUndoRecord -= WillFlushUndoRecord;
            SetVFXAsset(null, true);

            if (s_ViewPresenter == this)
                s_ViewPresenter = null;
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

        IEnumerable<VFXSlotContainerPresenter> AllSlotContainerPresenters
        {
            get
            {
                var operatorPresenters = m_Elements.OfType<VFXSlotContainerPresenter>();
                var blockPresenters = (m_Elements.OfType<VFXContextPresenter>().SelectMany(t => t.allChildren.OfType<VFXBlockPresenter>())).Cast<VFXSlotContainerPresenter>();
                var contextSlotContainers = m_Elements.OfType<VFXContextPresenter>().Select(t => t.slotPresenter).Where(t => t != null).Cast<VFXSlotContainerPresenter>();

                return operatorPresenters.Concat(blockPresenters).Concat(contextSlotContainers);
            }
        }

        public void RecreateNodeEdges()
        {
            HashSet<VFXDataEdgePresenter> unusedEdges = new HashSet<VFXDataEdgePresenter>();
            foreach (var e in m_Elements.OfType<VFXDataEdgePresenter>())
            {
                unusedEdges.Add(e);
            }

            var allLinkables = AllSlotContainerPresenters.ToArray();
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
                for (int slotIndex = 0; slotIndex < output.inputFlowSlot.Length; ++slotIndex)
                {
                    var inputFlowSlot = output.inputFlowSlot[slotIndex];
                    foreach (var link in inputFlowSlot.link)
                    {
                        var inPresenter = elements.OfType<VFXContextPresenter>().FirstOrDefault(x => x.model == link.context);
                        if (inPresenter == null)
                            break;

                        var outputAnchor = inPresenter.flowOutputAnchors.Where(o => o.slotIndex == link.slotIndex).FirstOrDefault();
                        var inputAnchor = outPresenter.flowInputAnchors.Where(o => o.slotIndex == slotIndex).FirstOrDefault();

                        var edgePresenter = m_Elements.OfType<VFXFlowEdgePresenter>().FirstOrDefault(t => t.input == inputAnchor && t.output == outputAnchor);
                        if (edgePresenter != null)
                            unusedEdges.Remove(edgePresenter);
                        else
                        {
                            edgePresenter = CreateInstance<VFXFlowEdgePresenter>();
                            edgePresenter.output = outputAnchor;
                            edgePresenter.input = inputAnchor;
                            base.AddElement(edgePresenter);
                        }
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

        public override void AddElement(EdgePresenter edge)
        {
            if (edge is VFXFlowEdgePresenter)
            {
                var flowEdge = (VFXFlowEdgePresenter)edge;

                var outputFlowAnchor = flowEdge.output as VFXFlowAnchorPresenter;
                var inputFlowAnchor = flowEdge.input as VFXFlowAnchorPresenter;

                var contextOutput = outputFlowAnchor.Owner;
                var contextInput = inputFlowAnchor.Owner;

                contextOutput.LinkTo(contextInput, outputFlowAnchor.slotIndex, inputFlowAnchor.slotIndex);

                // disconnect this edge as it will not be added by add element
                edge.input = null;
                edge.output = null;
            }
            else if (edge is VFXDataEdgePresenter)
            {
                var flowEdge = edge as VFXDataEdgePresenter;
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

                // Remove connections from context
                foreach (var slot in context.inputSlots.Concat(context.outputSlots))
                    slot.UnlinkAll(true, true);

                // Remove connections from blocks
                foreach (VFXBlockPresenter blockPres in (element as VFXContextPresenter).blockPresenters)
                {
                    foreach (var slot in blockPres.slotContainer.outputSlots.Concat(blockPres.slotContainer.inputSlots))
                    {
                        slot.UnlinkAll(true, true);
                    }
                }

                // remove flow connections from context
                // TODO update data types
                context.UnlinkAll();
                // Detach from graph
                context.Detach();
            }
            else if (element is VFXBlockPresenter)
            {
                var block = element as VFXBlockPresenter;
                block.contextPresenter.RemoveBlock(block.block);
            }
            else if (element is VFXSlotContainerPresenter)
            {
                var operatorPresenter = element as VFXSlotContainerPresenter;
                VFXSlot slotToClean = null;
                do
                {
                    slotToClean = operatorPresenter.slotContainer.inputSlots.Concat(operatorPresenter.slotContainer.outputSlots)
                        .FirstOrDefault(o => o.HasLink(true));
                    if (slotToClean)
                    {
                        slotToClean.UnlinkAll(true, true);
                    }
                }
                while (slotToClean != null);

                m_Graph.RemoveChild(operatorPresenter.model);
                RecreateNodeEdges();
            }
            else if (element is VFXFlowEdgePresenter)
            {
                var flowEdge = element as VFXFlowEdgePresenter;

                var inputAnchor = flowEdge.input as VFXFlowAnchorPresenter;
                var outputAnchor = flowEdge.output as VFXFlowAnchorPresenter;

                var contextInput = inputAnchor.Owner as VFXContext;
                var contextOutput = outputAnchor.Owner as VFXContext;

                contextInput.UnlinkFrom(contextOutput, outputAnchor.slotIndex, inputAnchor.slotIndex);
            }
            else if (element is VFXDataEdgePresenter)
            {
                var edge = element as VFXDataEdgePresenter;
                var to = edge.input as VFXDataAnchorPresenter;

                if (to != null)
                {
                    var slot = to.model;
                    if (slot != null)
                    {
                        slot.UnlinkAll();
                    }
                }
            }
            else if (element is Preview3DPresenter)
            {
                //TODO
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

            List<NodeAnchorPresenter> result;
            if (dict.TryGetValue(presenter.anchorType, out result))
            {
                result.Remove(presenter);
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
                var allSlotContainerPresenters = AllSlotContainerPresenters;


                IEnumerable<NodeAnchorPresenter> allCandidates = Enumerable.Empty<NodeAnchorPresenter>();

                if (startAnchorPresenter.direction == Direction.Input)
                {
                    var startAnchorOperatorPresenter = (startAnchorPresenter as VFXDataAnchorPresenter);
                    if (startAnchorOperatorPresenter != null) // is is an input from another operator
                    {
                        var currentOperator = startAnchorOperatorPresenter.sourceNode.slotContainer;
                        var childrenOperators = new HashSet<IVFXSlotContainer>();
                        CollectChildOperator(currentOperator, childrenOperators);
                        allSlotContainerPresenters = allSlotContainerPresenters.Where(o => !childrenOperators.Contains(o.slotContainer));
                        var toSlot = startAnchorOperatorPresenter.model;
                        allCandidates = allSlotContainerPresenters.SelectMany(o => o.outputAnchors).Where(o =>
                            {
                                var candidate = o as VFXDataAnchorPresenter;
                                return toSlot.CanLink(candidate.model) && candidate.model.CanLink(toSlot);
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
                    allSlotContainerPresenters = allSlotContainerPresenters.Where(o => !parentOperators.Contains(o.slotContainer));
                    allCandidates = allSlotContainerPresenters.SelectMany(o => o.inputAnchors).Where(o =>
                        {
                            var candidate = o as VFXDataAnchorPresenter;
                            var toSlot = candidate.model;
                            return toSlot.CanLink(startAnchorOperatorPresenter.model) && startAnchorOperatorPresenter.model.CanLink(toSlot);
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

        public VFXCurrentAttributeParameter AddVFXCurrentAttributeParameter(Vector2 pos, VFXModelDescriptorCurrentAttributeParameters desc)
        {
            var model = desc.CreateInstance();
            AddVFXModel(pos, model);
            return model;
        }

        public VFXSourceAttributeParameter AddVFXSourceAttributeParameter(Vector2 pos, VFXModelDescriptorSourceAttributeParameters desc)
        {
            var model = desc.CreateInstance();
            AddVFXModel(pos, model);
            return model;
        }

        public VFXSourceAttributeParameter AddVFXAttributeParameter(Vector2 pos, VFXModelDescriptorSourceAttributeParameters desc)
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

            foreach (var pair in m_registeredEvent)
            {
                foreach (var evt in pair.Value)
                {
                    pair.Key.onInvalidateDelegate -= evt;
                }
            }
            m_registeredEvent.Clear();
        }

        private Dictionary<VFXModel, List<VFXModel.InvalidateEvent>> m_registeredEvent = new Dictionary<VFXModel, List<VFXModel.InvalidateEvent>>();
        public void AddInvalidateDelegate(VFXModel model, VFXModel.InvalidateEvent evt)
        {
            model.onInvalidateDelegate += evt;
            if (!m_registeredEvent.ContainsKey(model))
            {
                m_registeredEvent.Add(model, new List<VFXModel.InvalidateEvent>());
            }
            m_registeredEvent[model].Add(evt);
        }

        public void RemoveInvalidateDelegate(VFXModel model, VFXModel.InvalidateEvent evt)
        {
            List<VFXModel.InvalidateEvent> evtList;
            if (m_registeredEvent.TryGetValue(model, out evtList))
            {
                model.onInvalidateDelegate -= evt;
                evtList.Remove(evt);
                if (evtList.Count == 0)
                {
                    m_registeredEvent.Remove(model);
                }
            }
        }

        public bool HasVFXAsset()
        {
            return m_VFXAsset != null;
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
                    RemoveInvalidateDelegate(m_Graph, SyncPresentersFromModel);
                    RemoveInvalidateDelegate(m_Graph, InvalidateExpressionGraph);
                }

                m_VFXAsset = vfx == null ? new VFXAsset() : vfx;
                m_Graph = m_VFXAsset.GetOrCreateGraph();

                AddInvalidateDelegate(m_Graph, SyncPresentersFromModel);
                AddInvalidateDelegate(m_Graph, InvalidateExpressionGraph);

                // First trigger
                RecompileExpressionGraphIfNeeded();


                // Doesn't work for some reason
                //View.FrameAll();

#if ENABLE_VIEW_3D_PRESENTER
                if (presenter != null)
                    RemoveElement(presenter);
                presenter = CreateInstance<Preview3DPresenter>();
                AddElement(presenter);
#endif
            }
            SyncPresentersFromModel(m_Graph, VFXModel.InvalidationCause.kStructureChanged);
        }

        public void SyncPresentersFromModel(VFXModel model, VFXModel.InvalidationCause cause)
        {
            switch (cause)
            {
                case VFXModel.InvalidationCause.kStructureChanged:
                {
                    Dictionary<VFXModel, VFXNodePresenter> syncedModels = null;
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

        private void AddPresentersFromModel(VFXModel model, Dictionary<VFXModel, VFXNodePresenter> syncedModels)
        {
            VFXNodePresenter newPresenter = m_PresenterFactory.Create(model) as VFXNodePresenter;

            syncedModels[model] = newPresenter;
            if (newPresenter != null)
            {
                newPresenter.Init(model, this);
                AddElement(newPresenter);
            }
            RecreateNodeEdges();
            RecreateFlowEdges();
        }

        private void RemovePresentersFromModel(VFXModel model, Dictionary<VFXModel, VFXNodePresenter> syncedModels)
        {
            var presenter = syncedModels[model];
            syncedModels.Remove(model);

            if (presenter != null)
                m_Elements.RemoveAll(x => x as VFXNodePresenter == presenter); // We don't call RemoveElement as it modifies the model...
        }

        [SerializeField]
        private VFXAsset m_VFXAsset;
        [SerializeField]
        private VFXGraph m_Graph;

        private VFXView m_View; // Don't call directly as it is lazy initialized
    }
}
