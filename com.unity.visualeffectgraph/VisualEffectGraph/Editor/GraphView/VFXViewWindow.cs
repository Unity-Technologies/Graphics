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
            return viewPresenter.View;
        }

        protected override GraphViewPresenter BuildPresenters()
        {
            if (!string.IsNullOrEmpty(m_DisplayedAssetPath))
            {
                VFXAsset asset = AssetDatabase.LoadAssetAtPath<VFXAsset>(m_DisplayedAssetPath);
                viewPresenter.SetVFXAsset(asset, true);
            }
            return viewPresenter;
        }

        protected new void OnEnable()
        {
            base.OnEnable();
            var objs = Selection.objects;
            if (objs != null && objs.Length == 1 && objs[0] is VFXAsset)
            {
                viewPresenter.SetVFXAsset(objs[0] as VFXAsset, true);
            }
            else if (!string.IsNullOrEmpty(m_DisplayedAssetPath))
            {
                VFXAsset asset = AssetDatabase.LoadAssetAtPath<VFXAsset>(m_DisplayedAssetPath);

                viewPresenter.SetVFXAsset(asset, true);
            }
            else
                viewPresenter.SetVFXAsset(viewPresenter.GetVFXAsset(), true);
        }

        protected new void OnDisable()
        {
            viewPresenter.SetVFXAsset(null, false);
            base.OnDisable();
        }

        void OnSelectionChange()
        {
            var objs = Selection.objects;
            if (objs != null && objs.Length == 1 && objs[0] is VFXAsset)
            {
                m_DisplayedAssetPath = AssetDatabase.GetAssetPath(objs[0] as VFXAsset);
                viewPresenter.SetVFXAsset(objs[0] as VFXAsset, false);
            }
        }

        void Update()
        {
            var graph = viewPresenter.GetGraph();
            if (graph != null)
                graph.RecompileIfNeeded();
            viewPresenter.RecompileExpressionGraphIfNeeded();
        }

        [SerializeField]
        private string m_DisplayedAssetPath;

        static public VFXViewPresenter viewPresenter
        {
            get
            {
                if (s_ViewPresenter == null)
                    s_ViewPresenter = ScriptableObject.CreateInstance<VFXViewPresenter>();

                return s_ViewPresenter;
            }
        }

        static VFXViewPresenter s_ViewPresenter;
    }
}
