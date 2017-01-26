using System;
using RMGUI.GraphView;
using UnityEngine;

namespace UnityEditor.VFX.UI
{
    [Serializable]
    class VFXViewWindow : GraphViewEditorWindow
    {
        [MenuItem("Window/VFXEditorNew")]
        public static void ShowWindow()
        {
            GetWindow<VFXViewWindow>();
        }

        protected override GraphView BuildView()
        {
            BuildPresenters();
            return m_ViewPresenter.View;
        }

        protected override GraphViewPresenter BuildPresenters()
        {
            if (m_ViewPresenter == null)
                m_ViewPresenter = CreateInstance<VFXViewPresenter>();
            return m_ViewPresenter;
        }

        protected new void OnEnable()
        {
            base.OnEnable();
            var objs = Selection.objects;
            if (objs != null && objs.Length == 1 && objs[0] is VFXModelContainer)
            {
                m_ViewPresenter.SetModelContainer(objs[0] as VFXModelContainer, true);
            }
            else
                m_ViewPresenter.SetModelContainer(m_ViewPresenter.GetModelContainer(), true);
        }

        protected new void OnDisable()
        {
            m_ViewPresenter.SetModelContainer(null,false);
            base.OnDisable();
        }

        void OnSelectionChange()
        {
            var objs = Selection.objects;
            if (objs != null && objs.Length == 1 && objs[0] is VFXModelContainer)
            {
                m_ViewPresenter.SetModelContainer(objs[0] as VFXModelContainer, false);
            }
        }

        [SerializeField]
        private VFXViewPresenter m_ViewPresenter;
    }
}
