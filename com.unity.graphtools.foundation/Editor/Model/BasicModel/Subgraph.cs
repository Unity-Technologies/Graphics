using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    /// <summary>
    /// Holds information about a subgraph.
    /// </summary>
    [Serializable]
    class Subgraph
    {
        [SerializeReference]
        GraphAssetModel m_GraphAssetModel;

        [SerializeField]
        string m_AssetGUID;

        [SerializeField]
        long m_AssetLocalId;

        [SerializeField]
        int m_GraphAssetObjectInstanceID;

        [SerializeField]
        string m_Title;

        /// <summary>
        /// The title of the subgraph.
        /// </summary>
        public string Title
        {
            get
            {
                if (m_GraphAssetModel != null)
                {
                    m_Title = m_GraphAssetModel.Name;
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
        /// The graph asset model of the subgraph.
        /// </summary>
        public IGraphAssetModel GraphAssetModel
        {
            get
            {
                EnsureGraphAssetModelIsLoaded();
                SetReferenceGraphAssetModel();
                return m_GraphAssetModel;
            }
            set
            {
                m_GraphAssetModel = (GraphAssetModel)value;
                SetReferenceGraphAssetModel();
            }
        }

        void EnsureGraphAssetModelIsLoaded()
        {
            if (m_GraphAssetModel != null)
                return;

            if (!string.IsNullOrEmpty(m_AssetGUID) && m_AssetLocalId != 0)
            {
                var graphAssetModelPath = AssetDatabase.GUIDToAssetPath(m_AssetGUID);
                TryLoad(graphAssetModelPath, m_AssetLocalId, m_AssetGUID, out m_GraphAssetModel);
            }

            if (m_GraphAssetModel == null && m_GraphAssetObjectInstanceID != 0)
                m_GraphAssetModel = EditorUtility.InstanceIDToObject(m_GraphAssetObjectInstanceID) as GraphAssetModel;
        }

        void SetReferenceGraphAssetModel()
        {
            if (m_GraphAssetModel == null)
                return;

            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(m_GraphAssetModel, out m_AssetGUID, out m_AssetLocalId);
            m_GraphAssetObjectInstanceID = m_GraphAssetModel.GetInstanceID();
        }

        static bool TryLoad(string path, long localFileId, string assetGuid, out GraphAssetModel graphAssetModel)
        {
            graphAssetModel = null;

            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var asset in assets)
            {
                if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out var guid, out long localId))
                    continue;

                // We want to load an asset with the same guid and localId
                if (assetGuid == guid && localId == localFileId)
                    return asset as GraphAssetModel;
            }

            return graphAssetModel != null;
        }
    }
}
