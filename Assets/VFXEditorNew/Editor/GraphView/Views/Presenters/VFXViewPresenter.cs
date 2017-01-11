using System;
using RMGUI.GraphView;
using UnityEngine;

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
                m_ModelContainer.m_Roots.Remove(((VFXContextPresenter)element).Model);

            EditorUtility.SetDirty(m_ModelContainer);
        }

        public void AddModel(VFXModel model)
        {
            m_ModelContainer.m_Roots.Add(model);
            AddPresentersFromModel(model);
            EditorUtility.SetDirty(m_ModelContainer);
        }

        private void AddPresentersFromModel(VFXModel model)
        {
            if (model is VFXContext)
            {
                VFXContext context = (VFXContext)model;
                var presenter = CreateInstance<VFXContextPresenter>();
                presenter.InitModel(context);
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

                // Doesnt work for some reasons
                //View.FrameAll();
            }
        }

        [SerializeField]
        private VFXModelContainer m_ModelContainer;

        private VFXView m_View; // Dont call directly as it is lazy initialized
    }
}
