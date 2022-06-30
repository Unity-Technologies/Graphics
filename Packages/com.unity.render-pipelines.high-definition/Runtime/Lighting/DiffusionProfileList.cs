using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// A volume component that holds a list of Diffusion Profile.
    /// </summary>
    [Serializable, VolumeComponentMenuForRenderPipeline("Material/Diffusion Profile List", typeof(HDRenderPipeline))]
    [HDRPHelpURLAttribute("Override-Diffusion-Profile")]
    public class DiffusionProfileList : VolumeComponent
    {
        /// <summary>
        /// List of diffusion profiles used inside the volume.
        /// </summary>
        [Tooltip("List of diffusion profiles used inside the volume.")]
        [SerializeField]
        public DiffusionProfileSettingsParameter diffusionProfiles = new DiffusionProfileSettingsParameter(default(DiffusionProfileSettings[]));
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a <see cref="DiffusionProfileSettings"/> value.
    /// </summary>
    [Serializable]
    public sealed class DiffusionProfileSettingsParameter : VolumeParameter<DiffusionProfileSettings[]>
    {
        static System.Buffers.ArrayPool<DiffusionProfileSettings> s_ArrayPool =
            System.Buffers.ArrayPool<DiffusionProfileSettings>.Create(DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT, 5);

        // To accumulate diffusion profiles when resolving stack and not make a new allocation everytime,
        // We allocate once an array with max size, and store the ammount of slots used here.
        internal DiffusionProfileSettings[] accumulatedArray = null;
        internal int accumulatedCount = 0;

        /// <summary>
        /// Creates a new <see cref="DiffusionProfileSettingsParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public DiffusionProfileSettingsParameter(DiffusionProfileSettings[] value, bool overrideState = true)
            : base(value, overrideState) { }


        // Perform custom interpolation: We want to accumulate profiles instead of replacing them

        void AddProfile(DiffusionProfileSettings profile)
        {
            if (profile == null)
                return;
            for (int i = 0; i < accumulatedCount; i++)
            {
                if (profile == m_Value[i])
                    return;
            }

            m_Value[accumulatedCount++] = profile;
        }

        /// <summary>
        /// Interpolates two values using a factor <paramref name="t"/>.
        /// </summary>
        /// <remarks>
        /// By default, this method does a "snap" interpolation, meaning it returns the value
        /// <paramref name="to"/> if <paramref name="t"/> is higher than 0, and <paramref name="from"/>
        /// otherwise.
        /// </remarks>
        /// <param name="from">The start value.</param>
        /// <param name="to">The end value.</param>
        /// <param name="t">The interpolation factor in range [0,1].</param>
        public override void Interp(DiffusionProfileSettings[] from, DiffusionProfileSettings[] to, float t)
        {
            m_Value = s_ArrayPool.Rent(DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT);

            accumulatedCount = 0;
            m_Value[accumulatedCount++] = HDRenderPipeline.currentPipeline?.defaultDiffusionProfile;

            if (to != null)
            {
                foreach (var profile in to)
                {
                    AddProfile(profile);
                    if (accumulatedCount >= DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT)
                        break;
                }
            }
            if (from != null)
            {
                foreach (var profile in from)
                {
                    AddProfile(profile);
                    if (accumulatedCount >= DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT)
                        break;
                }
            }

            for (int i = accumulatedCount; i < m_Value.Length; i++)
                m_Value[i] = null;

            if (accumulatedArray != null)
                s_ArrayPool.Return(accumulatedArray);
            accumulatedArray = m_Value;
        }

        /// <summary>
        /// Override this method to free all allocated resources
        /// </summary>
        public override void Release()
        {
            if (accumulatedArray != null)
                s_ArrayPool.Return(accumulatedArray);
            accumulatedArray = null;
        }
    }
}
