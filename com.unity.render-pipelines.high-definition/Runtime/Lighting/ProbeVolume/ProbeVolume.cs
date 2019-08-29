using System;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    [Serializable]
    public struct ProbeVolumeArtistParameters
    {
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

        public ProbeVolumeArtistParameters parameters = new ProbeVolumeArtistParameters(Color.white);

        private int id = -1;
        private static int s_IDNext = 0;
        public int GetID()
        {
            if (id == -1) { id = s_IDNext++; }
            return id;
        }

        // TODO: Replace with real data from lightmapper.
        public Vector3[] GetDataStub()
        {
            var res = new Vector3[parameters.resolutionX * parameters.resolutionY * parameters.resolutionZ];
            for (int i = 0, iLen = res.Length; i < iLen; ++i)
            {
                Vector3 value = new Vector3(parameters.debugColor.r, parameters.debugColor.g, parameters.debugColor.b);
                Vector3 random = new Vector3((Random.value * 2.0f - 1.0f) * 0.1f, (Random.value * 2.0f - 1.0f) * 0.1f, (Random.value * 2.0f - 1.0f) * 0.1f);

                res[i] = value + random;

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
            id = -1;
            ProbeVolumeManager.manager.RegisterVolume(this);
        }

        protected void OnDisable()
        {
            ProbeVolumeManager.manager.DeRegisterVolume(this);
        }

        protected void Update()
        {
        }

        protected void OnValidate()
        {
            parameters.Constrain();
        }
    }
} // UnityEngine.Experimental.Rendering.HDPipeline
