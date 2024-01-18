using System;
using UnityEngine.Assertions;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.Rendering.HighDefinition;
#endif
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDAdditionalLightData : IVersionable<HDAdditionalLightData.Version>
    {
        enum Version
        {
            _Unused00,
            _Unused01,
            ShadowNearPlane,
            LightLayer,
            ShadowLayer,
            _Unused02,
            ShadowResolution,
            RemoveAdditionalShadowData,
            AreaLightShapeTypeLogicIsolation,
            PCSSUIUpdate,
            MoveEmissionMesh,
            EnableApplyRangeAttenuationOnBoxLight,
            UpdateLightShapeToCore,
            UpdateLightUnitsToCore,
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

        /// <summary>
        /// Type used previous isolation of AreaLightShape as we use Point for realtime area light due to culling
        /// </summary>
        [Obsolete]
        enum LightTypeExtent
        {
            Punctual,
            Rectangle,
            Tube
        };

        Version IVersionable<Version>.version
        {
            get => m_Version;
            set => m_Version = value;
        }

#pragma warning disable 0618, 0612
        [SerializeField]
        [ExcludeCopy]
        private Version m_Version = MigrationDescription.LastVersion<Version>();

        private static readonly MigrationDescription<Version, HDAdditionalLightData> k_HDLightMigrationSteps
            = MigrationDescription.New(
            MigrationStep.New(Version.ShadowNearPlane, (HDAdditionalLightData data) =>
            {
                // Added ShadowNearPlane to HDRP additional light data, we don't use Light.shadowNearPlane anymore
                // ShadowNearPlane have been move to HDRP as default legacy unity clamp it to 0.1 and we need to be able to go below that
                data.shadowNearPlane = data.legacyLight.shadowNearPlane;
            }),
            MigrationStep.New(Version.LightLayer, (HDAdditionalLightData data) =>
            {
                // Migrate HDAdditionalLightData.lightLayer to Light.renderingLayerMask
                data.legacyLight.renderingLayerMask = LightLayerToRenderingLayerMask((int)data.m_LightLayers, data.legacyLight.renderingLayerMask);
            }),
            MigrationStep.New(Version.ShadowLayer, (HDAdditionalLightData data) =>
            {
                // Added the ShadowLayer
                // When we upgrade the option to decouple light and shadow layers will be disabled
                // so we can sync the shadow layer mask (from the legacyLight) and the new light layer mask
                data.lightlayersMask = (RenderingLayerMask)RenderingLayerMaskToLightLayer(data.legacyLight.renderingLayerMask);
            }),
            MigrationStep.New(Version.ShadowResolution, (HDAdditionalLightData data) =>
            {
                var additionalShadow = data.GetComponent<AdditionalShadowData>();
                if (additionalShadow != null)
                {
                    data.m_ObsoleteCustomShadowResolution = additionalShadow.customResolution;
                    data.m_ObsoleteContactShadows = additionalShadow.contactShadows;

                    data.shadowDimmer = additionalShadow.shadowDimmer;
                    data.volumetricShadowDimmer = additionalShadow.volumetricShadowDimmer;
                    data.shadowFadeDistance = additionalShadow.shadowFadeDistance;
                    data.shadowTint = additionalShadow.shadowTint;
                    data.normalBias = additionalShadow.normalBias;
                    data.shadowUpdateMode = additionalShadow.shadowUpdateMode;
                    data.shadowCascadeRatios = additionalShadow.shadowCascadeRatios;
                    data.shadowCascadeBorders = additionalShadow.shadowCascadeBorders;
                    data.shadowAlgorithm = additionalShadow.shadowAlgorithm;
                    data.shadowVariant = additionalShadow.shadowVariant;
                    data.shadowPrecision = additionalShadow.shadowPrecision;
                    CoreUtils.Destroy(additionalShadow);
                }

                data.shadowResolution.@override = data.m_ObsoleteCustomShadowResolution;
                switch (data.m_ObsoleteShadowResolutionTier)
                {
                    case ShadowResolutionTier.Low: data.shadowResolution.level = 0; break;
                    case ShadowResolutionTier.Medium: data.shadowResolution.level = 1; break;
                    case ShadowResolutionTier.High: data.shadowResolution.level = 2; break;
                    case ShadowResolutionTier.VeryHigh: data.shadowResolution.level = 3; break;
                }
                data.shadowResolution.useOverride = !data.m_ObsoleteUseShadowQualitySettings;
                data.useContactShadow.@override = data.m_ObsoleteContactShadows;
            }),
            MigrationStep.New(Version.RemoveAdditionalShadowData, (HDAdditionalLightData data) =>
            {
                var shadow = data.GetComponent<AdditionalShadowData>();
                if (shadow != null)
                    CoreUtils.Destroy(shadow);
            }),
            MigrationStep.New(Version.AreaLightShapeTypeLogicIsolation, (HDAdditionalLightData data) =>
            {
                // It is now mixed in an other Enum: PointLightHDType
                // As it is int that live under Enum and used for serialization,
                // there is no serialization issue but we must move datas where they should be.
                switch ((LightTypeExtent)data.m_PointlightHDType)
                {
                    case LightTypeExtent.Punctual:
                        data.m_PointlightHDType = PointLightHDType.Punctual;
                        break;
                    case LightTypeExtent.Rectangle:
                        data.m_PointlightHDType = PointLightHDType.Area;
                        data.m_AreaLightShape = AreaLightShape.Rectangle;
                        break;
                    case LightTypeExtent.Tube:
                        data.m_PointlightHDType = PointLightHDType.Area;
                        data.m_AreaLightShape = AreaLightShape.Tube;
                        break;
                    //No other AreaLight types where supported at this time
                }
            }),
            MigrationStep.New(Version.PCSSUIUpdate, (HDAdditionalLightData data) =>
            {
                // The min filter size is now in the [0..1] range when user facing
                data.minFilterSize = data.minFilterSize * 1000.0f;
            }),
            MigrationStep.New(Version.MoveEmissionMesh, (HDAdditionalLightData data) =>
            {
                MeshRenderer emissiveMesh = data.GetComponent<MeshRenderer>();
                bool emissiveMeshWasHere = emissiveMesh != null;
                ShadowCastingMode oldShadowCastingMode = default;
                MotionVectorGenerationMode oldMotionVectorMode = default;
                if (emissiveMeshWasHere)
                {
                    oldShadowCastingMode = emissiveMesh.shadowCastingMode;
                    oldMotionVectorMode = emissiveMesh.motionVectorGenerationMode;
                }

                CoreUtils.Destroy(data.GetComponent<MeshFilter>());
                CoreUtils.Destroy(emissiveMesh);

                if (emissiveMeshWasHere)
                {
                    data.m_AreaLightEmissiveMeshShadowCastingMode = oldShadowCastingMode;
                    data.m_AreaLightEmissiveMeshMotionVectorGenerationMode = oldMotionVectorMode;
                }
            }),
            MigrationStep.New(Version.EnableApplyRangeAttenuationOnBoxLight, (HDAdditionalLightData data) =>
            {
                // When enabling range attenuation for box light, the default value was "true"
                // causing a migration issue. So when we migrate we setup applyRangeAttenuation to false
                // if we are a box light to keep the previous behavior
                if (data.type == HDLightType.Spot)
                {
                    if (data.spotLightShape == SpotLightShape.Box)
                    {
                        data.applyRangeAttenuation = false;
                    }
                }
            }),
            MigrationStep.New(Version.UpdateLightShapeToCore, (HDAdditionalLightData data) =>
            {
                var light = data.GetComponent<Light>();
                if (light != null)
                {
                    if (light.type == LightType.Point && data.m_PointlightHDType == PointLightHDType.Area)
                    {
                        if (data.m_AreaLightShape == AreaLightShape.Rectangle)
                        {
                            light.type = LightType.Rectangle;
                        }
                        else if (data.m_AreaLightShape == AreaLightShape.Tube)
                        {
                            light.type = LightType.Tube;
                        }
                        else
                        {
                            Assert.IsTrue(data.m_AreaLightShape == AreaLightShape.Disc);
                            light.type = LightType.Disc;
                        }
                    }
                    else if (light.type == LightType.Spot)
                    {
                        if (data.m_SpotLightShape == SpotLightShape.Box)
                        {
                            light.type = LightType.Box;
                        }
                        else if (data.m_SpotLightShape == SpotLightShape.Pyramid)
                        {
                            light.type = LightType.Pyramid;
                        }
                        else
                        {
                            Assert.IsTrue(data.m_SpotLightShape == SpotLightShape.Cone);
                        }
                    }
                }
            }),
            MigrationStep.New(Version.UpdateLightUnitsToCore, (HDAdditionalLightData data) =>
            {
                // Copy data from the HDRP's HDAdditionalLight component to the Unity's Light component
                var light = data.GetComponent<Light>();
                light.enableSpotReflector = data.m_EnableSpotReflector;
                light.luxAtDistance = data.m_LuxAtDistance;
                // The light unit should already be compatible with the light type, since HDRP already checks
                light.lightUnit = data.m_LightUnit;
                // HDRP has stored Light.intensity in candela for point and spot lights, lux for directional lights, and
                // nits for area lights. This is great, and means that we don't need to perform any migration for this
                // field.
                if (light.type == LightType.Pyramid)
                {
                    // The UI expects areaSize.x to be pyramid aspect ratio from now on.
                    // This is a temporary solution until we break out areaSize into multiple fields
                    light.areaSize = new Vector2(data.aspectRatio, light.areaSize.y);
                }
            })
            );
#pragma warning restore 0618, 0612

        void Migrate()
        {
            k_HDLightMigrationSteps.Migrate(this);
            // OnValidate might be called before migration but migration is needed to call UpdateBounds() properly so we call it again here to make sure that they are updated properly.
            OnValidate();
        }

        #region Obsolete fields
        // To be able to have correct default values for our lights and to also control the conversion of intensity from the light editor (so it is compatible with GI)
        // we add intensity (for each type of light we want to manage).
        [Obsolete("Use Light.renderingLayerMask instead")]
        [FormerlySerializedAs("lightLayers")]
        [ExcludeCopy]
        RenderingLayerMask m_LightLayers = RenderingLayerMask.LightLayerDefault;

        [Obsolete]
        [SerializeField]
        [FormerlySerializedAs("m_ShadowResolutionTier")]
        [ExcludeCopy]
        ShadowResolutionTier m_ObsoleteShadowResolutionTier = ShadowResolutionTier.Medium;
        [Obsolete]
        [SerializeField]
        [FormerlySerializedAs("m_UseShadowQualitySettings")]
        [ExcludeCopy]
        bool m_ObsoleteUseShadowQualitySettings = false;

        [FormerlySerializedAs("m_CustomShadowResolution")]
        [Obsolete]
        [SerializeField]
        [ExcludeCopy]
        int m_ObsoleteCustomShadowResolution = k_DefaultShadowResolution;

        [FormerlySerializedAs("m_ContactShadows")]
        [Obsolete]
        [SerializeField]
        [ExcludeCopy]
        bool m_ObsoleteContactShadows = false;
        #endregion
    }
}
