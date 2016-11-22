using UnityEngine;
using UnityEngine.Graphing;
using System.Collections.Generic;

namespace UnityEditor.Graphing.Drawing
{
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
    }
}