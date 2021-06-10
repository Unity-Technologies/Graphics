using System.Collections.Generic;
using Unity.Collections;
using System;
using System.Linq;
using UnityEditor;

using Brick = UnityEngine.Experimental.Rendering.ProbeBrickIndex.Brick;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering
{
    class ProbeSubdivisionContext
    {
        public List<(ProbeVolume component, ProbeReferenceVolume.Volume volume)> probeVolumes = new List<(ProbeVolume, ProbeReferenceVolume.Volume)>();
        public List<(Renderer component, ProbeReferenceVolume.Volume volume)> renderers = new List<(Renderer, ProbeReferenceVolume.Volume)>();
        public List<(Vector3Int position, ProbeReferenceVolume.Volume volume)> cells = new List<(Vector3Int, ProbeReferenceVolume.Volume)>();
        public List<(Terrain, ProbeReferenceVolume.Volume volume)> terrains = new List<(Terrain, ProbeReferenceVolume.Volume)>();
        public ProbeReferenceVolumeAuthoring refVolume;

        // Limit the time we can spend in the subdivision for realtime debug subdivision
        public float subdivisionStartTime;

        public void Initialize(ProbeReferenceVolumeAuthoring refVolume)
        {
            this.refVolume = refVolume;
            float cellSize = refVolume.cellSizeInMeters;
            subdivisionStartTime = Time.realtimeSinceStartup;

            foreach (var pv in UnityEngine.Object.FindObjectsOfType<ProbeVolume>())
            {
                if (!pv.isActiveAndEnabled)
                    continue;

                ProbeReferenceVolume.Volume volume = new ProbeReferenceVolume.Volume(Matrix4x4.TRS(pv.transform.position, pv.transform.rotation, pv.GetExtents()), pv.maxSubdivisionMultiplier, pv.minSubdivisionMultiplier);
                probeVolumes.Add((pv, volume));
            }

            // Find all renderers in the scene
            foreach (var r in UnityEngine.Object.FindObjectsOfType<Renderer>())
            {
                if (!r.enabled || !r.gameObject.activeSelf)
                    continue;

                var flags = GameObjectUtility.GetStaticEditorFlags(r.gameObject);
                if ((flags & StaticEditorFlags.ContributeGI) == 0)
                    continue;

                var volume = ProbePlacement.ToVolume(r.bounds);

                renderers.Add((r, volume));
            }

            foreach (var terrain in UnityEngine.Object.FindObjectsOfType<Terrain>())
            {
                if (!terrain.isActiveAndEnabled)
                    continue;

                var volume = ProbePlacement.ToVolume(terrain.terrainData.bounds);

                terrains.Add((terrain, volume));
            }

            // Generate all the unique cell positions from probe volumes:
            var refVolTransform = ProbeReferenceVolume.instance.GetTransform();
            var cellTrans = Matrix4x4.TRS(refVolTransform.posWS, refVolTransform.rot, Vector3.one);
            HashSet<Vector3Int> cellPositions = new HashSet<Vector3Int>();
            foreach (var pv in probeVolumes)
            {
                var probeVolume = pv.component;
                var halfSize = probeVolume.size / 2.0f;
                var minCellPosition = (probeVolume.transform.position - halfSize) / cellSize;
                var maxCellPosition = (probeVolume.transform.position + halfSize) / cellSize;

                Vector3Int min = new Vector3Int(Mathf.FloorToInt(minCellPosition.x), Mathf.FloorToInt(minCellPosition.y), Mathf.FloorToInt(minCellPosition.z));
                Vector3Int max = new Vector3Int(Mathf.CeilToInt(maxCellPosition.x), Mathf.CeilToInt(maxCellPosition.y), Mathf.CeilToInt(maxCellPosition.z));

                for (int x = min.x; x < max.x; x++)
                {
                    for (int y = min.y; y < max.y; y++)
                        for (int z = min.z; z < max.z; z++)
                        {
                            var cellPos = new Vector3Int(x, y, z);
                            if (cellPositions.Add(cellPos))
                            {
                                // Calculate the cell volume:
                                ProbeReferenceVolume.Volume cellVolume = new ProbeReferenceVolume.Volume();
                                cellVolume.corner = new Vector3(cellPos.x * cellSize, cellPos.y * cellSize, cellPos.z * cellSize);
                                cellVolume.X = new Vector3(cellSize, 0, 0);
                                cellVolume.Y = new Vector3(0, cellSize, 0);
                                cellVolume.Z = new Vector3(0, 0, cellSize);
                                cells.Add((cellPos, cellVolume));
                            }
                        }
                }
            }
        }
    }
}
