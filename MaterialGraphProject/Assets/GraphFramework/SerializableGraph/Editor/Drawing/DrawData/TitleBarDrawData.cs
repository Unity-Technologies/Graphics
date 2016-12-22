using UnityEngine;
using UnityEngine.Graphing;
using System.Collections.Generic;
using UnityEngine.MaterialGraph;

namespace UnityEditor.Graphing.Drawing
{
    // TODO JOCE: Should most probably derive from GraphElementPresenter.
    public class TitleBarDrawData : ScriptableObject
    {
        List<TitleBarButtonDrawData> m_leftItems;
        List<TitleBarButtonDrawData> m_rightItems;
        IGraphAsset m_graphAsset;

        public IEnumerable<TitleBarButtonDrawData> leftItems
        {
            get { return m_leftItems; }
        }

        public IEnumerable<TitleBarButtonDrawData> rightItems
        {
            get { return m_rightItems; }
        }

        protected TitleBarDrawData()
        {
        }

        public void Initialize(IGraphAsset graphAsset)
        {
            m_graphAsset = graphAsset;
            m_leftItems = new List<TitleBarButtonDrawData>();
            m_rightItems = new List<TitleBarButtonDrawData>();

            var currentGraphItem = CreateInstance<TitleBarButtonDrawData>();
            currentGraphItem.text = graphAsset != null ? graphAsset.GetScriptableObject().name : "";
            m_leftItems.Add(currentGraphItem);

            var updateAsset = CreateInstance<TitleBarButtonDrawData>();
            updateAsset.text = "Update Asset";
            updateAsset.onClick += UpdateAsset;
            m_leftItems.Add(updateAsset);

            var showInProjectItem = CreateInstance<TitleBarButtonDrawData>();
            showInProjectItem.text = "Show in project";
            showInProjectItem.onClick += OnShowInProjectClick;
            m_leftItems.Add(showInProjectItem);

            var optionsItem = CreateInstance<TitleBarButtonDrawData>();
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
