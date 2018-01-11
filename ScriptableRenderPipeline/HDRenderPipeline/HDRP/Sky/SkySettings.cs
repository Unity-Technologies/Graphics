using System;
using System.Diagnostics;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    // This class is used to associate a unique ID to a sky class.
    // This is needed to be able to automatically register sky classes and avoid collisions and refactoring class names causing data compatibility issues.
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class SkyUniqueID : Attribute
    {
        public readonly int uniqueID;

        public SkyUniqueID(int uniqueID)
        {
            this.uniqueID = uniqueID;
        }
    }

    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public sealed class EnvUpdateParameter : VolumeParameter<EnvironementUpdateMode>
    {
        public EnvUpdateParameter(EnvironementUpdateMode value, bool overrideState = false)
            : base(value, overrideState) { }
    }

    public abstract class SkySettings : VolumeComponent
    {
        [Tooltip("Rotation of the sky.")]
        public ClampedFloatParameter    rotation = new ClampedFloatParameter(0.0f, 0.0f, 360.0f);
        [Tooltip("Exposure of the sky in EV.")]
        public FloatParameter           exposure = new FloatParameter(0.0f);
        [Tooltip("Intensity multiplier for the sky.")]
        public MinFloatParameter        multiplier = new MinFloatParameter(1.0f, 0.0f);
        [Tooltip("Specify how the environment lighting should be updated.")]
        public EnvUpdateParameter       updateMode = new EnvUpdateParameter(EnvironementUpdateMode.OnChanged);
        [Tooltip("If environment update is set to realtime, period in seconds at which it is updated (0.0 means every frame).")]
        public MinFloatParameter        updatePeriod = new MinFloatParameter(0.0f, 0.0f);

        // Unused for now. In the future we might want to expose this option for very high range skies.
        bool m_useMIS = false;
        public bool useMIS { get { return m_useMIS; } }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 13;
                hash = hash * 23 + rotation.GetHashCode();
                hash = hash * 23 + exposure.GetHashCode();
                hash = hash * 23 + multiplier.GetHashCode();

                // TODO: Fixme once we switch to .Net 4.6+
                //>>>
                hash = hash * 23 + ((int)updateMode.value).GetHashCode();
                //<<<

                hash = hash * 23 + updatePeriod.GetHashCode();
                return hash;
            }
        }

        public static int GetUniqueID<T>()
        {
            return GetUniqueID(typeof(T));
        }

        public static int GetUniqueID(Type type)
        {
            var uniqueIDs = type.GetCustomAttributes(typeof(SkyUniqueID), false);
            if (uniqueIDs.Length == 0)
                return -1;
            else
                return ((SkyUniqueID)uniqueIDs[0]).uniqueID;

        }

        public abstract SkyRenderer CreateRenderer();
    }
}
