using System;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{


    [Serializable]
    public partial struct DensityVolumeArtistParameters
    {
        public Color     albedo;       // Single scattering albedo [0, 1]. Alpha is ignored
        public float     meanFreePath; // In meters [1, inf]. Should be chromatic - this is an optimization!
        public float     asymmetry;    // [-1, 1]. Not currently available for density volumes

        public Texture3D volumeMask;
        public Vector3   textureScrollingSpeed;
        public Vector3   textureTiling;

        [FormerlySerializedAs("m_PositiveFade")]
        public Vector3  positiveFade;
        [FormerlySerializedAs("m_NegativeFade")]
        public Vector3  negativeFade;

        [SerializeField, FormerlySerializedAs("m_UniformFade")]
        internal float    m_EditorUniformFade;
        [SerializeField]
        internal Vector3  m_EditorPositiveFade;
        [SerializeField]
        internal Vector3  m_EditorNegativeFade;
        [SerializeField, FormerlySerializedAs("advancedFade"), FormerlySerializedAs("m_AdvancedFade")]
        internal bool     m_EditorAdvancedFade;

        public Vector3   size;
        public bool      invertFade;

        public float     distanceFadeStart;
        public float     distanceFadeEnd;

        public  int      textureIndex; // This shouldn't be public... Internal, maybe?
        private Vector3  volumeScrollingAmount;

        public DensityVolumeArtistParameters(Color color, float _meanFreePath, float _asymmetry)
        {
            albedo                = color;
            meanFreePath          = _meanFreePath;
            asymmetry             = _asymmetry;

            volumeMask            = null;
            textureIndex          = -1;
            textureScrollingSpeed = Vector3.zero;
            textureTiling         = Vector3.one;
            volumeScrollingAmount = textureScrollingSpeed;

            size                  = Vector3.one;

            positiveFade          = Vector3.zero;
            negativeFade          = Vector3.zero;
            invertFade            = false;

            distanceFadeStart     = 10000;
            distanceFadeEnd       = 10000;

            m_EditorPositiveFade = Vector3.zero;
            m_EditorNegativeFade = Vector3.zero;
            m_EditorUniformFade  = 0;
            m_EditorAdvancedFade = false;
        }

        public void Update(bool animate, float time)
        {
            //Update scrolling based on deltaTime
            if (volumeMask != null)
            {
                float animationTime = animate ? time : 0.0f;
                volumeScrollingAmount = (textureScrollingSpeed * animationTime);
                // Switch from right-handed to left-handed coordinate system.
                volumeScrollingAmount.x = -volumeScrollingAmount.x;
                volumeScrollingAmount.y = -volumeScrollingAmount.y;
            }
        }

        public void Constrain()
        {
            albedo.r = Mathf.Clamp01(albedo.r);
            albedo.g = Mathf.Clamp01(albedo.g);
            albedo.b = Mathf.Clamp01(albedo.b);
            albedo.a = 1.0f;

            meanFreePath = Mathf.Clamp(meanFreePath, 1.0f, float.MaxValue);

            asymmetry = Mathf.Clamp(asymmetry, -1.0f, 1.0f);

            volumeScrollingAmount = Vector3.zero;

            distanceFadeStart = Mathf.Max(0, distanceFadeStart);
            distanceFadeEnd   = Mathf.Max(distanceFadeStart, distanceFadeEnd);
        }

        public DensityVolumeEngineData ConvertToEngineData()
        {
            DensityVolumeEngineData data = new DensityVolumeEngineData();

            data.extinction     = VolumeRenderingUtils.ExtinctionFromMeanFreePath(meanFreePath);
            data.scattering     = VolumeRenderingUtils.ScatteringFromExtinctionAndAlbedo(data.extinction, (Vector3)(Vector4)albedo);

            data.textureIndex   = textureIndex;
            data.textureScroll  = volumeScrollingAmount;
            data.textureTiling  = textureTiling;

            // Clamp to avoid NaNs.
            Vector3 positiveFade = this.positiveFade;
            Vector3 negativeFade = this.negativeFade;

            data.rcpPosFaceFade.x = Mathf.Min(1.0f / positiveFade.x, float.MaxValue);
            data.rcpPosFaceFade.y = Mathf.Min(1.0f / positiveFade.y, float.MaxValue);
            data.rcpPosFaceFade.z = Mathf.Min(1.0f / positiveFade.z, float.MaxValue);

            data.rcpNegFaceFade.y = Mathf.Min(1.0f / negativeFade.y, float.MaxValue);
            data.rcpNegFaceFade.x = Mathf.Min(1.0f / negativeFade.x, float.MaxValue);
            data.rcpNegFaceFade.z = Mathf.Min(1.0f / negativeFade.z, float.MaxValue);

            data.invertFade = invertFade ? 1 : 0;

            float distFadeLen = Mathf.Max(distanceFadeEnd - distanceFadeStart, 0.00001526f);

            data.rcpDistFadeLen         = 1.0f / distFadeLen;
            data.endTimesRcpDistFadeLen = distanceFadeEnd * data.rcpDistFadeLen;

            return data;
        }
    } // class DensityVolumeParameters

    [ExecuteAlways]
    [AddComponentMenu("Rendering/Density Volume", 1100)]
    public partial class DensityVolume : MonoBehaviour
    {
        public DensityVolumeArtistParameters parameters = new DensityVolumeArtistParameters(Color.white, 10.0f, 0.0f);

        private Texture3D previousVolumeMask = null;

        public Action OnTextureUpdated;

        //Gather and Update any parameters that may have changed
        public void PrepareParameters(bool animate, float time)
        {
            //Texture has been updated notify the manager
            if (previousVolumeMask != parameters.volumeMask)
            {
                NotifyUpdatedTexure();
                previousVolumeMask = parameters.volumeMask;
            }

            parameters.Update(animate, time);
        }

        private void NotifyUpdatedTexure()
        {
            if (OnTextureUpdated != null)
            {
                OnTextureUpdated();
            }
        }

        private void OnEnable()
        {
            DensityVolumeManager.manager.RegisterVolume(this);
        }

        private void OnDisable()
        {
            DensityVolumeManager.manager.DeRegisterVolume(this);
        }

        private void Update()
        {
        }

        private void OnValidate()
        {
            parameters.Constrain();
        }
    }
} // UnityEngine.Experimental.Rendering.HDPipeline
