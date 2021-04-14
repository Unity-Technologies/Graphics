using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Rendering
{
    // TODO: Use this structure in the actual authoring component rather than just a mean to group output parameters.
    internal struct ProbeDilationSettings
    {
        public bool dilate;
        public int maxDilationSamples;
        public float maxDilationSampleDistance;
        public float dilationValidityThreshold;
        public bool greedyDilation;

        public int brickSize;   // Not really a dilation setting, but used during dilation.
    }

    [ExecuteAlways]
    [AddComponentMenu("Light/Experimental/Probe Reference Volume")]
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
        public enum ProbeShadingMode
        {
            Size,
            SH,
            Validity
        }

        [SerializeField]
        private ProbeReferenceVolumeProfile m_Profile = null;
#if UNITY_EDITOR
        private ProbeReferenceVolumeProfile m_PrevProfile = null;
#endif

        internal ProbeReferenceVolumeProfile profile { get { return m_Profile; } }
        internal int brickSize { get { return m_Profile.brickSize; } }
        internal int cellSize { get { return m_Profile.cellSize; } }
        internal int maxSubdivision { get { return m_Profile.maxSubdivision; } }
        internal float normalBias { get { return m_Profile.normalBias; } }

#if UNITY_EDITOR
        [SerializeField]
        private bool m_DrawProbes;
        [SerializeField]
        private bool m_DrawBricks;
        [SerializeField]
        private bool m_DrawCells;

        // Debug shading
        [SerializeField]
        private ProbeShadingMode m_ProbeShading;
        [SerializeField]
        private float m_CullingDistance = 200;
        [SerializeField]
        private float m_Exposure;

        // Dilation
        [SerializeField]
        private bool m_Dilate = false;
        [SerializeField]
        private int m_MaxDilationSamples = 16;
        [SerializeField]
        private float m_MaxDilationSampleDistance = 1f;
        [SerializeField]
        private float m_DilationValidityThreshold = 0.25f;
        [SerializeField]
        private bool m_GreedyDilation = false;

        private ProbeVolumeAsset m_PrevAsset = null;
#endif
        public ProbeVolumeAsset volumeAsset = null;

        internal void QueueAssetLoading()
        {
            if (volumeAsset == null || m_Profile == null)
                return;

            var refVol = ProbeReferenceVolume.instance;
            refVol.Clear();
            refVol.SetTRS(transform.position, transform.rotation, m_Profile.brickSize);
            refVol.SetMaxSubdivision(m_Profile.maxSubdivision);
            refVol.SetNormalBias(m_Profile.normalBias);

            refVol.AddPendingAssetLoading(volumeAsset);
        }

        internal void QueueAssetRemoval()
        {
            if (volumeAsset == null)
                return;

#if UNITY_EDITOR
            m_PrevAsset = null;
#endif

            ProbeReferenceVolume.instance.AddPendingAssetRemoval(volumeAsset);
        }

        private void Start()
        {
#if UNITY_EDITOR
            if (m_Profile == null)
                m_Profile = CreateReferenceVolumeProfile(gameObject.scene, gameObject.name);
#endif
            QueueAssetLoading();
        }

#if UNITY_EDITOR

        private void OnValidate()
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

        private void OnDisable()
        {
            QueueAssetRemoval();
        }

        private void OnDestroy()
        {
            QueueAssetRemoval();
        }

        internal bool ShouldCull(Vector3 cellPosition, Vector3 originWS = default(Vector3))
        {
            if (m_Profile == null)
                return true;

            Vector3 cellCenterWS = cellPosition * m_Profile.cellSize + originWS + Vector3.one * (m_Profile.cellSize / 2.0f);
            if (Vector3.Distance(SceneView.lastActiveSceneView.camera.transform.position, cellCenterWS) > m_CullingDistance)
                return true;

            var frustumPlanes = GeometryUtility.CalculateFrustumPlanes(SceneView.lastActiveSceneView.camera);
            var volumeAABB = new Bounds(cellCenterWS, m_Profile.cellSize * Vector3.one);

            return !GeometryUtility.TestPlanesAABB(frustumPlanes, volumeAABB);
        }

        private void OnDrawGizmos()
        {
            if (!enabled || !gameObject.activeSelf)
                return;

            Handles.zTest = CompareFunction.LessEqual;

            if (m_DrawCells)
            {
                // Fetching this from components instead of from the reference volume allows the user to
                // preview how cells will look before they commit to a bake.
                using (new Handles.DrawingScope(Color.green, ProbeReferenceVolume.instance.GetRefSpaceToWS()))
                {
                    foreach (var cell in ProbeReferenceVolume.instance.cells.Values)
                    {
                        if (ShouldCull(cell.position, transform.position))
                            continue;

                        var positionF = new Vector3(cell.position.x, cell.position.y, cell.position.z);
                        var center = positionF * m_Profile.cellSize + m_Profile.cellSize * 0.5f * Vector3.one;
                        Handles.DrawWireCube(center, Vector3.one * m_Profile.cellSize);
                    }
                }
            }

            if (m_DrawBricks)
            {
                using (new Handles.DrawingScope(Color.blue, ProbeReferenceVolume.instance.GetRefSpaceToWS()))
                {
                    // Read refvol transform
                    foreach (var cell in ProbeReferenceVolume.instance.cells.Values)
                    {
                        if (ShouldCull(cell.position, ProbeReferenceVolume.instance.GetTransform().posWS))
                            continue;

                        if (cell.bricks == null)
                            continue;

                        foreach (var brick in cell.bricks)
                        {
                            Vector3 scaledSize = Vector3.one * Mathf.Pow(3, brick.size);
                            Vector3 scaledPos = brick.position + scaledSize / 2;
                            Handles.DrawWireCube(scaledPos, scaledSize);
                        }
                    }
                }
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
