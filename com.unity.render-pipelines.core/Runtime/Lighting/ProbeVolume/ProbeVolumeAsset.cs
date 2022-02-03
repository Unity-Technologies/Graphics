using System;
using System.IO;
using UnityEngine.SceneManagement;
using Unity.Collections.LowLevel.Unsafe;

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
        [SerializeField] internal int simplificationLevels;
        [SerializeField] internal float minDistanceBetweenProbes;

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

        internal bool ResolveCells(TextAsset cellDataAsset, TextAsset cellOptionalDataAsset, TextAsset cellSharedDataAsset, TextAsset cellSupportDataAsset)
        {
            if (cellDataAsset == null || cellSharedDataAsset == null)
                return false;

            // The unpacking here is the "inverse" of ProbeBakingGI.WriteBakingCells flattening

            static int AlignUp16(int count)
            {
                var alignment = 16;
                var remainder = count % alignment;
                return count + (remainder == 0 ? 0 : alignment - remainder);
            }

            var cellData = cellDataAsset.GetData<byte>();
            var shL0L1DataByteCount = totalCellCounts.probesCount * UnsafeUtility.SizeOf<float>() * kL0L1ScalarCoefficientsCount;
            var validityByteStart = AlignUp16(shL0L1DataByteCount);
            var validityByteCount = totalCellCounts.probesCount * UnsafeUtility.SizeOf<float>();

            if ((shL0L1DataByteCount + validityByteCount) != cellData.Length)
                return false;
            var shL0L1Data = cellData.GetSubArray(0, shL0L1DataByteCount).Reinterpret<float>(1);
            var validityData = cellData.GetSubArray(validityByteStart, validityByteCount).Reinterpret<float>(1);


            var cellOptionalData = cellOptionalDataAsset ? cellOptionalDataAsset.GetData<byte>() : default;
            var hasOptionalData = cellOptionalData.IsCreated;
            var shL2DataByteCount = totalCellCounts.probesCount * UnsafeUtility.SizeOf<float>() * kL2ScalarCoefficientsCount;
            if (hasOptionalData && (shL2DataByteCount + 3 * UnsafeUtility.SizeOf<float>()) != cellOptionalData.Length)
                return false;
            var shL2Data = hasOptionalData ? cellOptionalData.GetSubArray(0, shL2DataByteCount).Reinterpret<float>(1) : default;

            var cellSharedData = cellSharedDataAsset.GetData<byte>();
            var bricksByteCount = totalCellCounts.bricksCount * UnsafeUtility.SizeOf<ProbeBrickIndex.Brick>();
            if (bricksByteCount != cellSharedData.Length)
                return false;
            var bricksData = cellSharedData.GetSubArray(0, bricksByteCount).Reinterpret<ProbeBrickIndex.Brick>(1);

            var cellSupportData = cellSupportDataAsset ? cellSupportDataAsset.GetData<byte>() : default;
            var hasSupportData = cellSupportData.IsCreated;
            var positionsByteCount = totalCellCounts.probesCount * UnsafeUtility.SizeOf<Vector3>();
            var offsetByteStart = AlignUp16(positionsByteCount);
            var offsetByteCount = totalCellCounts.offsetsCount * UnsafeUtility.SizeOf<Vector3>();
            if (hasSupportData && offsetByteStart + offsetByteCount != cellSupportData.Length)
                return false;
            var positionsData = hasSupportData ? cellSupportData.GetSubArray(0, positionsByteCount).Reinterpret<Vector3>(1) : default;
            var offsetsData = hasSupportData ? cellSupportData.GetSubArray(offsetByteStart, offsetByteCount).Reinterpret<Vector3>(1) : default;

            var startCounts = new CellCounts();
            for (var i = 0; i < cells.Length; ++i)
            {
                var cell = cells[i];
                var counts = cellCounts[i];

                cell.bricks = bricksData.GetSubArray(startCounts.bricksCount, counts.bricksCount);
                cell.shL0L1Data = shL0L1Data.GetSubArray(startCounts.probesCount * kL0L1ScalarCoefficientsCount, counts.probesCount * kL0L1ScalarCoefficientsCount);
                cell.validity = validityData.GetSubArray(startCounts.probesCount, counts.probesCount);

                if (hasOptionalData)
                {
                    cell.shL2Data = shL2Data.GetSubArray(startCounts.probesCount * kL2ScalarCoefficientsCount, counts.probesCount * kL2ScalarCoefficientsCount);
                }

                if (hasSupportData)
                {
                    cell.probePositions = positionsData.GetSubArray(startCounts.probesCount, counts.probesCount);
                    cell.offsetVectors = offsetsData.GetSubArray(startCounts.offsetsCount, counts.offsetsCount);
                }

                startCounts.Add(counts);
            }

            return true;
        }

#if UNITY_EDITOR
        public void OnEnable()
        {
            m_AssetFullPath = UnityEditor.AssetDatabase.GetAssetPath(this);
        }

        internal const string assetName = "ProbeVolumeData";

        public static string GetPath(Scene scene)
            => Path.Combine(GetDirectory(scene.path, scene.name), assetName + ".asset");

        public static string GetDirectory(string scenePath, string sceneName)
        {
            string sceneDir = Path.GetDirectoryName(scenePath);
            string assetPath = Path.Combine(sceneDir, sceneName);
            if (!UnityEditor.AssetDatabase.IsValidFolder(assetPath))
                UnityEditor.AssetDatabase.CreateFolder(sceneDir, sceneName);

            return assetPath;
        }

        public static ProbeVolumeAsset CreateAsset(ProbeVolumePerSceneData data)
        {
            ProbeVolumeAsset asset = CreateInstance<ProbeVolumeAsset>();
            if (data.asset != null) asset.m_AssetFullPath = UnityEditor.AssetDatabase.GetAssetPath(data.asset);
            if (string.IsNullOrEmpty(asset.m_AssetFullPath)) asset.m_AssetFullPath = GetPath(data.gameObject.scene);

            UnityEditor.AssetDatabase.CreateAsset(asset, asset.m_AssetFullPath);
            return asset;
        }

        internal void StoreProfileData(ProbeReferenceVolumeProfile profile)
        {
            cellSizeInBricks = profile.cellSizeInBricks;
            simplificationLevels = profile.simplificationLevels;
            minDistanceBetweenProbes = profile.minDistanceBetweenProbes;
        }

        internal int GetBakingHashCode()
        {
            int hash = maxCellPosition.GetHashCode();
            hash = hash * 23 + minCellPosition.GetHashCode();
            hash = hash * 23 + globalBounds.GetHashCode();
            hash = hash * 23 + bands.GetHashCode();
            hash = hash * 23 + cellSizeInBricks.GetHashCode();
            hash = hash * 23 + simplificationLevels.GetHashCode();
            hash = hash * 23 + minDistanceBetweenProbes.GetHashCode();

            return hash;
        }
#endif
    }
}
