using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Experimental.Rendering
{
    /// <summary>
    /// A marker to determine what area of the scene is considered by the Probe Volumes system
    /// </summary>
    [ExecuteAlways]
    [AddComponentMenu("Light/Probe Volume (Experimental)")]
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
        /// Geometry distance offset
        /// </summary>
        [HideInInspector, Range(0f, 2f)]
        public float geometryDistanceOffset = 0.2f;

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

#if UNITY_EDITOR
        /// <summary>
        /// Returns the extents of the volume.
        /// </summary>
        /// <returns>The extents of the ProbeVolume.</returns>
        public Vector3 GetExtents()
        {
            return size;
        }

        internal void UpdateGlobalVolume(Scene scene)
        {
            if (gameObject.scene != scene) return;

            Bounds bounds = new Bounds();
            bool foundABound = false;
            bool ContributesToGI(Renderer renderer)
            {
                var flags = GameObjectUtility.GetStaticEditorFlags(renderer.gameObject) & StaticEditorFlags.ContributeGI;
                return (flags & StaticEditorFlags.ContributeGI) != 0;
            }

            void ExpandBounds(Bounds currBound)
            {
                if (!foundABound)
                {
                    bounds = currBound;
                    foundABound = true;
                }
                else
                {
                    bounds.Encapsulate(currBound);
                }
            }

            var renderers = UnityEngine.GameObject.FindObjectsOfType<Renderer>();

            foreach (Renderer renderer in renderers)
            {
                bool contributeGI = ContributesToGI(renderer) && renderer.gameObject.activeInHierarchy && renderer.enabled;

                if (contributeGI && renderer.gameObject.scene == scene)
                {
                    ExpandBounds(renderer.bounds);
                }
            }

            transform.position = bounds.center;

            float minBrickSize = ProbeReferenceVolume.instance.MinBrickSize();
            Vector3 tmpClamp = (bounds.size + new Vector3(minBrickSize, minBrickSize, minBrickSize));
            tmpClamp.x = Mathf.Max(0f, tmpClamp.x);
            tmpClamp.y = Mathf.Max(0f, tmpClamp.y);
            tmpClamp.z = Mathf.Max(0f, tmpClamp.z);
            size = tmpClamp;
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
                hash = hash * 23 + overridesSubdivLevels.GetHashCode();
                hash = hash * 23 + highestSubdivLevelOverride.GetHashCode();
                hash = hash * 23 + lowestSubdivLevelOverride.GetHashCode();
                hash = hash * 23 + geometryDistanceOffset.GetHashCode();
                hash = hash * 23 + objectLayerMask.GetHashCode();
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
            var debugDisplay = ProbeReferenceVolume.instance.debugDisplay;
            if (debugDisplay.realtimeSubdivision)
            {
                var profile = ProbeReferenceVolume.instance.sceneData.GetProfileForScene(gameObject.scene);
                if (profile == null)
                    return true;
                cellSizeInMeters = profile.cellSizeInMeters;
            }

            var cameraTransform = SceneView.lastActiveSceneView.camera.transform;

            Vector3 cellCenterWS = cellPosition * cellSizeInMeters + originWS + Vector3.one * (cellSizeInMeters / 2.0f);

            // Round down to cell size distance
            float roundedDownDist = Mathf.Floor(Vector3.Distance(cameraTransform.position, cellCenterWS) / cellSizeInMeters) * cellSizeInMeters;

            if (roundedDownDist > ProbeReferenceVolume.instance.debugDisplay.subdivisionViewCullingDistance)
                return true;

            var frustumPlanes = GeometryUtility.CalculateFrustumPlanes(SceneView.lastActiveSceneView.camera);
            var volumeAABB = new Bounds(cellCenterWS, cellSizeInMeters * Vector3.one);

            return !GeometryUtility.TestPlanesAABB(frustumPlanes, volumeAABB);
        }

        // TODO: We need to get rid of Handles.DrawWireCube to be able to have those at runtime as well.
        void OnDrawGizmos()
        {
            if (!ProbeReferenceVolume.instance.isInitialized || !IsResponsibleToDrawGizmo() || ProbeReferenceVolume.instance.sceneData == null)
                return;

            var debugDisplay = ProbeReferenceVolume.instance.debugDisplay;

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
                        foreach (var cell in ProbeReferenceVolume.instance.cells.Values)
                        {
                            if (ShouldCullCell(cell.position, ProbeReferenceVolume.instance.GetTransform().posWS))
                                continue;

                            if (cell.bricks == null)
                                continue;

                            foreach (var brick in cell.bricks)
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

                    Vector3 scaledSize = Vector3.one * Mathf.Pow(3, brick.subdivisionLevel);
                    Vector3 scaledPos = brick.position + scaledSize / 2;
                    brickGizmos.AddWireCube(scaledPos, scaledSize, subdivColors[brick.subdivisionLevel]);
                }

                brickGizmos.RenderWireframe(ProbeReferenceVolume.instance.GetRefSpaceToWS(), gizmoName: "Brick Gizmo Rendering");
            }

            if (debugDisplay.drawCells)
            {
                IEnumerable<Vector3> GetVisibleCellCenters()
                {
                    if (debugDisplay.realtimeSubdivision)
                    {
                        foreach (var kp in ProbeReferenceVolume.instance.realtimeSubdivisionInfo)
                        {
                            kp.Key.CalculateCenterAndSize(out var center, out var _);
                            yield return center;
                        }
                    }
                    else
                    {
                        foreach (var cell in ProbeReferenceVolume.instance.cells.Values)
                        {
                            if (ShouldCullCell(cell.position, ProbeReferenceVolume.instance.GetTransform().posWS))
                                continue;

                            var positionF = new Vector3(cell.position.x, cell.position.y, cell.position.z);
                            var center = positionF * cellSizeInMeters + cellSizeInMeters * 0.5f * Vector3.one;
                            yield return center;
                        }
                    }
                }

                Matrix4x4 trs = Matrix4x4.TRS(ProbeReferenceVolume.instance.GetTransform().posWS, ProbeReferenceVolume.instance.GetTransform().rot, Vector3.one);

                // For realtime subdivision, the matrix from ProbeReferenceVolume.instance can be wrong if the profile changed since the last bake
                if (debugDisplay.realtimeSubdivision)
                    trs = Matrix4x4.TRS(transform.position, Quaternion.identity, Vector3.one);

                // Fetching this from components instead of from the reference volume allows the user to
                // preview how cells will look before they commit to a bake.
                Gizmos.color = new Color(0, 1, 0.5f, 0.2f);
                Gizmos.matrix = trs;
                if (cellGizmo == null)
                    cellGizmo = new MeshGizmo();
                cellGizmo.Clear();
                foreach (var center in GetVisibleCellCenters())
                {
                    Gizmos.DrawCube(center, Vector3.one * cellSizeInMeters);
                    cellGizmo.AddWireCube(center, Vector3.one * cellSizeInMeters, new Color(0, 1, 0.5f, 1));
                }
                cellGizmo.RenderWireframe(Gizmos.matrix, gizmoName: "Brick Gizmo Rendering");
            }
        }
        #endregion

#endif // UNITY_EDITOR
    }
} // UnityEngine.Experimental.Rendering.HDPipeline
