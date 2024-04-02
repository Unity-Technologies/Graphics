using System;

namespace UnityEngine.Rendering.HighDefinition
{
    [Serializable]
    [SupportedOnRenderPipeline(typeof(HDRenderPipelineAsset))]
    [Categorization.CategoryInfo(Name = "Custom Post Process Order", Order = 30)]
    class CustomPostProcessOrdersSettings : IRenderPipelineGraphicsSettings
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
        [InspectorName("After Opaque And Sky")]
        internal CustomPostProcessVolumeComponentList m_BeforeTransparentCustomPostProcesses = new(CustomPostProcessInjectionPoint.AfterOpaqueAndSky);

        [SerializeField]
        [InspectorName("Before TAA")]
        internal CustomPostProcessVolumeComponentList m_BeforeTAACustomPostProcesses = new(CustomPostProcessInjectionPoint.BeforeTAA);

        [SerializeField]
        [InspectorName("Before Post Process")]
        internal CustomPostProcessVolumeComponentList m_BeforePostProcessCustomPostProcesses = new (CustomPostProcessInjectionPoint.BeforePostProcess);

        [SerializeField]
        [InspectorName("After Post Process Blurs")]
        internal CustomPostProcessVolumeComponentList m_AfterPostProcessBlursCustomPostProcesses = new (CustomPostProcessInjectionPoint.AfterPostProcessBlurs);

        [SerializeField]
        [InspectorName("After Post Process")]
        internal CustomPostProcessVolumeComponentList m_AfterPostProcessCustomPostProcesses = new (CustomPostProcessInjectionPoint.AfterPostProcess);
        #endregion

        #region Data Accessors

        public CustomPostProcessVolumeComponentList beforeTAACustomPostProcesses
        {
            get => m_BeforeTAACustomPostProcesses;
            set => m_BeforeTAACustomPostProcesses = value;
        }

        public CustomPostProcessVolumeComponentList beforePostProcessCustomPostProcesses
        {
            get => m_BeforePostProcessCustomPostProcesses;
            set => m_BeforePostProcessCustomPostProcesses = value;
        }

        public CustomPostProcessVolumeComponentList beforeTransparentCustomPostProcesses
        {
            get => m_BeforeTransparentCustomPostProcesses;
            set => m_BeforeTransparentCustomPostProcesses = value;
        }

        public CustomPostProcessVolumeComponentList afterPostProcessBlursCustomPostProcesses
        {
            get => m_AfterPostProcessBlursCustomPostProcesses;
            set => m_AfterPostProcessBlursCustomPostProcesses = value;
        }

        public CustomPostProcessVolumeComponentList afterPostProcessCustomPostProcesses
        {
            get => m_AfterPostProcessCustomPostProcesses;
            set => m_AfterPostProcessCustomPostProcesses = value;
        }

        #endregion

        public bool IsCustomPostProcessRegistered(Type customPostProcessType)
        {
            string type = customPostProcessType.AssemblyQualifiedName;
            return beforeTransparentCustomPostProcesses.Contains(type)
                   || beforePostProcessCustomPostProcesses.Contains(type)
                   || afterPostProcessBlursCustomPostProcesses.Contains(type)
                   || afterPostProcessCustomPostProcesses.Contains(type)
                   || beforeTAACustomPostProcesses.Contains(type);
        }
    }
}
