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
        // debug gizmo data
        class CellInstancedDebugProbes
        {
            public List<Matrix4x4[]> probeBuffers;
            public List<MaterialPropertyBlock> props;
            public List<int[]> probeMaps;
            public Hash128 cellHash;
            public Vector3 cellPosition;
        }

        private const int probesPerBatch = 1023;
#if UNITY_EDITOR
        private Mesh debugMesh;
        private Material debugMaterial;
#endif
        private List<CellInstancedDebugProbes> cellDebugData = new List<CellInstancedDebugProbes>();

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
        internal Vector3Int indexDimensions { get { return m_Profile.indexDimensions; } }

#if UNITY_EDITOR
        [SerializeField]
        private bool m_DrawProbes = false;
        [SerializeField]
        private bool m_DrawBricks = false;
        [SerializeField]
        private bool m_DrawCells = false;

        // Debug shading
        [SerializeField]
        private ProbeShadingMode m_ProbeShading;
        [SerializeField]
        private float m_CullingDistance = 200;
        [SerializeField]
        private float m_Exposure = 0f;

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

            ProbeReferenceVolume.instance.AddPendingAssetRemoval(volumeAsset);
        }

        private void Start()
        {
#if UNITY_EDITOR
            if (m_Profile == null)
                m_Profile = CreateReferenceVolumeProfile(gameObject.scene, gameObject.name);

            CheckInit();
#else   // In player we load on start
            QueueAssetLoading();
#endif
        }

