using System;

namespace UnityEngine.Rendering.HighDefinition
{
    enum LensAttenuationMode
    {
        ImperfectLens,
        PerfectLens
    }

    [Serializable]
    [SupportedOnRenderPipeline(typeof(HDRenderPipelineAsset))]
    [Categorization.CategoryInfo(Name = "Miscellaneous", Order = 100)]
    [Categorization.ElementInfo(Order = 30)]
    class LensSettings : IRenderPipelineGraphicsSettings
    {
        #region Version
        internal enum Version : int
        {
            Initial = 0,
        }

        [SerializeField][HideInInspector]
        private Version m_Version;

        /// <summary>Current version.</summary>
        public int version => (int)m_Version;
        #endregion

        bool IRenderPipelineGraphicsSettings.isAvailableInPlayerBuild => true;

        #region SerializeFields

        [SerializeField]
        [Tooltip("Set the attenuation mode of the lens that is used to compute exposure. With imperfect lens some energy is lost when converting from EV100 to the exposure multiplier.")]
        private LensAttenuationMode m_LensAttenuationMode;

        #endregion

        #region Data Accessors

        /// <summary>
        /// When enabled, imported shaders will use analytic derivatives for their Forward and GBuffer pass. This is a developer-only feature for testing.
        /// </summary>
        public LensAttenuationMode attenuationMode
        {
            get => m_LensAttenuationMode;
            set => this.SetValueAndNotify(ref m_LensAttenuationMode, value);
        }

        public float GetLensAttenuationValue()
        {
            return attenuationMode switch
            {
                LensAttenuationMode.ImperfectLens => 0.65f,
                LensAttenuationMode.PerfectLens => 0.78f,
                _ => throw new NotImplementedException(
                    $"Could not calculate a correct {attenuationMode} is not supported")
            };
        }
        #endregion
    }
}
