using System;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using UnityEditor.Experimental;
using Unity.Collections;
using System.Collections.Generic;

namespace UnityEngine.Rendering.HighDefinition
{
    internal enum ProbeSpacingMode
    {
        Density = 0,
        Resolution
    };

    [GenerateHLSL]
    internal enum VolumeBlendMode
    {
        Normal = 0,
        Additive,
        Subtractive
    }

    // Rather than hashing all the inputs that define a Probe Volume's bake state into a 128-bit int (16-bytes),
    // we simply store the raw state values (56-bytes)
    // While this is 3.5x more memory, it's still fairly low, and avoids the runtime cost of string appending garbage creation.
    // It also means we can never ever have hash collision issues (due to precision loss in string construction, or from hashing),
    // which means we always detect changes correctly.
    [Serializable]
    internal struct ProbeVolumeSettingsKey
    {
        public int id;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 size;
        public int resolutionX;
        public int resolutionY;
        public int resolutionZ;
    }

    [Serializable]
    internal struct ProbeVolumeArtistParameters
    {
        public bool drawProbes;
        public Color debugColor;
        public int payloadIndex;
        public Vector3 size;
        [SerializeField]
        private Vector3 m_PositiveFade;
        [SerializeField]
        private Vector3 m_NegativeFade;
        [SerializeField]
        private float m_UniformFade;
        public bool advancedFade;
        public float distanceFadeStart;
        public float distanceFadeEnd;

        public Vector3 scale;
        public Vector3 bias;
        public Vector4 octahedralDepthScaleBias;

        public ProbeSpacingMode probeSpacingMode;

        public float densityX;
        public float densityY;
        public float densityZ;

        public int resolutionX;
        public int resolutionY;
        public int resolutionZ;

        public VolumeBlendMode volumeBlendMode;
        public float weight;

        public float backfaceTolerance;
        public int dilationIterations;

        public LightLayerEnum lightLayers;

        public Vector3 positiveFade
        {
            get
            {
                return advancedFade ? m_PositiveFade : m_UniformFade * Vector3.one;
            }
            set
            {
                if (advancedFade)
                {
                    m_PositiveFade = value;
                }
                else
                {
                    m_UniformFade = value.x;
                }
            }
        }

        public Vector3 negativeFade
        {
            get
            {
                return advancedFade ? m_NegativeFade : m_UniformFade * Vector3.one;
            }
            set
            {
                if (advancedFade)
                {
                    m_NegativeFade = value;
                }
                else
                {
                    m_UniformFade = value.x;
                }
            }
        }

        public ProbeVolumeArtistParameters(Color debugColor)
        {
            this.debugColor = debugColor;
            this.drawProbes = false;
            this.payloadIndex = -1;
            this.size = Vector3.one;
            this.m_PositiveFade = Vector3.zero;
            this.m_NegativeFade = Vector3.zero;
            this.m_UniformFade = 0;
            this.advancedFade = false;
            this.distanceFadeStart = 10000.0f;
            this.distanceFadeEnd = 10000.0f;
            this.scale = Vector3.zero;
            this.bias = Vector3.zero;
            this.octahedralDepthScaleBias = Vector4.zero;
            this.probeSpacingMode = ProbeSpacingMode.Density;
            this.resolutionX = 4;
            this.resolutionY = 4;
            this.resolutionZ = 4;
            this.densityX = (float)this.resolutionX / this.size.x;
            this.densityY = (float)this.resolutionY / this.size.y;
            this.densityZ = (float)this.resolutionZ / this.size.z;
            this.volumeBlendMode = VolumeBlendMode.Normal;
            this.weight = 1;
            this.dilationIterations = 2;
            this.backfaceTolerance = 0.25f;
            this.lightLayers = LightLayerEnum.LightLayerDefault;
        }

