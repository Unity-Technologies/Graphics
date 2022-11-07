using System.Collections.Generic;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
using ProbeVolumeWithBounds = System.Collections.Generic.List<(UnityEngine.Rendering.ProbeVolume component, UnityEngine.Rendering.ProbeReferenceVolume.Volume volume)>;
#endif

namespace UnityEngine.Rendering
{
    /// <summary>
    /// A marker to determine what area of the scene is considered by the Probe Volumes system
    /// </summary>
    [ExecuteAlways]
    [AddComponentMenu("Light/Probe Volume")]
    public class ProbeVolume : MonoBehaviour
    {
        /// <summary>
        /// If is a global bolume
        /// </summary>
        public bool globalVolume = false;

        /// <summary>
        /// The size
        /// </summary>
        public Vector3 size = new Vector3(10, 10, 10);

        /// <summary>
        /// Override the renderer filters.
        /// </summary>
        [HideInInspector, Min(0)]
        public bool overrideRendererFilters = false;

        /// <summary>
        /// The minimum renderer bounding box volume size. This value is used to discard small renderers when the overrideMinRendererVolumeSize is enabled.
        /// </summary>
        [HideInInspector, Min(0)]
        public float minRendererVolumeSize = 0.1f;

        /// <summary>
        /// The <see cref="LayerMask"/>
        /// </summary>
        public LayerMask objectLayerMask = -1;

        /// <summary>
        /// The lowest subdivision level override
        /// </summary>
        [HideInInspector]
        public int lowestSubdivLevelOverride = 0;

        /// <summary>
        /// The highest subdivision level override
        /// </summary>
        [HideInInspector]
        public int highestSubdivLevelOverride = -1;

        /// <summary>
        /// If the subdivision levels need to be overriden
        /// </summary>
        [HideInInspector]
        public bool overridesSubdivLevels = false;

        [SerializeField] internal bool mightNeedRebaking = false;

        [SerializeField] internal Matrix4x4 cachedTransform;
        [SerializeField] internal int cachedHashCode;

        /// <summary>Whether spaces with no renderers need to be filled with bricks at lowest subdivision level.</summary>
        [HideInInspector]
        [Tooltip("Whether spaces with no renderers need to be filled with bricks at lowest subdivision level.")]
        public bool fillEmptySpaces = false;

#if UNITY_EDITOR
        /// <summary>
        /// Returns the extents of the volume.
        /// </summary>
        /// <returns>The extents of the ProbeVolume.</returns>
        public Vector3 GetExtents()
        {
            return size;
        }

        internal Bounds ComputeBounds(GIContributors.ContributorFilter filter, Scene? scene = null)
        {
            Bounds bounds = new Bounds();
            bool foundABound = false;

            void ExpandBounds(Bounds bound)
            {
                if (!foundABound)
                {
                    bounds = bound;
                    foundABound = true;
                }
                else
                {
                    bounds.Encapsulate(bound);
                }
            }

            var contributors = GIContributors.Find(filter, scene);
            foreach (var renderer in contributors.renderers)
                ExpandBounds(renderer.component.bounds);
            foreach (var terrain in contributors.terrains)
                ExpandBounds(terrain.boundsWithTrees);

            return bounds;
        }

        internal void UpdateGlobalVolume()
        {
            var scene = gameObject.scene;

            // Get minBrickSize from scene profile if available
            float minBrickSize = ProbeReferenceVolume.instance.MinBrickSize();
            if (ProbeReferenceVolume.instance.sceneData != null)
            {
                var profile = ProbeReferenceVolume.instance.sceneData.GetProfileForScene(scene);
                if (profile != null)
                    minBrickSize = profile.minBrickSize;
            }

            var bounds = ComputeBounds(GIContributors.ContributorFilter.All, scene);
            transform.position = bounds.center;
            size = Vector3.Max(bounds.size + new Vector3(minBrickSize, minBrickSize, minBrickSize), Vector3.zero);
        }

        internal void OnLightingDataAssetCleared()
        {
            mightNeedRebaking = true;
        }

        internal void OnBakeCompleted()
        {
            // We cache the data of last bake completed.
            cachedTransform = gameObject.transform.worldToLocalMatrix;
            cachedHashCode = GetHashCode();
            mightNeedRebaking = false;
        }

