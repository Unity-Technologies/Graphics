using System;
using System.ComponentModel;

namespace UnityEngine.Rendering.HighDefinition
{
    [Serializable]
    [Category("Miscellaneous")]
    [SupportedOnRenderPipeline(typeof(HDRenderPipelineAsset))]
    class RenderGraphSettings: IRenderPipelineGraphicsSettings
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
        [InspectorName("Dynamic Render Pass Culling")]
        [Tooltip("When enabled, rendering passes are automatically culled based on what is visible on the camera.")]
        private bool m_DynamicRenderPassCulling;
        #endregion

        #region Data Accessors

        /// <summary>
        /// When enabled, rendering passes are automatically culled based on what is visible on the camera.
        /// </summary>
        public bool dynamicRenderPassCullingEnabled
        {
            get => m_DynamicRenderPassCulling;
            set => this.SetValueAndNotify(ref m_DynamicRenderPassCulling, value);
        }

        #endregion
    }
}
