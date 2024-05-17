using System.Linq;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.SceneManagement;
using UnityEditor;

using Brick = UnityEngine.Rendering.ProbeBrickIndex.Brick;

namespace UnityEngine.Rendering
{
    class ProbeVolumeProfileInfo
    {
        public int simplificationLevels;
        public float minDistanceBetweenProbes;
        public Vector3 probeOffset;

        public int maxSubdivision => ProbeVolumeBakingSet.GetMaxSubdivision(simplificationLevels);
        public float minBrickSize => ProbeVolumeBakingSet.GetMinBrickSize(minDistanceBetweenProbes);
        public int cellSizeInBricks => ProbeVolumeBakingSet.GetCellSizeInBricks(simplificationLevels);
        public float cellSizeInMeters => (float)cellSizeInBricks * minBrickSize;

        public Vector3Int PositionToCell(Vector3 position) => Vector3Int.FloorToInt((position - probeOffset) / cellSizeInMeters);
    }

    public partial class AdaptiveProbeVolumes
    {
        static internal ProbeVolumeProfileInfo m_ProfileInfo = null;

        static void FindWorldBounds()
        {
            var prv = ProbeReferenceVolume.instance;
            prv.clearAssetsOnVolumeClear = true;

            var activeScene = SceneManager.GetActiveScene();
            var activeSet = ProbeVolumeBakingSet.GetBakingSetForScene(activeScene);

            bool hasFoundBounds = false;

            foreach (var sceneGUID in activeSet.sceneGUIDs)
            {
                var bakeData = activeSet.GetSceneBakeData(sceneGUID);
                if (bakeData.hasProbeVolume)
                {
                    if (hasFoundBounds)
                    {
                        globalBounds.Encapsulate(bakeData.bounds);
                    }
                    else
                    {
                        globalBounds = bakeData.bounds;
                        hasFoundBounds = true;
                    }
                }
            }

            ProbeReferenceVolume.instance.globalBounds = globalBounds;
        }

        static List<ProbeVolumePerSceneData> GetPerSceneDataList()
        {
            var fullPerSceneDataList = ProbeReferenceVolume.instance.perSceneDataList;
            if (!isBakingSceneSubset)
                return fullPerSceneDataList;

            List<ProbeVolumePerSceneData> usedPerSceneDataList = new ();
            foreach (var sceneData in fullPerSceneDataList)
            {
                if (partialBakeSceneList.Contains(ProbeReferenceVolume.GetSceneGUID(sceneData.gameObject.scene)))
                    usedPerSceneDataList.Add(sceneData);
            }
            return usedPerSceneDataList;
        }

        internal static List<ProbeVolume> GetProbeVolumeList()
        {
            var fullPvList = GameObject.FindObjectsByType<ProbeVolume>(FindObjectsSortMode.InstanceID);
            List<ProbeVolume> usedPVList;

            if (isBakingSceneSubset)
            {
                usedPVList = new List<ProbeVolume>();
                foreach (var pv in fullPvList)
                {
                    if (pv.isActiveAndEnabled && partialBakeSceneList.Contains(ProbeReferenceVolume.GetSceneGUID(pv.gameObject.scene)))
                        usedPVList.Add(pv);
                }
            }
            else
            {
                usedPVList = new List<ProbeVolume>(fullPvList);
            }

            return usedPVList;
        }

        static ProbeVolumeProfileInfo GetProfileInfoFromBakingSet(ProbeVolumeBakingSet set)
        {
            var result = new ProbeVolumeProfileInfo();
            result.minDistanceBetweenProbes = set.minDistanceBetweenProbes;
            result.simplificationLevels = set.simplificationLevels;
            result.probeOffset = set.probeOffset;
            return result;
        }

        static int PosToIndex(Vector3Int pos)
        {
            Vector3Int normalizedPos = pos - minCellPosition;
            return normalizedPos.z * (cellCount.x * cellCount.y) + normalizedPos.y * cellCount.x + normalizedPos.x;
        }

