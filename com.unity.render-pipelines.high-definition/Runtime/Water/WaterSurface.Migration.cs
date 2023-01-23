using System;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class WaterSurface : IVersionable<WaterSurface.Version>, ISerializationCallbackReceiver
    {
        enum Version
        {
            First,
            GenericRenderingLayers,
            AutomaticFading,

            Count,
        }

        [SerializeField]
        Version m_Version = MigrationDescription.LastVersion<Version>();
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


        /// <summary>
        /// OnAfterDeserialize implementation.
        /// </summary>
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            if (m_Version == Version.Count) // serializing a newly created object
                m_Version = Version.Count - 1; // mark as up to date
        }

        /// <summary>
        /// OnBeforeSerialize implementation.
        /// </summary>
        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            if (m_Version == Version.Count) // deserializing and object without version
                m_Version = Version.First; // reset to run the migration
        }
    }
}
