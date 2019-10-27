using System;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.Rendering.HighDefinition;
#endif
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDAdditionalLightData : ISerializationCallbackReceiver, IVersionable<HDAdditionalLightData.Version>
    {
        // TODO: Use proper migration toolkit

        enum Version
        {
            _Unused00,
            _Unused01,
            ShadowNearPlane,
            LightLayer,
            ShadowLayer,
            _Unused02,
            ShadowResolution,
        }

        /// <summary>
        /// Shadow Resolution Tier
        /// </summary>
        [Obsolete]
        enum ShadowResolutionTier
        {
            Low = 0,
            Medium,
            High,
            VeryHigh
        }

        Version IVersionable<Version>.version
        {
            get => m_Version;
            set => m_Version = value;
        }

#pragma warning disable 0618, 0612
        [SerializeField]
        private Version m_Version = Version.ShadowResolution;

        private static readonly MigrationDescription<Version, HDAdditionalLightData> k_HDLightMigrationSteps
            = MigrationDescription.New(
                MigrationStep.New(Version.ShadowNearPlane, (HDAdditionalLightData t) =>
                {
                    // Added ShadowNearPlane to HDRP additional light data, we don't use Light.shadowNearPlane anymore
                    // ShadowNearPlane have been move to HDRP as default legacy unity clamp it to 0.1 and we need to be able to go below that
                    t.shadowNearPlane = t.legacyLight.shadowNearPlane;
                }),
                MigrationStep.New(Version.LightLayer, (HDAdditionalLightData t) =>
                {
                    // Migrate HDAdditionalLightData.lightLayer to Light.renderingLayerMask
                    t.legacyLight.renderingLayerMask = LightLayerToRenderingLayerMask((int)t.m_LightLayers, t.legacyLight.renderingLayerMask);
                }),
                MigrationStep.New(Version.ShadowLayer, (HDAdditionalLightData t) =>
                {
                    // Added the ShadowLayer
                    // When we upgrade the option to decouple light and shadow layers will be disabled
                    // so we can sync the shadow layer mask (from the legacyLight) and the new light layer mask
                    t.lightlayersMask = (LightLayerEnum)RenderingLayerMaskToLightLayer(t.legacyLight.renderingLayerMask);
                }),
                MigrationStep.New(Version.ShadowResolution, (HDAdditionalLightData t) =>
                {
                    var additionalShadow = t.GetComponent<AdditionalShadowData>();
                    if (additionalShadow != null)
                    {
                        t.m_ObsoleteCustomShadowResolution = additionalShadow.customResolution;
                        t.m_ObsoleteContactShadows = additionalShadow.contactShadows;

                        t.shadowDimmer = additionalShadow.shadowDimmer;
                        t.volumetricShadowDimmer = additionalShadow.volumetricShadowDimmer;
                        t.shadowFadeDistance = additionalShadow.shadowFadeDistance;
                        t.shadowTint = additionalShadow.shadowTint;
                        t.normalBias = additionalShadow.normalBias;
                        t.constantBias = additionalShadow.constantBias;
                        t.shadowUpdateMode = additionalShadow.shadowUpdateMode;
                        t.shadowCascadeRatios = additionalShadow.shadowCascadeRatios;
                        t.shadowCascadeBorders = additionalShadow.shadowCascadeBorders;
                        t.shadowAlgorithm = additionalShadow.shadowAlgorithm;
                        t.shadowVariant = additionalShadow.shadowVariant;
                        t.shadowPrecision = additionalShadow.shadowPrecision;
                        CoreUtils.Destroy(additionalShadow);
                    }

                    t.shadowResolution.@override = t.m_ObsoleteCustomShadowResolution;
                    switch (t.m_ObsoleteShadowResolutionTier)
                    {
                        case ShadowResolutionTier.Low: t.shadowResolution.level = 0; break;
                        case ShadowResolutionTier.Medium: t.shadowResolution.level = 1; break;
                        case ShadowResolutionTier.High: t.shadowResolution.level = 2; break;
                        case ShadowResolutionTier.VeryHigh: t.shadowResolution.level = 3; break;
                    }
                    t.shadowResolution.useOverride = !t.m_ObsoleteUseShadowQualitySettings;
                    t.useContactShadow.@override = t.m_ObsoleteContactShadows;
                })
            );
#pragma warning restore 0618, 0612

        void ISerializationCallbackReceiver.OnAfterDeserialize() {}

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            UpdateBounds();
        }

        void OnEnable()
        {
            if (shadowUpdateMode == ShadowUpdateMode.OnEnable)
                m_ShadowMapRenderedSinceLastRequest = false;
            SetEmissiveMeshRendererEnabled(true);
        }

        void Awake()
        {
            k_HDLightMigrationSteps.Migrate(this);
#pragma warning disable 0618
            var shadow = GetComponent<AdditionalShadowData>();
            if (shadow != null)
                CoreUtils.Destroy(shadow);
#pragma warning restore 0618
        }

        #region Obsolete fields
        // To be able to have correct default values for our lights and to also control the conversion of intensity from the light editor (so it is compatible with GI)
        // we add intensity (for each type of light we want to manage).
        [Obsolete("Use Light.renderingLayerMask instead")]
        [FormerlySerializedAs("lightLayers")]
        LightLayerEnum m_LightLayers = LightLayerEnum.LightLayerDefault;

        [Obsolete]
        [SerializeField]
        [FormerlySerializedAs("m_ShadowResolutionTier")]
        ShadowResolutionTier m_ObsoleteShadowResolutionTier = ShadowResolutionTier.Medium;
        [Obsolete]
        [SerializeField]
        [FormerlySerializedAs("m_UseShadowQualitySettings")]
        bool m_ObsoleteUseShadowQualitySettings = false;

        [FormerlySerializedAs("m_CustomShadowResolution")]
        [Obsolete]
        [SerializeField]
        int m_ObsoleteCustomShadowResolution = k_DefaultShadowResolution;

        [FormerlySerializedAs("m_ContactShadows")]
        [Obsolete]
        [SerializeField]
        bool m_ObsoleteContactShadows = false;
        #endregion
    }
}
