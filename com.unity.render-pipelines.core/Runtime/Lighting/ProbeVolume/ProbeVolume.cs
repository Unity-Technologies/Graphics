using System;
using UnityEngine.Serialization;
using UnityEditor.Experimental;
using Unity.Collections;
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
        public bool globalVolume = false;
        public Vector3 size = new Vector3(10, 10, 10);
        [HideInInspector]
        public float maxSubdivisionMultiplier = 1;
        [HideInInspector]
        public float minSubdivisionMultiplier = 0;
        [HideInInspector, Range(0f, 2f)]
        public float geometryDistanceOffset = 0.2f;

        public LayerMask objectLayerMask = -1;

        [SerializeField] internal bool mightNeedRebaking = false;

        [SerializeField] internal Matrix4x4 cachedTransform;
        [SerializeField] internal int cachedHashCode;

        /// <summary>
        /// Returns the extents of the volume.
        /// </summary>
        /// <returns>The extents of the ProbeVolume.</returns>
        public Vector3 GetExtents()
        {
            return size;
        }

#if UNITY_EDITOR
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
                hash = hash * 23 + maxSubdivisionMultiplier.GetHashCode();
                hash = hash * 23 + minSubdivisionMultiplier.GetHashCode();
                hash = hash * 23 + geometryDistanceOffset.GetHashCode();
                hash = hash * 23 + objectLayerMask.GetHashCode();
            }

            return hash;
        }

#endif

        // Momentarily moving the gizmo rendering for bricks and cells to Probe Volume itself,
        // only the first probe volume in the scene will render them. The reason is that we dont have any
        // other non-hidden component related to APV.
        #region APVGizmo

        MeshGizmo brickGizmos;
        MeshGizmo cellGizmo;

        void DisposeGizmos()
        {
            brickGizmos?.Dispose();
            brickGizmos = null;
            cellGizmo?.Dispose();
            cellGizmo = null;
        }

        void OnDestroy()
        {
            DisposeGizmos();
        }

        void OnDisable()
        {
            DisposeGizmos();
        }
#if UNITY_EDITOR

        // Only the first PV of the available ones will draw gizmos.
        bool IsResponsibleToDrawGizmo()
        {
            var pvList = GameObject.FindObjectsOfType<ProbeVolume>();
            return this == pvList[0];
        }

        internal bool ShouldCullCell(Vector3 cellPosition, Vector3 originWS = default(Vector3))
        {
            var profile = ProbeReferenceVolume.instance.sceneData.GetProfileForScene(gameObject.scene);
            if (profile == null)
                return true;

            var cameraTransform = SceneView.lastActiveSceneView.camera.transform;

            Vector3 cellCenterWS = cellPosition * profile.cellSizeInMeters + originWS + Vector3.one * (profile.cellSizeInMeters / 2.0f);

            // Round down to cell size distance
            float roundedDownDist = Mathf.Floor(Vector3.Distance(cameraTransform.position, cellCenterWS) / profile.cellSizeInMeters) * profile.cellSizeInMeters;

            if (roundedDownDist > ProbeReferenceVolume.instance.debugDisplay.subdivisionViewCullingDistance)
                return true;

            var frustumPlanes = GeometryUtility.CalculateFrustumPlanes(SceneView.lastActiveSceneView.camera);
            var volumeAABB = new Bounds(cellCenterWS, profile.cellSizeInMeters * Vector3.one);

            return !GeometryUtility.TestPlanesAABB(frustumPlanes, volumeAABB);
        }

        // TODO: We need to get rid of Handles.DrawWireCube to be able to have those at runtime as well.
        void OnDrawGizmos()
        {
            if (!ProbeReferenceVolume.instance.isInitialized || !IsResponsibleToDrawGizmo() || ProbeReferenceVolume.instance.sceneData == null)
                return;

            var profile = ProbeReferenceVolume.instance.sceneData.GetProfileForScene(gameObject.scene);
            if (profile == null)
                return;

            var debugDisplay = ProbeReferenceVolume.instance.debugDisplay;

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
                            var center = positionF * profile.cellSizeInMeters + profile.cellSizeInMeters * 0.5f * Vector3.one;
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
                    Gizmos.DrawCube(center, Vector3.one * profile.cellSizeInMeters);
                    cellGizmo.AddWireCube(center, Vector3.one * profile.cellSizeInMeters, new Color(0, 1, 0.5f, 1));
                }
                cellGizmo.RenderWireframe(Gizmos.matrix, gizmoName: "Brick Gizmo Rendering");
            }
        }

#endif
        #endregion
    }
} // UnityEngine.Experimental.Rendering.HDPipeline
