using UnityEngine;
using UnityEngine.Graphing;
using System.Collections.Generic;
using UnityEngine.MaterialGraph;
using System.IO;
using System.Linq;

namespace UnityEditor.Graphing.Drawing
{
    // TODO JOCE: Not sure the title bar requires a presenter at all.
    public class TitleBarPresenter : ScriptableObject 
    {
        List<TitleBarButtonPresenter> m_leftItems;
        List<TitleBarButtonPresenter> m_rightItems;
		AbstractGraphEditWindow m_Owner;

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

		public void Initialize(AbstractGraphEditWindow graphWindow)
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

			var selectGraph = CreateInstance<TitleBarButtonPresenter>();
			selectGraph.text = "Select Graph";
			selectGraph.onClick += SelectGraph;
			m_leftItems.Add(selectGraph);

            var optionsItem = CreateInstance<TitleBarButtonPresenter>();
            optionsItem.text = "Options";
            m_rightItems.Add(optionsItem);
        }

		void OnShowInProjectClick()
		{
			if (m_Owner != null)
				m_Owner.PingAsset ();
		}

		class CallbackData
		{
			public IGraphAsset asset;
			public AbstractGraphEditWindow owner;
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

		void SelectGraph()
		{
			var options = FindAssets();
			var gm = new GenericMenu ();
			foreach (var option in options) {
				gm.AddItem (new GUIContent (AssetDatabase.GetAssetPath (option.GetScriptableObject())), false, Callback, new CallbackData(){asset = option, owner = m_Owner});
						
			}
			gm.ShowAsContext ();
		}

		void Callback(object userData)
		{
			if (!(userData is CallbackData))
				return;

			var cbData = (CallbackData)userData;
			cbData.owner.ChangeSelction (cbData.asset);
		}

        void UpdateAsset()
        {
			//TODO: We need two currently.. fix later
			m_Owner.UpdateAsset ();
			m_Owner.UpdateAsset ();
        }
    }
}
