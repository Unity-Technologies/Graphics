using UnityEngine.Serialization;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [ExecuteAlways]
    [RequireComponent(typeof(ReflectionProbe))]
    public partial class HDAdditionalReflectionData : HDProbe
    {
        ReflectionProbe m_LegacyProbe;
        /// <summary>Get the sibling component ReflectionProbe</summary>
        public ReflectionProbe reflectionProbe
        {
            get
            {
                if(m_LegacyProbe == null || m_LegacyProbe.Equals(null))
                {
                    m_LegacyProbe = GetComponent<ReflectionProbe>();
                }
                return m_LegacyProbe;
            }
        }

        public void CopyTo(HDAdditionalReflectionData data)
        {
            influenceVolume.CopyTo(data.influenceVolume);
            data.influenceVolume.shape = influenceVolume.shape; //force the legacy probe to refresh its size

            data.mode = mode;
            data.refreshMode = refreshMode;
            data.multiplier = multiplier;
            data.weight = weight;
        }

        internal override void Awake()
        {
            base.Awake();
            k_Migration.Migrate(this);
        }

        void OnEnable()
        {
            ReflectionSystem.RegisterProbe(this);
        }

        void OnDisable()
        {
            ReflectionSystem.UnregisterProbe(this);
        }

        void OnValidate()
        {
            ReflectionSystem.UnregisterProbe(this);

            if (isActiveAndEnabled)
                ReflectionSystem.RegisterProbe(this);
        }

#if UNITY_EDITOR
        void Update()
        {
            if (!Application.isPlaying)
                UpdateCubemapSize();
        }

        void UpdateCubemapSize()
        {
            HDRenderPipelineAsset hdrpAsset = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
            if (hdrpAsset == null)
                return; // not in hdrp

            reflectionProbe.resolution = (int)hdrpAsset.renderPipelineSettings.lightLoopSettings.reflectionCubemapSize;
        }
#endif

        public override Texture customTexture { get { return reflectionProbe.customBakedTexture; } set { reflectionProbe.customBakedTexture = value; } }
        public override Texture bakedTexture { get { return reflectionProbe.bakedTexture; } set { reflectionProbe.bakedTexture = value; } }

        public override ReflectionProbeMode mode
        {
            set
            {
                base.mode = value;
                reflectionProbe.mode = value; //ensure compatibility till we capture without the legacy component
                if(value == ReflectionProbeMode.Realtime)
                {
                    refreshMode = ReflectionProbeRefreshMode.EveryFrame;
                }
            }
        }
        public override ReflectionProbeRefreshMode refreshMode
        {
            set
            {
                base.refreshMode = value;
                reflectionProbe.refreshMode = value; //ensure compatibility till we capture without the legacy component
            }
        }

        internal override void UpdatedInfluenceVolumeShape(Vector3 size, Vector3 offset)
        {
            reflectionProbe.size = size;
            reflectionProbe.center = transform.rotation*offset;
        }
    }
}
