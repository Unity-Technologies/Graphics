using UnityEngine;
using UnityEngine.Graphing;
using System.Collections.Generic;
using UnityEngine.MaterialGraph;
using System.IO;
using System.Linq;
using UnityEditor.MaterialGraph.Drawing;

namespace UnityEditor.MaterialGraph.Drawing
{
    // TODO JOCE: Not sure the title bar requires a presenter at all.
    public class TitleBarPresenter : ScriptableObject
    {
        List<TitleBarButtonPresenter> m_leftItems;
        List<TitleBarButtonPresenter> m_rightItems;
		IMaterialGraphEditWindow m_Owner;

        public IEnumerable<TitleBarButtonPresenter> leftItems
        {
            get { return m_leftItems; }
        }

        public IEnumerable<TitleBarButtonPresenter> rightItems
        {
            get { return m_rightItems; }
        }

        protected TitleBarPresenter()
        {}

		public void Initialize(IMaterialGraphEditWindow graphWindow)
        {
			m_Owner = graphWindow;
            m_leftItems = new List<TitleBarButtonPresenter>();
            m_rightItems = new List<TitleBarButtonPresenter>();

            var currentGraphItem = CreateInstance<TitleBarButtonPresenter>();
			currentGraphItem.text = "Put name here";
            m_leftItems.Add(currentGraphItem);

            var updateAsset = CreateInstance<TitleBarButtonPresenter>();
            updateAsset.text = "Update Asset";
            updateAsset.onClick += UpdateAsset;
            m_leftItems.Add(updateAsset);

            var toSubGraph = CreateInstance<TitleBarButtonPresenter>();
            toSubGraph.text = "Selection -> Subgraph";
            toSubGraph.onClick += ToSubGraph;
            m_leftItems.Add(toSubGraph);

            var showInProjectItem = CreateInstance<TitleBarButtonPresenter>();
			showInProjectItem.text = "Show in project";
			showInProjectItem.onClick += OnShowInProjectClick;
			m_leftItems.Add(showInProjectItem);

            var optionsItem = CreateInstance<TitleBarButtonPresenter>();
			optionsItem.text = "Time";
			optionsItem.onClick += ToggleTime;
            m_rightItems.Add(optionsItem);
        }

		void OnShowInProjectClick()
		{
			if (m_Owner != null)
				m_Owner.PingAsset ();
		}

        void UpdateAsset()
        {
			m_Owner.UpdateAsset ();
        }

        void ToSubGraph()
        {
            m_Owner.ToSubGraph();
        }

		void ToggleTime()
		{
			m_Owner.ToggleRequiresTime();
		}
    }
}