        internal void Constrain()
        {
            this.distanceFadeStart = Mathf.Max(0, this.distanceFadeStart);
            this.distanceFadeEnd = Mathf.Max(this.distanceFadeStart, this.distanceFadeEnd);

            switch (this.probeSpacingMode)
            {
                case ProbeSpacingMode.Density:
                {
                    // Compute resolution from density and size.
                    this.densityX = Mathf.Max(1e-5f, this.densityX);
                    this.densityY = Mathf.Max(1e-5f, this.densityY);
                    this.densityZ = Mathf.Max(1e-5f, this.densityZ);

                    this.resolutionX = Mathf.Max(1, Mathf.RoundToInt(this.densityX * this.size.x));
                    this.resolutionY = Mathf.Max(1, Mathf.RoundToInt(this.densityY * this.size.y));
                    this.resolutionZ = Mathf.Max(1, Mathf.RoundToInt(this.densityZ * this.size.z));
                    break;
                }

                case ProbeSpacingMode.Resolution:
                {
                    // Compute density from resolution and size.
                    this.resolutionX = Mathf.Max(1, this.resolutionX);
                    this.resolutionY = Mathf.Max(1, this.resolutionY);
                    this.resolutionZ = Mathf.Max(1, this.resolutionZ);

                    this.densityX = (float)this.resolutionX / Mathf.Max(1e-5f, this.size.x);
                    this.densityY = (float)this.resolutionY / Mathf.Max(1e-5f, this.size.y);
                    this.densityZ = (float)this.resolutionZ / Mathf.Max(1e-5f, this.size.z);
                    break;
                }

                default:
                {
                    Debug.Assert(false, "Error: ProbeVolume: Encountered unsupported Probe Spacing Mode: " + this.probeSpacingMode);
                    break;
                }
            }
        }

        internal ProbeVolumeEngineData ConvertToEngineData()
        {
            ProbeVolumeEngineData data = new ProbeVolumeEngineData();

            data.weight = this.weight;

            data.debugColor.x = this.debugColor.r;
            data.debugColor.y = this.debugColor.g;
            data.debugColor.z = this.debugColor.b;

            // Clamp to avoid NaNs.
            Vector3 positiveFade = Vector3.Max(this.positiveFade, new Vector3(1e-5f, 1e-5f, 1e-5f));
            Vector3 negativeFade = Vector3.Max(this.negativeFade, new Vector3(1e-5f, 1e-5f, 1e-5f));

            data.rcpPosFaceFade.x = Mathf.Min(1.0f / positiveFade.x, float.MaxValue);
            data.rcpPosFaceFade.y = Mathf.Min(1.0f / positiveFade.y, float.MaxValue);
            data.rcpPosFaceFade.z = Mathf.Min(1.0f / positiveFade.z, float.MaxValue);

            data.rcpNegFaceFade.y = Mathf.Min(1.0f / negativeFade.y, float.MaxValue);
            data.rcpNegFaceFade.x = Mathf.Min(1.0f / negativeFade.x, float.MaxValue);
            data.rcpNegFaceFade.z = Mathf.Min(1.0f / negativeFade.z, float.MaxValue);

            data.volumeBlendMode = (int)this.volumeBlendMode;

            float distFadeLen = Mathf.Max(this.distanceFadeEnd - this.distanceFadeStart, 0.00001526f);
            data.rcpDistFadeLen = 1.0f / distFadeLen;
            data.endTimesRcpDistFadeLen = this.distanceFadeEnd * data.rcpDistFadeLen;

            data.scale = this.scale;
            data.bias = this.bias;
            data.octahedralDepthScaleBias = this.octahedralDepthScaleBias;

            data.resolution = new Vector3(this.resolutionX, this.resolutionY, this.resolutionZ);
            data.resolutionInverse = new Vector3(1.0f / (float)this.resolutionX, 1.0f / (float)this.resolutionY, 1.0f / (float)this.resolutionZ);

            data.lightLayers = (uint)this.lightLayers;

            data.unused = 0.0f;

            return data;
        }

    } // class ProbeVolumeArtistParameters

