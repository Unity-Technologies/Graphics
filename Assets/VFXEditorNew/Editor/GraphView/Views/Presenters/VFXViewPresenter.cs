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
            SetModelContainer(m_ModelContainer != null ? m_ModelContainer : CreateInstance<VFXModelContainer>());

            if (m_FlowAnchorPresenters == null)
                m_FlowAnchorPresenters = new List<VFXFlowAnchorPresenter>();
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

        public override void RemoveElement(GraphElementPresenter element)
        {
            base.RemoveElement(element);
            if (element is VFXContextPresenter)
            {
                VFXContext context = ((VFXContextPresenter)element).Model;
                if (context.GetParent().GetNbChildren() == 1) // Context is the only child of system, delete the system
                    m_ModelContainer.m_Roots.Remove(context.GetParent());
                context.Detach();
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

        public override List<NodeAnchorPresenter> GetCompatibleAnchors(NodeAnchorPresenter startAnchor, NodeAdapter nodeAdapter)
        {
            var res = new List<NodeAnchorPresenter>();

            if (!(startAnchor is VFXFlowAnchorPresenter))
                return res;

            var startFlowAnchor = (VFXFlowAnchorPresenter)startAnchor;

            foreach (var anchor in m_FlowAnchorPresenters)
            {
                VFXModel owner = anchor.Owner;
                if (owner == null ||
                    startAnchor == anchor || 
                    !anchor.IsConnectable() || 
                    startAnchor.direction == anchor.direction ||
                    owner == startFlowAnchor.Owner)
                    continue;

                if (owner is VFXContext)
                {
                    VFXSystem system = ((VFXContext)owner).GetParent();
                    if (system == null)
                        continue;

                    int indexOffset = startAnchor.direction == Direction.Output ? 0 : 1;
                    if (system.AcceptChild(startFlowAnchor.Owner, system.GetIndex(owner) + indexOffset))
                        res.Add(anchor);
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

        private void AddPresentersFromModel(VFXModel model)
        {
            if (model is VFXSystem)
            {
                VFXSystem system = (VFXSystem)model;
                foreach (var context in system.GetChildren())
                    AddPresentersFromModel(context);
                // Add the connections if any
                for (int i = 0; i < system.GetNbChildren() - 1; ++i)
                {
                    var inModel = system.GetChild(i);
                    var outModel = system.GetChild(i + 1);
                    var inPresenter = elements.OfType<VFXContextPresenter>().First(x => x.Model == inModel);
                    var outPresenter = elements.OfType<VFXContextPresenter>().First(x => x.Model == outModel);
                }
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

        public void SetModelContainer(VFXModelContainer container)
        {
            if (m_ModelContainer != container)
            {
                if (m_ModelContainer != null && !EditorUtility.IsPersistent(m_ModelContainer))
                    DestroyImmediate(m_ModelContainer);

                m_Elements.Clear();     
                m_ModelContainer = container;

                if (m_ModelContainer != null)
                    foreach (var model in m_ModelContainer.m_Roots)
                        AddPresentersFromModel(model);

                Debug.Log("SET MODEL CONTAINER TO " + (container == null ? "null" : container.ToString()));

                // Doesnt work for some reasons
                View.contentViewContainer.Touch(ChangeType.Repaint);
                //View.FrameAll();
            }
        }

        [SerializeField]
        private VFXModelContainer m_ModelContainer;

        private VFXView m_View; // Dont call directly as it is lazy initialized
    }
}
