using System;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>Artist-friendly density volume parametrization.</summary>
    [Serializable]
    public partial struct DensityVolumeArtistParameters
    {
        /// <summary>Single scattering albedo: [0, 1]. Alpha is ignored.</summary>
        public Color     albedo;
        /// <summary>Mean free path, in meters: [1, inf].</summary>
        public float     meanFreePath; // Should be chromatic - this is an optimization!
        /// <summary>Anisotropy of the phase function: [-1, 1]. Positive values result in forward scattering, and negative values - in backward scattering.</summary>
        [FormerlySerializedAs("asymmetry")]
        public float     anisotropy;   // . Not currently available for density volumes

        /// <summary>Texture containing density values.</summary>
        public Texture3D volumeMask;
        /// <summary>Scrolling speed of the density texture.</summary>
        public Vector3   textureScrollingSpeed;
        /// <summary>Tiling rate of the density texture.</summary>
        public Vector3   textureTiling;

        /// <summary>Edge fade factor along the positive X, Y and Z axes.</summary>
        [FormerlySerializedAs("m_PositiveFade")]
        public Vector3   positiveFade;
        /// <summary>Edge fade factor along the negative X, Y and Z axes.</summary>
        [FormerlySerializedAs("m_NegativeFade")]
        public Vector3   negativeFade;

        [SerializeField, FormerlySerializedAs("m_UniformFade")]
        internal float   m_EditorUniformFade;
        [SerializeField]
        internal Vector3 m_EditorPositiveFade;
        [SerializeField]
        internal Vector3 m_EditorNegativeFade;
        [SerializeField, FormerlySerializedAs("advancedFade"), FormerlySerializedAs("m_AdvancedFade")]
        internal bool    m_EditorAdvancedFade;

        /// <summary>Dimensions of the volume.</summary>
        public Vector3   size;
        /// <summary>Inverts the fade gradient.</summary>
        public bool      invertFade;

        /// <summary>Distance at which density fading starts.</summary>
        public float     distanceFadeStart;
        /// <summary>Distance at which density fading ends.</summary>
        public float     distanceFadeEnd;
        [SerializeField]
        internal int     textureIndex;
        /// <summary>Allows translation of the tiling density texture.</summary>
        [SerializeField, FormerlySerializedAs("volumeScrollingAmount")]
        public Vector3   textureOffset;

        /// <summary>Constructor.</summary>
        /// <param name="color">Single scattering albedo.</param>
        /// <param name="_meanFreePath">Mean free path.</param>
        /// <param name="_anisotropy">Anisotropy.</param>
        public DensityVolumeArtistParameters(Color color, float _meanFreePath, float _anisotropy)
        {
            albedo                = color;
            meanFreePath          = _meanFreePath;
            anisotropy            = _anisotropy;

            volumeMask            = null;
            textureIndex          = -1;
            textureScrollingSpeed = Vector3.zero;
            textureTiling         = Vector3.one;
            textureOffset         = textureScrollingSpeed;

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

        internal void Update(float time)
        {
            //Update scrolling based on deltaTime
            if (volumeMask != null)
            {
                textureOffset = (textureScrollingSpeed * time);
                // Switch from right-handed to left-handed coordinate system.
                textureOffset.x = -textureOffset.x;
                textureOffset.y = -textureOffset.y;
            }
        }

        internal void Constrain()
        {
            albedo.r = Mathf.Clamp01(albedo.r);
            albedo.g = Mathf.Clamp01(albedo.g);
            albedo.b = Mathf.Clamp01(albedo.b);
            albedo.a = 1.0f;

            meanFreePath = Mathf.Clamp(meanFreePath, 1.0f, float.MaxValue);

            anisotropy = Mathf.Clamp(anisotropy, -1.0f, 1.0f);

            textureOffset = Vector3.zero;

            distanceFadeStart = Mathf.Max(0, distanceFadeStart);
            distanceFadeEnd   = Mathf.Max(distanceFadeStart, distanceFadeEnd);
        }

        internal DensityVolumeEngineData ConvertToEngineData()
        {
            DensityVolumeEngineData data = new DensityVolumeEngineData();

            data.extinction     = VolumeRenderingUtils.ExtinctionFromMeanFreePath(meanFreePath);
            data.scattering     = VolumeRenderingUtils.ScatteringFromExtinctionAndAlbedo(data.extinction, (Vector3)(Vector4)albedo);

            data.textureIndex   = textureIndex;
            data.textureScroll  = textureOffset;
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

    /// <summary>Density volume class.</summary>
    [HelpURL(Documentation.baseURL + Documentation.version + Documentation.subURL + "Density-Volume" + Documentation.endURL)]
    [ExecuteAlways]
    [AddComponentMenu("Rendering/Density Volume")]
    public partial class DensityVolume : MonoBehaviour
    {
        /// <summary>Density volume parameters.</summary>
        public DensityVolumeArtistParameters parameters = new DensityVolumeArtistParameters(Color.white, 10.0f, 0.0f);

        private Texture3D previousVolumeMask = null;
#if UNITY_EDITOR
        private int volumeMaskHash = 0;
#endif

        /// <summary>Action shich should be performed after updating the texture.</summary>
        public Action OnTextureUpdated;


        /// <summary>Gather and Update any parameters that may have changed.</summary>
        internal void PrepareParameters(float time)
        {
            //Texture has been updated notify the manager
            bool updated = previousVolumeMask != parameters.volumeMask;
#if UNITY_EDITOR
            int newMaskHash = parameters.volumeMask ? parameters.volumeMask.imageContentsHash.GetHashCode() : 0;
            updated |= newMaskHash != volumeMaskHash;
#endif

            if (updated)
            {
                NotifyUpdatedTexure();
                previousVolumeMask = parameters.volumeMask;
#if UNITY_EDITOR
                volumeMaskHash = newMaskHash;
#endif
            }

            parameters.Update(time);
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

#if UNITY_EDITOR
            // Handle scene visibility
            UnityEditor.SceneVisibilityManager.visibilityChanged += UpdateDecalVisibility;
#endif
        }

#if UNITY_EDITOR
        void UpdateDecalVisibility()
        {
            if (UnityEditor.SceneVisibilityManager.instance.IsHidden(gameObject))
            {
                if (DensityVolumeManager.manager.ContainsVolume(this))
                    DensityVolumeManager.manager.DeRegisterVolume(this);
            }
            else
            {
                if (!DensityVolumeManager.manager.ContainsVolume(this))
                    DensityVolumeManager.manager.RegisterVolume(this);
            }
        }

#endif

        private void OnDisable()
        {
            DensityVolumeManager.manager.DeRegisterVolume(this);

#if UNITY_EDITOR
            UnityEditor.SceneVisibilityManager.visibilityChanged -= UpdateDecalVisibility;
#endif
        }

        private void Update()
        {
        }

        private void OnValidate()
        {
            parameters.Constrain();
        }
    }
}