    [ExecuteAlways]
    [AddComponentMenu("Light/Experimental/Probe Volume")]
    internal class ProbeVolume : MonoBehaviour
    {
#if UNITY_EDITOR
        // Debugging code
        private Material m_DebugMaterial = null;
        private Mesh m_DebugMesh = null;
        private List<Matrix4x4[]> m_DebugProbeMatricesList;
        private List<Mesh> m_DebugProbePointMeshList;
#endif
        private ProbeVolumeSettingsKey bakeKey = new ProbeVolumeSettingsKey
        {
            id = 0,
            position = Vector3.zero,
            rotation = Quaternion.identity,
            size = Vector3.zero,
            resolutionX = 0,
            resolutionY = 0,
            resolutionZ = 0
        };

        internal bool dataUpdated = false;

        [SerializeField] internal ProbeVolumeAsset probeVolumeAsset = null;
        [SerializeField] internal ProbeVolumeArtistParameters parameters = new ProbeVolumeArtistParameters(Color.white);

        internal int GetID()
        {
            return GetInstanceID();
        }

        private void BakeKeyClear()
        {
            bakeKey = new ProbeVolumeSettingsKey
            {
                id = 0,
                position = Vector3.zero,
                rotation = Quaternion.identity,
                size = Vector3.zero,
                resolutionX = 0,
                resolutionY = 0,
                resolutionZ = 0
            };
        }

        internal (SphericalHarmonicsL1[], float[], float[]) GetData()
        {
            dataUpdated = false;

            if (!probeVolumeAsset)
                return (null, null, null);

            return (probeVolumeAsset.dataSH, probeVolumeAsset.dataValidity, probeVolumeAsset.dataOctahedralDepth);
        }

        protected void Awake()
        {
            Migrate();
        }

        bool CheckMigrationRequirement()
        {
            if (probeVolumeAsset && probeVolumeAsset.Version == (int)ProbeVolumeAsset.AssetVersion.Current)
                return false;

            return false;
        }

        void ApplyMigration()
        {
        }

        void Migrate()
        {
            // Must not be called at deserialization time if require other component
            while (CheckMigrationRequirement())
            {
                ApplyMigration();
            }
        }

        protected void OnEnable()
        {
#if UNITY_EDITOR
            OnValidate();
#endif

            ProbeVolumeManager.manager.RegisterVolume(this);

            // Signal update
            if (probeVolumeAsset)
                dataUpdated = true;

#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            m_DebugMesh = Resources.GetBuiltinResource<Mesh>("New-Sphere.fbx");
            m_DebugMaterial = new Material(Shader.Find("HDRP/Lit"));
#endif
        }

        protected void OnDisable()
        {
            ProbeVolumeManager.manager.DeRegisterVolume(this);
#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                return;
#endif
        }

        internal bool IsAssetCompatible()
        {
            if (probeVolumeAsset)
            {
                return parameters.resolutionX == probeVolumeAsset.resolutionX &&
                       parameters.resolutionY == probeVolumeAsset.resolutionY &&
                       parameters.resolutionZ == probeVolumeAsset.resolutionZ;
            }

            return false;
        }

#if UNITY_EDITOR
        protected void Update()
        {
            OnValidate();
        }

        internal void ForceBakingEnabled()
        {
            BakeKeyClear();
            OnValidate();
        }

        internal void ForceBakingDisabled()
        {
            UnityEditor.Experimental.Lightmapping.SetAdditionalBakedProbes(GetID(), null);
        }