        public override int GetHashCode()
        {
            int hash = 17;

            unchecked
            {
                hash = hash * 23 + size.GetHashCode();
                hash = hash * 23 + gameObject.transform.worldToLocalMatrix.GetHashCode();
                hash = hash * 23 + overridesSubdivLevels.GetHashCode();
                hash = hash * 23 + highestSubdivLevelOverride.GetHashCode();
                hash = hash * 23 + lowestSubdivLevelOverride.GetHashCode();
                hash = hash * 23 + overrideRendererFilters.GetHashCode();
                if (overrideRendererFilters)
                {
                    hash = hash * 23 + minRendererVolumeSize.GetHashCode();
                    hash = hash * 23 + objectLayerMask.value.GetHashCode();
                }
            }

            return hash;
        }

        internal float GetMinSubdivMultiplier()
        {
            float maxSubdiv = ProbeReferenceVolume.instance.GetMaxSubdivision() - 1;
            return overridesSubdivLevels ? Mathf.Clamp(lowestSubdivLevelOverride / maxSubdiv, 0.0f, 1.0f) : 0.0f;
        }

        internal float GetMaxSubdivMultiplier()
        {
            float maxSubdiv = ProbeReferenceVolume.instance.GetMaxSubdivision() - 1;
            return overridesSubdivLevels ? Mathf.Clamp(highestSubdivLevelOverride / maxSubdiv, 0.0f, 1.0f) : 1.0f;
        }

        // Momentarily moving the gizmo rendering for bricks and cells to Probe Volume itself,
        // only the first probe volume in the scene will render them. The reason is that we dont have any
        // other non-hidden component related to APV.
        #region APVGizmo

        static List<ProbeVolume> sProbeVolumeInstances = new();

        MeshGizmo brickGizmos;
        MeshGizmo cellGizmo;

        void DisposeGizmos()
        {
            brickGizmos?.Dispose();
            brickGizmos = null;
            cellGizmo?.Dispose();
            cellGizmo = null;
        }

        void OnEnable()
        {
            sProbeVolumeInstances.Add(this);
        }

        void OnDisable()
        {
            sProbeVolumeInstances.Remove(this);
            DisposeGizmos();
        }

        // Only the first PV of the available ones will draw gizmos.
        bool IsResponsibleToDrawGizmo() => sProbeVolumeInstances.Count > 0 && sProbeVolumeInstances[0] == this;

        internal bool ShouldCullCell(Vector3 cellPosition, Vector3 originWS = default(Vector3))
        {
            var cellSizeInMeters = ProbeReferenceVolume.instance.MaxBrickSize();
            var debugDisplay = ProbeReferenceVolume.instance.probeVolumeDebug;
            if (debugDisplay.realtimeSubdivision)
            {
                var profile = ProbeReferenceVolume.instance.sceneData.GetProfileForScene(gameObject.scene);
                if (profile == null)
                    return true;
                cellSizeInMeters = profile.cellSizeInMeters;
            }

            var cameraTransform = Camera.current.transform;

            Vector3 cellCenterWS = cellPosition * cellSizeInMeters + originWS + Vector3.one * (cellSizeInMeters / 2.0f);

            // Round down to cell size distance
            float roundedDownDist = Mathf.Floor(Vector3.Distance(cameraTransform.position, cellCenterWS) / cellSizeInMeters) * cellSizeInMeters;

            if (roundedDownDist > ProbeReferenceVolume.instance.probeVolumeDebug.subdivisionViewCullingDistance)
                return true;

            var frustumPlanes = GeometryUtility.CalculateFrustumPlanes(Camera.current);
            var volumeAABB = new Bounds(cellCenterWS, cellSizeInMeters * Vector3.one);

            return !GeometryUtility.TestPlanesAABB(frustumPlanes, volumeAABB);
        }