        static internal bool CanFreezePlacement()
        {
            if (!ProbeReferenceVolume.instance.supportLightingScenarios)
                return false;

            // Check if all the scene datas in the scene have a baking set, if  not then we cannot enable this option.
            var sceneDataList = GetPerSceneDataList();
            if (sceneDataList.Count == 0)
                return false;

            foreach (var sceneData in sceneDataList)
            {
                if (sceneData.serializedBakingSet == null || sceneData.serializedBakingSet.GetSceneCellIndexList(sceneData.sceneGUID) == null)
                    return false;
            }

            return true;
        }

        static NativeList<Vector3> RunPlacement()
        {
            // Overwrite loaded settings with data from profile. Note that the m_BakingSet.profile is already patched up if isFreezingPlacement
            float prevBrickSize = ProbeReferenceVolume.instance.MinBrickSize();
            int prevMaxSubdiv = ProbeReferenceVolume.instance.GetMaxSubdivision();
            Vector3 prevOffset = ProbeReferenceVolume.instance.ProbeOffset();
            ProbeReferenceVolume.instance.SetSubdivisionDimensions(m_ProfileInfo.minBrickSize, m_ProfileInfo.maxSubdivision, m_ProfileInfo.probeOffset);

            // All probes need to be baked only once for the whole batch and not once per cell
            // The reason is that the baker is not deterministic so the same probe position baked in two different cells may have different values causing seams artefacts.
            m_BakingBatch = new BakingBatch(cellCount);

            // Run subdivision
            ProbeSubdivisionResult result;
            using (new BakingSetupProfiling(BakingSetupProfiling.Stages.BakeBricks))
                result = GetWorldSubdivision();

            // Compute probe positions
            NativeList<Vector3> positions;
            using (new BakingSetupProfiling(BakingSetupProfiling.Stages.ApplySubdivisionResults))
                positions = ApplySubdivisionResults(result);

            // Restore loaded asset settings
            ProbeReferenceVolume.instance.SetSubdivisionDimensions(prevBrickSize, prevMaxSubdiv, prevOffset);

            return positions;
        }

        static ProbeSubdivisionResult GetWorldSubdivision()
        {
            if (isFreezingPlacement)
                return GetBricksFromLoaded();

            var ctx = PrepareProbeSubdivisionContext();
            return BakeBricks(ctx, m_BakingBatch.contributors);
        }

        static NativeList<Vector3> ApplySubdivisionResults(ProbeSubdivisionResult results)
        {
            int cellIdx = 0, freq = 10; // Don't refresh progress bar at every iteration because it's slow
            BakingSetupProfiling.GetProgressRange(out float progress0, out float progress1);

            var positions = new NativeList<Vector3>(Allocator.Persistent);
            foreach ((var position, var bounds, var bricks) in results.cells)
            {
                if (++cellIdx % freq == 0)
                    EditorUtility.DisplayProgressBar("Baking Probe Volumes", $"Subdividing cell {cellIdx} out of {results.cells.Count}", Mathf.Lerp(progress0, progress1, cellIdx / (float)results.cells.Count));

                int positionStart = positions.Length;

                ConvertBricksToPositions(bricks, out var probePositions, out var brickSubdivLevels);
                DeduplicateProbePositions(in probePositions, in brickSubdivLevels, m_BakingBatch, positions, out var probeIndices);

                BakingCell cell = new BakingCell()
                {
                    index = PosToIndex(position),
                    position = position,
                    bounds = bounds,
                    bricks = bricks,
                    probePositions = probePositions,
                    probeIndices = probeIndices,
                };

                m_BakingBatch.cells.Add(cell);
                m_BakingBatch.cellIndex2SceneReferences[cell.index] = new HashSet<string>(results.scenesPerCells[cell.position]);
            }

            return positions;
        }

