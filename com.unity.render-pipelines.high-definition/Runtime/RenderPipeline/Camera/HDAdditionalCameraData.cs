using System;
using System.Collections.Generic;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    //[Obsolete("Only keeped for migration from old pattern Camera+HDAdditionalCameraData to HDCamera inheriting from Camera")]
    public partial class HDAdditionalCameraData : MonoBehaviour, IVersionable<HDAdditionalCameraData.Version>
    {
        // --- FORMER SERIALIZED DATA ---
        [SerializeField] HDCamera.ClearColorMode clearColorMode;
        [SerializeField] Color backgroundColorHDR;
        [SerializeField] bool clearDepth;
        [SerializeField] LayerMask volumeLayerMask;
        [SerializeField] Transform volumeAnchorOverride;
        [SerializeField] HDCamera.AntialiasingMode antialiasing;
        [SerializeField] HDCamera.SMAAQualityLevel SMAAQuality;
        [SerializeField] bool dithering;
        [SerializeField] bool stopNaNs;
        [SerializeField] float taaSharpenStrength = 0.6f;
        [SerializeField] HDPhysicalCamera physicalParameters;
        [SerializeField] HDCamera.FlipYMode flipYMode;
        [SerializeField] bool fullscreenPassthrough;
        [SerializeField] bool allowDynamicResolution;
        [SerializeField] bool customRenderingSettings;
        [SerializeField] bool invertFaceCulling;
        [SerializeField] LayerMask probeLayerMask;
        [SerializeField] bool hasPersistentHistory;
        [SerializeField] event Action<ScriptableRenderContext, HDCamera> customRender;
        [SerializeField] event HDCamera.RequestAccessDelegate requestGraphicsBuffer;
        [SerializeField] float probeCustomFixedExposure;
        [SerializeField] HDCamera.NonObliqueProjectionGetter nonObliqueProjectionGetter;

        [SerializeField, FormerlySerializedAs("renderingPathCustomFrameSettings")]
        FrameSettings m_RenderingPathCustomFrameSettings;
        [SerializeField] FrameSettingsOverrideMask renderingPathCustomFrameSettingsOverrideMask;
        [SerializeField] FrameSettingsRenderType defaultFrameSettings;

        // --- LEGACY MIGRATED DATA
#pragma warning disable 649 // Field never assigned
        [SerializeField, FormerlySerializedAs("renderingPath"), Obsolete("For Data Migration")]
        int m_ObsoleteRenderingPath;
#pragma warning disable 618 // Type or member is obsolete
        [SerializeField, FormerlySerializedAs("serializedFrameSettings"), FormerlySerializedAs("m_FrameSettings"), Obsolete("For Data Migration")]
        ObsoleteFrameSettings m_ObsoleteFrameSettings;
#pragma warning restore 618
#pragma warning restore 649

        // --- KEEPED MIGRATION PATTERN ---
        // This is only to put in the new HDCamera only the last compatible version of former HDAdditionalCameraData
        enum Version
        {
            None,
            First,
            SeparatePassThrough,
            UpgradingFrameSettingsToStruct,
            AddAfterPostProcessFrameSetting,
            AddFrameSettingSpecularLighting, // Not used anymore
            AddReflectionSettings,
            AddCustomPostprocessAndCustomPass,
            RemovalOfAdditionalDataPattern
        }

        [SerializeField, FormerlySerializedAs("version")]
        Version m_Version;

        static readonly MigrationDescription<Version, HDAdditionalCameraData> k_Migration = MigrationDescription.New(
            MigrationStep.New(Version.SeparatePassThrough, (HDAdditionalCameraData data) =>
            {
#pragma warning disable 618 // Type or member is obsolete
                switch ((int)data.m_ObsoleteRenderingPath)
#pragma warning restore 618
                {
                    case 0: //former RenderingPath.UseGraphicsSettings
                        data.fullscreenPassthrough = false;
                        data.customRenderingSettings = false;
                        break;
                    case 1: //former RenderingPath.Custom
                        data.fullscreenPassthrough = false;
                        data.customRenderingSettings = true;
                        break;
                    case 2: //former RenderingPath.FullscreenPassthrough
                        data.fullscreenPassthrough = true;
                        data.customRenderingSettings = false;
                        break;
                }
            }),
            MigrationStep.New(Version.UpgradingFrameSettingsToStruct, (HDAdditionalCameraData data) =>
            {
#pragma warning disable 618 // Type or member is obsolete
                if (data.m_ObsoleteFrameSettings != null)
                    FrameSettings.MigrateFromClassVersion(ref data.m_ObsoleteFrameSettings, ref data.m_RenderingPathCustomFrameSettings, ref data.renderingPathCustomFrameSettingsOverrideMask);
#pragma warning restore 618
            }),
            MigrationStep.New(Version.AddAfterPostProcessFrameSetting, (HDAdditionalCameraData data)
                => FrameSettings.MigrateToAfterPostprocess(ref data.m_RenderingPathCustomFrameSettings)
            ),
            MigrationStep.New(Version.AddReflectionSettings, (HDAdditionalCameraData data)
                => FrameSettings.MigrateToDefaultReflectionSettings(ref data.m_RenderingPathCustomFrameSettings)
            ),
            MigrationStep.New(Version.AddCustomPostprocessAndCustomPass, (HDAdditionalCameraData data)
                => FrameSettings.MigrateToCustomPostprocessAndCustomPass(ref data.m_RenderingPathCustomFrameSettings)
            ),
            MigrationStep.New(Version.RemovalOfAdditionalDataPattern, (HDAdditionalCameraData data) =>
            {
                //TODO:
                // 1 - Copy the Camera values into a temporary GameObject with a HDCamera component
                // 2 - Copy above FORMER SERIALIZED DATA values to the HDCamera component
                // 3 - Remove the Camera from this GameObject
                // 4 - Add new HDCamera to this GameObject (at the position of the former Camera if possible)
                // 5 - Copy value of the HDCamera from the temporary GameObject into the one on this GameObject
                // 6 - Destroy temporary GameObject
                // 7 - Destroy this HDAdditionalCameraData component (check with the migration mechanisme if possible or add the action into next editor loop / coroutine for runtime)
            })
        );

        Version IVersionable<Version>.version { get => m_Version; set => m_Version = value; }
        
        void Awake() => k_Migration.Migrate(this);
    }
}
