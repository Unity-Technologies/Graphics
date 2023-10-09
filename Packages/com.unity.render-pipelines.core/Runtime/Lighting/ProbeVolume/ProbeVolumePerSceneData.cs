using System;
using System.Collections.Generic;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Rendering
{
    /// <summary>
    /// A component that stores baked probe volume state and data references. Normally hidden from the user.
    /// </summary>
    [ExecuteAlways]
    [AddComponentMenu("")] // Hide.
    public class ProbeVolumePerSceneData : MonoBehaviour
    {
        [SerializeField] internal ProbeVolumeBakingSet bakingSet;
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
            bakingSet = null;

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }

        internal void QueueSceneLoading()
        {
            if (bakingSet == null)
                return;

            var refVol = ProbeReferenceVolume.instance;
            refVol.AddPendingSceneLoading(sceneGUID);
        }

        internal void QueueSceneRemoval()
        {
            if (bakingSet != null)
                ProbeReferenceVolume.instance.AddPendingSceneRemoval(sceneGUID);
        }

        void OnEnable()
        {
            ProbeReferenceVolume.instance.RegisterPerSceneData(this);

            if (ProbeReferenceVolume.instance.sceneData != null)
                Initialize();
        }

        void OnDisable()
        {
            ProbeReferenceVolume.instance.UnregisterPerSceneDataMigration(this);

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
            MigrateIfNeeded();

            ProbeReferenceVolume.instance.RegisterBakingSet(this);

            QueueSceneRemoval();
            QueueSceneLoading();
        }

        internal bool ResolveCellData()
        {
            if (bakingSet != null)
                return bakingSet.ResolveCellData(sceneGUID);

            return false;
        }

        internal void MigrateIfNeeded()
        {
#if UNITY_EDITOR
            if (ProbeReferenceVolume.instance.sceneData != null)
            {
                if (String.IsNullOrEmpty(sceneGUID))
                {
                    sceneGUID = ProbeVolumeSceneData.GetSceneGUID(gameObject.scene);
                    bakingSet = ProbeReferenceVolume.instance.sceneData.GetBakingSetForScene(sceneGUID);
                    EditorUtility.SetDirty(this);
                }
            }
            else
            {
                ProbeReferenceVolume.instance.RegisterPerSceneDataMigration(this);
            }
#endif
        }
    }
}
