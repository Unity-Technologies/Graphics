using System;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class WaterSurface : IVersionable<WaterSurface.Version>
    {
        enum Version
        {
            First,
            GenericRenderingLayers,
            AutomaticFading,
            FoamRemap,

            Count,
        }

        [SerializeField]
        Version m_Version = MigrationDescription.LastVersion<Version>() - 1;
        Version IVersionable<Version>.version { get => m_Version; set => m_Version = value; }

        static readonly MigrationDescription<Version, WaterSurface> k_Migration = MigrationDescription.New(
            MigrationStep.New(Version.GenericRenderingLayers, (WaterSurface s) =>
            {
#pragma warning disable 618 // Type or member is obsolete
                uint decal = (uint)s.decalLayerMask << 8;
                s.renderingLayerMask = (RenderingLayerMask)decal | s.lightLayerMask;
#pragma warning restore 618
            }),
            MigrationStep.New(Version.AutomaticFading, (WaterSurface s) =>
            {
#pragma warning disable 618 // Type or member is obsolete
                s.largeBand0FadeMode = s.largeBand0FadeToggle ? FadeMode.Custom : FadeMode.None;
                s.largeBand1FadeMode = s.largeBand1FadeToggle ? FadeMode.Custom : FadeMode.None;
                s.ripplesFadeMode = s.ripplesFadeToggle ? FadeMode.Custom : FadeMode.None;
#pragma warning restore 618
            }),
            MigrationStep.New(Version.FoamRemap, (WaterSurface s) =>
            {
                s.foamPersistenceMultiplier = Mathf.Min(s.foamPersistenceMultiplier * 3.0f, 1.0f);
            })
        );

        /// <summary>Specifies the decal layers that affect the water surface.</summary>
        [SerializeField, Obsolete("Use renderingLayerMask instead @from(2023.1) (UnityUpgradable) -> renderingLayerMask")]
        public RenderingLayerMask decalLayerMask = RenderingLayerMask.RenderingLayer1; // old DecalLayerDefault is rendering layer 1

        /// <summary>Specifies the light layers that affect the water surface.</summary>
        [SerializeField, Obsolete("Use renderingLayerMask instead @from(2023.1) (UnityUpgradable) -> renderingLayerMask")]
        public RenderingLayerMask lightLayerMask = RenderingLayerMask.LightLayerDefault;

        /// <summary></summary>
        [SerializeField, Obsolete("Use largeBand0FadeMode instead @from(2023.1)")]
        public bool largeBand0FadeToggle = true;

        /// <summary></summary>
        [SerializeField, Obsolete("Use largeBand1FadeMode instead @from(2023.1)")]
        public bool largeBand1FadeToggle = true;

        /// <summary></summary>
        [SerializeField, Obsolete("Use ripplesFadeMode instead @from(2023.1)")]
        public bool ripplesFadeToggle = true;
    }
}