        protected void OnValidate()
        {
            if (ShaderConfig.s_ProbeVolumesEvaluationMode == ProbeVolumesEvaluationModes.Disabled)
                return;

            ProbeVolumeSettingsKey bakeKeyCurrent = ComputeProbeVolumeSettingsKeyFromProbeVolume(this);
            if (ProbeVolumeSettingsKeyEquals(ref bakeKey, ref bakeKeyCurrent) &&
                m_DebugProbeMatricesList != null) { return; }

            parameters.Constrain();

            bakeKey = bakeKeyCurrent;

            if (probeVolumeAsset)
            {
                if (!IsAssetCompatible())
                {
                    Debug.LogWarningFormat("The asset \"{0}\" assigned to Probe Volume \"{1}\" does not have matching data dimensions ({2}x{3}x{4} vs. {5}x{6}x{7}), please rebake.",
                        probeVolumeAsset.name, this.name,
                        probeVolumeAsset.resolutionX, probeVolumeAsset.resolutionY, probeVolumeAsset.resolutionZ,
                        parameters.resolutionX, parameters.resolutionY, parameters.resolutionZ);
                }

                dataUpdated = true;
            }

            SetupProbePositions();
        }

        internal void OnLightingDataCleared()
        {
            probeVolumeAsset = null;
            dataUpdated = true;
            BakeKeyClear();
        }

        internal void OnLightingDataAssetCleared()
        {
            if (probeVolumeAsset == null)
                return;

            string assetPath = UnityEditor.AssetDatabase.GetAssetPath(probeVolumeAsset);
            if (assetPath == "")
                return;

            UnityEditor.AssetDatabase.DeleteAsset(assetPath);
            UnityEditor.AssetDatabase.Refresh();
            BakeKeyClear();
        }

