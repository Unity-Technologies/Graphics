using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Serializable]
    public sealed class SkyResolutionParameter : VolumeParameter<SkyResolution>
    {
        public SkyResolutionParameter(SkyResolution val, bool overrideState = false)
            : base(val, overrideState)
        {

        }
    }

    [Serializable]
    public sealed class EnvUpdateParameter : VolumeParameter<EnvironementUpdateMode>
    {
        public EnvUpdateParameter(EnvironementUpdateMode val, bool overrideState = false)
            : base(val, overrideState)
        {

        }
    }

    public abstract class SkySettings : VolumeComponent
    {

        [Tooltip("Rotation of the sky.")]
        public ClampedFloatParameter    rotation = new ClampedFloatParameter(0.0f, 0.0f, 360.0f);
        [Tooltip("Exposure of the sky in EV.")]
        public FloatParameter           exposure = new FloatParameter(0.0f);
        [Tooltip("Intensity multiplier for the sky.")]
        public MinFloatParameter        multiplier = new MinFloatParameter(1.0f, 0.0f);
        [Tooltip("Resolution of the environment lighting generated from the sky.")]
        public SkyResolutionParameter   resolution = new SkyResolutionParameter(SkyResolution.SkyResolution256);
        [Tooltip("Specify how the environment lighting should be updated.")]
        public EnvUpdateParameter       updateMode = new EnvUpdateParameter(EnvironementUpdateMode.OnChanged);
        [Tooltip("If environment update is set to realtime, period in seconds at which it is updated (0.0 means every frame).")]
        public MinFloatParameter        updatePeriod = new MinFloatParameter(0.0f, 0.0f);

        [Tooltip("If enabled, this sky setting will be the one used for baking the GI. Only one should be enabled at any given time.")]
        public bool     useForBaking = false;

        // Unused for now. In the future we might want to expose this option for very high range skies.
        private bool    m_useMIS = false;
        public bool useMIS { get { return m_useMIS; } }

        // This list will hold the sky settings that should be used for baking.
        // In practice we will always use the last one registered but we use a list to be able to roll back to the previous one once the user deletes the superfluous instances.
        private static List<SkySettings>    m_BakingSkySettings = new List<SkySettings>();

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
                return hash;
            }
        }

        static public SkySettings GetBakingSkySettings()
        {
            if (m_BakingSkySettings.Count == 0)
                return null;
            else
                return m_BakingSkySettings[m_BakingSkySettings.Count - 1];
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            OnValidate();
        }

        protected override void OnDisable()
        {
            m_BakingSkySettings.Remove(this);
            base.OnDisable();
        }

        public void OnValidate()
        {
            if(useForBaking && !m_BakingSkySettings.Contains(this))
            {
                if(m_BakingSkySettings.Count != 0)
                {
                    Debug.LogWarning("One sky component was already set for baking, only the latest one will be used.");
                }
                m_BakingSkySettings.Add(this);
            }

            if (!useForBaking)
            {
                m_BakingSkySettings.Remove(this);
            }

        }


        public abstract SkyRenderer GetRenderer();
    }
}
