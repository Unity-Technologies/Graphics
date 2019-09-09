using System;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using UnityEditor.Experimental;
using Unity.Collections;
using System.Collections.Generic;

namespace UnityEngine.Rendering.HighDefinition
{
    [Serializable]
    public struct ProbeVolumeArtistParameters
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

        public Vector4 scaleBias;

        public int resolutionX;
        public int resolutionY;
        public int resolutionZ;

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
            this.scaleBias = Vector4.zero;
            this.resolutionX = 0;
            this.resolutionY = 0;
            this.resolutionZ = 0;
        }

        public void Constrain()
        {
            this.distanceFadeStart = Mathf.Max(0, this.distanceFadeStart);
            this.distanceFadeEnd = Mathf.Max(this.distanceFadeStart, this.distanceFadeEnd);
        }

        public ProbeVolumeEngineData ConvertToEngineData()
        {
            ProbeVolumeEngineData data = new ProbeVolumeEngineData();

            data.debugColor.x = this.debugColor.r;
            data.debugColor.y = this.debugColor.g;
            data.debugColor.z = this.debugColor.b;

            // Clamp to avoid NaNs.
            Vector3 positiveFade = this.positiveFade;
            Vector3 negativeFade = this.negativeFade;

            data.rcpPosFaceFade.x = Mathf.Min(1.0f / positiveFade.x, float.MaxValue);
            data.rcpPosFaceFade.y = Mathf.Min(1.0f / positiveFade.y, float.MaxValue);
            data.rcpPosFaceFade.z = Mathf.Min(1.0f / positiveFade.z, float.MaxValue);

            data.rcpNegFaceFade.y = Mathf.Min(1.0f / negativeFade.y, float.MaxValue);
            data.rcpNegFaceFade.x = Mathf.Min(1.0f / negativeFade.x, float.MaxValue);
            data.rcpNegFaceFade.z = Mathf.Min(1.0f / negativeFade.z, float.MaxValue);

            float distFadeLen = Mathf.Max(this.distanceFadeEnd - this.distanceFadeStart, 0.00001526f);
            data.rcpDistFadeLen = 1.0f / distFadeLen;
            data.endTimesRcpDistFadeLen = this.distanceFadeEnd * data.rcpDistFadeLen;

            data.scaleBias = this.scaleBias;

            data.resolution = new Vector3(this.resolutionX, this.resolutionY, this.resolutionZ);
            data.resolutionInverse = new Vector3(1.0f / (float)this.resolutionX, 1.0f / (float)this.resolutionY, 1.0f / (float)this.resolutionZ);

            return data;
        }

    } // class ProbeVolumeArtistParameters

    [ExecuteAlways]
    [AddComponentMenu("Rendering/Probe Volume")]
    public class ProbeVolume : MonoBehaviour
    {
        // public Texture ProbeVolumeTexture { get; set; }

        enum Version
        {
            First,
            // Add new version here and they will automatically be the Current one
            Max,
            Current = Max - 1
        }

        [SerializeField]
        int m_Version = (int)Version.First;

        // Debugging code
        private Material m_DebugMaterial = null;
        private Mesh m_DebugMesh = null;
        private List<Matrix4x4[]> m_DebugProbeMatricesList;
        private List<Mesh> m_DebugProbePointMeshList;
        private Hash128 m_DebugProbeInputHash = new Hash128();

        public bool dataUpdated = false;

        [SerializeField]
        public SphericalHarmonicsL1[] data = null;

        public ProbeVolumeArtistParameters parameters = new ProbeVolumeArtistParameters(Color.white);

        private int id = -1;
        private static int s_IDNext = 0;

        // TODO: Need a more permanent ID here
        public int GetID()
        {
            if (id == -1) { id = s_IDNext++; }
            return id;
        }

        public SphericalHarmonicsL1[] GetData()
        {
            dataUpdated = false;
            return data;
        }

        protected void Awake()
        {
            Migrate();
        }

        bool CheckMigrationRequirement()
        {
            //exit as quicker as possible
            if (m_Version == (int)Version.Current)
                return false;

            // TODO: Implement any migration checks.

            return false;
        }

        void ApplyMigration()
        {
            // TODO: Implement any migrations here.
        }

        void Migrate()
        {
            //Must not be called at deserialisation time if require other component
            while (CheckMigrationRequirement())
            {
                ApplyMigration();
            }
        }

        protected void OnEnable()
        {
            ProbeVolumeManager.manager.RegisterVolume(this);

            m_DebugMesh = Resources.GetBuiltinResource<Mesh>("New-Sphere.fbx");
            m_DebugMaterial = new Material(Shader.Find("HDRP/Lit"));

#if UNITY_EDITOR
            EnableBaking();
#endif
        }

        protected void OnDisable()
        {
            ProbeVolumeManager.manager.DeRegisterVolume(this);
#if UNITY_EDITOR
            DisableBaking();
#endif
        }

#if UNITY_EDITOR
        protected void Update()
        {
            if (parameters.drawProbes)
                DrawProbes();
        }
        
        protected void OnValidate()
        {
            parameters.Constrain();
            SetupPositions();
        }

        protected void OnBakeCompleted()
        {
            if (!this.gameObject.activeInHierarchy)
                return;

            if (id == -1)
                return;

            data = new SphericalHarmonicsL1[parameters.resolutionX * parameters.resolutionY * parameters.resolutionZ];

            var nativeData = new NativeArray<SphericalHarmonicsL2>(data.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            UnityEditor.Experimental.Lightmapping.GetAdditionalBakedProbes(id, nativeData);

            // TODO: Remove this data copy.
            for (int i = 0, iLen = data.Length; i < iLen; ++i)
            {
                data[i].shAr = new Vector4(nativeData[i][0, 1], nativeData[i][0, 2], nativeData[i][0, 3], nativeData[i][0, 0]);
                data[i].shAg = new Vector4(nativeData[i][1, 1], nativeData[i][1, 2], nativeData[i][1, 3], nativeData[i][1, 0]);
                data[i].shAb = new Vector4(nativeData[i][2, 1], nativeData[i][2, 2], nativeData[i][2, 3], nativeData[i][2, 0]);
            }

            nativeData.Dispose();

            dataUpdated = true;
        }

        public void DisableBaking()
        {
            UnityEditor.Lightmapping.bakeCompleted -= OnBakeCompleted;

            if (id != -1)
                UnityEditor.Experimental.Lightmapping.SetAdditionalBakedProbes(id, null);
        }

        public void EnableBaking()
        {
            UnityEditor.Lightmapping.bakeCompleted += OnBakeCompleted;

            // Reset matrices hash to recreate all positions
            m_DebugProbeInputHash = new Hash128();

            SetupPositions();
        }

        protected void SetupPositions()
        {
            if (!this.gameObject.activeInHierarchy)
                return;

            GetID();

            float debugProbeSize = 0.1f;

            string inputsString =
                        id.ToString() +
                        debugProbeSize.ToString() +
                        this.transform.position.ToString() +
                        this.transform.rotation.ToString() +
                        parameters.size.ToString() +
                        parameters.resolutionX.ToString() +
                        parameters.resolutionY.ToString() +
                        parameters.resolutionZ.ToString();

            Hash128 debugProbeInputHash = Hash128.Compute(inputsString);

            if (m_DebugProbeInputHash == debugProbeInputHash)
                return;

            int probeCount = parameters.resolutionX * parameters.resolutionY * parameters.resolutionZ;
            Vector3[] positions = new Vector3[probeCount];

            OrientedBBox obb = new OrientedBBox(Matrix4x4.TRS(this.transform.position, this.transform.rotation, parameters.size));

            Vector3 probeSteps = new Vector3(parameters.size.x / (float)parameters.resolutionX, parameters.size.y / (float)parameters.resolutionY, parameters.size.z / (float)parameters.resolutionZ);

            Vector3 probeStartPosition = obb.center
                - obb.right   * (parameters.size.x - probeSteps.x) * 0.5f
                - obb.up      * (parameters.size.y - probeSteps.y) * 0.5f
                - obb.forward * (parameters.size.z - probeSteps.z) * 0.5f;

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
                        Vector3 position = probeStartPosition + (probeSteps.x * x * obb.right) + (probeSteps.y * y * obb.up) + (probeSteps.z * z * obb.forward);
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

            m_DebugProbeInputHash = debugProbeInputHash;

            UnityEditor.Experimental.Lightmapping.SetAdditionalBakedProbes(id, positions);
        }

        public void DrawProbes()
        {
            SetupPositions();

            int layer = 0;
            int submeshIndex = 0;

            Material material = m_DebugMaterial;

            if (!material)
                return;

            material.enableInstancing = true;

            if (UnityEditor.Selection.activeGameObject != this.gameObject)
            {
                foreach (Mesh debugMesh in m_DebugProbePointMeshList)
                    Graphics.DrawMesh(debugMesh, Matrix4x4.identity, material, layer);
            }
            else
            {
                Mesh mesh = m_DebugMesh;

                if (!mesh)
                    return;

                MaterialPropertyBlock properties = null;
                ShadowCastingMode castShadows = ShadowCastingMode.Off;
                bool receiveShadows = false;
                
                Camera camera = null;
                LightProbeUsage lightProbeUsage = LightProbeUsage.Off;
                LightProbeProxyVolume lightProbeProxyVolume = null;

                foreach (Matrix4x4[] matrices in m_DebugProbeMatricesList)
                {
                    Graphics.DrawMeshInstanced(mesh, submeshIndex, material, matrices, matrices.Length, properties, castShadows, receiveShadows, layer, camera, lightProbeUsage, lightProbeProxyVolume);
                }
            }
        }
    }
#endif

} // UnityEngine.Experimental.Rendering.HDPipeline
