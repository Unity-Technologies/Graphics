using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using System.IO;
using System;
using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Experimental.Rendering
{
    // TMP to be moved to ProbeReferenceVolume when we define the concept, here it is just to make stuff compile
    enum ProbeVolumeState
    {
        Default = 0,
        Invalid = 999
    }

    [ExecuteAlways]
    [AddComponentMenu("")] // Hide.
    internal class ProbeVolumePerSceneData : MonoBehaviour, ISerializationCallbackReceiver
    {
        [System.Serializable]
        struct SerializableAssetItem
        {
            [SerializeField] public ProbeVolumeState state;
            [SerializeField] public ProbeVolumeAsset asset;
        }

        internal Dictionary<ProbeVolumeState, ProbeVolumeAsset> assets = new Dictionary<ProbeVolumeState, ProbeVolumeAsset>();

        [SerializeField] List<SerializableAssetItem> serializedAssets;

        [NonSerialized] ProbeVolumeState m_CurrentState = ProbeVolumeState.Default;
        [NonSerialized] ProbeVolumeState m_PreviousState = ProbeVolumeState.Invalid;

        // Field used for the realtime subdivision preview
        [NonSerialized]
        internal Dictionary<ProbeReferenceVolume.Volume, List<ProbeBrickIndex.Brick>> realtimeSubdivisionInfo = new Dictionary<ProbeReferenceVolume.Volume, List<ProbeBrickIndex.Brick>>();

        MeshGizmo brickGizmos;
        MeshGizmo cellGizmo;

        void DisposeGizmos()
        {
            brickGizmos?.Dispose();
            brickGizmos = null;
            cellGizmo?.Dispose();
            cellGizmo = null;
        }

        /// <summary>
        /// OnAfterDeserialize implementation.
        /// </summary>
        public void OnAfterDeserialize()
        {
            if (serializedAssets == null) return;

            assets = new Dictionary<ProbeVolumeState, ProbeVolumeAsset>();
            foreach (var assetItem in serializedAssets)
            {
                assets.Add(assetItem.state, assetItem.asset);
            }
        }

        /// <summary>
        /// OnBeforeSerialize implementation.
        /// </summary>
        public void OnBeforeSerialize()
        {
            if (assets == null || serializedAssets == null) return;

            serializedAssets.Clear();
            foreach (var k in assets.Keys)
            {
                SerializableAssetItem item;
                item.state = k;
                item.asset = assets[k];
                serializedAssets.Add(item);
            }
        }

        internal void StoreAssetForState(ProbeVolumeState state, ProbeVolumeAsset asset)
        {
            assets[state] = asset;
        }

        internal void InvalidateAllAssets()
        {
            foreach (var asset in assets.Values)
            {
                if (asset != null)
                    ProbeReferenceVolume.instance.AddPendingAssetRemoval(asset);
            }

            assets.Clear();
        }

        internal ProbeVolumeAsset GetCurrentStateAsset()
        {
            if (assets.ContainsKey(m_CurrentState)) return assets[m_CurrentState];
            else return null;
        }

        internal void QueueAssetLoading()
        {
            if (assets.ContainsKey(m_CurrentState) && assets[m_CurrentState] != null)
                ProbeReferenceVolume.instance.AddPendingAssetLoading(assets[m_CurrentState]);
        }

        internal void QueueAssetRemoval()
        {
            if (assets.ContainsKey(m_CurrentState) && assets[m_CurrentState] != null)
                ProbeReferenceVolume.instance.AddPendingAssetRemoval(assets[m_CurrentState]);
        }

        void OnEnable()
        {
            QueueAssetLoading();
        }

        void OnDisable()
        {
            QueueAssetRemoval();
            DisposeGizmos();
        }

        void OnDestroy()
        {
            QueueAssetRemoval();
            DisposeGizmos();
        }

        void Update()
        {
            // Query state from ProbeReferenceVolume.instance.
            // This is temporary here until we implement a state system.
            m_CurrentState = ProbeVolumeState.Default;

            if (m_PreviousState != m_CurrentState)
            {
                if (assets.ContainsKey(m_PreviousState) && assets[m_PreviousState] != null)
                    ProbeReferenceVolume.instance.AddPendingAssetRemoval(assets[m_PreviousState]);

                QueueAssetLoading();
            }
            m_PreviousState = m_CurrentState;
        }

#if UNITY_EDITOR
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
            if (!enabled || !gameObject.activeSelf || !ProbeReferenceVolume.instance.isInitialized || ProbeReferenceVolume.instance.sceneData == null)
                return;

            var profile = ProbeReferenceVolume.instance.sceneData.GetProfileForScene(gameObject.scene);
            if (profile == null) return;

            var debugDisplay = ProbeReferenceVolume.instance.debugDisplay;

            if (debugDisplay.drawBricks)
            {
                var subdivColors = ProbeReferenceVolume.instance.subdivisionDebugColors;

                IEnumerable<ProbeBrickIndex.Brick> GetVisibleBricks()
                {
                    if (debugDisplay.realtimeSubdivision)
                    {
                        // realtime subdiv cells are already culled
                        foreach (var kp in realtimeSubdivisionInfo)
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
                        foreach (var kp in realtimeSubdivisionInfo)
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
    }
}
