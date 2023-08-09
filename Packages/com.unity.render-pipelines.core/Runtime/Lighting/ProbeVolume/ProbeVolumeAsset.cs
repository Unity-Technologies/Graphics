using System;
using System.IO;
using UnityEngine.SceneManagement;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;

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

        internal int maxSubdivision => simplificationLevels + 1; // we add one for the top subdiv level which is the same size as a cell
        internal float minBrickSize => Mathf.Max(0.01f, minDistanceBetweenProbes * 3.0f);

        internal bool CompatibleWith(ProbeVolumeAsset otherAsset)
        {
            return (maxSubdivision == otherAsset.maxSubdivision) && (minBrickSize == otherAsset.minBrickSize) && (cellSizeInBricks == otherAsset.cellSizeInBricks)
                && (chunkSizeInBricks == otherAsset.chunkSizeInBricks);
        }

        internal bool IsInvalid()
        {
            return maxCellPosition.x < minCellPosition.x || maxCellPosition.y < minCellPosition.y || maxCellPosition.z < minCellPosition.z;
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

        NativeArray<T> GetSubArray<T>(NativeArray<byte> input, int count, ref int offset) where T : struct
        {
            var size = count * UnsafeUtility.SizeOf<T>();
            if (offset + size > input.Length)
                return default;

            var result = input.GetSubArray(offset, size).Reinterpret<T>(1);
            offset = AlignUp16(offset + size);
            return result;
        }

        // The unpacking in Resolve functions is the "inverse" of ProbeBakingGI.WriteBakingCells flattening
        internal bool ResolveSharedCellData(TextAsset cellSharedDataAsset, TextAsset cellSupportDataAsset)
        {
            if (cellSharedDataAsset == null)
                return false;

            var chunkSizeInProbeCount = chunkSizeInBricks * ProbeBrickPool.kBrickProbeCountTotal;
            var totalProbeCount = totalCellCounts.chunksCount * chunkSizeInProbeCount;

            // Shared Data
            var cellSharedData = cellSharedDataAsset.GetData<byte>();

            var offset = 0;
            var bricksData = GetSubArray<ProbeBrickIndex.Brick>(cellSharedData, totalCellCounts.bricksCount, ref offset);
            var validityNeighMaskData = GetSubArray<byte>(cellSharedData, totalProbeCount, ref offset);
            if (offset != AlignUp16(cellSharedData.Length))
                return false;

            // Support Data
            var cellSupportData = cellSupportDataAsset ? cellSupportDataAsset.GetData<byte>() : default;
            var hasSupportData = cellSupportData.IsCreated;

            offset = 0;
            var positionsData = hasSupportData ? GetSubArray<Vector3>(cellSupportData, totalProbeCount, ref offset) : default;
            var touchupInteractionData = hasSupportData ? GetSubArray<float>(cellSupportData, totalProbeCount, ref offset) : default;
            var validityData = hasSupportData ? GetSubArray<float>(cellSupportData, totalProbeCount, ref offset) : default;
            var offsetsData = hasSupportData ? GetSubArray<Vector3>(cellSupportData, totalProbeCount, ref offset) : default;

            if (hasSupportData && offset != AlignUp16(cellSupportData.Length))
                return false;

            // Resolve per cell
            var startCounts = new CellCounts();
            for (var i = 0; i < cells.Length; ++i)
            {
                var cell = cells[i];
                var counts = cellCounts[i];

                var chunksOffset = startCounts.chunksCount * chunkSizeInProbeCount;
                var chunksSize = counts.chunksCount * chunkSizeInProbeCount;

                cell.bricks = bricksData.GetSubArray(startCounts.bricksCount, counts.bricksCount);
                cell.validityNeighMaskData = validityNeighMaskData.GetSubArray(chunksOffset, chunksSize);

                if (hasSupportData)
                {
                    cell.probePositions = positionsData.GetSubArray(chunksOffset, chunksSize);
                    cell.touchupVolumeInteraction = touchupInteractionData.GetSubArray(chunksOffset, chunksSize);
                    cell.offsetVectors = offsetsData.GetSubArray(chunksOffset, chunksSize);
                    cell.validity = validityData.GetSubArray(chunksOffset, chunksSize);
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
            var totalProbeCount = totalCellCounts.chunksCount * chunkSizeInProbeCount;

            // L0L1 Data
            var cellData = cellDataAsset.GetData<byte>();

            /// 3 4 component textures, 1 half and 2 bytes. Aligned on the size of a chunk.
            var offset = 0;
            var shL0L1RxData = GetSubArray<ushort>(cellData, totalProbeCount * 4, ref offset);
            var shL1GL1RyData = GetSubArray<byte>(cellData, totalProbeCount * 4, ref offset);
            var shL1BL1RzData = GetSubArray<byte>(cellData, totalProbeCount * 4, ref offset);
            if (offset != AlignUp16(cellData.Length))
                return false;

            // Optional L2 data
            var cellOptionalData = cellOptionalDataAsset ? cellOptionalDataAsset.GetData<byte>() : default;
            var hasOptionalData = cellOptionalData.IsCreated;

            offset = 0;
            var shL2Data_0 = GetSubArray<byte>(cellOptionalData, totalProbeCount * 4, ref offset);
            var shL2Data_1 = GetSubArray<byte>(cellOptionalData, totalProbeCount * 4, ref offset);
            var shL2Data_2 = GetSubArray<byte>(cellOptionalData, totalProbeCount * 4, ref offset);
            var shL2Data_3 = GetSubArray<byte>(cellOptionalData, totalProbeCount * 4, ref offset);

            if (hasOptionalData && offset != AlignUp16(cellOptionalData.Length))
                return false;

            var startCounts = new CellCounts();
            for (var i = 0; i < cells.Length; ++i)
            {
                var counts = cellCounts[i];
                var cellState = new ProbeReferenceVolume.Cell.PerScenarioData();

                var chunksOffset = startCounts.chunksCount * chunkSizeInProbeCount * 4;
                var chunksSize = counts.chunksCount * chunkSizeInProbeCount * 4;

                cellState.shL0L1RxData = shL0L1RxData.GetSubArray(chunksOffset, chunksSize);
                cellState.shL1GL1RyData = shL1GL1RyData.GetSubArray(chunksOffset, chunksSize);
                cellState.shL1BL1RzData = shL1BL1RzData.GetSubArray(chunksOffset, chunksSize);

                if (hasOptionalData)
                {
                    cellState.shL2Data_0 = shL2Data_0.GetSubArray(chunksOffset, chunksSize);
                    cellState.shL2Data_1 = shL2Data_1.GetSubArray(chunksOffset, chunksSize);
                    cellState.shL2Data_2 = shL2Data_2.GetSubArray(chunksOffset, chunksSize);
                    cellState.shL2Data_3 = shL2Data_3.GetSubArray(chunksOffset, chunksSize);
                }

                if (stateIndex == 0)
                    cells[i].scenario0 = cellState;
                else
                    cells[i].scenario1 = cellState;

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
