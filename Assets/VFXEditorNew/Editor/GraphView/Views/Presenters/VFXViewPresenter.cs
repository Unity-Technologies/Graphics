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
                AddModel(new VFXContext(VFXContextDesc.CreateBasic(VFXContextDesc.Type.kTypeInit)));
                AddModel(new VFXContext(VFXContextDesc.CreateBasic(VFXContextDesc.Type.kTypeUpdate)));
                AddModel(new VFXContext(VFXContextDesc.CreateBasic(VFXContextDesc.Type.kTypeOutput)));
            }
        }

        public void Init(VFXModelContainer modelContainer)
        {
            m_ModelContainer = modelContainer;
        }

        public void AddModel(VFXModel model)
        {
            m_ModelContainer.m_Roots.Add(model);

            if (model is VFXContext)
            {
                var presenter = CreateInstance<VFXContextPresenter>();
                presenter.Model = (VFXContext)model;
                presenter.position = new Rect(0, 0, 256, 256);
                AddElement(presenter);
            }

            EditorUtility.SetDirty(m_ModelContainer);
        }

        VFXModelContainer m_ModelContainer;

    }
}
