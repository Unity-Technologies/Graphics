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
        public bool squaredDistWeighting;
        public float brickSize;   // Not really a dilation setting, but used during dilation.
    }

    internal struct VirtualOffsetSettings
    {
        public bool useVirtualOffset;
        public float outOfGeoOffset;
        public float searchMultiplier;
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
        bool m_DilationInvSquaredWeight = true;

        [SerializeField]
        bool m_EnableDilation = true;

        // Virtual offset proof of concept.
        [SerializeField]
        bool m_EnableVirtualOffset = true;
        [SerializeField]
        float m_VirtualOffsetGeometrySearchMultiplier = 0.2f;
        [SerializeField]
        float m_VirtualOffsetBiasOutOfGeometry = 0.01f;

        // Field used for the realtime subdivision preview
        [NonSerialized]
        internal Dictionary<ProbeReferenceVolume.Volume, List<ProbeBrickIndex.Brick>> realtimeSubdivisionInfo = new Dictionary<ProbeReferenceVolume.Volume, List<ProbeBrickIndex.Brick>>();

        MeshGizmo m_MeshGizmo;

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
            m_MeshGizmo?.Dispose();
            m_MeshGizmo = null;

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
            var prv = ProbeReferenceVolume.instance;

            if (!enabled || !gameObject.activeSelf || !prv.isInitialized)
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
                        foreach (var kp in realtimeSubdivisionInfo)
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
                            var center = positionF * m_Profile.cellSizeInMeters + m_Profile.cellSizeInMeters * 0.5f * Vector4.one;
                            center.w = cellInfo.loaded ? 1.0f : 0.0f;
                            yield return center;
                        }
                    }
                }

                foreach (var center in GetVisibleCellCentersAndState())
                {
                    bool loaded = center.w == 1.0f;
                    m_MeshGizmo.AddWireCube(center, Vector3.one * m_Profile.cellSizeInMeters, loaded ? new Color(0, 1, 0.5f, 1) : new Color(1, 0.0f, 0.0f, 1));
                    m_MeshGizmo.AddCube(center, Vector3.one * m_Profile.cellSizeInMeters, loaded ? new Color(0, 1, 0.5f, 0.2f) : new Color(1, 0.0f, 0.0f, 0.2f));
                }
            }

            m_MeshGizmo.Draw(Matrix4x4.identity, gizmoName: "Probe Volume Cell Rendering");
        }

        public ProbeDilationSettings GetDilationSettings()
        {
            ProbeDilationSettings settings;
            settings.dilationValidityThreshold =  m_DilationValidityThreshold;
            settings.dilationDistance = m_EnableDilation ? m_MaxDilationSampleDistance : 0.0f;
            settings.dilationIterations = m_DilationIterations;
            settings.squaredDistWeighting = m_DilationInvSquaredWeight;
            settings.brickSize = brickSize;

            return settings;
        }

        public VirtualOffsetSettings GetVirtualOffsetSettings()
        {
            VirtualOffsetSettings settings;
            settings.useVirtualOffset = m_EnableVirtualOffset;
            settings.searchMultiplier = m_VirtualOffsetGeometrySearchMultiplier;
            settings.outOfGeoOffset = m_VirtualOffsetBiasOutOfGeometry;

            return settings;
        }

#endif
    }
}
