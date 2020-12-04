#if UNITY_EDITOR

using System.Collections.Generic;
using Unity.Collections;
using System;
using UnityEditor;

using Brick = UnityEngine.Rendering.HighDefinition.ProbeBrickIndex.Brick;
using UnityEngine.SceneManagement;

namespace UnityEngine.Rendering.HighDefinition
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

    public class ProbeGIBaking
    {
        static bool init = false;

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
            ProbeReferenceVolumeAuthoring refVolAuthoring = null;

            foreach(var rV in refVolAuthList)
            {
                refVolAuthoring = rV;
                refVolAuthoring.VolumeAsset = null;
            }

            if (refVolAuthoring == null)
                return;
            
            var refVol = ProbeReferenceVolume.instance;
            refVol.Clear();
            refVol.SetTRS(refVolAuthoring.transform.position, refVolAuthoring.transform.rotation, refVolAuthoring.brickSize);
            refVol.SetMaxSubdivision(refVolAuthoring.maxSubdivision);
            refVol.SetNormalBias(refVolAuthoring.normalBias);
        }

        private static void OnBakeStarted()
        {
            RunPlacement();
        }

        private static void OnAdditionalProbesBakeCompleted()
        {
            // TODO: Settings should be copied into ProbeReferenceVolume?
            var refVolAuthoring = GameObject.FindObjectOfType<ProbeReferenceVolumeAuthoring>();
            if (refVolAuthoring == null)
            {
                Debug.Log("Error: No ProbeReferenceVolumeAuthoring component found.");
                return;
            }

            var numCells = ProbeReferenceVolume.instance.Cells.Count;

            var probeVolumeAsset = ProbeVolumeAsset.CreateAsset(SceneManagement.SceneManager.GetActiveScene());

            for (int c = 0; c < numCells; ++c)
            {
                var cell = ProbeReferenceVolume.instance.Cells[c];

                if (cell.probePositions == null)
                    continue;

                Debug.Log("Bake completed for id " + cell.index);
                int numProbes = cell.probePositions.Length;
                Debug.Assert(numProbes > 0);

                var sh = new NativeArray<SphericalHarmonicsL2>(numProbes, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                var validity = new NativeArray<float>(numProbes, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                
                UnityEditor.Experimental.Lightmapping.GetAdditionalBakedProbes(cell.index, sh, validity);

                cell.sh = new SphericalHarmonicsL1[numProbes];
                cell.validity = new float[numProbes];
                for (int i = 0; i < numProbes; ++i)
                {
                    Vector4[] channels = new Vector4[3];

                    // compare to SphericalHarmonicsL2::GetShaderConstantsFromNormalizedSH
                    channels[0] = new Vector4(sh[i][0, 3], sh[i][0, 1], sh[i][0, 2], sh[i][0, 0]);
                    channels[1] = new Vector4(sh[i][1, 3], sh[i][1, 1], sh[i][1, 2], sh[i][1, 0]);
                    channels[2] = new Vector4(sh[i][2, 3], sh[i][2, 1], sh[i][2, 2], sh[i][2, 0]);

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
                    cell.validity[i] = validity[i];
                }

                // reset index
                UnityEditor.Experimental.Lightmapping.SetAdditionalBakedProbes(cell.index, null);

                DilateInvalidProbes(cell.probePositions, cell.bricks, cell.sh, cell.validity, ref refVolAuthoring);

                // add cell to asset
                probeVolumeAsset.cells.Add(cell);

                if (UnityEditor.Lightmapping.giWorkflowMode != UnityEditor.Lightmapping.GIWorkflowMode.Iterative)
                    UnityEditor.EditorUtility.SetDirty(probeVolumeAsset);

                refVolAuthoring.VolumeAsset = probeVolumeAsset;
            }

            refVolAuthoring.QueueAssetLoading();

            UnityEditor.Experimental.Lightmapping.additionalBakedProbesCompleted -= OnAdditionalProbesBakeCompleted;
        }

        private static void OnLightingDataCleared()
        {
            Clear();
        }

        static float CalculateSurfaceArea(Matrix4x4 transform, Mesh mesh) {
            var triangles = mesh.triangles;
            var vertices = mesh.vertices;

            for (int i = 0; i < vertices.Length; ++i)
            {
                vertices[i] = transform * vertices[i];
            }

            double sum = 0.0;
            for (int i = 0; i < triangles.Length; i += 3) {
                Vector3 corner = vertices[triangles[i]];
                Vector3 a = vertices[triangles[i + 1]] - corner;
                Vector3 b = vertices[triangles[i + 2]] - corner;

                sum += Vector3.Cross(a, b).magnitude;
            }

            return (float)(sum / 2.0);
        }
        
        private static void DilateInvalidProbes(Vector3[] probePositions,
            List<Brick> bricks, SphericalHarmonicsL1[] sh, float[] validity, ref ProbeReferenceVolumeAuthoring settings)
        {
            // For each brick
            List<DilationProbe> culledProbes = new List<DilationProbe>();
            List<DilationProbe> nearProbes = new List<DilationProbe>(settings.MaxDilationSamples);
            for (int brickIdx = 0; brickIdx < bricks.Count; brickIdx++)
            {
                // Find probes that are in bricks nearby
                CullDilationProbes(brickIdx, bricks, validity, ref settings, culledProbes);

                // Iterate probes in current brick
                for (int probeOffset = 0; probeOffset < 64; probeOffset++)
                {
                    int probeIdx = brickIdx * 64 + probeOffset;

                    // Skip valid probes
                    if (validity[probeIdx] <= settings.DilationValidityThreshold)
                        continue;

                    // Find distance weighted probes nearest to current probe
                    FindNearProbes(probeIdx, probePositions, ref settings, culledProbes, nearProbes, out float invDistSum);

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
            float[] validity, ref ProbeReferenceVolumeAuthoring settings, List<DilationProbe> outProbeIndices)
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
                float interval = settings.MaxDilationSampleDistance / settings.brickSize;
                maxDistance = interval * Mathf.Ceil(maxDistance / interval);

                Vector3 currentBrickCenter = currentBrick.position + Vector3.one * currentBrickSize / 2f;
                Vector3 otherBrickCenter = otherBrick.position + Vector3.one * otherBrickSize / 2f;

                if (Vector3.Distance(currentBrickCenter, otherBrickCenter) <= maxDistance)
                {
                    for (int probeOffset = 0; probeOffset < 64; probeOffset++)
                    {
                        int otherProbeIdx = otherBrickIdx * 64 + probeOffset;

                        if (validity[otherProbeIdx] <= settings.DilationValidityThreshold)
                        {
                            outProbeIndices.Add(new DilationProbe(otherProbeIdx, 0));
                        }
                    }
                }
            }
        }

        // Given a probe index, find nearby probes weighted by inverse distance
        private static void FindNearProbes(int probeIdx, Vector3[] probePositions,
            ref ProbeReferenceVolumeAuthoring settings, List<DilationProbe> culledProbes, List<DilationProbe> outNearProbes, out float invDistSum)
        {
            outNearProbes.Clear();
            invDistSum = 0;

            // Sort probes by distance to prioritize closer ones
            for (int culledProbeIdx = 0; culledProbeIdx < culledProbes.Count; culledProbeIdx++)
            {
                float dist = Vector3.Distance(probePositions[culledProbes[culledProbeIdx].idx], probePositions[probeIdx]);
                culledProbes[culledProbeIdx] = new DilationProbe(culledProbes[culledProbeIdx].idx, dist);
            }

            if (!settings.GreedyDilation)
            {
                culledProbes.Sort();
            }

            // Return specified amount of probes under given max distance
            int numSamples = 0;
            for (int sortedProbeIdx = 0; sortedProbeIdx < culledProbes.Count; sortedProbeIdx++)
            {
                if (numSamples >= settings.MaxDilationSamples)
                    return;

                var current = culledProbes[sortedProbeIdx];
                if (current.dist <= settings.MaxDilationSampleDistance)
                {
                    var invDist = 1f / (current.dist * current.dist);
                    invDistSum += invDist;
                    outNearProbes.Add(new DilationProbe(current.idx, invDist));

                    numSamples++;
                }
            }
        }

        public static void RunPlacement()
        {
            var refVolAuthoring = GameObject.FindObjectOfType<ProbeReferenceVolumeAuthoring>();
            if (refVolAuthoring == null)
                return;

            var refVol = ProbeReferenceVolume.instance;

            Clear();

            UnityEditor.Experimental.Lightmapping.additionalBakedProbesCompleted += OnAdditionalProbesBakeCompleted;

            var volumeScale = refVolAuthoring.transform.localScale;
            var CellSize = refVolAuthoring.cellSize;
            var xCells = (int)Mathf.Ceil(volumeScale.x / CellSize);
            var yCells = (int)Mathf.Ceil(volumeScale.y / CellSize);
            var zCells = (int)Mathf.Ceil(volumeScale.z / CellSize);

            // create cells
            List<Vector3Int> cellPositions = new List<Vector3Int>();
            
            for (var x = 0; x < xCells; ++x)
                for (var y = 0; y < yCells; ++y)
                    for (var z = 0; z < zCells; ++z)
                        cellPositions.Add(new Vector3Int(x, y, z));

            int index = 0;

            bool placementHappened = false;
            int totalBricks = 0;
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
                ProbePlacement.CreateInfluenceVolumes(cell.position, renderers, probeVolumes, refVolAuthoring, cellTrans, out influenceVolumes, out sceneRefs);

                Vector3[] probePositionsArr = null;
                List<Brick> bricks = null;

                ProbePlacement.Subdivide(cell.position, refVol, refVolAuthoring.cellSize, refVolTransform.posWS, refVolTransform.rot,
                    influenceVolumes, ref probePositionsArr, ref bricks);

                if (probePositionsArr.Length > 0 && bricks.Count > 0)
                {
                    UnityEditor.Experimental.Lightmapping.SetAdditionalBakedProbes(cell.index, probePositionsArr);
                    cell.probePositions = probePositionsArr;
                    cell.bricks = bricks;
                    placementHappened = true;
                    totalBricks += bricks.Count;
                }

                ProbeReferenceVolume.instance.Cells.Add(cell);
            }

            if (placementHappened)
                Debug.Log("Probe Placement completed. " + totalBricks + " Bricks placed.");
        }
    }
}

#endif
