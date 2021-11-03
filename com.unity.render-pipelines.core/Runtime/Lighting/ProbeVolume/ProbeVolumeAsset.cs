using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.SceneManagement;

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
            BinaryRuntimeDebugSplit,
            Max,
            Current = Max - 1
        }

        public int Version => m_Version;

        [SerializeField] protected internal int m_Version = (int)AssetVersion.Current;

        [SerializeField] internal ProbeReferenceVolume.Cell[] cells;
        [SerializeField] internal CellCounts[] cellCounts;
        [SerializeField] internal CellCounts totalCellCounts;

        [SerializeField] internal Vector3Int maxCellPosition;
        [SerializeField] internal Vector3Int minCellPosition;
        [SerializeField] internal Bounds globalBounds;

        [SerializeField] internal ProbeVolumeSHBands bands;

        internal int CoefficientVectorsCount => bands == ProbeVolumeSHBands.SphericalHarmonicsL2 ? 7 : 3;

        [SerializeField] string m_AssetFullPath = "UNINITIALIZED!";

        // Profile info
        [SerializeField] internal int cellSizeInBricks;
        [SerializeField] internal float minDistanceBetweenProbes;
        [SerializeField] internal int simplificationLevels;

        // Binary data (stored in ProbeVolumeSceneData)
        internal TextAsset cellDataAsset;
        internal TextAsset cellSupportDataAsset;

        [Serializable]
        internal struct CellCounts
        {
            public int bricksCount;
            public int probesCount;
            public int offsetsCount;

            public void Add(CellCounts o)
            {
                bricksCount += o.bricksCount;
                probesCount += o.probesCount;
                offsetsCount += o.offsetsCount;
            }
        }

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

        internal void ResolveCells()
        {
            // The unpacking here is the "inverse" of ProbeBakingGI.WriteBakingCells

            static int AlignUp16(int count)
            {
                var alignment = 16;
                var remainder = count % alignment;
                return count + (remainder == 0 ? 0 : alignment - remainder);
            }

            LoadBinaryData();

            var cellData = assetToBytes[cellDataAsset];
            var bricksByteCount = totalCellCounts.bricksCount * UnsafeUtility.SizeOf<ProbeBrickIndex.Brick>();
            var shDataByteStart = AlignUp16(bricksByteCount);
            var shDataByteCount = totalCellCounts.probesCount * UnsafeUtility.SizeOf<Vector4>() * CoefficientVectorsCount;
            var bricksData = cellData.GetSubArray(0, bricksByteCount).Reinterpret<ProbeBrickIndex.Brick>(1);
            var shData = cellData.GetSubArray( shDataByteStart, shDataByteCount).Reinterpret<Vector4>(1);

            var cellSupportData = cellSupportDataAsset ? assetToBytes[cellSupportDataAsset] : default;
            var hasSupportData = cellSupportData.IsCreated;
            var positionsByteCount = totalCellCounts.probesCount * UnsafeUtility.SizeOf<Vector3>();
            var validityByteStart = AlignUp16(positionsByteCount);
            var validityByteCount = totalCellCounts.probesCount * UnsafeUtility.SizeOf<float>();
            var offsetByteStart = AlignUp16(positionsByteCount) + AlignUp16(validityByteCount);
            var offsetByteCount = totalCellCounts.offsetsCount * UnsafeUtility.SizeOf<Vector3>();
            var positionsData = hasSupportData ? cellSupportData.GetSubArray(0, positionsByteCount).Reinterpret<Vector3>(1) : default;
            var validityData = hasSupportData ? cellSupportData.GetSubArray(validityByteStart, validityByteCount).Reinterpret<float>(1) : default;
            var offsetsData = hasSupportData ? cellSupportData.GetSubArray(offsetByteStart, offsetByteCount).Reinterpret<Vector3>(1) : default;

            var startCounts = new CellCounts();
            for (var i = 0; i < cells.Length; ++i)
            {
                var cell = cells[i];
                var counts = cellCounts[i];

                cell.bricks = bricksData.GetSubArray(startCounts.bricksCount, counts.bricksCount);
                cell.shData = shData.GetSubArray(startCounts.probesCount * CoefficientVectorsCount, counts.probesCount * CoefficientVectorsCount);

                if (hasSupportData)
                {
                    cell.probePositions = positionsData.GetSubArray(startCounts.probesCount, counts.probesCount);
                    cell.validity = validityData.GetSubArray(startCounts.probesCount, counts.probesCount);
                    cell.offsetVectors = offsetsData.GetSubArray(startCounts.offsetsCount, counts.offsetsCount);
                }

                startCounts.Add(counts);
            }
        }

        // TEMP SECTION: Manually extract a NA from bytes until we have access to: https://github.cds.internal.unity3d.com/unity/unity/pull/6458
        bool isBlobCreated;
        internal Dictionary<TextAsset, NativeArray<byte>> assetToBytes = new();
        internal void ReleaseBinaryData() => OnDisable();
        internal void LoadBinaryData() { OnEnable(); }
        void OnEnable()
        {
            if (!isBlobCreated && cellDataAsset)
            {
                assetToBytes[cellDataAsset] = new NativeArray<byte>(cellDataAsset.bytes, Allocator.Persistent);

                if(cellSupportDataAsset)
                    assetToBytes[cellSupportDataAsset] = new NativeArray<byte>(cellSupportDataAsset.bytes, Allocator.Persistent);

                isBlobCreated = true;
            }
        }

        void OnDisable()
        {
            isBlobCreated = false;

            foreach (var bytes in assetToBytes.Values)
                bytes.Dispose();

            assetToBytes.Clear();
        }
        // END TEMP SECTION

#if UNITY_EDITOR
        internal static string GetFileName(Scene scene)
        {
            string assetName = "ProbeVolumeData";

            String scenePath = scene.path;
            String sceneDir = System.IO.Path.GetDirectoryName(scenePath);
            String sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);

            String assetPath = System.IO.Path.Combine(sceneDir, sceneName);

            if (!UnityEditor.AssetDatabase.IsValidFolder(assetPath))
                UnityEditor.AssetDatabase.CreateFolder(sceneDir, sceneName);

            String assetFileName = UnityEditor.AssetDatabase.GenerateUniqueAssetPath(assetName + ".asset");

            assetFileName = System.IO.Path.Combine(assetPath, assetFileName);

            return assetFileName;
        }

        public static ProbeVolumeAsset CreateAsset(Scene scene)
        {
            ProbeVolumeAsset asset = ScriptableObject.CreateInstance<ProbeVolumeAsset>();
            string assetFileName = GetFileName(scene);

            UnityEditor.AssetDatabase.CreateAsset(asset, assetFileName);

            asset.m_AssetFullPath = assetFileName;

            return asset;
        }

        internal void StoreProfileData(ProbeReferenceVolumeProfile profile)
        {
            cellSizeInBricks = profile.cellSizeInBricks;
            simplificationLevels = profile.simplificationLevels;
            minDistanceBetweenProbes = profile.minDistanceBetweenProbes;
        }
#endif
    }
}
