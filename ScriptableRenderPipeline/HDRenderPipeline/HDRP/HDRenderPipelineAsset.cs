using UnityEngine.Serialization;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    // The HDRenderPipeline assumes linear lighting. Doesn't work with gamma.
    public class HDRenderPipelineAsset : RenderPipelineAsset, ISerializationCallbackReceiver
    {
        HDRenderPipelineAsset()
        {
        }

        protected override IRenderPipeline InternalCreatePipeline()
        {
            return new HDRenderPipeline(this);
        }

        [SerializeField]
        RenderPipelineResources m_RenderPipelineResources;
        public RenderPipelineResources renderPipelineResources
        {
            get { return m_RenderPipelineResources; }
            set { m_RenderPipelineResources = value; }
        }

        // To be able to turn on/off FrameSettings properties at runtime for debugging purpose without affecting the original one
        // we create a runtime copy (m_ActiveFrameSettings that is used, and any parametrization is done on serialized frameSettings)
        [SerializeField]
        [FormerlySerializedAs("serializedFrameSettings")]
        FrameSettings m_FrameSettings = new FrameSettings(); // This are the defaultFrameSettings for all the camera and apply to sceneView, public to be visible in the inspector
        // Not serialized, not visible, the settings effectively used
        FrameSettings m_FrameSettingsRuntime = new FrameSettings();

        bool m_frameSettingsIsDirty = true;
        public bool frameSettingsIsDirty
        {
            get { return m_frameSettingsIsDirty; }
        }

        public FrameSettings GetFrameSettings()
        {
            return m_FrameSettingsRuntime;
        }

        // See comment in FrameSettings.UpdateDirtyFrameSettings()
        // for detail about this function
        public void UpdateDirtyFrameSettings()
        {
            if (m_frameSettingsIsDirty)
            {
                m_FrameSettings.CopyTo(m_FrameSettingsRuntime);

                m_frameSettingsIsDirty = false;
            }
        }

        public ReflectionSystemParameters reflectionSystemParameters
        {
            get
            {
                return new ReflectionSystemParameters
                {
                    maxPlanarReflectionProbes = renderPipelineSettings.lightLoopSettings.maxPlanarReflectionProbes,
                    planarReflectionProbeSize = renderPipelineSettings.lightLoopSettings.planarReflectionTextureSize
                };
            }
        }

        // Store the various RenderPipelineSettings for each platform (for now only one)
        public RenderPipelineSettings renderPipelineSettings = new RenderPipelineSettings();

        // Return the current use RenderPipelineSettings (i.e for the current platform)
        public RenderPipelineSettings GetRenderPipelineSettings()
        {
            return renderPipelineSettings;
        }

        [SerializeField]
        public DiffusionProfileSettings diffusionProfileSettings;

        public override Shader GetDefaultShader()
        {
            return m_RenderPipelineResources.defaultShader;
        }

        public override Material GetDefaultMaterial()
        {
            return m_RenderPipelineResources.defaultDiffuseMaterial;
        }

        // Note: This function is HD specific
        public Material GetDefaultDecalMaterial()
        {
            return m_RenderPipelineResources.defaultDecalMaterial;
        }

        public override Material GetDefaultParticleMaterial()
        {
            return null;
        }

        public override Material GetDefaultLineMaterial()
        {
            return null;
        }

        public override Material GetDefaultTerrainMaterial()
        {
            return null;
        }

        public override Material GetDefaultUIMaterial()
        {
            return null;
        }

        public override Material GetDefaultUIOverdrawMaterial()
        {
            return null;
        }

        public override Material GetDefaultUIETC1SupportedMaterial()
        {
            return null;
        }

        public override Material GetDefault2DMaterial()
        {
            return null;
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            // This is call on load or when this settings are change.
            // When FrameSettings are manipulated we reset them to reflect the change, discarding all the Debug Windows change.
            // Tag as dirty so frameSettings are correctly initialize at next HDRenderPipeline.Render() call
            m_frameSettingsIsDirty = true;
        }
    }
}
