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
        private Mesh m_DebugProbeMesh = null;
        private List<Matrix4x4[]> m_ProbeMatricesList;
        private Hash128 m_ProbeMatricesInputHash = new Hash128();

        public ProbeVolumeArtistParameters parameters = new ProbeVolumeArtistParameters(Color.white);

        private int id = -1;
        private static int s_IDNext = 0;

        // TODO: Need a more permanent ID here
        public int GetID()
        {
            if (id == -1) { id = s_IDNext++; }
            return id;
        }

        public Vector3[] GetData()
        {
            var res = new Vector3[parameters.resolutionX * parameters.resolutionY * parameters.resolutionZ];
            
            var nativeData = new NativeArray<SphericalHarmonicsL2>(res.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            UnityEditor.Experimental.Lightmapping.GetAdditionalBakedProbes(id, nativeData);

            for (int i = 0, iLen = res.Length; i < iLen; ++i)
            {
                SphericalHarmonicsL2 additionalProbe = nativeData[i];
                res[i] = new Vector3(additionalProbe[0, 0], additionalProbe[1, 0], additionalProbe[2, 0]);
            }

            return res;
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

            m_DebugProbeMesh = Resources.GetBuiltinResource<Mesh>("New-Sphere.fbx");
            m_DebugMaterial = new Material(Shader.Find("HDRP/Lit"));

            // Reset matrices hash to recreate all positions
            m_ProbeMatricesInputHash = new Hash128();
            SetupPositions();

        }

        protected void OnDisable()
        {
            ProbeVolumeManager.manager.DeRegisterVolume(this);

            if (id != -1)
                UnityEditor.Experimental.Lightmapping.SetAdditionalBakedProbes(id, null);
        }

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

        protected void SetupPositions()
        {
            if (!this.gameObject.activeInHierarchy)
                return;

            if (id == -1)
            {
                GetID();
            }

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

            Hash128 probeMatricesInputHash = Hash128.Compute(inputsString);

            if (m_ProbeMatricesInputHash == probeMatricesInputHash)
                return;

            int probeCount = parameters.resolutionX * parameters.resolutionY * parameters.resolutionZ;
            Vector3[] positions = new Vector3[probeCount];

            OrientedBBox obb = new OrientedBBox(Matrix4x4.TRS(this.transform.position, this.transform.rotation, parameters.size));

            Vector3 probeSteps = new Vector3(parameters.size.x / (float)parameters.resolutionX, parameters.size.y / (float)parameters.resolutionY, parameters.size.z / (float)parameters.resolutionZ);

            Vector3 probeStartPosition = obb.center
                - obb.right   * (parameters.size.x - probeSteps.x) * 0.5f
                - obb.up      * (parameters.size.y - probeSteps.y) * 0.5f
                - obb.forward * (parameters.size.z - probeSteps.z) * 0.5f;

            int i = 0;

            Quaternion rotation = Quaternion.identity;
            Vector3 scale = new Vector3(debugProbeSize, debugProbeSize, debugProbeSize);

            m_ProbeMatricesList = new List<Matrix4x4[]>();

            int probesInCurrentBatch = System.Math.Min(1023, probeCount);
            Matrix4x4[] probeMatrices = new Matrix4x4[probesInCurrentBatch];
            int indexInBatch = 0;
            int processedProbesCount = 0;
            for (int z = 0; z < parameters.resolutionZ; ++z)
            {
                for (int y = 0; y < parameters.resolutionY; ++y)
                {
                    for (int x = 0; x < parameters.resolutionX; ++x)
                    {
                        Vector3 position = probeStartPosition + (probeSteps.x * x * obb.right) + (probeSteps.y * y * obb.up) + (probeSteps.z * z * obb.forward);
                        positions[i] = position;

                        Matrix4x4 matrix = new Matrix4x4();
                        matrix.SetTRS(position, rotation, scale);
                        probeMatrices[indexInBatch] = matrix;

                        indexInBatch++;
                        processedProbesCount++;
                        if (indexInBatch >= 1023)
                        {
                            m_ProbeMatricesList.Add(probeMatrices);
                            int probesToGo = probeCount - processedProbesCount;
                            probesInCurrentBatch = System.Math.Min(1023, probesToGo);
                            probeMatrices = new Matrix4x4[probesInCurrentBatch];
                            indexInBatch = 0;
                        }

                        i++;
                    }
                }
            }

            m_ProbeMatricesInputHash = probeMatricesInputHash;

            UnityEditor.Experimental.Lightmapping.SetAdditionalBakedProbes(id, positions);
        }

        public void DrawProbes()
        {
            SetupPositions();

            Mesh mesh = m_DebugProbeMesh;

            if (!mesh)
                return;

            int submeshIndex = 0;

            Material material = m_DebugMaterial;

            if (!material)
                return;

            material.enableInstancing = true;

            MaterialPropertyBlock properties = null;
            ShadowCastingMode castShadows = ShadowCastingMode.Off;
            bool receiveShadows = false;
            int layer = 0;
            Camera camera = null;
            LightProbeUsage lightProbeUsage = LightProbeUsage.Off;
            LightProbeProxyVolume lightProbeProxyVolume = null;

            foreach (Matrix4x4[] matrices in m_ProbeMatricesList)
            {
                Graphics.DrawMeshInstanced(mesh, submeshIndex, material, matrices, matrices.Length, properties, castShadows, receiveShadows, layer, camera, lightProbeUsage, lightProbeProxyVolume);
            }
        }
    }

} // UnityEngine.Experimental.Rendering.HDPipeline
