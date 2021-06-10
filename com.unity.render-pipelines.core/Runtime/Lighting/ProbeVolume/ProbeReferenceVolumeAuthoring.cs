using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using System.IO;
using System;

#if UNITY_EDITOR
using UnityEditor;
using System.Collections.Generic;
#endif

namespace UnityEngine.Experimental.Rendering
{
    // TODO: Use this structure in the actual authoring component rather than just a mean to group output parameters.
    internal struct ProbeDilationSettings
    {
        public float dilationDistance;
        public float dilationValidityThreshold;
        public float dilationIterations;
        public float brickSize;   // Not really a dilation setting, but used during dilation.
    }

    [ExecuteAlways]
    [AddComponentMenu("Light/Probe Reference Volume (Experimental)")]
    internal class ProbeReferenceVolumeAuthoring : MonoBehaviour
    {
#if UNITY_EDITOR
        internal static ProbeReferenceVolumeProfile CreateReferenceVolumeProfile(Scene scene, string targetName)
        {
            string path;
            if (string.IsNullOrEmpty(scene.path))
            {
                path = "Assets/";
            }
            else
            {
                var scenePath = Path.GetDirectoryName(scene.path);
                var extPath = scene.name;
                var profilePath = scenePath + Path.DirectorySeparatorChar + extPath;

                if (!AssetDatabase.IsValidFolder(profilePath))
                {
                    var directories = profilePath.Split(Path.DirectorySeparatorChar);
                    string rootPath = "";
                    foreach (var directory in directories)
                    {
                        var newPath = rootPath + directory;
                        if (!AssetDatabase.IsValidFolder(newPath))
                            AssetDatabase.CreateFolder(rootPath.TrimEnd(Path.DirectorySeparatorChar), directory);
                        rootPath = newPath + Path.DirectorySeparatorChar;
                    }
                }

                path = profilePath + Path.DirectorySeparatorChar;
            }

            path += targetName + " Profile.asset";
            path = AssetDatabase.GenerateUniqueAssetPath(path);

            var profile = ScriptableObject.CreateInstance<ProbeReferenceVolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return profile;
        }

#endif

        [SerializeField]
        ProbeReferenceVolumeProfile m_Profile = null;
#if UNITY_EDITOR
        ProbeReferenceVolumeProfile m_PrevProfile = null;
#endif

        internal ProbeReferenceVolumeProfile profile { get { return m_Profile; } }
        internal float brickSize { get { return m_Profile.minBrickSize; } }
        internal float cellSizeInMeters { get { return m_Profile.cellSizeInMeters; } }
        internal int maxSubdivision { get { return m_Profile.maxSubdivision; } }

#if UNITY_EDITOR
        // Dilation
        [SerializeField]
        float m_MaxDilationSampleDistance = 1f;
        [SerializeField]
        int m_DilationIterations = 1;

        [SerializeField]
        bool m_EnableDilation = true;

        // Field used for the realtime subdivision preview
        [NonSerialized]
        internal Dictionary<ProbeReferenceVolume.Volume, List<ProbeBrickIndex.Brick>> realtimeSubdivisionInfo = new Dictionary<ProbeReferenceVolume.Volume, List<ProbeBrickIndex.Brick>>();

        MeshGizmo brickGizmos;
        MeshGizmo cellGizmo;

        // In some cases Unity will magically popuplate this private field with a correct value even though it should not be serialized.
        // The [NonSerialized] attribute allows to force the asset to be null in case a domain reload happens.
        [NonSerialized]
        ProbeVolumeAsset m_PrevAsset = null;
#endif
        [SerializeField]
        float m_DilationValidityThreshold = 0.25f;

        public ProbeVolumeAsset volumeAsset = null;

        internal void LoadProfileInformation()
        {
            if (m_Profile == null)
                return;

            var refVol = ProbeReferenceVolume.instance;
            refVol.SetTRS(Vector3.zero, Quaternion.identity, m_Profile.minBrickSize);
            refVol.SetMaxSubdivision(m_Profile.maxSubdivision);
            refVol.dilationValidtyThreshold = m_DilationValidityThreshold;
        }

