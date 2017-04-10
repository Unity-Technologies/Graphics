using System;
using System.Collections.Generic;
using System.Linq;
using UIElements.GraphView;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.VFX.UI
{
	[Serializable]
	class VFXViewPresenter : GraphViewPresenter
	{
		[SerializeField]
		public List<VFXFlowAnchorPresenter> m_FlowAnchorPresenters;

        [SerializeField]
        public Dictionary<Type,List<NodeAnchorPresenter>> m_DataInputAnchorPresenters = new Dictionary<Type, List<NodeAnchorPresenter>>();

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
            m_PresenterFactory[typeof(VFXParameter)] = typeof(VFXParameterPresenter);
        }

        protected new void OnEnable()
		{
			base.OnEnable();

            if (m_FlowAnchorPresenters == null)
                m_FlowAnchorPresenters = new List<VFXFlowAnchorPresenter>();

            if (m_DataOutputAnchorPresenters == null)
                m_DataOutputAnchorPresenters = new Dictionary<Type,List<NodeAnchorPresenter>>();

            if (m_DataInputAnchorPresenters == null)
                m_DataInputAnchorPresenters = new Dictionary<Type, List<NodeAnchorPresenter>>();

            SetGraphAsset(m_GraphAsset != null ? m_GraphAsset : CreateInstance<VFXGraphAsset>(), true);
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
            m_Elements.RemoveAll(e => e is VFXDataEdgePresenter);

            var operatorPresenters = m_Elements.OfType<VFXNodePresenter>().Cast<VFXLinkablePresenter>();
            var blockPresenters = (m_Elements.OfType<VFXContextPresenter>().SelectMany(t => t.allChildren.OfType<VFXBlockPresenter>())).Cast<VFXLinkablePresenter>();

            var allLinkables = operatorPresenters.Concat(blockPresenters).ToArray();
            foreach (var operatorPresenter in allLinkables)
            {
                var modelOperator = operatorPresenter.slotContainer;
                foreach (var input in modelOperator.inputSlots)
                {
                    if (input.HasLink())
                    {
                        var edgePresenter = CreateInstance<VFXDataEdgePresenter>();

                        var operatorPresenterFrom = allLinkables.FirstOrDefault(t => input.refSlot.owner == t.slotContainer);
                        var operatorPresenterTo = allLinkables.FirstOrDefault(t => modelOperator == t.slotContainer );

                        if (operatorPresenterFrom != null && operatorPresenterTo != null)
                        {
                            var anchorFrom = operatorPresenterFrom.outputAnchors.FirstOrDefault(o => (o as VFXDataAnchorPresenter).model == input.refSlot);
                            var anchorTo = operatorPresenterTo.inputAnchors.FirstOrDefault(o => (o as VFXDataAnchorPresenter).model == input);
                            if (anchorFrom != null && anchorTo != null)
                            {
                                edgePresenter.output = anchorFrom;
                                edgePresenter.input = anchorTo;
                                base.AddElement(edgePresenter);
                            }
                        }
                    }
                }
            }
        }

		public override void AddElement(EdgePresenter edge)
		{
			if (edge is VFXFlowEdgePresenter)
			{
				var flowEdge = (VFXFlowEdgePresenter)edge;

				var context0 = ((VFXFlowAnchorPresenter)flowEdge.output).Owner as VFXContext;
				var context1 = ((VFXFlowAnchorPresenter)flowEdge.input).Owner as VFXContext;

				VFXSystem.ConnectContexts(context0, context1, m_GraphAsset.root);
			}
            else if (edge is VFXDataEdgePresenter)
            {
                var flowEdge = edge as VFXDataEdgePresenter;
                var fromAnchor = flowEdge.output as VFXDataAnchorPresenter;
                var toAnchor = flowEdge.input as VFXDataAnchorPresenter;

                //Update connection
                var slotInput = toAnchor?toAnchor.model:null;
                var slotOuput = fromAnchor ? fromAnchor.model:null;
                if (slotInput && slotOuput)
                {
                    slotInput.Link(slotOuput);
                    RecreateNodeEdges();
                }
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

				// First we need to disconnect context if needed
				VFXSystem.DisconnectContext(context, m_GraphAsset.root);
				var system = context.GetParent();
				var index = system.GetIndex(context);
				if (index < system.GetNbChildren() - 1)
					VFXSystem.DisconnectContext(system.GetChild(index + 1), m_GraphAsset.root);

				// now context should be in its own system
				m_GraphAsset.root.RemoveChild(context.GetParent());
				context.Detach();
			}
            else if (element is VFXNodePresenter)
            {
                var operatorPresenter = element as VFXNodePresenter;
                var allSlots = operatorPresenter.node.inputSlots.Concat(operatorPresenter.node.outputSlots);
                foreach (var slot in allSlots)
                {
                    slot.UnlinkAll();
                }
                m_GraphAsset.root.RemoveChild(operatorPresenter.node);
                RecreateNodeEdges();
            }
			else if (element is VFXFlowEdgePresenter)
			{
				var anchorPresenter = ((VFXFlowEdgePresenter)element).input;
				var context = ((VFXFlowAnchorPresenter)anchorPresenter).Owner as VFXContext;
				if (context != null)
					VFXSystem.DisconnectContext(context, m_GraphAsset.root);
			}
            else if (element is VFXDataEdgePresenter)
            {
                var edge = element as VFXDataEdgePresenter;
                var to = edge.input as VFXDataAnchorPresenter;

                //Update connection (*wip* : will be a function of VFXOperator)
                var slot = to.model;
                if (slot != null)
                {
                    slot.UnlinkAll();
                    RecreateNodeEdges();
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

        public void RegisterDataAnchorPresenter(VFXBlockDataInputAnchorPresenter presenter)
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

        public void UnregisterDataAnchorPresenter(VFXBlockDataInputAnchorPresenter presenter)
        {
            List<NodeAnchorPresenter> list;
            if (m_DataInputAnchorPresenters.TryGetValue(presenter.anchorType, out list))
            {
                list.Remove(presenter);
            }
        }

        public void RegisterDataAnchorPresenter(VFXBlockDataOutputAnchorPresenter presenter)
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

        public void UnregisterDataAnchorPresenter(VFXBlockDataOutputAnchorPresenter presenter)
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


            var all = operatorInput.outputSlots.SelectMany(s => s.LinkedSlots.Select(o => o.m_Owner));
            var children = all.Cast<IVFXSlotContainer>();
            foreach (var child in children)
            {
                CollectChildOperator(child, hashChildren);
            }
        }

        public override List<NodeAnchorPresenter> GetCompatibleAnchors(NodeAnchorPresenter startAnchorPresenter, NodeAdapter nodeAdapter)
		{
            if (startAnchorPresenter is VFXDataAnchorPresenter )
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
            newContext.position = pos;

			// needs to create a temp system to hold the context
            var system = ScriptableObject.CreateInstance<VFXSystem>();
            system.AddChild(newContext);

            m_GraphAsset.root.AddChild(system);

            return newContext;
        }

        public void AddVFXOperator(Vector2 pos, VFXModelDescriptor<VFXOperator> desc)
        {
            var model = desc.CreateInstance();
            model.position = pos;
            m_GraphAsset.root.AddChild(model);
        }

        public void AddVFXParameter(Vector2 pos, VFXModelDescriptorParameters desc)
        {
            var model = desc.CreateInstance();
            model.position = pos;
            m_GraphAsset.root.AddChild(model);
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

        public VFXGraphAsset GetGraphAsset()
        {
            return m_GraphAsset;
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

        public void SetGraphAsset(VFXGraphAsset graph, bool force)
		{
            if (m_GraphAsset != graph || force)
			{
                // Do we have a leak without this line ?
                /*if (m_GraphAsset != null && !EditorUtility.IsPersistent(m_GraphAsset))
                    DestroyImmediate(m_GraphAsset);*/

                Clear();
                Debug.Log(string.Format("SET GRAPH ASSET new:{0} old:{1} force:{2}", graph, m_GraphAsset, force));
               
                if (m_GraphAsset != null)
                    m_GraphAsset.root.onInvalidateDelegate -= SyncPresentersFromModel;
                
                m_GraphAsset = graph;

                if (m_GraphAsset != null)
                {
                    m_GraphAsset.root.onInvalidateDelegate += SyncPresentersFromModel;
                    SyncPresentersFromModel(m_GraphAsset.root,VFXModel.InvalidationCause.kStructureChanged); // First call to trigger a sync
                }

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

            Debug.Log("Invalidate Model: " + model + " Cause: " + cause + " nbElements:" + m_Elements.Count);
        }

        private void AddPresentersFromModel(VFXModel model,Dictionary<VFXModel, IVFXPresenter> syncedModels)
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
                newPresenter.Init(model,this);
                AddElement(presenter);
            }
            RecreateNodeEdges(); //TODOPAUL : Filter this call
        }

        private void RemovePresentersFromModel(VFXModel model,Dictionary<VFXModel,IVFXPresenter> syncedModels)
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
				m_Elements.RemoveAll(x => (bool)(x == presenter)); // We dont call RemoveElement as it modifies the model...
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
		private VFXGraphAsset m_GraphAsset;

		private VFXView m_View; // Don't call directly as it is lazy initialized
	}
}
