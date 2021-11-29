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
        public static string GetPath(Scene scene, int state, bool createFolder)
            => GetPath(scene.path, scene.name, state, createFolder);

        public static string GetPath(string scenePath, string sceneName, int state, bool createFolder)
        {
            const string assetName = "ProbeVolumeData";

            string sceneDir = Path.GetDirectoryName(scenePath);
            string assetPath = Path.Combine(sceneDir, sceneName);
            if (createFolder && !UnityEditor.AssetDatabase.IsValidFolder(assetPath))
                UnityEditor.AssetDatabase.CreateFolder(sceneDir, sceneName);

            var fileName = assetName + "-" + state + ".asset";
            return Path.Combine(assetPath, fileName);
        }

        public static ProbeVolumeAsset CreateAsset(Scene scene, int state)
        {
            ProbeVolumeAsset asset = CreateInstance<ProbeVolumeAsset>();
            asset.m_AssetFullPath = GetPath(scene, state, true);

            UnityEditor.AssetDatabase.CreateAsset(asset, asset.m_AssetFullPath);
            return asset;
        }

        internal void StoreProfileData(ProbeReferenceVolumeProfile profile)
        {
            cellSizeInBricks = profile.cellSizeInBricks;
            simplificationLevels = profile.simplificationLevels;
            minDistanceBetweenProbes = profile.minDistanceBetweenProbes;
        }

        internal static bool Compatible(ProbeVolumeAsset a, ProbeVolumeAsset b)
        {
            if (a == null || b == null)
                return false;

            if (a.maxCellPosition != b.maxCellPosition) return false;
            if (a.minCellPosition != b.minCellPosition) return false;
            if (a.globalBounds != b.globalBounds) return false;
            if (a.bands != b.bands) return false;
            if (a.cellSizeInBricks != b.cellSizeInBricks) return false;
            if (a.minDistanceBetweenProbes != b.minDistanceBetweenProbes) return false;
            if (a.simplificationLevels != b.simplificationLevels) return false;
            if (a.cells.Count != b.cells.Count) return false;
            for (int i = 0; i < a.cells.Count; i++)
            {
                if (a.cells[i].position != b.cells[i].position) return false;
                if (a.cells[i].minSubdiv != b.cells[i].minSubdiv) return false;
                if (a.cells[i].indexChunkCount != b.cells[i].indexChunkCount) return false;
                if (a.cells[i].shChunkCount != b.cells[i].shChunkCount) return false;
                if (a.cells[i].bricks.Count != b.cells[i].bricks.Count) return false;
                if (a.cells[i].probePositions.Length != b.cells[i].probePositions.Length) return false;

                for (int j = 0; j < a.cells[i].bricks.Count; j++)
                {
                    if (a.cells[i].bricks[j].position != b.cells[i].bricks[j].position) return false;
                    if (a.cells[i].bricks[j].subdivisionLevel != b.cells[i].bricks[j].subdivisionLevel) return false;
                }
                for (int j = 0; j < a.cells[i].probePositions.Length; j++)
                {
                    if (a.cells[i].probePositions[j] != b.cells[i].probePositions[j]) return false;
                }
            }

            return true;
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
