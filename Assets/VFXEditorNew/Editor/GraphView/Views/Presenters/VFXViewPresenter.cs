using RMGUI.GraphView;
using UnityEngine;

namespace UnityEditor.VFX.UI
{
    class VFXViewPresenter : GraphViewPresenter
    {
        void OnEnable()
        {
            if (m_ModelContainer == null)
                SetModelContainer(CreateInstance<VFXModelContainer>());
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

        public void AddModel(Vector2 pos,VFXModel model)
        {
            m_ModelContainer.m_Roots.Add(model);
            AddPresentersFromModel(pos,model);
            EditorUtility.SetDirty(m_ModelContainer);
        }

        private void AddPresentersFromModel(Vector2 pos,VFXModel model)
        {
            if (model is VFXContext)
            {
                var presenter = CreateInstance<VFXContextPresenter>();
                presenter.Model = (VFXContext)model;
                presenter.position = new Rect(pos.x, pos.y, 256, 256);
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
                        AddPresentersFromModel(Vector2.zero, model);
            }
        }

        VFXModelContainer m_ModelContainer;
    }
}
