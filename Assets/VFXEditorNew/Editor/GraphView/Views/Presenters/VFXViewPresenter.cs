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
        protected new void OnEnable()
        {
            base.OnEnable();
            SetModelContainer(m_ModelContainer != null ? m_ModelContainer : CreateInstance<VFXModelContainer>());
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
                presenter.Init(context);
                presenter.position = new Rect(context.Position.x, context.Position.y, 100, 100);
                AddElement(presenter);
                presenter.m_view = View;
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