        // TODO: We need to get rid of Handles.DrawWireCube to be able to have those at runtime as well.
        void OnDrawGizmos()
        {
            if (!ProbeReferenceVolume.instance.isInitialized || !IsResponsibleToDrawGizmo() || ProbeReferenceVolume.instance.sceneData == null)
                return;

            var debugDisplay = ProbeReferenceVolume.instance.probeVolumeDebug;

            var cellSizeInMeters = ProbeReferenceVolume.instance.MaxBrickSize();
            if (debugDisplay.realtimeSubdivision)
            {
                var profile = ProbeReferenceVolume.instance.sceneData.GetProfileForScene(gameObject.scene);
                if (profile == null)
                    return;
                cellSizeInMeters = profile.cellSizeInMeters;
            }

            if (debugDisplay.drawBricks)
            {
                var subdivColors = ProbeReferenceVolume.instance.subdivisionDebugColors;

                IEnumerable<ProbeBrickIndex.Brick> GetVisibleBricks()
                {
                    if (debugDisplay.realtimeSubdivision)
                    {
                        // realtime subdiv cells are already culled
                        foreach (var kp in ProbeReferenceVolume.instance.realtimeSubdivisionInfo)
                        {
                            var cellVolume = kp.Key;

                            foreach (var brick in kp.Value)
                            {
                                yield return brick;
                            }
                        }
                    }
                    else
                    {
                        foreach (var cellInfo in ProbeReferenceVolume.instance.cells.Values)
                        {
                            if (!cellInfo.loaded)
                                continue;

                            if (ShouldCullCell(cellInfo.cell.position, ProbeReferenceVolume.instance.GetTransform().posWS))
                                continue;

                            if (cellInfo.cell.bricks == null)
                                continue;

                            foreach (var brick in cellInfo.cell.bricks)
                                yield return brick;
                        }
                    }
                }

                if (brickGizmos == null)
                    brickGizmos = new MeshGizmo((int)(Mathf.Pow(3, ProbeBrickIndex.kMaxSubdivisionLevels) * MeshGizmo.vertexCountPerCube));

                brickGizmos.Clear();
                foreach (var brick in GetVisibleBricks())
                {
                    if (brick.subdivisionLevel < 0)
                        continue;

                    float brickSize = ProbeReferenceVolume.instance.BrickSize(brick.subdivisionLevel);
                    float minBrickSize = ProbeReferenceVolume.instance.MinBrickSize();
                    Vector3 scaledSize = new Vector3(brickSize, brickSize, brickSize);
                    Vector3 scaledPos = new Vector3(brick.position.x * minBrickSize, brick.position.y * minBrickSize, brick.position.z * minBrickSize) + scaledSize / 2;
                    brickGizmos.AddWireCube(scaledPos, scaledSize, subdivColors[brick.subdivisionLevel]);
                }

                brickGizmos.RenderWireframe(Matrix4x4.identity, gizmoName: "Brick Gizmo Rendering");
            }

            if (debugDisplay.drawCells)
            {
                IEnumerable<Vector4> GetVisibleCellCentersAndState()
                {
                    if (debugDisplay.realtimeSubdivision)
                    {
                        foreach (var kp in ProbeReferenceVolume.instance.realtimeSubdivisionInfo)
                        {
                            var center = kp.Key.center;
                            yield return new Vector4(center.x, center.y, center.z, 1.0f);
                        }
                    }
                    else
                    {
                        foreach (var cellInfo in ProbeReferenceVolume.instance.cells.Values)
                        {
                            if (ShouldCullCell(cellInfo.cell.position, ProbeReferenceVolume.instance.GetTransform().posWS))
                                continue;

                            var cell = cellInfo.cell;
                            var positionF = new Vector4(cell.position.x, cell.position.y, cell.position.z, 0.0f);
                            var center = positionF * cellSizeInMeters + cellSizeInMeters * 0.5f * Vector4.one;
                            center.w = cellInfo.loaded ? 1.0f : 0.0f;
                            yield return center;
                        }
                    }
                }

                Matrix4x4 trs = Matrix4x4.TRS(ProbeReferenceVolume.instance.GetTransform().posWS, ProbeReferenceVolume.instance.GetTransform().rot, Vector3.one);
                var oldGizmoMatrix = Gizmos.matrix;

                if (cellGizmo == null)
                    cellGizmo = new MeshGizmo();
                cellGizmo.Clear();
                foreach (var center in GetVisibleCellCentersAndState())
                {
                    bool loaded = center.w == 1.0f;

                    Gizmos.color = loaded ? new Color(0, 1, 0.5f, 0.2f) : new Color(1, 0.0f, 0.0f, 0.2f);
                    Gizmos.matrix = trs;

                    Gizmos.DrawCube(center, Vector3.one * cellSizeInMeters);
                    cellGizmo.AddWireCube(center, Vector3.one * cellSizeInMeters, loaded ? new Color(0, 1, 0.5f, 1) : new Color(1, 0.0f, 0.0f, 1));
                }
                cellGizmo.RenderWireframe(Gizmos.matrix, gizmoName: "Brick Gizmo Rendering");
                Gizmos.matrix = oldGizmoMatrix;
            }
        }
        #endregion

#endif // UNITY_EDITOR
    }
} // UnityEngine.Rendering.HDPipeline
