using System;
using UIElements.GraphView;
using UnityEngine;
using UnityEditor.VFX;
using UnityEditor;

namespace  UnityEditor.VFX.UI
{
    [Serializable]
    class VFXViewWindow : GraphViewEditorWindow
    {
        [MenuItem("Window/VFXEditor")]
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

            if (!string.IsNullOrEmpty(m_DisplayedAssetPath))
            {
                VFXAsset asset = AssetDatabase.LoadAssetAtPath<VFXAsset>(m_DisplayedAssetPath);
                m_ViewPresenter.SetVFXAsset(asset, true);
            }
            return m_ViewPresenter;
        }

        protected new void OnEnable()
        {
            base.OnEnable();
            var objs = Selection.objects;
            if (objs != null && objs.Length == 1 && objs[0] is VFXAsset)
            {
                m_ViewPresenter.SetVFXAsset(objs[0] as VFXAsset, true);
            }
            else if (!string.IsNullOrEmpty(m_DisplayedAssetPath))
            {
                VFXAsset asset = AssetDatabase.LoadAssetAtPath<VFXAsset>(m_DisplayedAssetPath);

                m_ViewPresenter.SetVFXAsset(asset, true);
            }
            else
                m_ViewPresenter.SetVFXAsset(m_ViewPresenter.GetVFXAsset(), true);
        }

        protected new void OnDisable()
        {
            m_ViewPresenter.SetVFXAsset(null, false);
            base.OnDisable();
        }

        void OnSelectionChange()
        {
            var objs = Selection.objects;
            if (objs != null && objs.Length == 1 && objs[0] is VFXAsset)
            {
                m_DisplayedAssetPath = AssetDatabase.GetAssetPath(objs[0] as VFXAsset);
                m_ViewPresenter.SetVFXAsset(objs[0] as VFXAsset, false);
            }
        }

        void Update()
        {
            var graph = m_ViewPresenter.GetGraph();
            if (graph != null)
                graph.RecompileIfNeeded();
        }

        [SerializeField]
        private string m_DisplayedAssetPath;

        [SerializeField]
        private VFXViewPresenter m_ViewPresenter;
    }
}
