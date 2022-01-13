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

        [SerializeField] string m_AssetFullPath = "UNINITIALIZED!";

        // Profile info
        [SerializeField] internal int cellSizeInBricks;
        [SerializeField] internal float minDistanceBetweenProbes;
        [SerializeField] internal int simplificationLevels;

        // Binary data (stored in ProbeVolumeSceneData, injected on load)
        internal TextAsset cellDataAsset;
        internal TextAsset cellOptionalDataAsset;
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

        internal const int kL0L1ScalarCoefficientsCount = 12;
        internal const int kL2ScalarCoefficientsCount = 15;

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

        internal bool ResolveCells()
        {
            if (cellDataAsset == null)
                return false;

            // The unpacking here is the "inverse" of ProbeBakingGI.WriteBakingCells flattening

            static int AlignUp16(int count)
            {
                var alignment = 16;
                var remainder = count % alignment;
                return count + (remainder == 0 ? 0 : alignment - remainder);
            }

            var cellData = cellDataAsset.GetData<byte>();
            var bricksByteCount = totalCellCounts.bricksCount * UnsafeUtility.SizeOf<ProbeBrickIndex.Brick>();
            var shL0L1DataByteStart = AlignUp16(bricksByteCount);
            var shL0L1DataByteCount = totalCellCounts.probesCount * UnsafeUtility.SizeOf<float>() * kL0L1ScalarCoefficientsCount;
            var bricksData = cellData.GetSubArray(0, bricksByteCount).Reinterpret<ProbeBrickIndex.Brick>(1);
            var shL0L1Data = cellData.GetSubArray(shL0L1DataByteStart, shL0L1DataByteCount).Reinterpret<float>(1);

            var cellOptionalData = cellOptionalDataAsset ? cellOptionalDataAsset.GetData<byte>() : default;
            var hasOptionalData = cellOptionalData.IsCreated;
            var shL2DataByteCount = totalCellCounts.probesCount * UnsafeUtility.SizeOf<float>() * kL2ScalarCoefficientsCount;
            var shL2Data = hasOptionalData ? cellOptionalData.GetSubArray(0, shL2DataByteCount).Reinterpret<float>(1) : default;

            var cellSupportData = cellSupportDataAsset ? cellSupportDataAsset.GetData<byte>() : default;
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
                cell.shL0L1Data = shL0L1Data.GetSubArray(startCounts.probesCount * kL0L1ScalarCoefficientsCount, counts.probesCount * kL0L1ScalarCoefficientsCount);

                if (hasOptionalData)
                {
                    cell.shL2Data = shL2Data.GetSubArray(startCounts.probesCount * kL2ScalarCoefficientsCount, counts.probesCount * kL2ScalarCoefficientsCount);
                }

                if (hasSupportData)
                {
                    cell.probePositions = positionsData.GetSubArray(startCounts.probesCount, counts.probesCount);
                    cell.validity = validityData.GetSubArray(startCounts.probesCount, counts.probesCount);
                    cell.offsetVectors = offsetsData.GetSubArray(startCounts.offsetsCount, counts.offsetsCount);
                }

                startCounts.Add(counts);
            }

            return true;
        }

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
