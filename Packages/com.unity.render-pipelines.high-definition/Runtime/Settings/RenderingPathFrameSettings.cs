using System;

// To be able to turn on/off FrameSettings properties at runtime for debugging purpose without affecting the original one
// we create a runtime copy (m_ActiveFrameSettings that is used, and any parametrization is done on serialized frameSettings)

namespace UnityEngine.Rendering.HighDefinition
{
    [Serializable]
    [SupportedOnRenderPipeline(typeof(HDRenderPipelineAsset))]
    [Categorization.CategoryInfo(Name = "Frame Settings (Default Values)", Order = 10)]
    class RenderingPathFrameSettings : IRenderPipelineGraphicsSettings
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
        [SerializeField] FrameSettings m_Camera = FrameSettingsDefaults.Get(FrameSettingsRenderType.Camera);
        [SerializeField] FrameSettings m_CustomOrBakedReflection = FrameSettingsDefaults.Get(FrameSettingsRenderType.CustomOrBakedReflection);
        [SerializeField] FrameSettings m_RealtimeReflection = FrameSettingsDefaults.Get(FrameSettingsRenderType.RealtimeReflection);
        #endregion

        #region Data Accessors

        internal ref FrameSettings GetDefaultFrameSettings(FrameSettingsRenderType type)
        {
            switch (type)
            {
                case FrameSettingsRenderType.Camera:
                    return ref m_Camera;
                case FrameSettingsRenderType.CustomOrBakedReflection:
                    return ref m_CustomOrBakedReflection;
                case FrameSettingsRenderType.RealtimeReflection:
                    return ref m_RealtimeReflection;
                default:
                    throw new ArgumentException($"Unknown {nameof(FrameSettingsRenderType)}");
            }
        }

        #endregion
    }
}
