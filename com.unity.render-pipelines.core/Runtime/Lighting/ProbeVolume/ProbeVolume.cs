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

        MeshGizmo m_MeshGizmo;

        void DisposeGizmos()
        {
            m_MeshGizmo?.Dispose();
            m_MeshGizmo = null;
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
            var prv = ProbeReferenceVolume.instance;

            if (!prv.isInitialized || !IsResponsibleToDrawGizmo() || prv.sceneData == null)
                return;

            var profile = ProbeReferenceVolume.instance.sceneData.GetProfileForScene(gameObject.scene);
            if (profile == null)
                return;

            var debugDisplay = prv.debugDisplay;

            if (m_MeshGizmo == null)
                m_MeshGizmo = new MeshGizmo((int)(Mathf.Pow(3, ProbeBrickIndex.kMaxSubdivisionLevels) * 12), prv.cells.Count * 12); // 12 lines and 12 triangles per cube

            m_MeshGizmo.Clear();

            if (debugDisplay.drawBricks)
            {
                var subdivColors = prv.subdivisionDebugColors;

                IEnumerable<ProbeBrickIndex.Brick> GetVisibleBricks()
                {
                    if (debugDisplay.realtimeSubdivision)
                    {
                        // realtime subdiv cells are already culled
                        foreach (var kp in prv.realtimeSubdivisionInfo)
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
                        foreach (var cellInfo in prv.cells.Values)
                        {
                            if (!cellInfo.loaded)
                                continue;

                            var cell = cellInfo.cell;
                            if (ShouldCullCell(cell.position, prv.GetTransform().posWS))
                                continue;

                            if (cell.bricks == null)
                                continue;

                            foreach (var brick in cell.bricks)
                                yield return brick;
                        }
                    }
                }

                foreach (var brick in GetVisibleBricks())
                {
                    if (brick.subdivisionLevel < 0)
                        continue;

                    float brickSize = prv.BrickSize(brick.subdivisionLevel);
                    float minBrickSize = prv.MinBrickSize();
                    Vector3 scaledSize = new Vector3(brickSize, brickSize, brickSize);
                    Vector3 scaledPos = new Vector3(brick.position.x * minBrickSize, brick.position.y * minBrickSize, brick.position.z * minBrickSize) + scaledSize / 2;

                    m_MeshGizmo.AddWireCube(scaledPos, scaledSize, subdivColors[brick.subdivisionLevel]);
                }
            }

            if (debugDisplay.drawCells)
            {
                IEnumerable<Vector4> GetVisibleCellCentersAndState()
                {
                    if (debugDisplay.realtimeSubdivision)
                    {
                        foreach (var kp in prv.realtimeSubdivisionInfo)
                        {
                            kp.Key.CalculateCenterAndSize(out var center, out var _);
                            yield return new Vector4(center.x, center.y, center.z, 1.0f);
                        }
                    }
                    else
                    {
                        foreach (var cellInfo in prv.cells.Values)
                        {
                            var cell = cellInfo.cell;
                            if (ShouldCullCell(cell.position, prv.GetTransform().posWS))
                                continue;

                            var positionF = new Vector4(cell.position.x, cell.position.y, cell.position.z, 0.0f);
                            var center = positionF * profile.cellSizeInMeters + profile.cellSizeInMeters * 0.5f * Vector4.one;
                            center.w = cellInfo.loaded ? 1.0f : 0.0f;
                            yield return center;
                        }
                    }
                }

                foreach (var center in GetVisibleCellCentersAndState())
                {
                    bool loaded = center.w == 1.0f;
                    m_MeshGizmo.AddWireCube(center, Vector3.one * profile.cellSizeInMeters, loaded ? new Color(0, 1, 0.5f, 1) : new Color(1, 0.0f, 0.0f, 1));
                    m_MeshGizmo.AddCube(center, Vector3.one * profile.cellSizeInMeters, loaded ? new Color(0, 1, 0.5f, 0.2f) : new Color(1, 0.0f, 0.0f, 0.2f));
                }
            }

            m_MeshGizmo.Draw(Matrix4x4.identity, gizmoName: "Probe Volume Cell Rendering");
        }

#endif
        #endregion
    }
} // UnityEngine.Experimental.Rendering.HDPipeline
