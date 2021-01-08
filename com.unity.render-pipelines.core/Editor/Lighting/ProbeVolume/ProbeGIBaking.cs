#if UNITY_EDITOR

using System.Collections.Generic;
using Unity.Collections;
using System;
using UnityEditor;

using Brick = UnityEngine.Rendering.ProbeBrickIndex.Brick;
using UnityEngine.SceneManagement;

namespace UnityEngine.Rendering
{
    struct DilationProbe : IComparable<DilationProbe>
    {
        public int idx;
        public float dist;

        public DilationProbe(int idx, float dist)
        {
            this.idx = idx;
            this.dist = dist;
        }

        public int CompareTo(DilationProbe other)
        {
            return dist.CompareTo(other.dist);
        }
    }

    struct BakingCell
    {
        public ProbeReferenceVolume.Cell cell;
        public int[] probeIndices;
        public int numUniqueProbes;
    }

    [InitializeOnLoad]
    internal class ProbeGIBaking
    {
        private static bool init = false;
        private static Dictionary<int, List<Scene>> cellIndex2SceneReferences = new Dictionary<int, List<Scene>>();
        private static List<BakingCell> bakingCells = new List<BakingCell>();
        private static ProbeReferenceVolumeAuthoring bakingReferenceVolumeAuthoring = null;

        static ProbeGIBaking()
        {
            Init();
        }

        public static void Init()
        {
            if (!init)
            {
                init = true;
                Lightmapping.lightingDataCleared += OnLightingDataCleared;
                Lightmapping.bakeStarted += OnBakeStarted;
            }
        }

        static public void Clear()
        {
            var refVolAuthList = GameObject.FindObjectsOfType<ProbeReferenceVolumeAuthoring>();

            foreach (var refVolAuthoring in refVolAuthList)
            {
                if (!refVolAuthoring.enabled || !refVolAuthoring.gameObject.activeSelf)
                    continue;

                refVolAuthoring.volumeAsset = null;

                var refVol = ProbeReferenceVolume.instance;
                refVol.Clear();
                refVol.SetTRS(refVolAuthoring.transform.position, refVolAuthoring.transform.rotation, refVolAuthoring.brickSize);
                refVol.SetMaxSubdivision(refVolAuthoring.maxSubdivision);
                refVol.SetNormalBias(refVolAuthoring.normalBias);
            }

            cellIndex2SceneReferences.Clear();

            foreach (var bakingCell in bakingCells)
            {
                UnityEditor.Experimental.Lightmapping.SetAdditionalBakedProbes(bakingCell.cell.index, null);
            }

            bakingCells.Clear();
        }

        private static ProbeReferenceVolumeAuthoring GetCardinalAuthoringComponent()
        {
            var refVolAuthList = GameObject.FindObjectsOfType<ProbeReferenceVolumeAuthoring>();
            List<ProbeReferenceVolumeAuthoring> enabledVolumes = new List<ProbeReferenceVolumeAuthoring>();

            foreach (var refVolAuthoring in refVolAuthList)
            {
                if (!refVolAuthoring.enabled || !refVolAuthoring.gameObject.activeSelf)
                    continue;

                enabledVolumes.Add(refVolAuthoring);
            }

            int numVols = enabledVolumes.Count;

            if (numVols == 0)
                return null;

            if (numVols == 1)
                return enabledVolumes[0];

            var reference = enabledVolumes[0];
            for (int c = 1; c < numVols; ++c)
            {
                var compare = enabledVolumes[c];
                if (reference.transform.position != compare.transform.position)
                    return null;

                if (reference.transform.localScale != compare.transform.localScale)
                    return null;

                if (reference.profile != compare.profile)
                    return null;
            }

            return reference;
        }

        private static void OnBakeStarted()
        {
            bakingReferenceVolumeAuthoring = GetCardinalAuthoringComponent();

            if (bakingReferenceVolumeAuthoring == null)
            {
                Debug.Log("Scene(s) have multiple inconsistent ProbeReferenceVolumeAuthoring components. Please ensure they use identical profiles and transforms before baking.");
                return;
            }

            RunPlacement();
        }

