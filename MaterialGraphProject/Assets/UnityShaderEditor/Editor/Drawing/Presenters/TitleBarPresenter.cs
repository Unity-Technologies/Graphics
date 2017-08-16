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
		MaterialGraphEditWindow m_Owner;

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

		public void Initialize(MaterialGraphEditWindow graphWindow)
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

		public static List<IGraphAsset> FindAssets()
		{
			var assets = new List<IGraphAsset>();
			List<string> guids = AssetDatabase.FindAssets(string.Format("t:MaterialGraphAsset", typeof(MaterialGraphAsset))).ToList();
			guids.AddRange( AssetDatabase.FindAssets(string.Format("t:MaterialSubGraphAsset", typeof(MaterialSubGraphAsset))));

			for( int i = 0; i < guids.Count; i++ )
			{
				string assetPath = AssetDatabase.GUIDToAssetPath( guids[i] );
				ScriptableObject asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>( assetPath );
				if( asset != null && EditorUtility.IsPersistent(asset) && asset is IGraphAsset)
				{
					assets.Add((IGraphAsset)asset);
				}
			}
			return assets;
		}

        void UpdateAsset()
        {
			m_Owner.UpdateAsset ();
        }

		void ToggleTime()
		{
			m_Owner.ToggleRequiresTime();
		}
    }
}
