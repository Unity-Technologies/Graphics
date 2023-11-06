using System;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class PhysicallyBasedSky : IVersionable<PhysicallyBasedSky.Version>
    {
        /// <summary>
        /// The version used during the migration
        /// </summary>
        protected enum Version
        {
            /// <summary>Version Step</summary>
            Initial,
            /// <summary>Version Step</summary>
            TypeEnum,
            /// <summary>Version Step</summary>
            SharedPlanet,

            /// <summary>Latest Version</summary>
            Count,
        }

        /// <summary>
        /// The migration steps for PhysicallyBasedSky
        /// </summary>
        protected static readonly MigrationDescription<Version, PhysicallyBasedSky> k_Migration = MigrationDescription.New(
            MigrationStep.New(Version.TypeEnum, (PhysicallyBasedSky p) =>
            {
#pragma warning disable 618 // Type or member is obsolete
                p.type.value = p.m_ObsoleteEarthPreset.value ? PhysicallyBasedSkyModel.EarthAdvanced : PhysicallyBasedSkyModel.Custom;
                p.type.overrideState = p.m_ObsoleteEarthPreset.overrideState;
#pragma warning restore 618
            }),
            MigrationStep.New(Version.SharedPlanet, (PhysicallyBasedSky p) =>
            {
                // Previously, ozone was approximated by tinting air, undo that
                p.airTint.value = Color.white;

                #if UNITY_EDITOR
                var profiles = UnityEditor.AssetDatabase.FindAssets("t:" + typeof(VolumeProfile).Name);
                foreach (var guid in profiles)
                {
                    var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                    if (!UnityEditor.AssetDatabase.IsMainAssetAtPathLoaded(path))
                        continue;
                    var profile = UnityEditor.AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
                    if (!profile.components.Contains(p))
                        continue;

                    if (!profile.TryGet<VisualEnvironment>(out var env))
                        env = profile.Add<VisualEnvironment>();

#pragma warning disable 618 // Type or member is obsolete
                    var type = p.type.overrideState ? p.type.value : PhysicallyBasedSkyModel.EarthAdvanced;

                    env.planetRadius.value = p.planetaryRadius.value / 1000.0f;
                    env.planetRadius.overrideState = (type == PhysicallyBasedSkyModel.Custom) && p.planetaryRadius.overrideState;

                    env.planetCenter.value = p.planetCenterPosition.value / 1000.0f;
                    env.planetCenter.overrideState = (type != PhysicallyBasedSkyModel.EarthSimple) && p.planetCenterPosition.overrideState;
#pragma warning restore 618

                    UnityEditor.EditorUtility.SetDirty(env);
                    UnityEditor.EditorUtility.SetDirty(profile);

                    return;
                }
                #endif
            })
        );

        void Awake()
        {
            k_Migration.Migrate(this);
        }

        [SerializeField]
        Version m_SkyVersion = MigrationDescription.LastVersion<Version>();
        Version IVersionable<Version>.version { get => m_SkyVersion; set => m_SkyVersion = value; }

        /// <summary>Obsolete field. Simplifies the interface by using parameters suitable to simulate Earth.</summary>
        [SerializeField, FormerlySerializedAs("earthPreset"), Obsolete("For Data Migration")]
        BoolParameter m_ObsoleteEarthPreset = new BoolParameter(true);

        /// <summary> Radius of the planet (distance from the center of the planet to the sea level). Units: meters. </summary>
        [SerializeField, Obsolete("For Data Migration")]
        MinFloatParameter planetaryRadius = new MinFloatParameter(k_DefaultEarthRadius, 0);

        /// <summary> Position of the center of the planet in the world space. Units: meters. Does not affect the precomputation. </summary>
        [SerializeField, Obsolete("For Data Migration")]
        Vector3Parameter planetCenterPosition = new Vector3Parameter(new Vector3(0, -k_DefaultEarthRadius, 0));

    }
}