        private static void DeduplicateProbePositions(in Vector3[] probePositions, in int[] brickSubdivLevel, BakingBatch batch,
            NativeList<Vector3> uniquePositions, out int[] indices)
        {
            indices = new int[probePositions.Length];
            int uniqueIndex = batch.positionToIndex.Count;

            for (int i = 0; i < probePositions.Length; i++)
            {
                var pos = probePositions[i];
                var brickSubdiv = brickSubdivLevel[i];
                int probeHash = batch.GetProbePositionHash(pos);

                if (batch.positionToIndex.TryGetValue(probeHash, out var index))
                {
                    indices[i] = index;
                    int oldBrickLevel = batch.uniqueBrickSubdiv[probeHash];
                    if (brickSubdiv < oldBrickLevel)
                        batch.uniqueBrickSubdiv[probeHash] = brickSubdiv;
                }
                else
                {
                    batch.positionToIndex[probeHash] = uniqueIndex;
                    indices[i] = uniqueIndex;
                    batch.uniqueBrickSubdiv[probeHash] = brickSubdiv;
                    uniquePositions.Add(pos);
                    uniqueIndex++;
                }
            }
        }

        static ProbeSubdivisionResult GetBricksFromLoaded()
        {
            var dataList = GetPerSceneDataList();
            var result = new ProbeSubdivisionResult();

            foreach (var data in dataList)
            {
                var cellSize = m_ProfileInfo.minDistanceBetweenProbes * 3.0f * m_ProfileInfo.cellSizeInBricks;
                Vector3 cellDimensions = new Vector3(cellSize, cellSize, cellSize);

                // Loop through cells in asset, we need to be careful as there'll be duplicates.
                // As we go through the cells we fill ProbeSubdivisionResult as we go.
                var cells = m_BakingSet.GetSceneCellIndexList(data.sceneGUID);
                foreach (var cellIndex in cells)
                {
                    var cellDesc = m_BakingSet.GetCellDesc(cellIndex);
                    var cellData = m_BakingSet.GetCellData(cellIndex);
                    var cellPos = cellDesc.position;

                    if (!result.scenesPerCells.ContainsKey(cellPos))
                    {
                        result.scenesPerCells[cellPos] = new HashSet<string>();

                        var center = new Vector3((cellPos.x + 0.5f) * cellSize, (cellPos.y + 0.5f) * cellSize, (cellPos.z + 0.5f) * cellSize);
                        result.cells.Add((cellPos, new Bounds(center, cellDimensions), cellData.bricks.ToArray()));
                    }
                    result.scenesPerCells[cellPos].Add(data.sceneGUID);
                }
            }

            return result;
        }

        static internal ProbeSubdivisionContext PrepareProbeSubdivisionContext(bool liveContext = false)
        {
            ProbeSubdivisionContext ctx = new ProbeSubdivisionContext();

            // Prepare all the information in the scene for baking GI.
            Vector3 refVolOrigin = Vector3.zero; // TODO: This will need to be center of the world bounds.
            var perSceneDataList = GetPerSceneDataList();

            if (m_BakingSet == null)
            {
                if (perSceneDataList.Count == 0) return ctx;
                SetBakingContext(perSceneDataList);
            }

            var profileInfo = m_ProfileInfo;
            if (liveContext || m_ProfileInfo == null)
                profileInfo = GetProfileInfoFromBakingSet(m_BakingSet);

            ctx.Initialize(m_BakingSet, profileInfo, refVolOrigin);
            return ctx;
        }