        internal void OnProbesBakeCompleted()
        {
            if (this.gameObject == null || !this.gameObject.activeInHierarchy)
                return;

            int numProbes = parameters.resolutionX * parameters.resolutionY * parameters.resolutionZ;
            SphericalHarmonicsL1[] data = new SphericalHarmonicsL1[numProbes];
            float[] dataValidity = new float[numProbes];
            float[] dataOctahedralDepth = new float[numProbes * 8 * 8];

            var sh = new NativeArray<SphericalHarmonicsL2>(numProbes, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var validity = new NativeArray<float>(numProbes, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var octahedralDepth = new NativeArray<float>(numProbes * 8 * 8, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            if(UnityEditor.Experimental.Lightmapping.GetAdditionalBakedProbes(GetID(), sh, validity, octahedralDepth))
            {
                // TODO: Remove this data copy.
                for (int i = 0, iLen = data.Length; i < iLen; ++i)
                {
                    data[i].shAr = new Vector4(sh[i][0, 3], sh[i][0, 1], sh[i][0, 2], sh[i][0, 0] - sh[i][0, 6]);
                    data[i].shAg = new Vector4(sh[i][1, 3], sh[i][1, 1], sh[i][1, 2], sh[i][1, 0] - sh[i][1, 6]);
                    data[i].shAb = new Vector4(sh[i][2, 3], sh[i][2, 1], sh[i][2, 2], sh[i][2, 0] - sh[i][2, 6]);

                    dataValidity[i] = validity[i];

                    for (int j = 0; j < 64; ++j)
                    {
                        dataOctahedralDepth[i * 64 + j] = octahedralDepth[i * 64 + j];
                    }
                }

                if (!probeVolumeAsset || GetID() != probeVolumeAsset.instanceID)
                    probeVolumeAsset = ProbeVolumeAsset.CreateAsset(GetID());

                probeVolumeAsset.instanceID = GetID();
                probeVolumeAsset.dataSH = data;
                probeVolumeAsset.dataValidity = dataValidity;
                probeVolumeAsset.dataOctahedralDepth = dataOctahedralDepth;
                probeVolumeAsset.resolutionX = parameters.resolutionX;
                probeVolumeAsset.resolutionY = parameters.resolutionY;
                probeVolumeAsset.resolutionZ = parameters.resolutionZ;

                if (UnityEditor.Lightmapping.giWorkflowMode != UnityEditor.Lightmapping.GIWorkflowMode.Iterative)
                    UnityEditor.EditorUtility.SetDirty(probeVolumeAsset);

                UnityEditor.AssetDatabase.Refresh();

                dataUpdated = true;
            }

            sh.Dispose();
            validity.Dispose();
            octahedralDepth.Dispose();
        }

        internal void OnBakeCompleted()
        {
            if (!probeVolumeAsset)
                return;

            probeVolumeAsset.Dilate(parameters.backfaceTolerance, parameters.dilationIterations);
            dataUpdated = true;
        }

        private static ProbeVolumeSettingsKey ComputeProbeVolumeSettingsKeyFromProbeVolume(ProbeVolume probeVolume)
        {
            return new ProbeVolumeSettingsKey
            {
                id = probeVolume.GetID(),
                position = probeVolume.transform.position,
                rotation = probeVolume.transform.rotation,
                size = probeVolume.parameters.size,
                resolutionX = probeVolume.parameters.resolutionX,
                resolutionY = probeVolume.parameters.resolutionY,
                resolutionZ = probeVolume.parameters.resolutionZ
            };
        }

        private static bool ProbeVolumeSettingsKeyEquals(ref ProbeVolumeSettingsKey a, ref ProbeVolumeSettingsKey b)
        {
            return (a.id == b.id)
                && (a.position == b.position)
                && (a.rotation == b.rotation)
                && (a.size == b.size)
                && (a.resolutionX == b.resolutionX)
                && (a.resolutionY == b.resolutionY)
                && (a.resolutionZ == b.resolutionZ);
        }

        private void SetupProbePositions()
        {
            if (!this.gameObject.activeInHierarchy)
                return;

            float debugProbeSize = Gizmos.probeSize;

            int probeCount = parameters.resolutionX * parameters.resolutionY * parameters.resolutionZ;
            Vector3[] positions = new Vector3[probeCount];

            OrientedBBox obb = new OrientedBBox(Matrix4x4.TRS(this.transform.position, this.transform.rotation, parameters.size));

            Vector3 probeSteps = new Vector3(parameters.size.x / (float)parameters.resolutionX, parameters.size.y / (float)parameters.resolutionY, parameters.size.z / (float)parameters.resolutionZ);

            // TODO: Determine why we need to negate obb.forward but not other basis vectors in order to make positions start at the {left, lower, back} corner
            // and end at the {right, top, front} corner (which our atlasing code assumes).
            Vector3 probeStartPosition = obb.center
                - obb.right   * (parameters.size.x - probeSteps.x) * 0.5f
                - obb.up      * (parameters.size.y - probeSteps.y) * 0.5f
                + obb.forward * (parameters.size.z - probeSteps.z) * 0.5f;

            Quaternion rotation = Quaternion.identity;
            Vector3 scale = new Vector3(debugProbeSize, debugProbeSize, debugProbeSize);

            // Debugging objects start here
            int maxBatchSize = 1023;
            int probesInCurrentBatch = System.Math.Min(maxBatchSize, probeCount);
            int indexInCurrentBatch = 0;

            // Everything around cached matrices for the probe spheres
            m_DebugProbeMatricesList = new List<Matrix4x4[]>();
            Matrix4x4[] currentprobeMatrices = new Matrix4x4[probesInCurrentBatch];
            int[] indices = new int[probesInCurrentBatch];

            // Everything around point meshes for non-selected ProbeVolumes
            m_DebugProbePointMeshList = new List<Mesh>();
            int[] currentProbeDebugIndices = new int[probesInCurrentBatch];
            Vector3[] currentProbeDebugPositions = new Vector3[probesInCurrentBatch];

            int processedProbes = 0;

            for (int z = 0; z < parameters.resolutionZ; ++z)
            {
                for (int y = 0; y < parameters.resolutionY; ++y)
                {
                    for (int x = 0; x < parameters.resolutionX; ++x)
                    {
                        Vector3 position = probeStartPosition + (probeSteps.x * x * obb.right) + (probeSteps.y * y * obb.up) + (probeSteps.z * z * -obb.forward);
                        positions[processedProbes] = position;

                        currentProbeDebugIndices[indexInCurrentBatch] = indexInCurrentBatch;
                        currentProbeDebugPositions[indexInCurrentBatch] = position;

                        Matrix4x4 matrix = new Matrix4x4();
                        matrix.SetTRS(position, rotation, scale);
                        currentprobeMatrices[indexInCurrentBatch] = matrix;

                        indexInCurrentBatch++;
                        processedProbes++;

                        int probesLeft = probeCount - processedProbes;

                        if (indexInCurrentBatch >= 1023 || probesLeft == 0)
                        {
                            Mesh currentProbeDebugMesh = new Mesh();
                            currentProbeDebugMesh.SetVertices(currentProbeDebugPositions);
                            currentProbeDebugMesh.SetIndices(currentProbeDebugIndices, MeshTopology.Points, 0);

                            m_DebugProbePointMeshList.Add(currentProbeDebugMesh);
                            m_DebugProbeMatricesList.Add(currentprobeMatrices);

                            // More sets follow, reallocate
                            if (probesLeft > 0)
                            {
                                probesInCurrentBatch = System.Math.Min(maxBatchSize, probesLeft);

                                currentProbeDebugPositions = new Vector3[probesInCurrentBatch];
                                currentProbeDebugIndices = new int[probesInCurrentBatch];
                                currentprobeMatrices = new Matrix4x4[probesInCurrentBatch];

                                indexInCurrentBatch = 0;
                            }
                        }
                    }
                }
            }

            UnityEditor.Experimental.Lightmapping.SetAdditionalBakedProbes(GetID(), positions);
        }

        private static bool ShouldDrawGizmos(ProbeVolume probeVolume)
        {
            if (ShaderConfig.s_ProbeVolumesEvaluationMode == ProbeVolumesEvaluationModes.Disabled)
                return false;

            UnityEditor.SceneView sceneView = UnityEditor.SceneView.currentDrawingSceneView;

            if (sceneView == null)
                sceneView = UnityEditor.SceneView.lastActiveSceneView;

            if (sceneView != null && !sceneView.drawGizmos)
                return false;

            if (!probeVolume.enabled)
                return false;

            return probeVolume.parameters.drawProbes;
        }

        [UnityEditor.DrawGizmo(UnityEditor.GizmoType.NotInSelectionHierarchy)]
        private static void DrawProbes(ProbeVolume probeVolume, UnityEditor.GizmoType gizmoType)
        {
            if (!ShouldDrawGizmos(probeVolume))
                return;

            probeVolume.OnValidate();

            var pointMeshList = probeVolume.m_DebugProbePointMeshList;

            probeVolume.m_DebugMaterial.SetPass(8);
            foreach (Mesh debugMesh in pointMeshList)
                Graphics.DrawMeshNow(debugMesh, Matrix4x4.identity);
        }

        internal void DrawSelectedProbes()
        {
            if (!ShouldDrawGizmos(this))
                return;

            OnValidate();

            int layer = 0;

            Material material = m_DebugMaterial;

            if (!material)
                return;

            material.enableInstancing = true;

            Mesh mesh = m_DebugMesh;

            if (!mesh)
                return;

            int submeshIndex = 0;
            MaterialPropertyBlock properties = null;
            ShadowCastingMode castShadows = ShadowCastingMode.Off;
            bool receiveShadows = false;

            Camera emptyCamera = null;
            LightProbeUsage lightProbeUsage = LightProbeUsage.Off;
            LightProbeProxyVolume lightProbeProxyVolume = null;

            foreach (Matrix4x4[] matrices in m_DebugProbeMatricesList)
                Graphics.DrawMeshInstanced(mesh, submeshIndex, material, matrices, matrices.Length, properties, castShadows, receiveShadows, layer, emptyCamera, lightProbeUsage, lightProbeProxyVolume);
        }
#endif
    }
} // UnityEngine.Experimental.Rendering.HDPipeline
