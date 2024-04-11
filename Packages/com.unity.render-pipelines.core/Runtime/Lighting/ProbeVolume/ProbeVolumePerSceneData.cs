using System;
using System.Collections.Generic;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Rendering
{
    /// <summary>
    /// A component that stores baked probe volume state and data references. Normally hidden in the hierarchy.
    /// </summary>
    [ExecuteAlways]
    [AddComponentMenu("")] // Hide.
    public class ProbeVolumePerSceneData : MonoBehaviour
    {
        /// <summary>The baking set this scene is part of.</summary>
        public ProbeVolumeBakingSet bakingSet => serializedBakingSet;

        [SerializeField, FormerlySerializedAs("bakingSet")] internal ProbeVolumeBakingSet serializedBakingSet;
        [SerializeField] internal string sceneGUID = "";

        // All code bellow is only kept in order to be able to cleanup obsolete data.
        [Serializable]
        internal struct ObsoletePerScenarioData
        {
            public int sceneHash;
            public TextAsset cellDataAsset; // Contains L0 L1 SH data
            public TextAsset cellOptionalDataAsset; // Contains L2 SH data
        }

        [Serializable]
        struct ObsoleteSerializablePerScenarioDataItem
        {
#pragma warning disable 649 // is never assigned to, and will always have its default value
            public string scenario;
            public ObsoletePerScenarioData data;
#pragma warning restore 649
        }

        [FormerlySerializedAs("asset")]
        [SerializeField] internal ObsoleteProbeVolumeAsset obsoleteAsset;
        [FormerlySerializedAs("cellSharedDataAsset")]
        [SerializeField] internal TextAsset obsoleteCellSharedDataAsset; // Contains bricks and validity data
        [FormerlySerializedAs("cellSupportDataAsset")]
        [SerializeField] internal TextAsset obsoleteCellSupportDataAsset; // Contains debug data
        [FormerlySerializedAs("serializedScenarios")]
        [SerializeField] List<ObsoleteSerializablePerScenarioDataItem> obsoleteSerializedScenarios = new();

#if UNITY_EDITOR
        void DeleteAsset(Object asset)
        {
            if (asset != null && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string guid, out long instanceID))
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                AssetDatabase.DeleteAsset(assetPath);
            }
        }
#endif

        internal void Clear()
        {
            QueueSceneRemoval();
            serializedBakingSet = null;

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }

        internal void QueueSceneLoading()
        {
            if (serializedBakingSet == null)
                return;

            var refVol = ProbeReferenceVolume.instance;
            refVol.AddPendingSceneLoading(sceneGUID, serializedBakingSet);
        }

        internal void QueueSceneRemoval()
        {
            if (serializedBakingSet != null)
                ProbeReferenceVolume.instance.AddPendingSceneRemoval(sceneGUID);
        }

        void OnEnable()
        {
            ProbeReferenceVolume.instance.RegisterPerSceneData(this);
        }

        void OnDisable()
        {
            QueueSceneRemoval();
            ProbeReferenceVolume.instance.UnregisterPerSceneData(this);
        }

        private void OnValidate()
        {
#if UNITY_EDITOR
            // Cleanup old obsolete data.
            if (obsoleteAsset != null)
            {
                DeleteAsset(obsoleteAsset);
                DeleteAsset(obsoleteCellSharedDataAsset);
                DeleteAsset(obsoleteCellSupportDataAsset);
                foreach(var scenario in obsoleteSerializedScenarios)
                {
                    DeleteAsset(scenario.data.cellDataAsset);
                    DeleteAsset(scenario.data.cellOptionalDataAsset);
                }

                obsoleteAsset = null;
                obsoleteCellSharedDataAsset = null;
                obsoleteCellSupportDataAsset = null;
                obsoleteSerializedScenarios = null;

                EditorUtility.SetDirty(this);
            }
#endif
        }

        internal void Initialize()
        {
            ProbeReferenceVolume.instance.RegisterBakingSet(this);

            QueueSceneRemoval();
            QueueSceneLoading();
        }

        internal bool ResolveCellData()
        {
            if (serializedBakingSet != null)
                return serializedBakingSet.ResolveCellData(serializedBakingSet.GetSceneCellIndexList(sceneGUID));

            return false;
        }
    }
}
