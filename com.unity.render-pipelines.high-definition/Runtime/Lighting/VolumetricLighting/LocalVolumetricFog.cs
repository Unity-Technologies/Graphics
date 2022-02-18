using System;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>Artist-friendly Local Volumetric Fog parametrization.</summary>
    [Serializable]
    public partial struct LocalVolumetricFogArtistParameters
    {
        /// <summary>Single scattering albedo: [0, 1]. Alpha is ignored.</summary>
        [ColorUsage(false)]
        public Color albedo;
        /// <summary>Mean free path, in meters: [1, inf].</summary>
        public float meanFreePath; // Should be chromatic - this is an optimization!
        /// <summary>Anisotropy of the phase function: [-1, 1]. Positive values result in forward scattering, and negative values - in backward scattering.</summary>
        [FormerlySerializedAs("asymmetry")]
        public float anisotropy;   // . Not currently available for Local Volumetric Fog

        /// <summary>Texture containing density values.</summary>
        public Texture volumeMask;
        /// <summary>Scrolling speed of the density texture.</summary>
        public Vector3 textureScrollingSpeed;
        /// <summary>Tiling rate of the density texture.</summary>
        public Vector3 textureTiling;

        /// <summary>Edge fade factor along the positive X, Y and Z axes.</summary>
        [FormerlySerializedAs("m_PositiveFade")]
        public Vector3 positiveFade;
        /// <summary>Edge fade factor along the negative X, Y and Z axes.</summary>
        [FormerlySerializedAs("m_NegativeFade")]
        public Vector3 negativeFade;

        [SerializeField, FormerlySerializedAs("m_UniformFade")]
        internal float m_EditorUniformFade;
        [SerializeField]
        internal Vector3 m_EditorPositiveFade;
        [SerializeField]
        internal Vector3 m_EditorNegativeFade;
        [SerializeField, FormerlySerializedAs("advancedFade"), FormerlySerializedAs("m_AdvancedFade")]
        internal bool m_EditorAdvancedFade;

        /// <summary>Dimensions of the volume.</summary>
        public Vector3 size;
        /// <summary>Inverts the fade gradient.</summary>
        public bool invertFade;

        /// <summary>Distance at which density fading starts.</summary>
        public float distanceFadeStart;
        /// <summary>Distance at which density fading ends.</summary>
        public float distanceFadeEnd;
        /// <summary>Allows translation of the tiling density texture.</summary>
        [SerializeField, FormerlySerializedAs("volumeScrollingAmount")]
        public Vector3 textureOffset;

        /// <summary>When Blend Distance is above 0, controls which kind of falloff is applied to the transition area.</summary>
        public LocalVolumetricFogFalloffMode falloffMode;

        /// <summary>Minimum fog distance you can set in the meanFreePath parameter</summary>
        internal const float kMinFogDistance = 0.05f;

        /// <summary>Constructor.</summary>
        /// <param name="color">Single scattering albedo.</param>
        /// <param name="_meanFreePath">Mean free path.</param>
        /// <param name="_anisotropy">Anisotropy.</param>
        public LocalVolumetricFogArtistParameters(Color color, float _meanFreePath, float _anisotropy)
        {
            albedo = color;
            meanFreePath = _meanFreePath;
            anisotropy = _anisotropy;

            volumeMask = null;
            textureScrollingSpeed = Vector3.zero;
            textureTiling = Vector3.one;
            textureOffset = textureScrollingSpeed;

            size = Vector3.one;

            positiveFade = Vector3.zero;
            negativeFade = Vector3.zero;
            invertFade = false;

            distanceFadeStart = 10000;
            distanceFadeEnd = 10000;

            falloffMode = LocalVolumetricFogFalloffMode.Linear;

            m_EditorPositiveFade = Vector3.zero;
            m_EditorNegativeFade = Vector3.zero;
            m_EditorUniformFade = 0;
            m_EditorAdvancedFade = false;
        }

        internal void Update(float time)
        {
            //Update scrolling based on deltaTime
            if (volumeMask != null)
            {
                // Switch from right-handed to left-handed coordinate system.
                textureOffset = -(textureScrollingSpeed * time);
            }
        }

        internal void Constrain()
        {
            albedo.r = Mathf.Clamp01(albedo.r);
            albedo.g = Mathf.Clamp01(albedo.g);
            albedo.b = Mathf.Clamp01(albedo.b);
            albedo.a = 1.0f;

            meanFreePath = Mathf.Clamp(meanFreePath, kMinFogDistance, float.MaxValue);

            anisotropy = Mathf.Clamp(anisotropy, -1.0f, 1.0f);

            textureOffset = Vector3.zero;

            distanceFadeStart = Mathf.Max(0, distanceFadeStart);
            distanceFadeEnd = Mathf.Max(distanceFadeStart, distanceFadeEnd);
        }

        internal LocalVolumetricFogEngineData ConvertToEngineData()
        {
            LocalVolumetricFogEngineData data = new LocalVolumetricFogEngineData();

            data.extinction = VolumeRenderingUtils.ExtinctionFromMeanFreePath(meanFreePath);
            data.scattering = VolumeRenderingUtils.ScatteringFromExtinctionAndAlbedo(data.extinction, (Vector4)albedo);

            var atlas = LocalVolumetricFogManager.manager.volumeAtlas.GetAtlas();
            data.atlasOffset = LocalVolumetricFogManager.manager.volumeAtlas.GetTextureOffset(volumeMask);
            data.atlasOffset.x /= (float)atlas.width;
            data.atlasOffset.y /= (float)atlas.height;
            data.atlasOffset.z /= (float)atlas.volumeDepth;
            data.useVolumeMask = volumeMask != null ? 1 : 0;
            float volumeMaskSize = volumeMask != null ? (float)volumeMask.width : 0.0f; // Volume Mask Textures are always cubic
            data.maskSize = new Vector4(volumeMaskSize / atlas.width, volumeMaskSize / atlas.height, volumeMaskSize / atlas.volumeDepth, volumeMaskSize);
            data.textureScroll = textureOffset;
            data.textureTiling = textureTiling;

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
            data.falloffMode = falloffMode;

            float distFadeLen = Mathf.Max(distanceFadeEnd - distanceFadeStart, 0.00001526f);

            data.rcpDistFadeLen = 1.0f / distFadeLen;
            data.endTimesRcpDistFadeLen = distanceFadeEnd * data.rcpDistFadeLen;

            return data;
        }
    } // class LocalVolumetricFogParameters

    /// <summary>Local Volumetric Fog class.</summary>
    [HDRPHelpURLAttribute("Local-Volumetric-Fog")]
    [ExecuteAlways]
    [AddComponentMenu("Rendering/Local Volumetric Fog")]
    public partial class LocalVolumetricFog : MonoBehaviour
    {
        /// <summary>Local Volumetric Fog parameters.</summary>
        public LocalVolumetricFogArtistParameters parameters = new LocalVolumetricFogArtistParameters(Color.white, 10.0f, 0.0f);

        private Texture previousVolumeMask = null;
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
                if (parameters.volumeMask != null)
                    LocalVolumetricFogManager.manager.AddTextureIntoAtlas(parameters.volumeMask);

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
            LocalVolumetricFogManager.manager.RegisterVolume(this);

#if UNITY_EDITOR
            // Handle scene visibility
            UnityEditor.SceneVisibilityManager.visibilityChanged += UpdateLocalVolumetricFogVisibility;
#endif
        }

#if UNITY_EDITOR
        void UpdateLocalVolumetricFogVisibility()
        {
            if (UnityEditor.SceneVisibilityManager.instance.IsHidden(gameObject))
            {
                if (LocalVolumetricFogManager.manager.ContainsVolume(this))
                    LocalVolumetricFogManager.manager.DeRegisterVolume(this);
            }
            else
            {
                if (!LocalVolumetricFogManager.manager.ContainsVolume(this))
                    LocalVolumetricFogManager.manager.RegisterVolume(this);
            }
        }

#endif

        private void OnDisable()
        {
            LocalVolumetricFogManager.manager.DeRegisterVolume(this);

#if UNITY_EDITOR
            UnityEditor.SceneVisibilityManager.visibilityChanged -= UpdateLocalVolumetricFogVisibility;
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
