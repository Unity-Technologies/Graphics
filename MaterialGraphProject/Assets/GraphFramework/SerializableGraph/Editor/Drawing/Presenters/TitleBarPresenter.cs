using UnityEngine;
using UnityEngine.Graphing;
using System.Collections.Generic;
using UnityEngine.MaterialGraph;

namespace UnityEditor.Graphing.Drawing
{
    // TODO JOCE: Not sure the title bar requires a presenter at all.
    public class TitleBarPresenter : ScriptableObject
    {
        List<TitleBarButtonPresenter> m_leftItems;
        List<TitleBarButtonPresenter> m_rightItems;
        IGraphAsset m_graphAsset;

        public IEnumerable<TitleBarButtonPresenter> leftItems
        {
            get { return m_leftItems; }
        }

        public IEnumerable<TitleBarButtonPresenter> rightItems
        {
            get { return m_rightItems; }
        }

        protected TitleBarPresenter()
        {
        }

        public void Initialize(IGraphAsset graphAsset)
        {
            m_graphAsset = graphAsset;
            m_leftItems = new List<TitleBarButtonPresenter>();
            m_rightItems = new List<TitleBarButtonPresenter>();

            var currentGraphItem = CreateInstance<TitleBarButtonPresenter>();
            currentGraphItem.text = graphAsset != null ? graphAsset.GetScriptableObject().name : "";
            m_leftItems.Add(currentGraphItem);

            var updateAsset = CreateInstance<TitleBarButtonPresenter>();
            updateAsset.text = "Update Asset";
            updateAsset.onClick += UpdateAsset;
            m_leftItems.Add(updateAsset);

            var showInProjectItem = CreateInstance<TitleBarButtonPresenter>();
            showInProjectItem.text = "Show in project";
            showInProjectItem.onClick += OnShowInProjectClick;
            m_leftItems.Add(showInProjectItem);

            var optionsItem = CreateInstance<TitleBarButtonPresenter>();
            optionsItem.text = "Options";
            m_rightItems.Add(optionsItem);
        }

        void OnShowInProjectClick()
        {
            if (m_graphAsset != null)
                EditorGUIUtility.PingObject(m_graphAsset.GetScriptableObject());
        }

        void UpdateAsset()
        {
            if (m_graphAsset != null && m_graphAsset is MaterialGraphAsset)
            {
                var mg = (MaterialGraphAsset)m_graphAsset;
                mg.RegenerateInternalShader();
            }
        }
    }
}
