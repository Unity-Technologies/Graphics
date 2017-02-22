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
            if (objs != null && objs.Length == 1 && objs[0] is VFXGraphAsset)
            {
                m_ViewPresenter.SetGraphAsset(objs[0] as VFXGraphAsset, true);
            }
            else
                m_ViewPresenter.SetGraphAsset(m_ViewPresenter.GetGraphAsset(), true);
        }

        protected new void OnDisable()
        {
            m_ViewPresenter.SetGraphAsset(null,false);
            base.OnDisable();
        }

        void OnSelectionChange()
        {
            var objs = Selection.objects;
            if (objs != null && objs.Length == 1 && objs[0] is VFXGraphAsset)
            {
                m_ViewPresenter.SetGraphAsset(objs[0] as VFXGraphAsset, false);
            }
        }

        [SerializeField]
        private VFXViewPresenter m_ViewPresenter;
    }
}
