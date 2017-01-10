using RMGUI.GraphView;
using UnityEngine;

namespace UnityEditor.VFX.UI
{
    class VFXViewPresenter : GraphViewPresenter
    {
        void OnEnable()
        {
            if (m_ModelContainer == null)
            {
                m_ModelContainer = CreateInstance<VFXModelContainer>();
            }
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

            if (model is VFXContext)
            {
                var presenter = CreateInstance<VFXContextPresenter>();
                presenter.Model = (VFXContext)model;
                presenter.position = new Rect(pos.x, pos.y, 256, 256);
                AddElement(presenter);
            }

            EditorUtility.SetDirty(m_ModelContainer);
        }

        VFXModelContainer m_ModelContainer;

    }
}
