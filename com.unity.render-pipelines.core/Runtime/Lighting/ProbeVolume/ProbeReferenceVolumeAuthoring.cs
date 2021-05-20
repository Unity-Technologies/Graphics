using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
using System.Collections.Generic;
#endif

namespace UnityEngine.Experimental.Rendering
{
    // TODO: Use this structure in the actual authoring component rather than just a mean to group output parameters.
    internal struct ProbeDilationSettings
    {
        public bool dilate;
        public int maxDilationSamples;
        public float maxDilationSampleDistance;
        public float dilationValidityThreshold;
        public bool greedyDilation;

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
        internal float brickSize { get { return m_Profile.brickSize; } }
        internal int cellSize { get { return m_Profile.cellSize; } }
        internal int maxSubdivision { get { return m_Profile.maxSubdivision; } }

#if UNITY_EDITOR
        // Dilation
        [SerializeField]
        bool m_Dilate = false;
        [SerializeField]
        int m_MaxDilationSamples = 16;
        [SerializeField]
        float m_MaxDilationSampleDistance = 1f;
        [SerializeField]
        float m_DilationValidityThreshold = 0.25f;
        [SerializeField]
        bool m_GreedyDilation = false;

        Dictionary<ProbeReferenceVolume.Cell, MeshGizmo> brickGizmos = new Dictionary<ProbeReferenceVolume.Cell, MeshGizmo>();
        MeshGizmo cellGizmo;

        // In some cases Unity will magically popuplate this private field with a correct value even though it should not be serialized.
        // The [NonSerialized] attribute allows to force the asset to be null in case a domain reload happens.
        [System.NonSerialized]
        ProbeVolumeAsset m_PrevAsset = null;
#endif
        public ProbeVolumeAsset volumeAsset = null;

        internal void QueueAssetLoading()
        {
            if (volumeAsset == null || m_Profile == null)
                return;

            var refVol = ProbeReferenceVolume.instance;
            refVol.SetTRS(Vector3.zero, Quaternion.identity, m_Profile.brickSize);
            refVol.SetMaxSubdivision(m_Profile.maxSubdivision);

            refVol.AddPendingAssetLoading(volumeAsset);
        }

        internal void QueueAssetRemoval()
        {
            if (volumeAsset == null)
                return;

#if UNITY_EDITOR
            foreach (var meshGizmo in brickGizmos.Values)
                meshGizmo.Dispose();
            brickGizmos.Clear();
            cellGizmo?.Dispose();

            m_PrevAsset = null;
#endif

            ProbeReferenceVolume.instance.AddPendingAssetRemoval(volumeAsset);
        }

        void Start()
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

            Vector3 cellCenterWS = cellPosition * m_Profile.cellSize + originWS + Vector3.one * (m_Profile.cellSize / 2.0f);

            // Round down to cell size distance
            float roundedDownDist = Mathf.Floor(Vector3.Distance(cameraTransform.position, cellCenterWS) / m_Profile.cellSize) * m_Profile.cellSize;

            if (roundedDownDist > ProbeReferenceVolume.instance.debugDisplay.subdivisionViewCullingDistance)
                return true;

            var frustumPlanes = GeometryUtility.CalculateFrustumPlanes(SceneView.lastActiveSceneView.camera);
            var volumeAABB = new Bounds(cellCenterWS, m_Profile.cellSize * Vector3.one);

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
                foreach (var cell in ProbeReferenceVolume.instance.cells.Values)
                {
                    if (ShouldCullCell(cell.position, ProbeReferenceVolume.instance.GetTransform().posWS))
                        continue;

                    if (cell.bricks == null)
                        continue;

                    if (!brickGizmos.TryGetValue(cell, out var meshGizmo))
                        meshGizmo = AddBrickGizmo(cell);

                    meshGizmo.RenderWireframe(ProbeReferenceVolume.instance.GetRefSpaceToWS(), gizmoName: "Brick Gizmo Rendering");

                    MeshGizmo AddBrickGizmo(ProbeReferenceVolume.Cell cell)
                    {
                        var meshGizmo = new MeshGizmo((int)(Mathf.Pow(3, ProbeBrickIndex.kMaxSubdivisionLevels) * MeshGizmo.vertexCountPerCube));
                        meshGizmo.Clear();
                        foreach (var brick in cell.bricks)
                        {
                            Vector3 scaledSize = Vector3.one * Mathf.Pow(3, brick.subdivisionLevel);
                            Vector3 scaledPos = brick.position + scaledSize / 2;
                            meshGizmo.AddWireCube(scaledPos, scaledSize, subdivColors[brick.subdivisionLevel]);
                        }
                        brickGizmos[cell] = meshGizmo;
                        return meshGizmo;
                    }
                }
            }

            if (debugDisplay.drawCells)
            {
                // Fetching this from components instead of from the reference volume allows the user to
                // preview how cells will look before they commit to a bake.
                Gizmos.color = new Color(0, 1, 0.5f, 0.2f);
                Gizmos.matrix = Matrix4x4.TRS(ProbeReferenceVolume.instance.GetTransform().posWS, ProbeReferenceVolume.instance.GetTransform().rot, Vector3.one);
                if (cellGizmo == null)
                    cellGizmo = new MeshGizmo();
                cellGizmo.Clear();
                foreach (var cell in ProbeReferenceVolume.instance.cells.Values)
                {
                    if (ShouldCullCell(cell.position, transform.position))
                        continue;

                    var positionF = new Vector3(cell.position.x, cell.position.y, cell.position.z);
                    var center = positionF * m_Profile.cellSize + m_Profile.cellSize * 0.5f * Vector3.one;
                    Gizmos.DrawCube(center, Vector3.one * m_Profile.cellSize);
                    cellGizmo.AddWireCube(center, Vector3.one * m_Profile.cellSize, new Color(0, 1, 0.5f, 1));
                }
                cellGizmo.RenderWireframe(Gizmos.matrix, gizmoName: "Brick Gizmo Rendering");
            }
        }

        public ProbeDilationSettings GetDilationSettings()
        {
            ProbeDilationSettings settings;
            settings.dilate = m_Dilate;
            settings.dilationValidityThreshold = m_DilationValidityThreshold;
            settings.greedyDilation = m_GreedyDilation;
            settings.maxDilationSampleDistance = m_MaxDilationSampleDistance;
            settings.maxDilationSamples = m_MaxDilationSamples;
            settings.brickSize = brickSize;

            return settings;
        }

#endif
    }
}
