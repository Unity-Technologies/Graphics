using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Serializable]
    public sealed class SkyResolutionParameter : VolumeParameter<SkyResolution> { }

    [Serializable]
    public sealed class EnvUpdateParameter : VolumeParameter<EnvironementUpdateMode> { }

    public abstract class SkySettings : VolumeComponent
    {

        [Tooltip("Rotation of the sky.")]
        public ClampedFloatParameter    rotation = new ClampedFloatParameter { value = 0.0f, min = 0.0f, max = 360.0f };
        [Tooltip("Exposure of the sky in EV.")]
        public FloatParameter           exposure = new FloatParameter { value = 0.0f };
        [Tooltip("Intensity multiplier for the sky.")]
        public ClampedFloatParameter    multiplier = new ClampedFloatParameter { value = 1.0f, min = 0.0f, clampMode = ParameterClampMode.Min };
        [Tooltip("Resolution of the environment lighting generated from the sky.")]
        public SkyResolutionParameter   resolution = new SkyResolutionParameter { value = SkyResolution.SkyResolution256 };
        [Tooltip("Specify how the environment lighting should be updated.")]
        public EnvUpdateParameter       updateMode = new EnvUpdateParameter { value = EnvironementUpdateMode.OnChanged };
        [Tooltip("If environment update is set to realtime, period in seconds at which it is updated (0.0 means every frame).")]
        public ClampedFloatParameter    updatePeriod = new ClampedFloatParameter { value = 0.0f, min = 0.0f, clampMode = ParameterClampMode.Min };
        [Tooltip("If a lighting override cubemap is provided, this cubemap will be used to compute lighting instead of the result from the visible sky.")]
        public CubemapParameter         lightingOverride = new CubemapParameter { value = null };

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
                hash = hash * 23 + ((int)resolution.value).GetHashCode(); // Enum.GetHashCode generates garbage on .NET 3.5... Wtf !?
                hash = hash * 23 + ((int)updateMode.value).GetHashCode();
                //<<<

                hash = hash * 23 + updatePeriod.GetHashCode();
                hash = lightingOverride != null ? hash * 23 + rotation.GetHashCode() : hash;
                return hash;
            }
        }

        public abstract SkyRenderer GetRenderer();
    }
}
