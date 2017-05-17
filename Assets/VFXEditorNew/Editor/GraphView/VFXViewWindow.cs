using System;
using UIElements.GraphView;
using UnityEngine;
using UnityEditor.VFX.UI;
using UnityEditor.VFX;
using UnityEditor;

namespace  UnityEditor.VFX.UI
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

            if (!string.IsNullOrEmpty(m_DisplayedAssetPath))
            {
                VFXGraphAsset asset = AssetDatabase.LoadAssetAtPath<VFXGraphAsset>(m_DisplayedAssetPath);
                m_ViewPresenter.SetGraphAsset(asset, true);
            }
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
            else if (!string.IsNullOrEmpty(m_DisplayedAssetPath))
            {
                VFXGraphAsset asset = AssetDatabase.LoadAssetAtPath<VFXGraphAsset>(m_DisplayedAssetPath);

                m_ViewPresenter.SetGraphAsset(asset, true);
            }
            else
                m_ViewPresenter.SetGraphAsset(m_ViewPresenter.GetGraphAsset(), true);
        }

        protected new void OnDisable()
        {
            m_ViewPresenter.SetGraphAsset(null, false);
            base.OnDisable();
        }

        void OnSelectionChange()
        {
            var objs = Selection.objects;
            if (objs != null && objs.Length == 1 && objs[0] is VFXGraphAsset)
            {
                m_DisplayedAssetPath = AssetDatabase.GetAssetPath(objs[0] as VFXGraphAsset);
                m_ViewPresenter.SetGraphAsset(objs[0] as VFXGraphAsset, false);
            }
        }

        void Update()
        {
            var graphAsset = m_ViewPresenter.GetGraphAsset();
            if (graphAsset != null)
                graphAsset.RecompileIfNeeded();
        }

        [SerializeField]
        private string m_DisplayedAssetPath;

        [SerializeField]
        private VFXViewPresenter m_ViewPresenter;
    }
}
