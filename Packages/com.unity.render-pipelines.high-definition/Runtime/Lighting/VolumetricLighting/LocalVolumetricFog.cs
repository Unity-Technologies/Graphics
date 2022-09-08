using System;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

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

        /// <summary>
        /// Specifies how the fog in the volume will interact with the fog.
        /// </summary>
        public LocalVolumetricFogBlendingMode blendingMode;

        /// <summary>
        /// Rendering priority of the volume, higher priority will be rendered first.
        /// </summary>
        public int priority;

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

        /// <summary>The mask mode to use when writing this volume in the volumetric fog.</summary>
        public LocalVolumetricFogMaskMode maskMode;

        /// <summary>The material used to mask the local volumetric fog when the mask mode is set to Material. The material needs to use the "Fog Volume" material type in Shader Graph.</summary>
        public Material materialMask;

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
            blendingMode = LocalVolumetricFogBlendingMode.Additive;
            priority = 0;
            anisotropy = _anisotropy;

            volumeMask = null;
            materialMask = null;
            textureScrollingSpeed = Vector3.zero;
            textureTiling = Vector3.one;
            textureOffset = textureScrollingSpeed;

            size = Vector3.one;

            positiveFade = Vector3.one * 0.1f;
            negativeFade = Vector3.one * 0.1f;
            invertFade = false;

            distanceFadeStart = 10000;
            distanceFadeEnd = 10000;

            falloffMode = LocalVolumetricFogFalloffMode.Linear;
            maskMode = LocalVolumetricFogMaskMode.Texture;

            m_EditorPositiveFade = positiveFade;
            m_EditorNegativeFade = negativeFade;
            m_EditorUniformFade = 0.1f;
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

            data.blendingMode = blendingMode;
            data.albedo = (Vector3)(Vector4)albedo;

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

        /// <summary>Action shich should be performed after updating the texture.</summary>
        public Action OnTextureUpdated;


        /// <summary>Gather and Update any parameters that may have changed.</summary>
        internal void PrepareParameters(float time)
        {
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
            UnityEditor.SceneVisibilityManager.visibilityChanged -= UpdateLocalVolumetricFogVisibility;
            UnityEditor.SceneVisibilityManager.visibilityChanged += UpdateLocalVolumetricFogVisibility;
            SceneView.duringSceneGui -= UpdateLocalVolumetricFogVisibilityPrefabStage;
            SceneView.duringSceneGui += UpdateLocalVolumetricFogVisibilityPrefabStage;
#endif
        }

#if UNITY_EDITOR
        void UpdateLocalVolumetricFogVisibility()
        {
            bool isVisible = !UnityEditor.SceneVisibilityManager.instance.IsHidden(gameObject);
            UpdateLocalVolumetricFogVisibility(isVisible);
        }

        void UpdateLocalVolumetricFogVisibilityPrefabStage(SceneView sv)
        {
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null)
            {
                bool isVisible = true;
                bool isInPrefabStage = gameObject.scene == stage.scene;

                if (!isInPrefabStage && stage.mode == PrefabStage.Mode.InIsolation)
                    isVisible = false;
                if (!isInPrefabStage && CoreUtils.IsSceneViewPrefabStageContextHidden())
                    isVisible = false;

                UpdateLocalVolumetricFogVisibility(isVisible);
            }
        }

        void UpdateLocalVolumetricFogVisibility(bool isVisible)
        {
            if (isVisible)
            {
                if (!LocalVolumetricFogManager.manager.ContainsVolume(this))
                    LocalVolumetricFogManager.manager.RegisterVolume(this);
            }
            else
            {
                if (LocalVolumetricFogManager.manager.ContainsVolume(this))
                    LocalVolumetricFogManager.manager.DeRegisterVolume(this);
            }
        }
#endif

        private void OnDisable()
        {
            LocalVolumetricFogManager.manager.DeRegisterVolume(this);

#if UNITY_EDITOR
            UnityEditor.SceneVisibilityManager.visibilityChanged -= UpdateLocalVolumetricFogVisibility;
            SceneView.duringSceneGui -= UpdateLocalVolumetricFogVisibilityPrefabStage;
#endif
        }

        private void OnValidate()
        {
            parameters.Constrain();
        }
    }
}
