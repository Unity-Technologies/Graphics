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

        public void Init(VFXModelContainer modelContainer)
        {
            m_ModelContainer = modelContainer;
        }

        public override void RemoveElement(GraphElementPresenter element)
        {
            base.RemoveElement(element);
            if (element is VFXContextPresenter)
                m_ModelContainer.m_Roots.Remove(((VFXContextPresenter)element).Model);

            EditorUtility.SetDirty(m_ModelContainer);
        }

        public void AddModel(VFXModel model, VFXView view)
        {
            m_ModelContainer.m_Roots.Add(model);
            AddPresentersFromModel(model,view);
            EditorUtility.SetDirty(m_ModelContainer);
        }

        private void AddPresentersFromModel(VFXModel model,VFXView view)
        {
            if (model is VFXContext)
            {
                VFXContext context = (VFXContext)model;
                var presenter = CreateInstance<VFXContextPresenter>();
                presenter.InitModel((VFXContext)model);
                presenter.position = new Rect(context.Position.x, context.Position.y, 100, 100);
                AddElement(presenter);
                presenter.m_view = view;
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
            }
        }

        [SerializeField]
        private VFXModelContainer m_ModelContainer;
    }
}
