using System;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    [HelpURL(Documentation.baseURL + Documentation.version + Documentation.subURL + "HDRP-Camera" + Documentation.endURL)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public class HDAdditionalCameraData : MonoBehaviour, IVersionable<HDAdditionalCameraData.Version>
    {
        #region Datas

#pragma warning disable 414 // Field never used
#pragma warning disable 649 // Field never assigned
        // before component merge datas (HDAdditionalData part)
        [SerializeField] HDCamera.ClearColorMode clearColorMode;
        [SerializeField] Color backgroundColorHDR;
        [SerializeField] bool clearDepth;
        [SerializeField] LayerMask volumeLayerMask;
        [SerializeField] Transform volumeAnchorOverride;
        [SerializeField] HDCamera.AntialiasingMode antialiasing;
        [SerializeField] HDCamera.SMAAQualityLevel SMAAQuality;
        [SerializeField] bool dithering;
        [SerializeField] bool stopNaNs;
        [SerializeField] float taaSharpenStrength;
        [SerializeField] HDPhysicalCamera physicalParameters;
        [SerializeField] HDCamera.FlipYMode flipYMode;
        [SerializeField] bool fullscreenPassthrough;
        [SerializeField] bool allowDynamicResolution;
        [SerializeField] bool customRenderingSettings;
        [SerializeField] bool invertFaceCulling;
        [SerializeField] LayerMask probeLayerMask;
        [SerializeField] bool hasPersistentHistory;

        [SerializeField, FormerlySerializedAs("renderingPathCustomFrameSettings")]
        FrameSettings m_RenderingPathCustomFrameSettings;
        [SerializeField] FrameSettingsOverrideMask renderingPathCustomFrameSettingsOverrideMask;
        [SerializeField] FrameSettingsRenderType defaultFrameSettings;


        // legacy component datas (HDAdditionalData part)
        [SerializeField, FormerlySerializedAs("renderingPath"), Obsolete("For Data Migration")]
        int m_ObsoleteRenderingPath;
        [SerializeField]
        [FormerlySerializedAs("serializedFrameSettings"), FormerlySerializedAs("m_FrameSettings")]
#pragma warning disable 618 // Type or member is obsolete
        ObsoleteFrameSettings m_ObsoleteFrameSettings;
#pragma warning restore 618
#pragma warning restore 649
#pragma warning disable 414

        #endregion

        #region Migration

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
            MergeHDAdditionalCameraDataIntoHDCamera
        }

        [SerializeField, FormerlySerializedAs("version")]
        Version m_Version;
        
        Version IVersionable<Version>.version { get => m_Version; set => m_Version = value; }
            
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
            MigrationStep.New(Version.MergeHDAdditionalCameraDataIntoHDCamera, (HDAdditionalCameraData data) =>
            {
                //TODO: migration
                //1 - Create a temporary GameObject with a HDCamera
                //2 - Copy the Camera values on this GameObject into the HDCamera
                //3 - Copy the HDAdditionalCameraData values into the HDCamera
                //4 - Prepare a delegate call that will do:
                //    i   - Remove this HDAdditionalCameraData from this GameObject
                //    ii  - Remove the Camera from this GameObject
                //    iii - Copy the HDCamera component on this GameObject
                //    iv  - Destroy the temporary GameObject
            })
        );

        #endregion

        void Awake() => k_Migration.Migrate(this);
    }
}