#if UNITY_EDITOR

        private void OnValidate()
        {
            if (!enabled || !gameObject.activeSelf)
                return;

            if (m_Profile != null)
            {
                bool hasIndexDimensionChangedOnProfileSwitch = m_PrevProfile == null || (m_PrevProfile != null && m_PrevProfile.indexDimensions != m_Profile.indexDimensions);
                if (hasIndexDimensionChangedOnProfileSwitch)
                {
                    var refVol = ProbeReferenceVolume.instance;
                    refVol.AddPendingIndexDimensionChange(indexDimensions);
                }

                m_PrevProfile = m_Profile;
                QueueAssetLoading();
            }

            if (volumeAsset != m_PrevAsset && m_PrevAsset != null)
            {
                ProbeReferenceVolume.instance.AddPendingAssetRemoval(m_PrevAsset);
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

        private bool ShouldCull(Vector3 cellPosition)
        {
            var refVolTranslation = this.transform.position;
            var refVolRotation = this.transform.rotation;

            Transform cam = SceneView.lastActiveSceneView.camera.transform;
            Vector3 camPos = cam.position;
            Vector3 camVec = cam.forward;

            float halfCellSize = m_Profile.cellSize * 0.5f;

            Vector3 cellPos = cellPosition * m_Profile.cellSize + halfCellSize * Vector3.one + refVolTranslation;
            Vector3 camToCell = cellPos - camPos;

            float angle = Vector3.Dot(camVec.normalized, camToCell.normalized);

            bool shouldRender = (camToCell.magnitude < m_CullingDistance && angle > 0);// || (Mathf.Abs(camToCell.x) < halfCellSize && Mathf.Abs(camToCell.y) < halfCellSize && Mathf.Abs(camToCell.z) < halfCellSize);

            return !shouldRender;
        }

        private void CreateInstancedProbes()
        {
            foreach (var cell in ProbeReferenceVolume.instance.cells.Values)
            {
                if (cell.sh == null || cell.sh.Length == 0)
                    continue;

                float largestBrickSize = cell.bricks.Count == 0 ? 0 : cell.bricks[0].size;

                List<Matrix4x4[]> probeBuffers = new List<Matrix4x4[]>();
                List<MaterialPropertyBlock> props = new List<MaterialPropertyBlock>();
                List<int[]> probeMaps = new List<int[]>();

                // Batch probes for instanced rendering
                for (int brickSize = 0; brickSize < largestBrickSize + 1; brickSize++)
                {
                    List<Matrix4x4> probeBuffer = new List<Matrix4x4>();
                    List<int> probeMap = new List<int>();

                    for (int i = 0; i < cell.probePositions.Length; i++)
                    {
                        // Skip probes which aren't of current brick size
                        if (cell.bricks[i / 64].size == brickSize)
                        {
                            probeBuffer.Add(Matrix4x4.TRS(cell.probePositions[i], Quaternion.identity, Vector3.one * (0.3f * (brickSize + 1))));
                            probeMap.Add(i);
                        }

                        // Batch limit reached or out of probes
                        if (probeBuffer.Count >= probesPerBatch || i == cell.probePositions.Length - 1)
                        {
                            MaterialPropertyBlock prop = new MaterialPropertyBlock();
                            float gradient = largestBrickSize == 0 ? 1 : brickSize / largestBrickSize;
                            prop.SetColor("_Color", Color.Lerp(Color.red, Color.green, gradient));
                            props.Add(prop);

                            probeBuffers.Add(probeBuffer.ToArray());
                            probeBuffer = new List<Matrix4x4>();
                            probeMaps.Add(probeMap.ToArray());
                            probeMap = new List<int>();
                        }
                    }
                }

                var debugData = new CellInstancedDebugProbes();
                debugData.probeBuffers = probeBuffers;
                debugData.props = props;
                debugData.probeMaps = probeMaps;
                debugData.cellPosition = cell.position;

                Vector4[][] shBuffer = new Vector4[4][];
                for (int i = 0; i < shBuffer.Length; i++)
                    shBuffer[i] = new Vector4[probesPerBatch];

                Vector4[] validityColors = new Vector4[probesPerBatch];

                for (int batchIndex = 0; batchIndex < debugData.probeMaps.Count; batchIndex++)
                {
                    for (int indexInBatch = 0; indexInBatch < debugData.probeMaps[batchIndex].Length; indexInBatch++)
                    {
                        int probeIdx = debugData.probeMaps[batchIndex][indexInBatch];

                        shBuffer[0][indexInBatch] = new Vector4(cell.sh[probeIdx][0, 3], cell.sh[probeIdx][0, 1], cell.sh[probeIdx][0, 2], cell.sh[probeIdx][0, 0]);
                        shBuffer[1][indexInBatch] = new Vector4(cell.sh[probeIdx][1, 3], cell.sh[probeIdx][1, 1], cell.sh[probeIdx][1, 2], cell.sh[probeIdx][1, 0]);
                        shBuffer[2][indexInBatch] = new Vector4(cell.sh[probeIdx][2, 3], cell.sh[probeIdx][2, 1], cell.sh[probeIdx][2, 2], cell.sh[probeIdx][2, 0]);

                        validityColors[indexInBatch] = Color.Lerp(Color.green, Color.red, cell.validity[probeIdx]);
                    }

                    debugData.props[batchIndex].SetVectorArray("_R", shBuffer[0]);
                    debugData.props[batchIndex].SetVectorArray("_G", shBuffer[1]);
                    debugData.props[batchIndex].SetVectorArray("_B", shBuffer[2]);

                    debugData.props[batchIndex].SetVectorArray("_Validity", validityColors);
                }

                cellDebugData.Add(debugData);
            }
        }

        private void CleanCachedProbeData()
        {
            //(Entity e, CellMetadata metadata, CellInstancedDebugProbes cached)
            //if (metadata.Hash != cached.cellHash)
            cellDebugData = new List<CellInstancedDebugProbes>();
        }

        private void OnDrawGizmos()
        {
            if (!enabled || !gameObject.activeSelf)
                return;

            if (m_DrawCells)
            {
                // Fetching this from components instead of from the reference volume allows the user to
                // preview how cells will look before they commit to a bake.
                Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
                Gizmos.color = Color.green;

                foreach (var cell in ProbeReferenceVolume.instance.cells.Values)
                {
                    if (ShouldCull(cell.position))
                        continue;

                    var positionF = new Vector3(cell.position.x, cell.position.y, cell.position.z);
                    var center = positionF * m_Profile.cellSize + m_Profile.cellSize * 0.5f * Vector3.one;
                    Gizmos.DrawWireCube(center, Vector3.one * m_Profile.cellSize);
                }
            }

            if (m_DrawBricks)
            {
                Gizmos.matrix = ProbeReferenceVolume.instance.GetRefSpaceToWS();
                Gizmos.color = Color.blue;

                // Read refvol transform
                foreach (var cell in ProbeReferenceVolume.instance.cells.Values)
                {
                    if (ShouldCull(cell.position))
                        continue;

                    if (cell.bricks == null)
                        continue;

                    foreach (var brick in cell.bricks)
                    {
                        Vector3 scaledSize = Vector3.one * Mathf.Pow(3, brick.size);
                        Vector3 scaledPos = brick.position + scaledSize / 2;
                        Gizmos.DrawWireCube(scaledPos, scaledSize);
                    }
                }
            }
        }

        public void DrawProbeGizmos()
        {
            if (m_DrawProbes)
            {
                // TODO: Update data on ref vol changes
                if (cellDebugData.Count == 0)
                    CreateInstancedProbes();

                // Debug data has not been loaded yet.
                if (debugMesh == null || debugMaterial == null)
                    return;

                foreach (var debug in cellDebugData)
                {
                    if (ShouldCull(debug.cellPosition))
                        continue;

                    for (int i = 0; i < debug.probeBuffers.Count; ++i)
                    {
                        var probeBuffer = debug.probeBuffers[i];
                        var props = debug.props[i];
                        props.SetInt("_ShadingMode", (int)m_ProbeShading);
                        props.SetFloat("_Exposure", -m_Exposure);
                        props.SetFloat("_ProbeSize", Gizmos.probeSize * 100);

                        Graphics.DrawMeshInstanced(debugMesh, 0, debugMaterial, probeBuffer, probeBuffer.Length, props, ShadowCastingMode.Off, false, 0, null, LightProbeUsage.Off, null);
                    }
                }
            }
        }

        private void CheckInit()
        {
            if (debugMesh == null || debugMaterial == null)
            {
                // Load debug mesh, material
                debugMesh = AssetDatabase.LoadAssetAtPath<Mesh>("Packages/com.unity.render-pipelines.core/Editor/Resources/DebugProbe.fbx");
                debugMaterial = new Material(Shader.Find("Hidden/InstancedProbeShader")) { enableInstancing = true };
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