        static internal ProbeSubdivisionResult BakeBricks(ProbeSubdivisionContext ctx, in GIContributors contributors)
        {
            var result = new ProbeSubdivisionResult();

            if (ctx.probeVolumes.Count == 0)
                return result;

            using (var gpuResources = ProbePlacement.AllocateGPUResources(ctx.probeVolumes.Count, ctx.profile))
            {
                // subdivide all the cells and generate brick positions
                foreach (var cell in ctx.cells)
                {
                    var scenesInCell = new HashSet<string>();

                    // Calculate overlaping probe volumes to avoid unnecessary work
                    var overlappingProbeVolumes = new List<(ProbeVolume component, ProbeReferenceVolume.Volume volume, Bounds bounds)>();
                    foreach (var probeVolume in ctx.probeVolumes)
                    {
                        if (ProbeVolumePositioning.OBBAABBIntersect(probeVolume.volume, cell.bounds, probeVolume.bounds))
                        {
                            overlappingProbeVolumes.Add(probeVolume);
                            scenesInCell.Add(ProbeReferenceVolume.GetSceneGUID(probeVolume.component.gameObject.scene));
                        }
                    }

                    // Calculate valid renderers to avoid unnecessary work (a renderer needs to overlap a probe volume and match the layer)
                    var filteredContributors = contributors.Filter(ctx.bakingSet, cell.bounds, overlappingProbeVolumes);

                    if (filteredContributors.Count == 0 && !overlappingProbeVolumes.Any(v => v.component.fillEmptySpaces))
                        continue;

                    var bricks = ProbePlacement.SubdivideCell(cell.position, cell.bounds, ctx, gpuResources, filteredContributors, overlappingProbeVolumes);
                    if (bricks.Length == 0)
                        continue;

                    foreach (var renderer in filteredContributors.renderers)
                        scenesInCell.Add(ProbeReferenceVolume.GetSceneGUID(renderer.component.gameObject.scene));
                    foreach (var terrain in filteredContributors.terrains)
                        scenesInCell.Add(ProbeReferenceVolume.GetSceneGUID(terrain.component.gameObject.scene));

                    result.cells.Add((cell.position, cell.bounds, bricks));
                    result.scenesPerCells[cell.position] = scenesInCell;
                }
            }

            return result;
        }

        static void ModifyProfileFromLoadedData(ProbeVolumeBakingSet bakingSet)
        {
            m_ProfileInfo.simplificationLevels = bakingSet.bakedSimplificationLevels;
            m_ProfileInfo.minDistanceBetweenProbes = bakingSet.bakedMinDistanceBetweenProbes;
            m_ProfileInfo.probeOffset = bakingSet.bakedProbeOffset;
            globalBounds = bakingSet.globalBounds;
        }

        // Converts brick information into positional data at kBrickProbeCountPerDim * kBrickProbeCountPerDim * kBrickProbeCountPerDim resolution
        internal static void ConvertBricksToPositions(Brick[] bricks, out Vector3[] outProbePositions, out int[] outBrickSubdiv)
        {
            int posIdx = 0;
            float scale = ProbeReferenceVolume.instance.MinBrickSize() / ProbeBrickPool.kBrickCellCount;
            Vector3 offset = ProbeReferenceVolume.instance.ProbeOffset();

            outProbePositions = new Vector3[bricks.Length * ProbeBrickPool.kBrickProbeCountTotal];
            outBrickSubdiv = new int[bricks.Length * ProbeBrickPool.kBrickProbeCountTotal];

            foreach (var b in bricks)
            {
                int brickSize = ProbeReferenceVolume.CellSize(b.subdivisionLevel);
                Vector3Int brickOffset = b.position * ProbeBrickPool.kBrickCellCount;

                for (int z = 0; z < ProbeBrickPool.kBrickProbeCountPerDim; z++)
                {
                    for (int y = 0; y < ProbeBrickPool.kBrickProbeCountPerDim; y++)
                    {
                        for (int x = 0; x < ProbeBrickPool.kBrickProbeCountPerDim; x++)
                        {
                            var probeOffset = brickOffset + new Vector3Int(x, y, z) * brickSize;

                            outProbePositions[posIdx] = offset + (Vector3)probeOffset * scale;
                            outBrickSubdiv[posIdx] = b.subdivisionLevel;

                            posIdx++;
                        }
                    }
                }
            }
        }
    }
}
