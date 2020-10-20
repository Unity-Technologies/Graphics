using System;
using System.Diagnostics;

namespace UnityEngine.Rendering.Universal
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

    public abstract class SkySettings : VolumeComponent
    {
        // TODO Parameters
        // TODO Review tooltips
        [Tooltip("Sets the rotation of the sky.")]
        public ClampedFloatParameter rotation = new ClampedFloatParameter(0.0f, 0.0f, 360.0f);

        [Tooltip("Specifies the intensity mode HDRP uses for the sky.")]
        public SkyIntensityParameter skyIntensityMode = new SkyIntensityParameter(SkyIntensityMode.Exposure);
        [Tooltip("Sets the exposure of the sky in EV.")]
        public FloatParameter exposure = new FloatParameter(0.0f);
        [Tooltip("Sets the intensity multiplier for the sky.")]
        public MinFloatParameter multiplier = new MinFloatParameter(1.0f, 0.0f);
        [Tooltip("Sets the absolute intensity (in Lux) of the current HDR texture set in HDRI Sky. Functions as a Lux intensity multiplier for the sky.")]
        public FloatParameter desiredLuxValue = new FloatParameter(20000);
        [Tooltip("Informative helper that displays the relative intensity (in Lux) for the current HDR texture set in HDRI Sky.")]
        public MinFloatParameter upperHemisphereLuxValue = new MinFloatParameter(1.0f, 0.0f);

        [Tooltip("Specifies when HDRP updates the environment lighting. When set to OnDemand, use HDRenderPipeline.RequestSkyEnvironmentUpdate() to request an update.")]
        public EnvUpdateParameter updateMode = new EnvUpdateParameter(EnvironmentUpdateMode.OnChanged);
        [Tooltip("Sets the period, in seconds, at which HDRP updates the environment ligting (0 means HDRP updates it every frame).")]
        public MinFloatParameter updatePeriod = new MinFloatParameter(0.0f, 0.0f);

        public override int GetHashCode()
        {
            int hash = 13;

            unchecked
            {
                hash = hash * 23 + rotation.GetHashCode();
                hash = hash * 23 + skyIntensityMode.GetHashCode();
                hash = hash * 23 + exposure.GetHashCode();
                hash = hash * 23 + multiplier.GetHashCode();
                hash = hash * 23 + desiredLuxValue.GetHashCode();
                // UpdateMode and period should not be part of the hash as they do not influence rendering itself.
                // TODO Other parameters
            }

            return hash;
        }

        public abstract Type GetSkyRendererType();
    }

    public enum EnvironmentUpdateMode
    {
        OnChanged = 0,
        OnDemand,
        Realtime
    }

    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public sealed class EnvUpdateParameter : VolumeParameter<EnvironmentUpdateMode>
    {
        public EnvUpdateParameter(EnvironmentUpdateMode value, bool overrideState = false)
            : base(value, overrideState) { }
    }

    public enum SkyIntensityMode
    {
        Exposure,
        Lux,
        Multiplier,
    }

    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public sealed class SkyIntensityParameter : VolumeParameter<SkyIntensityMode>
    {
        public SkyIntensityParameter(SkyIntensityMode value, bool overrideState = false)
            : base(value, overrideState) { }
    }
}
