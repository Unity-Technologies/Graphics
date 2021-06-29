using System;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRISky : IVersionable<HDRISky.Version>
    {
        /// <summary>
        /// The version used during the migration
        /// </summary>
        protected enum Version
        {
            /// <summary>Version Step</summary>
            Initial,
            /// <summary>Version Step</summary>
            GlobalWind,
        }

        /// <summary>
        /// Migration steps
        /// </summary>
        protected static readonly MigrationDescription<Version, HDRISky> k_Migration = MigrationDescription.New(
            MigrationStep.New(Version.GlobalWind, (HDRISky s) =>
            {
#pragma warning disable 618 // Type or member is obsolete
                float newOrientation = 0.0f;
                if (s.scrollDirection.overrideState)
                    newOrientation += s.scrollDirection.value + 270.0f;
                if (s.rotation.overrideState)
                    newOrientation -= s.rotation.value;
                if (newOrientation != 0.0f)
                {
                    s.scrollOrientation.Override(new WindParameter.WindParamaterValue
                    {
                        mode = WindParameter.WindOverrideMode.Custom,
                        customValue = newOrientation % 360.0f
                    });
                }
                s.distortionMode.value = !s.enableDistortion.value ? HDRISky.DistortionMode.None :
                    (s.procedural.value ? HDRISky.DistortionMode.Procedural : HDRISky.DistortionMode.Flowmap);
                s.distortionMode.overrideState = s.enableDistortion.overrideState || s.procedural.overrideState;
#pragma warning restore 618
            })
        );

        void Awake()
        {
            k_Migration.Migrate(this);
        }

        [SerializeField]
        Version m_SkyVersion;
        Version IVersionable<Version>.version { get => m_SkyVersion; set => m_SkyVersion = value; }

        [SerializeField, Obsolete("For Data Migration")]
        public BoolParameter enableDistortion = new BoolParameter(false);
        [SerializeField, Obsolete("For Data Migration")]
        public BoolParameter procedural = new BoolParameter(true);
        [SerializeField, Obsolete("For Data Migration")]
        public ClampedFloatParameter scrollDirection = new ClampedFloatParameter(0.0f, 0.0f, 360.0f);
    }
}
