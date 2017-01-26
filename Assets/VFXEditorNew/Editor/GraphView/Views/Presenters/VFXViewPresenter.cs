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

		protected new void OnEnable()
		{
			base.OnEnable();

			if (m_FlowAnchorPresenters == null)
				m_FlowAnchorPresenters = new List<VFXFlowAnchorPresenter>();

			SetModelContainer(m_ModelContainer != null ? m_ModelContainer : CreateInstance<VFXModelContainer>(),false);
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

		public override void AddElement(EdgePresenter edge)
		{
			if (edge is VFXFlowEdgePresenter)
			{
				var flowEdge = (VFXFlowEdgePresenter)edge;

				var context0 = ((VFXFlowAnchorPresenter)flowEdge.output).Owner as VFXContext;
				var context1 = ((VFXFlowAnchorPresenter)flowEdge.input).Owner as VFXContext;

				VFXSystem.ConnectContexts(context0, context1, m_ModelContainer);
				RecreateFlowEdges();
			}
		}

		public override void RemoveElement(GraphElementPresenter element)
		{
			base.RemoveElement(element);

			if (element is VFXContextPresenter)
			{
				VFXContext context = ((VFXContextPresenter)element).Model;

				// First we need to disconnect context if needed
				VFXSystem.DisconnectContext(context, m_ModelContainer);
				var system = context.GetParent();
				var index = system.GetIndex(context);
				if (index < system.GetNbChildren() - 1)
					VFXSystem.DisconnectContext(system.GetChild(index + 1), m_ModelContainer);

				// now context should be in its own system
				m_ModelContainer.m_Roots.Remove(context.GetParent());
				context.Detach();

				RecreateFlowEdges();
			}
			else if (element is VFXFlowEdgePresenter)
			{
				var anchorPresenter = ((VFXFlowEdgePresenter)element).input;
				var context = ((VFXFlowAnchorPresenter)anchorPresenter).Owner as VFXContext;
				if (context != null)
					VFXSystem.DisconnectContext(context, m_ModelContainer);
			}

			EditorUtility.SetDirty(m_ModelContainer);
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

		public override List<NodeAnchorPresenter> GetCompatibleAnchors(NodeAnchorPresenter startAnchorPresenter, NodeAdapter nodeAdapter)
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

		public void AddVFXContext(Vector2 pos,VFXContextDesc desc)
		{
			var model = new VFXContext(desc);
			model.Position = pos;

			// needs to create a temp system to hold the context
			var system = new VFXSystem();
			system.AddChild(model);

			m_ModelContainer.m_Roots.Add(system);
			AddPresentersFromModel(system);
			EditorUtility.SetDirty(m_ModelContainer);
		}

		private void RecreateFlowEdges()
		{
			m_Elements.RemoveAll(element => element is VFXFlowEdgePresenter);

			foreach (var model in m_ModelContainer.m_Roots)
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
				presenter.position = new Rect(context.Position.x, context.Position.y, 100, 100);
				AddElement(presenter);
			}
		}
        public VFXModelContainer GetModelContainer()
        {
            return m_ModelContainer;
        }
		public void SetModelContainer(VFXModelContainer container,bool force)
		{
			if (m_ModelContainer != container || force)
			{
                // Do we have a leak without this line ?
				/*if (m_ModelContainer != null && !EditorUtility.IsPersistent(m_ModelContainer))
					DestroyImmediate(m_ModelContainer);*/

				m_Elements.Clear();
				m_FlowAnchorPresenters.Clear();
				m_ModelContainer = container;

				if (m_ModelContainer != null)
					foreach (var model in m_ModelContainer.m_Roots)
						AddPresentersFromModel(model);

				// Doesn't work for some reason
				//View.FrameAll();
			}
		}

		[SerializeField]
		private VFXModelContainer m_ModelContainer;

		private VFXView m_View; // Don't call directly as it is lazy initialized
	}
}
