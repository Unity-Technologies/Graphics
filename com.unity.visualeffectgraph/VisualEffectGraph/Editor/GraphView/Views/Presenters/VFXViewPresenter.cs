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
    class VFXGraphViewPresenter : GraphViewPresenter
    {
        public VFXViewPresenter m_RealPresenter;
    }
    [Serializable]
    internal partial class VFXViewPresenter : Controller<VFXGraph>
    {
        private int m_UseCount;
        public int useCount
        {
            get { return m_UseCount; }
            set
            {
                m_UseCount = value;
                if (m_UseCount == 0)
                {
                    Manager.RemovePresenter(this);
                    this.OnDisable();
                }
            }
        }

        VFXGraphViewPresenter m_Presenter;

        public VFXGraphViewPresenter graphViewPresenter
        {
            get
            {
                if (m_Presenter == null)
                {
                    m_Presenter = CreateInstance<VFXGraphViewPresenter>();
                    m_Presenter.m_RealPresenter = this;
                }
                return m_Presenter;
            }
        }

        List<VFXFlowAnchorPresenter> m_FlowAnchorPresenters = new List<VFXFlowAnchorPresenter>();

        // Model / Presenters synchronization
        private Dictionary<VFXModel, VFXNodeController> m_SyncedModels = new Dictionary<VFXModel, VFXNodeController>();

        List<VFXDataEdgePresenter> m_DataEdges = new List<VFXDataEdgePresenter>();
        List<VFXFlowEdgePresenter> m_FlowEdges = new List<VFXFlowEdgePresenter>();

        private class PresenterFactory : BaseTypeFactory<VFXModel, Controller>
        {
            protected override Controller InternalCreate(Type valueType)
            {
                return (Controller)Controller.CreateInstance(valueType);
            }
        }
        private PresenterFactory m_PresenterFactory = new PresenterFactory();

        public Preview3DPresenter presenter { get; set; }

        public VFXViewPresenter()
        {
        }

        public IEnumerable<Controller> allChildren
        {
            get { return m_SyncedModels.Values.Cast<Controller>().Concat(m_DataEdges.Cast<Controller>()).Concat(m_FlowEdges.Cast<Controller>()); }
        }

        protected new void OnEnable()
        {
            base.OnEnable();
        }

        protected void OnDisable()
        {
            RemoveInvalidateDelegate(model, InvalidateExpressionGraph);
            ReleaseUndoStack();
            Undo.undoRedoPerformed -= SynchronizeUndoRedoState;
            Undo.willFlushUndoRecord -= WillFlushUndoRecord;
        }

        IEnumerable<VFXSlotContainerPresenter> AllSlotContainerPresenters
        {
            get
            {
                var operatorPresenters = m_SyncedModels.Values.OfType<VFXSlotContainerPresenter>();
                var blockPresenters = (contexts.SelectMany(t => t.blockPresenters)).Cast<VFXSlotContainerPresenter>();
                var contextSlotContainers = contexts.Select(t => t.slotContainerPresenter).Where(t => t != null).Cast<VFXSlotContainerPresenter>();

                return operatorPresenters.Concat(blockPresenters).Concat(contextSlotContainers);
            }
        }

        public void RecreateNodeEdges()
        {
            HashSet<VFXDataEdgePresenter> unusedEdges = new HashSet<VFXDataEdgePresenter>();
            foreach (var e in m_DataEdges)
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
                edge.OnRemoveFromGraph();
                m_DataEdges.Remove(edge);
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
                    var anchorFrom = operatorPresenterFrom.outputPorts.FirstOrDefault(o => (o as VFXDataAnchorPresenter).model == input.refSlot);
                    var anchorTo = operatorPresenterTo.inputPorts.FirstOrDefault(o => (o as VFXDataAnchorPresenter).model == input);

                    var edgePresenter = m_DataEdges.FirstOrDefault(t => t.input == anchorTo && t.output == anchorFrom);

                    if (edgePresenter != null)
                    {
                        unusedEdges.Remove(edgePresenter);
                    }
                    else
                    {
                        if (anchorFrom != null && anchorTo != null)
                        {
                            edgePresenter = CreateInstance<VFXDataEdgePresenter>();
                            edgePresenter.Init(anchorFrom, anchorTo);
                            m_DataEdges.Add(edgePresenter);
                        }
                    }
                }
            }

            foreach (VFXSlot subSlot in input.children)
            {
                RecreateInputSlotEdge(unusedEdges, allLinkables, slotContainer, subSlot);
            }
        }

        public IEnumerable<VFXContextPresenter> contexts
        {
            get { return m_SyncedModels.Values.OfType<VFXContextPresenter>(); }
        }
        public IEnumerable<VFXNodeController> nodes
        {
            get { return m_SyncedModels.Values; }
        }

        public void RecreateFlowEdges()
        {
            HashSet<VFXFlowEdgePresenter> unusedEdges = new HashSet<VFXFlowEdgePresenter>();
            foreach (var e in m_FlowEdges)
            {
                unusedEdges.Add(e);
            }

            var contextPresenters = contexts;
            foreach (var outPresenter in contextPresenters.ToArray())
            {
                var output = outPresenter.context;
                for (int slotIndex = 0; slotIndex < output.inputFlowSlot.Length; ++slotIndex)
                {
                    var inputFlowSlot = output.inputFlowSlot[slotIndex];
                    foreach (var link in inputFlowSlot.link)
                    {
                        var inPresenter = contexts.FirstOrDefault(x => x.model == link.context);
                        if (inPresenter == null)
                            break;

                        var outputAnchor = inPresenter.flowOutputAnchors.Where(o => o.slotIndex == link.slotIndex).FirstOrDefault();
                        var inputAnchor = outPresenter.flowInputAnchors.Where(o => o.slotIndex == slotIndex).FirstOrDefault();

                        var edgePresenter = m_FlowEdges.FirstOrDefault(t => t.input == inputAnchor && t.output == outputAnchor);
                        if (edgePresenter != null)
                            unusedEdges.Remove(edgePresenter);
                        else
                        {
                            edgePresenter = CreateInstance<VFXFlowEdgePresenter>();
                            edgePresenter.Init(outputAnchor, inputAnchor);
                            m_FlowEdges.Add(edgePresenter);
                        }
                    }
                }
            }

            foreach (var edge in unusedEdges)
            {
                edge.OnRemoveFromGraph();
                m_FlowEdges.Remove(edge);
            }
        }

        private enum RecordEvent
        {
            Add,
            Remove
        }

        public void AddElement(VFXDataEdgePresenter edge)
        {
            var fromAnchor = edge.output;
            var toAnchor = edge.input;

            //Update connection
            var slotInput = toAnchor != null ? toAnchor.model : null;
            var slotOuput = fromAnchor != null ? fromAnchor.model : null;
            if (slotInput && slotOuput)
            {
                //Save concerned object
                slotInput.Link(slotOuput);
                RecreateNodeEdges();
            }
            edge.OnRemoveFromGraph();
        }

        public void AddElement(VFXFlowEdgePresenter edge)
        {
            var flowEdge = (VFXFlowEdgePresenter)edge;

            var outputFlowAnchor = flowEdge.output as VFXFlowAnchorPresenter;
            var inputFlowAnchor = flowEdge.input as VFXFlowAnchorPresenter;

            var contextOutput = outputFlowAnchor.owner;
            var contextInput = inputFlowAnchor.owner;

            contextOutput.LinkTo(contextInput, outputFlowAnchor.slotIndex, inputFlowAnchor.slotIndex);

            edge.OnRemoveFromGraph();
        }

        public virtual void RemoveElement(Controller element)
        {
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

                model.RemoveChild(operatorPresenter.model);
                RecreateNodeEdges();
            }
            else if (element is VFXFlowEdgePresenter)
            {
                var flowEdge = element as VFXFlowEdgePresenter;


                var inputAnchor = flowEdge.input as VFXFlowAnchorPresenter;
                var outputAnchor = flowEdge.output as VFXFlowAnchorPresenter;

                if (inputAnchor != null && outputAnchor != null)
                {
                    var contextInput = inputAnchor.owner as VFXContext;
                    var contextOutput = outputAnchor.owner as VFXContext;

                    if (contextInput != null && contextOutput != null)
                        contextInput.UnlinkFrom(contextOutput, outputAnchor.slotIndex, inputAnchor.slotIndex);
                }
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

        protected override void ModelChanged(UnityEngine.Object obj)
        {
            SyncPresentersFromModel();

            NotifyChange(AnyThing);
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

        public List<VFXDataAnchorPresenter> GetCompatiblePorts(VFXDataAnchorPresenter startAnchorPresenter, NodeAdapter nodeAdapter)
        {
            var allSlotContainerPresenters = AllSlotContainerPresenters;


            IEnumerable<VFXDataAnchorPresenter> allCandidates = Enumerable.Empty<VFXDataAnchorPresenter>();

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
                    allCandidates = allSlotContainerPresenters.SelectMany(o => o.outputPorts).Where(o =>
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
                allCandidates = allSlotContainerPresenters.SelectMany(o => o.inputPorts).Where(o =>
                    {
                        var candidate = o as VFXDataAnchorPresenter;
                        var toSlot = candidate.model;
                        return toSlot.CanLink(startAnchorOperatorPresenter.model) && startAnchorOperatorPresenter.model.CanLink(toSlot);
                    }).ToList();
            }

            return allCandidates.ToList();
        }

        public List<VFXFlowAnchorPresenter> GetCompatiblePorts(VFXFlowAnchorPresenter startAnchorPresenter, NodeAdapter nodeAdapter)
        {
            var res = new List<VFXFlowAnchorPresenter>();

            var startFlowAnchorPresenter = (VFXFlowAnchorPresenter)startAnchorPresenter;
            foreach (var anchorPresenter in m_FlowAnchorPresenters)
            {
                VFXContext owner = anchorPresenter.owner;
                if (owner == null ||
                    startAnchorPresenter == anchorPresenter ||
                    !anchorPresenter.IsConnectable() ||
                    startAnchorPresenter.direction == anchorPresenter.direction ||
                    owner == startFlowAnchorPresenter.owner)
                    continue;

                var from = startFlowAnchorPresenter.owner;
                var to = owner;
                if (startAnchorPresenter.direction == Direction.Input)
                {
                    from = owner;
                    to = startFlowAnchorPresenter.owner;
                }

                if (VFXContext.CanLink(from, to))
                    res.Add(anchorPresenter);
            }
            return res;
        }

        private void AddVFXModel(Vector2 pos, VFXModel model)
        {
            model.position = pos;
            this.model.AddChild(model);
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

        public void Clear()
        {
            m_FlowAnchorPresenters.Clear();

            m_SyncedModels.Clear();
            m_DataEdges.Clear();
            m_FlowEdges.Clear();

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
            if (model != null && m_registeredEvent.TryGetValue(model, out evtList))
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

        public static class Manager
        {
            static Dictionary<VFXAsset, VFXViewPresenter> s_Presenters = new Dictionary<VFXAsset, VFXViewPresenter>();

            public static VFXViewPresenter GetPresenter(VFXAsset asset, bool forceUpdate = false)
            {
                VFXViewPresenter presenter;
                if (!s_Presenters.TryGetValue(asset, out presenter))
                {
                    presenter = CreateInstance<VFXViewPresenter>();
                    presenter.Init(asset);
                    s_Presenters[asset] = presenter;
                }
                else
                {
                    if (forceUpdate)
                    {
                        presenter.ForceReload();
                    }
                }

                return presenter;
            }

            static public void RemovePresenter(VFXViewPresenter presenter)
            {
                s_Presenters.Remove(presenter.GetVFXAsset());
            }
        }


        public void ForceReload()
        {
            Clear();

            if (model != null)
            {
                RemoveInvalidateDelegate(model, InvalidateExpressionGraph);
            }

            // Hack should not call init again
            base.Init(m_VFXAsset.GetOrCreateGraph());

            AddInvalidateDelegate(model, InvalidateExpressionGraph);

            // First trigger
            RecompileExpressionGraphIfNeeded();

#if ENABLE_VIEW_3D_PRESENTER
            if (presenter != null)
                RemoveElement(presenter);
            presenter = CreateInstance<Preview3DPresenter>();
            AddElement(presenter);
#endif
            ModelChanged(m_VFXAsset);
        }

        public void Init(VFXAsset vfx)
        {
            Clear();

            m_VFXAsset = vfx;
            base.Init(m_VFXAsset.GetOrCreateGraph());

            InitializeUndoStack();

            AddInvalidateDelegate(model, InvalidateExpressionGraph);

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


            m_PresenterFactory[typeof(VFXContext)] = typeof(VFXContextPresenter);
            m_PresenterFactory[typeof(VFXOperator)] = typeof(VFXOperatorPresenter);
            m_PresenterFactory[typeof(VFXParameter)] = typeof(VFXParameterPresenter);

            if (m_FlowAnchorPresenters == null)
                m_FlowAnchorPresenters = new List<VFXFlowAnchorPresenter>();

            if (m_VFXAsset)
            {
                InitializeUndoStack();
            }
            Undo.undoRedoPerformed += SynchronizeUndoRedoState;
            Undo.willFlushUndoRecord += WillFlushUndoRecord;

            SyncPresentersFromModel();
        }

        public void SyncPresentersFromModel()
        {
            var toRemove = m_SyncedModels.Keys.Except(model.children).ToList();
            foreach (var m in toRemove)
                RemovePresentersFromModel(m, m_SyncedModels);

            var toAdd = model.children.Except(m_SyncedModels.Keys).ToList();
            foreach (var m in toAdd)
                AddPresentersFromModel(m, m_SyncedModels);

            RecreateNodeEdges();
            RecreateFlowEdges();
        }

        private void AddPresentersFromModel(VFXModel model, Dictionary<VFXModel, VFXNodeController> syncedModels)
        {
            VFXNodeController newPresenter = m_PresenterFactory.Create(model) as VFXNodeController;

            syncedModels[model] = newPresenter;
            if (newPresenter != null)
            {
                newPresenter.Init(model, this);
                newPresenter.ForceUpdate();
            }
        }

        private void RemovePresentersFromModel(VFXModel model, Dictionary<VFXModel, VFXNodeController> syncedModels)
        {
            syncedModels.Remove(model);
        }

        [SerializeField]
        private VFXAsset m_VFXAsset;

        private VFXView m_View; // Don't call directly as it is lazy initialized
    }
}
