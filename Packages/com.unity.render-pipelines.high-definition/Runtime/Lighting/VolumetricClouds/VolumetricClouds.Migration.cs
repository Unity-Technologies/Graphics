using System;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class VolumetricClouds : IVersionable<VolumetricClouds.Version>, ISerializationCallbackReceiver
    {
        /// <summary>Called before serializing</summary>
        public void OnBeforeSerialize()
        {
            if (m_Version == Version.Count) // serializing a newly created object
                m_Version = Version.Count - 1; // mark as up to date
        }

        /// <summary>Called after deserializing</summary>
        public void OnAfterDeserialize()
        {
            if (m_Version == Version.Count) // deserializing and object without version
                m_Version = Version.Initial; // reset to run the migration
        }

        enum Version
        {
            Initial,
            GlobalWind,
            ShapeOffset,
            SharedRenderingSpace,

            Count
        }

        static readonly MigrationDescription<Version, VolumetricClouds> k_Migration = MigrationDescription.New(
            MigrationStep.New(Version.GlobalWind, (VolumetricClouds c) =>
            {
#pragma warning disable 618 // Type or member is obsolete
                c.globalWindSpeed.overrideState = c.m_ObsoleteWindSpeed.overrideState;
                c.globalWindSpeed.value = new WindParameter.WindParamaterValue
                {
                    mode = WindParameter.WindOverrideMode.Custom,
                    customValue = c.m_ObsoleteWindSpeed.value
                };

                c.orientation.overrideState = c.m_ObsoleteOrientation.overrideState;
                c.orientation.value = new WindParameter.WindParamaterValue
                {
                    mode = WindParameter.WindOverrideMode.Custom,
                    customValue = c.m_ObsoleteOrientation.value
                };
#pragma warning restore 618
            }),
            MigrationStep.New(Version.ShapeOffset, (VolumetricClouds c) =>
            {
#pragma warning disable 618 // Type or member is obsolete
                c.shapeOffset.overrideState = c.m_ObsoleteShapeOffsetX.overrideState || c.m_ObsoleteShapeOffsetY.overrideState || c.m_ObsoleteShapeOffsetZ.overrideState;
                c.shapeOffset.value = new Vector3(c.m_ObsoleteShapeOffsetX.value, c.m_ObsoleteShapeOffsetY.value, c.m_ObsoleteShapeOffsetZ.value);
#pragma warning restore 618
            }),
            MigrationStep.New(Version.SharedRenderingSpace, (VolumetricClouds c) =>
            {
                if (c.active == false || c.enable.value == false)
                    return;

                #if UNITY_EDITOR
                var profiles = UnityEditor.AssetDatabase.FindAssets("t:" + typeof(VolumeProfile).Name);
                foreach (var guid in profiles)
                {
                    var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                    if (!UnityEditor.AssetDatabase.IsMainAssetAtPathLoaded(path))
                        continue;
                    var profile = UnityEditor.AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
                    if (!profile.components.Contains(c))
                        continue;

                    if (!profile.TryGet<VisualEnvironment>(out var env))
                        env = profile.Add<VisualEnvironment>();

#pragma warning disable 618 // Type or member is obsolete
                    env.renderingSpace.value = c.localClouds.value ? RenderingSpace.World : RenderingSpace.Camera;
                    env.renderingSpace.overrideState = c.localClouds.overrideState;
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
        Version m_Version = Version.Count;
        Version IVersionable<Version>.version { get => m_Version; set => m_Version = value; }


        [SerializeField, Obsolete("For Data Migration")]
        internal BoolParameter localClouds = new(false);

        [SerializeField, FormerlySerializedAs("globalWindSpeed"), Obsolete("For Data Migration")]
        MinFloatParameter m_ObsoleteWindSpeed = new MinFloatParameter(1.0f, 0.0f);
        [SerializeField, FormerlySerializedAs("orientation"), Obsolete("For Data Migration")]
        ClampedFloatParameter m_ObsoleteOrientation = new ClampedFloatParameter(0.0f, 0.0f, 360.0f);

        [SerializeField, FormerlySerializedAs("shapeOffsetX"), Obsolete("For Data Migration")]
        FloatParameter m_ObsoleteShapeOffsetX = new FloatParameter(0.0f);
        [SerializeField, FormerlySerializedAs("shapeOffsetY"), Obsolete("For Data Migration")]
        FloatParameter m_ObsoleteShapeOffsetY = new FloatParameter(0.0f);
        [SerializeField, FormerlySerializedAs("shapeOffsetZ"), Obsolete("For Data Migration")]
        FloatParameter m_ObsoleteShapeOffsetZ = new FloatParameter(0.0f);
    }
}