        private static void OnAdditionalProbesBakeCompleted()
        {
            UnityEditor.Experimental.Lightmapping.additionalBakedProbesCompleted -= OnAdditionalProbesBakeCompleted;

            var numCells = bakingCells.Count;

            // Fetch results of all cells
            for (int c = 0; c < numCells; ++c)
            {
                var cell = bakingCells[c].cell;

                if (cell.probePositions == null)
                    continue;

                int numProbes = cell.probePositions.Length;
                Debug.Assert(numProbes > 0);

                int numUniqueProbes = bakingCells[c].numUniqueProbes;

                var sh = new NativeArray<SphericalHarmonicsL2>(numUniqueProbes, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                var validity = new NativeArray<float>(numUniqueProbes, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                var bakedProbeOctahedralDepth = new NativeArray<float>(numUniqueProbes * 64, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

                UnityEditor.Experimental.Lightmapping.GetAdditionalBakedProbes(cell.index, sh, validity, bakedProbeOctahedralDepth);

                cell.sh = new SphericalHarmonicsL1[numProbes];
                cell.validity = new float[numProbes];
                for (int i = 0; i < numProbes; ++i)
                {
                    Vector4[] channels = new Vector4[3];

                    int j = bakingCells[c].probeIndices[i];

                    // compare to SphericalHarmonicsL2::GetShaderConstantsFromNormalizedSH
                    channels[0] = new Vector4(sh[j][0, 3], sh[j][0, 1], sh[j][0, 2], sh[j][0, 0]);
                    channels[1] = new Vector4(sh[j][1, 3], sh[j][1, 1], sh[j][1, 2], sh[j][1, 0]);
                    channels[2] = new Vector4(sh[j][2, 3], sh[j][2, 1], sh[j][2, 2], sh[j][2, 0]);

                    // It can be shown that |L1_i| <= |2*L0|
                    // Precomputed Global Illumination in Frostbite by Yuriy O'Donnell.
                    // https://media.contentapi.ea.com/content/dam/eacom/frostbite/files/gdc2018-precomputedgiobalilluminationinfrostbite.pdf
                    //
                    // So divide by L0 brings us to [-2, 2],
                    // divide by 4 brings us to [-0.5, 0.5],
                    // and plus by 0.5 brings us to [0, 1].
                    for (int channel = 0; channel < 3; ++channel)
                    {
                        var l0 = channels[channel][3];

                        if (l0 != 0.0f)
                        {
                            for (int axis = 0; axis < 3; ++axis)
                            {
                                channels[channel][axis] = channels[channel][axis] / (l0 * 4.0f) + 0.5f;
                                Debug.Assert(channels[channel][axis] >= 0.0f && channels[channel][axis] <= 1.0f);
                            }
                        }
                    }

                    SphericalHarmonicsL1 sh1 = new SphericalHarmonicsL1();
                    sh1.shAr = channels[0];
                    sh1.shAg = channels[1];
                    sh1.shAb = channels[2];

                    cell.sh[i] = sh1;
                    cell.validity[i] = validity[j];
                }

                // Reset index
                UnityEditor.Experimental.Lightmapping.SetAdditionalBakedProbes(cell.index, null);

                DilateInvalidProbes(cell.probePositions, cell.bricks, cell.sh, cell.validity, bakingReferenceVolumeAuthoring.GetDilationSettings());

                ProbeReferenceVolume.instance.cells[cell.index] = cell;
            }

            // Map from each scene to an existing reference volume
            var scene2RefVol = new Dictionary<Scene, ProbeReferenceVolumeAuthoring>();
            foreach (var refVol in GameObject.FindObjectsOfType<ProbeReferenceVolumeAuthoring>())
                if (refVol.enabled)
                    scene2RefVol[refVol.gameObject.scene] = refVol;

            // Map from each reference volume to its asset
            var refVol2Asset = new Dictionary<ProbeReferenceVolumeAuthoring, ProbeVolumeAsset>();
            foreach (var refVol in scene2RefVol.Values)
            {
                refVol2Asset[refVol] = ProbeVolumeAsset.CreateAsset(refVol.gameObject.scene);
            }

            // Put cells into the respective assets
            foreach (var cell in ProbeReferenceVolume.instance.cells.Values)
            {
                foreach (var scene in cellIndex2SceneReferences[cell.index])
                {
                    // This scene has a reference volume authoring component in it?
                    ProbeReferenceVolumeAuthoring refVol = null;
                    if (scene2RefVol.TryGetValue(scene, out refVol))
                    {
                        var asset = refVol2Asset[refVol];
                        asset.cells.Add(cell);
                    }
                }
            }

            // Connect the assets to their components
            foreach (var pair in refVol2Asset)
            {
                var refVol = pair.Key;
                var asset = pair.Value;

                refVol.volumeAsset = asset;

                if (UnityEditor.Lightmapping.giWorkflowMode != UnityEditor.Lightmapping.GIWorkflowMode.Iterative)
                {
                    UnityEditor.EditorUtility.SetDirty(refVol);
                    UnityEditor.EditorUtility.SetDirty(refVol.volumeAsset);
                }
            }

            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();

            foreach (var refVol in refVol2Asset.Keys)
            {
                if (refVol.enabled && refVol.gameObject.activeSelf)
                    refVol.QueueAssetLoading();
            }
        }

        private static void OnLightingDataCleared()
        {
            Clear();
        }

        static float CalculateSurfaceArea(Matrix4x4 transform, Mesh mesh)
        {
            var triangles = mesh.triangles;
            var vertices = mesh.vertices;

            for (int i = 0; i < vertices.Length; ++i)
            {
                vertices[i] = transform * vertices[i];
            }

            double sum = 0.0;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 corner = vertices[triangles[i]];
                Vector3 a = vertices[triangles[i + 1]] - corner;
                Vector3 b = vertices[triangles[i + 2]] - corner;

                sum += Vector3.Cross(a, b).magnitude;
            }

            return (float)(sum / 2.0);
        }

        private static void DilateInvalidProbes(Vector3[] probePositions,
            List<Brick> bricks, SphericalHarmonicsL1[] sh, float[] validity, ProbeDilationSettings dilationSettings)
        {
            // For each brick
            List<DilationProbe> culledProbes = new List<DilationProbe>();
            List<DilationProbe> nearProbes = new List<DilationProbe>(dilationSettings.maxDilationSamples);
            for (int brickIdx = 0; brickIdx < bricks.Count; brickIdx++)
            {
                // Find probes that are in bricks nearby
                CullDilationProbes(brickIdx, bricks, validity, dilationSettings, culledProbes);

                // Iterate probes in current brick
                for (int probeOffset = 0; probeOffset < 64; probeOffset++)
                {
                    int probeIdx = brickIdx * 64 + probeOffset;

                    // Skip valid probes
                    if (validity[probeIdx] <= dilationSettings.dilationValidityThreshold)
                        continue;

                    // Find distance weighted probes nearest to current probe
                    FindNearProbes(probeIdx, probePositions, dilationSettings, culledProbes, nearProbes, out float invDistSum);

                    // Set invalid probe to weighted average of found neighboring probes
                    var shAverage = new SphericalHarmonicsL1();
                    for (int nearProbeIdx = 0; nearProbeIdx < nearProbes.Count; nearProbeIdx++)
                    {
                        var nearProbe = nearProbes[nearProbeIdx];
                        float weight = nearProbe.dist / invDistSum;
                        var target = sh[nearProbe.idx];
                        shAverage.shAr += target.shAr * weight;
                        shAverage.shAg += target.shAg * weight;
                        shAverage.shAb += target.shAb * weight;
                    }

                    sh[probeIdx] = shAverage;
                    validity[probeIdx] = validity[probeIdx];
                }
            }
        }

        // Given a brick index, find and accumulate probes in nearby bricks
        private static void CullDilationProbes(int brickIdx, List<Brick> bricks,
            float[] validity, ProbeDilationSettings dilationSettings, List<DilationProbe> outProbeIndices)
        {
            outProbeIndices.Clear();
            for (int otherBrickIdx = 0; otherBrickIdx < bricks.Count; otherBrickIdx++)
            {
                var currentBrick = bricks[brickIdx];
                var otherBrick = bricks[otherBrickIdx];

                float currentBrickSize = Mathf.Pow(3f, currentBrick.size);
                float otherBrickSize = Mathf.Pow(3f, otherBrick.size);

                // TODO: This should probably be revisited.
                float sqrt2 = 1.41421356237f;
                float maxDistance = sqrt2 * currentBrickSize + sqrt2 * otherBrickSize;
                float interval = dilationSettings.maxDilationSampleDistance / dilationSettings.brickSize;
                maxDistance = interval * Mathf.Ceil(maxDistance / interval);

                Vector3 currentBrickCenter = currentBrick.position + Vector3.one * currentBrickSize / 2f;
                Vector3 otherBrickCenter = otherBrick.position + Vector3.one * otherBrickSize / 2f;

                if (Vector3.Distance(currentBrickCenter, otherBrickCenter) <= maxDistance)
                {
                    for (int probeOffset = 0; probeOffset < 64; probeOffset++)
                    {
                        int otherProbeIdx = otherBrickIdx * 64 + probeOffset;

                        if (validity[otherProbeIdx] <= dilationSettings.dilationValidityThreshold)
                        {
                            outProbeIndices.Add(new DilationProbe(otherProbeIdx, 0));
                        }
                    }
                }
            }
        }

        // Given a probe index, find nearby probes weighted by inverse distance
        private static void FindNearProbes(int probeIdx, Vector3[] probePositions,
            ProbeDilationSettings dilationSettings, List<DilationProbe> culledProbes, List<DilationProbe> outNearProbes, out float invDistSum)
        {
            outNearProbes.Clear();
            invDistSum = 0;

            // Sort probes by distance to prioritize closer ones
            for (int culledProbeIdx = 0; culledProbeIdx < culledProbes.Count; culledProbeIdx++)
            {
                float dist = Vector3.Distance(probePositions[culledProbes[culledProbeIdx].idx], probePositions[probeIdx]);
                culledProbes[culledProbeIdx] = new DilationProbe(culledProbes[culledProbeIdx].idx, dist);
            }

            if (!dilationSettings.greedyDilation)
            {
                culledProbes.Sort();
            }

            // Return specified amount of probes under given max distance
            int numSamples = 0;
            for (int sortedProbeIdx = 0; sortedProbeIdx < culledProbes.Count; sortedProbeIdx++)
            {
                if (numSamples >= dilationSettings.maxDilationSamples)
                    return;

                var current = culledProbes[sortedProbeIdx];
                if (current.dist <= dilationSettings.maxDilationSampleDistance)
                {
                    var invDist = 1f / (current.dist * current.dist);
                    invDistSum += invDist;
                    outNearProbes.Add(new DilationProbe(current.idx, invDist));

                    numSamples++;
                }
            }
        }

        private static void DeduplicateProbePositions(in Vector3[] probePositions, out Vector3[] deduplicatedProbePositions, out int[] indices)
        {
            List<Vector3> uniqueProbePositions = new List<Vector3>();

            indices = new int[probePositions.Length];

            // find duplicates
            for (int i = 0; i < probePositions.Length; ++i)
            {
                Vector3 ppi = probePositions[i];
                bool isDuplicate = false;
                int index = uniqueProbePositions.Count;

                // push if not a duplicate
                for (int j = 0; j < uniqueProbePositions.Count; ++j)
                {
                    Vector3 ppj = uniqueProbePositions[j];

                    if (ppi == ppj)
                    {
                        isDuplicate = true;
                        index = j;
                        break;
                    }
                }

                if (!isDuplicate)
                    uniqueProbePositions.Add(ppi);

                indices[i] = index;
            }

            deduplicatedProbePositions = uniqueProbePositions.ToArray();
        }

        public static void RunPlacement()
        {
            var refVol = ProbeReferenceVolume.instance;

            Clear();

            UnityEditor.Experimental.Lightmapping.additionalBakedProbesCompleted += OnAdditionalProbesBakeCompleted;

            var volumeScale = bakingReferenceVolumeAuthoring.transform.localScale;
            var CellSize = bakingReferenceVolumeAuthoring.cellSize;
            var xCells = (int)Mathf.Ceil(volumeScale.x / CellSize);
            var yCells = (int)Mathf.Ceil(volumeScale.y / CellSize);
            var zCells = (int)Mathf.Ceil(volumeScale.z / CellSize);

            // create cells
            List<Vector3Int> cellPositions = new List<Vector3Int>();

            // TODO: rewrite for infinite space testing
            for (var x = -xCells; x < xCells; ++x)
                for (var y = -yCells; y < yCells; ++y)
                    for (var z = -zCells; z < zCells; ++z)
                        cellPositions.Add(new Vector3Int(x, y, z));

            int index = 0;

            // subdivide and create positions and add them to the bake queue
            foreach (var cellPos in cellPositions)
            {
                var cell = new ProbeReferenceVolume.Cell();
                cell.position = cellPos;
                cell.index = index++;

                var refVolTransform = refVol.GetTransform();
                var cellTrans = Matrix4x4.TRS(refVolTransform.posWS, refVolTransform.rot, Vector3.one);

                //BakeMesh[] bakeMeshes = GetEntityQuery(typeof(BakeMesh)).ToComponentDataArray<BakeMesh>();
                Renderer[] renderers = UnityEngine.Object.FindObjectsOfType<Renderer>();
                ProbeVolume[] probeVolumes = UnityEngine.Object.FindObjectsOfType<ProbeVolume>();

                Dictionary<Scene, int> sceneRefs;
                List<ProbeReferenceVolume.Volume> influenceVolumes;
                ProbePlacement.CreateInfluenceVolumes(cell.position, renderers, probeVolumes, bakingReferenceVolumeAuthoring, cellTrans, out influenceVolumes, out sceneRefs);

                // Each cell keeps a number of references it has to each scene it was influenced by
                // We use this list to determine which scene's ProbeVolume asset to assign this cells data to
                var sortedRefs = new SortedDictionary<int, Scene>();
                foreach (var item in sceneRefs)
                    sortedRefs[-item.Value] = item.Key;

                Vector3[] probePositionsArr = null;
                List<Brick> bricks = null;

                ProbePlacement.Subdivide(cell.position, refVol, bakingReferenceVolumeAuthoring.cellSize, refVolTransform.posWS, refVolTransform.rot,
                    influenceVolumes, ref probePositionsArr, ref bricks);

                if (probePositionsArr.Length > 0 && bricks.Count > 0)
                {
                    int[] indices = null;
                    Vector3[] deduplicatedProbePositions = null;
                    DeduplicateProbePositions(in probePositionsArr, out deduplicatedProbePositions, out indices);

                    UnityEditor.Experimental.Lightmapping.SetAdditionalBakedProbes(cell.index, deduplicatedProbePositions);
                    cell.probePositions = probePositionsArr;
                    cell.bricks = bricks;

                    BakingCell bakingCell = new BakingCell();
                    bakingCell.cell = cell;
                    bakingCell.probeIndices = indices;
                    bakingCell.numUniqueProbes = deduplicatedProbePositions.Length;

                    bakingCells.Add(bakingCell);
                    cellIndex2SceneReferences[cell.index] = new List<Scene>(sortedRefs.Values);
                }
            }
        }
    }
}

#endif