        internal void QueueAssetLoading()
        {
            LoadProfileInformation();

            if (volumeAsset != null)
                ProbeReferenceVolume.instance.AddPendingAssetLoading(volumeAsset);
        }

        internal void QueueAssetRemoval()
        {
            if (volumeAsset == null)
                return;

#if UNITY_EDITOR
            brickGizmos?.Dispose();
            brickGizmos = null;
            cellGizmo?.Dispose();
            cellGizmo = null;

            m_PrevAsset = null;
#endif

            ProbeReferenceVolume.instance.AddPendingAssetRemoval(volumeAsset);
        }

        void OnEnable()
        {
#if UNITY_EDITOR
            if (m_Profile == null)
                m_Profile = CreateReferenceVolumeProfile(gameObject.scene, gameObject.name);
#endif
            QueueAssetLoading();
        }

#if UNITY_EDITOR

        void OnValidate()
        {
            if (!enabled || !gameObject.activeSelf)
                return;

            if (m_Profile != null)
            {
                m_PrevProfile = m_Profile;
            }

            if (volumeAsset != m_PrevAsset && m_PrevAsset != null)
            {
                ProbeReferenceVolume.instance.AddPendingAssetRemoval(m_PrevAsset);
            }

            if (volumeAsset != m_PrevAsset)
            {
                QueueAssetLoading();
            }

            m_PrevAsset = volumeAsset;
        }

        void OnDisable()
        {
            QueueAssetRemoval();
        }

        void OnDestroy()
        {
            QueueAssetRemoval();
        }

        internal bool ShouldCullCell(Vector3 cellPosition, Vector3 originWS = default(Vector3))
        {
            if (m_Profile == null)
                return true;

            var cameraTransform = SceneView.lastActiveSceneView.camera.transform;

            Vector3 cellCenterWS = cellPosition * m_Profile.cellSizeInMeters + originWS + Vector3.one * (m_Profile.cellSizeInMeters / 2.0f);

            // Round down to cell size distance
            float roundedDownDist = Mathf.Floor(Vector3.Distance(cameraTransform.position, cellCenterWS) / m_Profile.cellSizeInMeters) * m_Profile.cellSizeInMeters;

            if (roundedDownDist > ProbeReferenceVolume.instance.debugDisplay.subdivisionViewCullingDistance)
                return true;

            var frustumPlanes = GeometryUtility.CalculateFrustumPlanes(SceneView.lastActiveSceneView.camera);
            var volumeAABB = new Bounds(cellCenterWS, m_Profile.cellSizeInMeters * Vector3.one);

            return !GeometryUtility.TestPlanesAABB(frustumPlanes, volumeAABB);
        }

        // TODO: We need to get rid of Handles.DrawWireCube to be able to have those at runtime as well.
        void OnDrawGizmos()
        {
            if (!enabled || !gameObject.activeSelf || !ProbeReferenceVolume.instance.isInitialized)
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
                            var center = positionF * m_Profile.cellSizeInMeters + m_Profile.cellSizeInMeters * 0.5f * Vector3.one;
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
                    Gizmos.DrawCube(center, Vector3.one * m_Profile.cellSizeInMeters);
                    cellGizmo.AddWireCube(center, Vector3.one * m_Profile.cellSizeInMeters, new Color(0, 1, 0.5f, 1));
                }
                cellGizmo.RenderWireframe(Gizmos.matrix, gizmoName: "Brick Gizmo Rendering");
            }
        }

        public ProbeDilationSettings GetDilationSettings()
        {
            ProbeDilationSettings settings;
            settings.dilationValidityThreshold =  m_DilationValidityThreshold;
            settings.dilationDistance = m_EnableDilation ? m_MaxDilationSampleDistance : 0.0f;
            settings.dilationIterations = m_DilationIterations;
            settings.brickSize = brickSize;

            return settings;
        }

#endif
    }
}
