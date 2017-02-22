using System;
using System.Collections.Generic;
using System.Linq;
using RMGUI.GraphView;
using UnityEngine;
using UnityEngine.RMGUI;

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

        protected new void OnEnable()
		{
			base.OnEnable();

            if (m_FlowAnchorPresenters == null)
                m_FlowAnchorPresenters = new List<VFXFlowAnchorPresenter>();

            if (m_DataOutputAnchorPresenters == null)
                m_DataOutputAnchorPresenters = new Dictionary<Type,List<NodeAnchorPresenter>>();

            if (m_DataInputAnchorPresenters == null)
                m_DataInputAnchorPresenters = new Dictionary<Type, List<NodeAnchorPresenter>>();

            SetGraphAsset(m_GraphAsset != null ? m_GraphAsset : CreateInstance<VFXGraphAsset>(), false);
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

        private void RecreateOperatorEdges()
        {
            m_Elements.RemoveAll(e => e is VFXOperatorEdgePresenter);

            var operatorPresenters = m_Elements.OfType<VFXOperatorPresenter>().Cast<VFXOperatorPresenter>().ToArray();
            foreach (var operatorPresenter in operatorPresenters)
            {
                var modelOperator = operatorPresenter.Operator;
                foreach (var input in modelOperator.InputSlots)
                {
                    if (input.parent != null)
                    {
                        var edgePresenter = CreateInstance<VFXOperatorEdgePresenter>();

                        var operatorPresenterFrom = operatorPresenters.First(e => e.Operator == input.parent);
                        var operatorPresenterTo = operatorPresenters.First(e => e.Operator == modelOperator);

                        var anchorFrom = operatorPresenterFrom.outputAnchors.First(o => (o as VFXOperatorAnchorPresenter).slotID == input.parentSlotID);
                        var anchorTo = operatorPresenterTo.inputAnchors.First(o => (o as VFXOperatorAnchorPresenter).slotID == input.slotID);

                        edgePresenter.output = anchorFrom;
                        edgePresenter.input = anchorTo;

                        base.AddElement(edgePresenter);
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
				RecreateFlowEdges();
			}
            else if (edge is EdgePresenter)
            {
                var flowEdge = edge as EdgePresenter;
                var fromAnchor = flowEdge.output as VFXOperatorAnchorPresenter;
                var toAnchor = flowEdge.input as VFXOperatorAnchorPresenter;

                //Update connection
                var inputSlots = toAnchor.sourceOperator.Operator.InputSlots;
                var sourceIndex = Array.FindIndex(inputSlots, s => s.slotID == toAnchor.slotID);


                inputSlots[sourceIndex].Connect(fromAnchor.sourceOperator.Operator, fromAnchor.slotID);
                toAnchor.sourceOperator.Operator.Invalidate(VFXModel.InvalidationCause.kParamChanged);

                toAnchor.sourceOperator.Init(toAnchor.sourceOperator.Operator);
                RecreateOperatorEdges();
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
				VFXContext context = ((VFXContextPresenter)element).Model;

				// First we need to disconnect context if needed
				VFXSystem.DisconnectContext(context, m_GraphAsset.root);
				var system = context.GetParent();
				var index = system.GetIndex(context);
				if (index < system.GetNbChildren() - 1)
					VFXSystem.DisconnectContext(system.GetChild(index + 1), m_GraphAsset.root);

				// now context should be in its own system
				m_GraphAsset.root.RemoveChild(context.GetParent());
				context.Detach();

				RecreateFlowEdges();
			}
            else if (element is VFXOperatorPresenter)
            {
                var operatorPresenter = element as VFXOperatorPresenter;
                var allOperator = m_Elements.OfType<VFXOperatorPresenter>().Cast<VFXOperatorPresenter>();
                foreach (var currentOperator in allOperator)
                {
                    var slotToDelete = currentOperator.Operator.InputSlots.Where(s => s.parent == operatorPresenter.Operator).ToArray();
                    if (slotToDelete.Length > 0)
                    {
                        foreach (var inputSlot in slotToDelete)
                        {
                            inputSlot.Disconnect();
                        }
                        currentOperator.Operator.Invalidate(VFXModel.InvalidationCause.kParamChanged);
                        currentOperator.Init(currentOperator.Operator);
                    }
                }
                m_GraphAsset.root.RemoveChild(operatorPresenter.Operator);
                RecreateOperatorEdges();
            }
			else if (element is VFXFlowEdgePresenter)
			{
				var anchorPresenter = ((VFXFlowEdgePresenter)element).input;
				var context = ((VFXFlowAnchorPresenter)anchorPresenter).Owner as VFXContext;
				if (context != null)
					VFXSystem.DisconnectContext(context, m_GraphAsset.root);
			}
            else if (element is VFXOperatorEdgePresenter)
            {
                var edge = element as VFXOperatorEdgePresenter;
                var to = edge.input as VFXOperatorAnchorPresenter;

                //Update connection (*wip* : will be a function of VFXOperator)
                var toOperator = to.sourceOperator.Operator;
                var toSlot = toOperator.InputSlots.First(o => o.slotID == to.slotID);
                toSlot.Disconnect();
                toOperator.Invalidate(VFXModel.InvalidationCause.kParamChanged);

                to.sourceOperator.Init(toOperator);
                RecreateOperatorEdges();
            }
            else
            {
                throw new NotImplementedException(string.Format("Unexpected type   : {0}", element.GetType().FullName));
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

        public void RegisterDataAnchorPresenter(VFXDataInputAnchorPresenter presenter)
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

        public void UnregisterDataAnchorPresenter(VFXDataInputAnchorPresenter presenter)
        {
            List<NodeAnchorPresenter> list;
            if (m_DataInputAnchorPresenters.TryGetValue(presenter.anchorType, out list))
            {
                list.Remove(presenter);
            }
        }

        public void RegisterDataAnchorPresenter(VFXDataOutputAnchorPresenter presenter)
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

        public void UnregisterDataAnchorPresenter(VFXDataOutputAnchorPresenter presenter)
        {
            List<NodeAnchorPresenter> list;
            if (m_DataOutputAnchorPresenters.TryGetValue(presenter.anchorType, out list))
            {
                list.Remove(presenter);
            }
        }

        private static void CollectParentOperator(VFXOperator operatorInput, HashSet<VFXOperator> listParent)
        {
            listParent.Add(operatorInput);
            foreach (var input in operatorInput.InputSlots)
            {
                if (input.parent != null)
                {
                    CollectParentOperator(input.parent, listParent);
                }
            }
        }

        private static void CollectChildOperator(VFXOperator operatorInput, HashSet<VFXOperator> listChildren, IEnumerable<VFXOperatorEdgePresenter> allEdges)
        {
            listChildren.Add(operatorInput);

            var ouputOperators = allEdges.Where(o => (o.input as VFXOperatorAnchorPresenter).sourceOperator.Operator == operatorInput)
                                .Select(o => (o.output as VFXOperatorAnchorPresenter).sourceOperator.Operator);
            foreach (var output in ouputOperators)
            {
                CollectChildOperator(output, listChildren, allEdges);
            }
        }

        public override List<NodeAnchorPresenter> GetCompatibleAnchors(NodeAnchorPresenter startAnchorPresenter, NodeAdapter nodeAdapter)
		{
            if (startAnchorPresenter is VFXOperatorAnchorPresenter)
            {
                var allOperatorPresenter = elements.OfType<VFXOperatorPresenter>();
                var currentOperator = (startAnchorPresenter as VFXOperatorAnchorPresenter).sourceOperator.Operator;
                if (startAnchorPresenter.direction == Direction.Input)
                {
                    var childrenOperators = new HashSet<VFXOperator>();
                    CollectChildOperator(currentOperator, childrenOperators, elements.OfType<VFXOperatorEdgePresenter>().Cast<VFXOperatorEdgePresenter>().ToArray());
                    allOperatorPresenter = allOperatorPresenter.Where(o => !childrenOperators.Contains(o.Operator));
                    return allOperatorPresenter.SelectMany(o => o.outputAnchors).ToList();
                }

                var parentOperators = new HashSet<VFXOperator>();
                CollectParentOperator(currentOperator, parentOperators);
                allOperatorPresenter = allOperatorPresenter.Where(o => !parentOperators.Contains(o.Operator));
                return allOperatorPresenter.SelectMany(o => o.inputAnchors).ToList();
            }
            else if (startAnchorPresenter is VFXDataAnchorPresenter)
            {
                var dictionary = startAnchorPresenter is VFXDataInputAnchorPresenter ? m_DataOutputAnchorPresenters : m_DataInputAnchorPresenters;

                List<NodeAnchorPresenter> presenters;
                if (!dictionary.TryGetValue(startAnchorPresenter.anchorType, out presenters))
                {
                    presenters = new List<NodeAnchorPresenter>();
                    dictionary[startAnchorPresenter.anchorType] = presenters;
                }

                return presenters;
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

            throw new NotImplementedException();
		}

		public void AddVFXContext(Vector2 pos, VFXModelDescriptor<VFXContext> desc)
		{
            VFXContext newContext = desc.CreateInstance();
            newContext.position = pos;

			// needs to create a temp system to hold the context
			var system = new VFXSystem();
            system.AddChild(newContext);

            m_GraphAsset.root.AddChild(system);

            AddPresentersFromModel(system);
        }


        public void AddVFXOperator(Vector2 pos, VFXOperator desc)
        {
            var model = desc;
            model.position = pos;
            m_GraphAsset.root.AddChild(model);
            AddPresentersFromModel(model);
        }


		private void RecreateFlowEdges()
		{
			m_Elements.RemoveAll(element => element is VFXFlowEdgePresenter);

			foreach (var model in m_GraphAsset.root.GetChildren())
				if (model is VFXSystem)
					CreateFlowEdges((VFXSystem)model);
		}

		private void CreateFlowEdges(VFXSystem system)
		{
		    if (elements.Count() == 0)
		        return;

			for (int i = 0; i < system.GetNbChildren() - 1; ++i)
			{
				var inModel = system.GetChild(i);
				var outModel = system.GetChild(i + 1);
				var inPresenter = elements.OfType<VFXContextPresenter>().First(x => x.Model == inModel);
				var outPresenter = elements.OfType<VFXContextPresenter>().First(x => x.Model == outModel);

				var edgePresenter = ScriptableObject.CreateInstance<VFXFlowEdgePresenter>();
				edgePresenter.output = inPresenter.outputAnchors[0];
				edgePresenter.input = outPresenter.inputAnchors[0];
				base.AddElement(edgePresenter);
			}
		}

		private void AddPresentersFromModel(VFXModel model)
		{
			if (model is VFXSystem)
			{
				VFXSystem system = (VFXSystem)model;

				foreach (var context in system.GetChildren())
					AddPresentersFromModel(context);

				// Add the connections if any
				CreateFlowEdges(system);
			}
			else if (model is VFXContext)
			{
				VFXContext context = (VFXContext)model;
				var presenter = CreateInstance<VFXContextPresenter>();
				presenter.Init(this,context);
				presenter.position = new Rect(context.position.x, context.position.y, 100, 100);
				AddElement(presenter);
			}
            else if (model is VFXOperator)
            {
                VFXOperator context = (VFXOperator)model;
                var presenter = CreateInstance<VFXOperatorPresenter>();
                presenter.Init(context);
                presenter.position = new Rect(context.position.x, context.position.y, 100, 100);
                AddElement(presenter);
            }
            else
            {
                throw new NotImplementedException();
            }
		}
        public VFXGraphAsset GetGraphAsset()
        {
            return m_GraphAsset;
        }
        public void SetGraphAsset(VFXGraphAsset graph, bool force)
		{
            if (m_GraphAsset != graph || force)
			{
                // Do we have a leak without this line ?
                /*if (m_GraphAsset != null && !EditorUtility.IsPersistent(m_GraphAsset))
                    DestroyImmediate(m_GraphAsset);*/

                m_Elements.Clear();
				m_FlowAnchorPresenters.Clear();
               
                if (m_GraphAsset != null)
                    m_GraphAsset.root.onInvalidateDelegate -= OnModelInvalidate;
                
                m_GraphAsset = graph;

                if (m_GraphAsset != null)
                {
                    foreach (var model in m_GraphAsset.root.GetChildren())
                        AddPresentersFromModel(model);
                    m_GraphAsset.root.onInvalidateDelegate += OnModelInvalidate;
                }

				// Doesn't work for some reason
				//View.FrameAll();
			}
		}

        private void OnModelInvalidate(VFXModel model,VFXModel.InvalidationCause cause)
        {
            // TODO Sync presenter from here!
            Debug.Log("Invalidate Model: " + model + " Cause: " + cause);      
        }

		[SerializeField]
		private VFXGraphAsset m_GraphAsset;

		private VFXView m_View; // Don't call directly as it is lazy initialized
	}
}
