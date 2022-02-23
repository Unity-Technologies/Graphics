using System;
using System.IO;
using UnityEngine.SceneManagement;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.Rendering
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
            BinaryTextureData,
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

        [SerializeField] internal int chunkSizeInBricks;

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
            public int chunksCount;

            public void Add(CellCounts o)
            {
                bricksCount += o.bricksCount;
                probesCount += o.probesCount;
                offsetsCount += o.offsetsCount;
                chunksCount += o.chunksCount;
            }
        }

        internal const int kL0L1ScalarCoefficientsCount = 12;
        internal const int kL2ScalarCoefficientsCount = 15;

        internal int maxSubdivision => simplificationLevels + 1; // we add one for the top subdiv level which is the same size as a cell
        internal float minBrickSize => Mathf.Max(0.01f, minDistanceBetweenProbes * 3.0f);

        internal bool CompatibleWith(ProbeVolumeAsset otherAsset)
        {
            return (maxSubdivision == otherAsset.maxSubdivision) && (minBrickSize == otherAsset.minBrickSize) && (cellSizeInBricks == otherAsset.cellSizeInBricks)
                && (chunkSizeInBricks == otherAsset.chunkSizeInBricks);
        }

        public string GetSerializedFullPath()
        {
            return m_AssetFullPath;
        }

        static int AlignUp16(int count)
        {
            var alignment = 16;
            var remainder = count % alignment;
            return count + (remainder == 0 ? 0 : alignment - remainder);
        }


        // The unpacking in Resolve functions is the "inverse" of ProbeBakingGI.WriteBakingCells flattening
        internal bool ResolveSharedCellData(TextAsset cellSharedDataAsset, TextAsset cellSupportDataAsset)
        {
            if (cellSharedDataAsset == null)
                return false;

            var chunkSizeInProbeCount = chunkSizeInBricks * ProbeBrickPool.kBrickProbeCountTotal;

            var cellSharedData = cellSharedDataAsset.GetData<byte>();
            var bricksByteCount = totalCellCounts.bricksCount * UnsafeUtility.SizeOf<ProbeBrickIndex.Brick>();

            var validityOldByteStart = AlignUp16(bricksByteCount);
            var validityOldByteCount = totalCellCounts.probesCount * UnsafeUtility.SizeOf<uint>();

            var validityNeighMaskByteStart = AlignUp16(validityOldByteStart + validityOldByteCount);
            var validityNeighMaskByteCount = totalCellCounts.chunksCount * chunkSizeInProbeCount * UnsafeUtility.SizeOf<byte>();

            if ((validityNeighMaskByteStart + validityNeighMaskByteCount) != cellSharedData.Length)
                return false;

            var bricksData = cellSharedData.GetSubArray(0, bricksByteCount).Reinterpret<ProbeBrickIndex.Brick>(1);
            var validityOldData = cellSharedData.GetSubArray(validityOldByteStart, validityOldByteCount).Reinterpret<uint>(1);
            var validityNeighMaskData = cellSharedData.GetSubArray(validityNeighMaskByteStart, validityNeighMaskByteCount).Reinterpret<byte>(1);

            var cellSupportData = cellSupportDataAsset ? cellSupportDataAsset.GetData<byte>() : default;
            var hasSupportData = cellSupportData.IsCreated;

            var positionsOldByteCount = totalCellCounts.probesCount * UnsafeUtility.SizeOf<Vector3>();
            var positionsByteStart = positionsOldByteCount;
            var positionsByteCount = totalCellCounts.chunksCount * chunkSizeInProbeCount * UnsafeUtility.SizeOf<Vector3>();
            var touchupInteractionStart = AlignUp16(positionsByteStart + positionsByteCount);
            var touchupInteractionByteCount = totalCellCounts.chunksCount * chunkSizeInProbeCount * UnsafeUtility.SizeOf<float>();
            var touchupInteractionOldStart = AlignUp16(touchupInteractionStart + touchupInteractionByteCount);
            var touchupInteractionOldByteCount = totalCellCounts.probesCount * UnsafeUtility.SizeOf<float>();
            var validityByteStart = AlignUp16(touchupInteractionOldStart + touchupInteractionOldByteCount);
            var validityByteCount = totalCellCounts.chunksCount * chunkSizeInProbeCount * UnsafeUtility.SizeOf<float>();
            var offsetOldByteStart = AlignUp16(validityByteStart + validityByteCount);
            var offsetOldByteCount = totalCellCounts.offsetsCount * UnsafeUtility.SizeOf<Vector3>();
            var offsetByteStart = AlignUp16(offsetOldByteStart + offsetOldByteCount);
            var offsetByteCount = totalCellCounts.chunksCount * chunkSizeInProbeCount * UnsafeUtility.SizeOf<Vector3>();

            if (hasSupportData && offsetByteStart + offsetByteCount != cellSupportData.Length)
                return false;
            var positionsOldData = hasSupportData ? cellSupportData.GetSubArray(0, positionsOldByteCount).Reinterpret<Vector3>(1) : default;
            var positionsData = hasSupportData ? cellSupportData.GetSubArray(positionsByteStart, positionsByteCount).Reinterpret<Vector3>(1) : default;
            var touchupInteractionData = hasSupportData ? cellSupportData.GetSubArray(touchupInteractionStart, touchupInteractionByteCount).Reinterpret<float>(1) : default;
            var touchupInteractionOldData = hasSupportData ? cellSupportData.GetSubArray(touchupInteractionOldStart, touchupInteractionOldByteCount).Reinterpret<float>(1) : default;
            var validityData = hasSupportData ? cellSupportData.GetSubArray(validityByteStart, validityByteCount).Reinterpret<float>(1) : default;
            var offsetsDataOld = hasSupportData ? cellSupportData.GetSubArray(offsetOldByteStart, offsetOldByteCount).Reinterpret<Vector3>(1) : default;
            var offsetsData = hasSupportData ? cellSupportData.GetSubArray(offsetByteStart, offsetByteCount).Reinterpret<Vector3>(1) : default;

            var startCounts = new CellCounts();
            for (var i = 0; i < cells.Length; ++i)
            {
                var cell = cells[i];
                var counts = cellCounts[i];

                cell.bricks = bricksData.GetSubArray(startCounts.bricksCount, counts.bricksCount);
                cell.validityOld = validityOldData.GetSubArray(startCounts.probesCount, counts.probesCount);
                cell.validityNeighMaskData = validityNeighMaskData.GetSubArray(startCounts.chunksCount * chunkSizeInProbeCount, counts.chunksCount * chunkSizeInProbeCount);

                if (hasSupportData)
                {
                    cell.probePositions = positionsData.GetSubArray(startCounts.chunksCount * chunkSizeInProbeCount, counts.chunksCount * chunkSizeInProbeCount);
                    cell.probePositionsOld = positionsOldData.GetSubArray(startCounts.probesCount, counts.probesCount);
                    cell.touchupVolumeInteraction = touchupInteractionData.GetSubArray(startCounts.chunksCount * chunkSizeInProbeCount, counts.chunksCount * chunkSizeInProbeCount);
                    cell.touchupVolumeInteractionOld = touchupInteractionOldData.GetSubArray(startCounts.probesCount, counts.probesCount);
                    cell.offsetVectors = offsetsData.GetSubArray(startCounts.chunksCount * chunkSizeInProbeCount, counts.chunksCount * chunkSizeInProbeCount);
                    cell.offsetVectorsOld = offsetsDataOld.GetSubArray(startCounts.offsetsCount, counts.offsetsCount);
                    cell.validity = validityData.GetSubArray(startCounts.chunksCount * chunkSizeInProbeCount, counts.chunksCount * chunkSizeInProbeCount);
                }

                startCounts.Add(counts);
            }

            return true;
        }

        internal bool ResolvePerScenarioCellData(TextAsset cellDataAsset, TextAsset cellOptionalDataAsset, int stateIndex)
        {
            if (cellDataAsset == null)
                return false;

            var chunkSizeInProbeCount = chunkSizeInBricks * ProbeBrickPool.kBrickProbeCountTotal;

            // L0L1 data
            var cellData = cellDataAsset.GetData<byte>();

            var shL0L1DataByteCountOld = totalCellCounts.probesCount * UnsafeUtility.SizeOf<float>() * kL0L1ScalarCoefficientsCount;
            /// 3 4 component textures, 1 float and 2 bytes. Aligned on the size of a chunk.
            var shL0R1xDataByteCount = totalCellCounts.chunksCount * chunkSizeInProbeCount * 4 * UnsafeUtility.SizeOf<ushort>();
            var shL1GR1yDataByteCount = totalCellCounts.chunksCount * chunkSizeInProbeCount * 4 * UnsafeUtility.SizeOf<byte>();
            var shL1B1zDataByteCount = totalCellCounts.chunksCount * chunkSizeInProbeCount * 4 * UnsafeUtility.SizeOf<byte>();

            if ((shL0L1DataByteCountOld + shL0R1xDataByteCount + shL1GR1yDataByteCount + shL1B1zDataByteCount) != cellData.Length)
                return false;

            var offset = 0;
            var shL0L1DataOld = cellData.GetSubArray(0, shL0L1DataByteCountOld).Reinterpret<float>(1);
            offset += shL0L1DataByteCountOld;
            var shL0L1RxData = cellData.GetSubArray(offset, shL0R1xDataByteCount).Reinterpret<ushort>(1);
            offset += shL0R1xDataByteCount;
            var shL1GL1RyData = cellData.GetSubArray(offset, shL1GR1yDataByteCount).Reinterpret<byte>(1);
            offset += shL1GR1yDataByteCount;
            var shL1BL1RzData = cellData.GetSubArray(offset, shL1B1zDataByteCount).Reinterpret<byte>(1);
            offset += shL1B1zDataByteCount;


            // Optional L2 data
            var cellOptionalData = cellOptionalDataAsset ? cellOptionalDataAsset.GetData<byte>() : default;
            var hasOptionalData = cellOptionalData.IsCreated;
            var shL2DataByteCountOld = totalCellCounts.probesCount * UnsafeUtility.SizeOf<float>() * kL2ScalarCoefficientsCount;
            var shL2DataByteCount = totalCellCounts.chunksCount * chunkSizeInProbeCount * 4 * UnsafeUtility.SizeOf<byte>();
            if (hasOptionalData && (4 * shL2DataByteCount + shL2DataByteCountOld + 3 * UnsafeUtility.SizeOf<float>()) != cellOptionalData.Length)
                return false;

            offset = 0;
            var shL2DataOld = hasOptionalData ? cellOptionalData.GetSubArray(offset, shL2DataByteCountOld).Reinterpret<float>(1) : default;
            offset += shL2DataByteCountOld + 3 * UnsafeUtility.SizeOf<float>();
            var shL2Data_0 = hasOptionalData ? cellOptionalData.GetSubArray(offset, shL2DataByteCount).Reinterpret<byte>(1) : default;
            offset += shL2DataByteCount;
            var shL2Data_1 = hasOptionalData ? cellOptionalData.GetSubArray(offset, shL2DataByteCount).Reinterpret<byte>(1) : default;
            offset += shL2DataByteCount;
            var shL2Data_2 = hasOptionalData ? cellOptionalData.GetSubArray(offset, shL2DataByteCount).Reinterpret<byte>(1) : default;
            offset += shL2DataByteCount;
            var shL2Data_3 = hasOptionalData ? cellOptionalData.GetSubArray(offset, shL2DataByteCount).Reinterpret<byte>(1) : default;

            var startCounts = new CellCounts();
            for (var i = 0; i < cells.Length; ++i)
            {
                var counts = cellCounts[i];
                var cellState = new ProbeReferenceVolume.Cell.PerScenarioData();

                cellState.shL0L1Data = shL0L1DataOld.GetSubArray(startCounts.probesCount * kL0L1ScalarCoefficientsCount, counts.probesCount * kL0L1ScalarCoefficientsCount);

                cellState.shL0L1RxData = shL0L1RxData.GetSubArray(startCounts.chunksCount * chunkSizeInProbeCount * 4, counts.chunksCount * chunkSizeInProbeCount * 4);
                cellState.shL1GL1RyData = shL1GL1RyData.GetSubArray(startCounts.chunksCount * chunkSizeInProbeCount * 4, counts.chunksCount * chunkSizeInProbeCount * 4);
                cellState.shL1BL1RzData = shL1BL1RzData.GetSubArray(startCounts.chunksCount * chunkSizeInProbeCount * 4, counts.chunksCount * chunkSizeInProbeCount * 4);

                if (hasOptionalData)
                {
                    cellState.shL2Data = shL2DataOld.GetSubArray(startCounts.probesCount * kL2ScalarCoefficientsCount, counts.probesCount * kL2ScalarCoefficientsCount);

                    cellState.shL2Data_0 = shL2Data_0.GetSubArray(startCounts.chunksCount * chunkSizeInProbeCount * 4, counts.chunksCount * chunkSizeInProbeCount * 4);
                    cellState.shL2Data_1 = shL2Data_1.GetSubArray(startCounts.chunksCount * chunkSizeInProbeCount * 4, counts.chunksCount * chunkSizeInProbeCount * 4);
                    cellState.shL2Data_2 = shL2Data_2.GetSubArray(startCounts.chunksCount * chunkSizeInProbeCount * 4, counts.chunksCount * chunkSizeInProbeCount * 4);
                    cellState.shL2Data_3 = shL2Data_3.GetSubArray(startCounts.chunksCount * chunkSizeInProbeCount * 4, counts.chunksCount * chunkSizeInProbeCount * 4);
                }

                if (stateIndex == 0) cells[i].scenario0 = cellState;
                else cells[i].scenario1 = cellState;

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
