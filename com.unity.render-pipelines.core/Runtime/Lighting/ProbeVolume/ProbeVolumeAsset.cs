using System;
using System.IO;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

namespace UnityEngine.Experimental.Rendering
{
    [PreferBinarySerialization]
    internal class ProbeVolumeAsset : ScriptableObject
    {
        [Serializable]
        internal enum AssetVersion
        {
            First,
            AddProbeVolumesAtlasEncodingModes,
            PV2,
            ChunkBasedIndex,
            Max,
            Current = Max - 1
        }

        [SerializeField] protected internal int m_Version = (int)AssetVersion.Current;
        [SerializeField] public int Version { get => m_Version; }

        [SerializeField] internal List<ProbeReferenceVolume.Cell> cells = new List<ProbeReferenceVolume.Cell>();

        [SerializeField] internal Vector3Int maxCellPosition;
        [SerializeField] internal Vector3Int minCellPosition;
        [SerializeField] internal Bounds globalBounds;


        [SerializeField] internal ProbeVolumeSHBands bands;

        [SerializeField] string m_AssetFullPath = "UNINITIALIZED!";

        // Profile info
        [SerializeField] internal int cellSizeInBricks;
        [SerializeField] internal float minDistanceBetweenProbes;
        [SerializeField] internal int simplificationLevels;

#if UNITY_EDITOR
        [SerializeField] private int bakingHash;
#endif

        internal int maxSubdivision => simplificationLevels + 1; // we add one for the top subdiv level which is the same size as a cell
        internal float minBrickSize => Mathf.Max(0.01f, minDistanceBetweenProbes * 3.0f);

        internal bool CompatibleWith(ProbeVolumeAsset otherAsset)
        {
            return (maxSubdivision == otherAsset.maxSubdivision) && (minBrickSize == otherAsset.minBrickSize) && (cellSizeInBricks == otherAsset.cellSizeInBricks);
        }
        public string GetSerializedFullPath()
        {
            return m_AssetFullPath;
        }

#if UNITY_EDITOR
        public void OnEnable()
        {
            m_AssetFullPath = UnityEditor.AssetDatabase.GetAssetPath(this);
        }

        public static string GetPath(Scene scene, string state, bool createFolder)
            => GetPath(scene.path, scene.name, state, createFolder);

        public static string GetPath(string scenePath, string sceneName, string state, bool createFolder)
        {
            const string assetName = "ProbeVolumeData";

            string sceneDir = Path.GetDirectoryName(scenePath);
            string assetPath = Path.Combine(sceneDir, sceneName);
            if (createFolder && !UnityEditor.AssetDatabase.IsValidFolder(assetPath))
                UnityEditor.AssetDatabase.CreateFolder(sceneDir, sceneName);

            return Path.Combine(assetPath, assetName + "-" + state + ".asset");
        }

        public static ProbeVolumeAsset CreateAsset(Scene scene, string state)
        {
            ProbeVolumeAsset asset = CreateInstance<ProbeVolumeAsset>();
            asset.m_AssetFullPath = GetPath(scene, state, true);

            UnityEditor.AssetDatabase.CreateAsset(asset, asset.m_AssetFullPath);
            return asset;
        }

        public void Rename(Scene scene, string name)
        {
            var newPath = GetPath(scene, name, true);
            UnityEditor.AssetDatabase.MoveAsset(m_AssetFullPath, newPath);
        }

        internal void StoreProfileData(ProbeReferenceVolumeProfile profile)
        {
            cellSizeInBricks = profile.cellSizeInBricks;
            simplificationLevels = profile.simplificationLevels;
            minDistanceBetweenProbes = profile.minDistanceBetweenProbes;
        }

        internal void ComputeBakingHash()
        {
            int hash = maxCellPosition.GetHashCode();
            hash = hash * 23 + minCellPosition.GetHashCode();
            hash = hash * 23 + globalBounds.GetHashCode();
            hash = hash * 23 + bands.GetHashCode();
            hash = hash * 23 + cellSizeInBricks.GetHashCode();
            hash = hash * 23 + minDistanceBetweenProbes.GetHashCode();
            hash = hash * 23 + simplificationLevels.GetHashCode();
            foreach (var cell in cells)
                hash += cell.ComputeBakingHash();

            bakingHash = hash;
        }

        internal static bool Compatible(ProbeVolumeAsset a, ProbeVolumeAsset b)
        {
            if (a == null || b == null)
                return false;

            // Not completely accurate but good enough
            return a.bakingHash == b.bakingHash;
        }
#endif
    }

#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(ProbeVolumeAsset))]
    class ProbeVolumeAssetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {}
    }
#endif
}
