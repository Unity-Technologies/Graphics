using System;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    /// <summary>
    /// Holds information about a subgraph.
    /// </summary>
    [Serializable]
    class Subgraph : ISerializationCallbackReceiver
    {
        [SerializeField]
        string m_AssetGUID;

        [SerializeField]
        long m_AssetLocalId;

        [SerializeField]
        int m_GraphAssetObjectInstanceID;

        [SerializeField]
        string m_Title;

        GraphModel m_GraphModel;

        /// <summary>
        /// The title of the subgraph.
        /// </summary>
        public string Title
        {
            get
            {
                if (GraphModel != null)
                {
                    m_Title = GraphModel.Name;
                    return m_Title;
                }
                return "! MISSING ! " + m_Title;
            }
        }

        /// <summary>
        /// The guid of the subgraph.
        /// </summary>
        public string AssetGuid => m_AssetGUID;

        /// <summary>
        /// The graph model of the subgraph.
        /// </summary>
        public GraphModel GraphModel
        {
            get
            {
                EnsureGraphAssetIsLoaded();
                SetReferenceGraphAsset();
                return m_GraphModel;
            }
            set
            {
                m_GraphModel = value;
                SetReferenceGraphAsset();
            }
        }

        void EnsureGraphAssetIsLoaded()
        {
            if (m_GraphModel?.Asset as Object == null)
            {
                m_GraphModel = null;
            }

            if (m_GraphModel != null)
                return;

            if (!string.IsNullOrEmpty(m_AssetGUID) && m_AssetLocalId != 0)
            {
                var graphAssetPath = AssetDatabase.GUIDToAssetPath(m_AssetGUID);
                if (TryLoad(graphAssetPath, m_AssetLocalId, m_AssetGUID, out var graphAsset))
                {
                    m_GraphModel = graphAsset.GraphModel as GraphModel;
                }
            }

            if (m_GraphModel == null && m_GraphAssetObjectInstanceID != 0)
            {
                var graphAsset = EditorUtility.InstanceIDToObject(m_GraphAssetObjectInstanceID) as GraphAsset;
                if (graphAsset != null)
                {
                    m_GraphModel = graphAsset.GraphModel as GraphModel;
                }
            }
        }

        void SetReferenceGraphAsset()
        {
            var asset = m_GraphModel?.Asset as Object;
            if (asset != null)
            {
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out m_AssetGUID, out m_AssetLocalId);
                m_GraphAssetObjectInstanceID = asset.GetInstanceID();
            }
        }

        static bool TryLoad(string path, long localFileId, string assetGuid, out GraphAsset graphAsset)
        {
            graphAsset = null;

            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var asset in assets)
            {
                if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out var guid, out long localId))
                    continue;

                // We want to load an asset with the same guid and localId
                if (assetGuid == guid && localId == localFileId)
                {
                    graphAsset = asset as GraphAsset;
                    return graphAsset != null;
                }
            }

            return false;
        }

        /// <inheritdoc />
        public void OnBeforeSerialize()
        {
            // Only save the object instance id for memory-based assets. This is needed for copy-paste operations.
            if (m_AssetGUID.Any(c => c != '0'))
            {
                m_GraphAssetObjectInstanceID = 0;
            }
        }

        /// <inheritdoc />
        public void OnAfterDeserialize()
        {
            m_GraphModel = null;
        }
    }
}
