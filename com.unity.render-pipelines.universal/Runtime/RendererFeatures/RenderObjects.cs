using System.Collections.Generic;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal.Internal;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Experimental.Rendering.Universal
{
    [MovedFrom("UnityEngine.Experimental.Rendering.LWRP")]public enum RenderQueueType
    {
        Opaque,
        Transparent,
    }

    [ExcludeFromPreset]
    [MovedFrom("UnityEngine.Experimental.Rendering.LWRP")]public class RenderObjects : ScriptableRendererFeature
    {
        [System.Serializable]
        public class RenderObjectsSettings
        {
            public string passTag = "RenderObjectsFeature";
            public RenderPassEvent Event = RenderPassEvent.AfterRenderingOpaques;

            public FilterSettings filterSettings = new FilterSettings();

            public Material overrideMaterial = null;
            public int overrideMaterialPassIndex = 0;

            public bool overrideDepthState = false;
            public CompareFunction depthCompareFunction = CompareFunction.LessEqual;
            public bool enableWrite = true;

            public StencilStateData stencilSettings = new StencilStateData();

            public CustomCameraSettings cameraSettings = new CustomCameraSettings();
        }

        [System.Serializable]
        public class FilterSettings
        {
            // TODO: expose opaque, transparent, all ranges as drop down
            public RenderQueueType RenderQueueType;
            public LayerMask LayerMask;
            public string[] PassNames;

            public FilterSettings()
            {
                RenderQueueType = RenderQueueType.Opaque;
                LayerMask = 0;
            }
        }

        [System.Serializable]
        public class CustomCameraSettings
        {
            public bool overrideCamera = false;
            public bool restoreCamera = true;
            public Vector4 offset;
            public float cameraFieldOfView = 60.0f;
        }

        public RenderObjectsSettings settings = new RenderObjectsSettings();

        RenderObjectsPass m_RenderObjectsPass;
        RenderObjectsPass m_DepthOnlyPass;

        public override void Create()
        {
            FilterSettings filter = settings.filterSettings;
            m_RenderObjectsPass = new RenderObjectsPass(settings.passTag, settings.Event, filter.PassNames,
                filter.RenderQueueType, filter.LayerMask, settings.cameraSettings);

            m_RenderObjectsPass.overrideMaterial = settings.overrideMaterial;
            m_RenderObjectsPass.overrideMaterialPassIndex = settings.overrideMaterialPassIndex;

            if (settings.overrideDepthState)
                m_RenderObjectsPass.SetDetphState(settings.enableWrite, settings.depthCompareFunction);

            if (settings.stencilSettings.overrideStencilState)
                m_RenderObjectsPass.SetStencilState(settings.stencilSettings.stencilReference,
                    settings.stencilSettings.stencilCompareFunction, settings.stencilSettings.passOperation,
                    settings.stencilSettings.failOperation, settings.stencilSettings.zFailOperation);

            if (settings.filterSettings.RenderQueueType == RenderQueueType.Opaque &&
                (settings.Event == RenderPassEvent.BeforeRenderingOpaques || settings.Event == RenderPassEvent.AfterRenderingOpaques))
            {
                var prepassEvent = RenderPassEvent.BeforeRenderingPrepasses + 10;
                m_DepthOnlyPass = new RenderObjectsPass(settings.passTag, prepassEvent, filter.PassNames,
                    filter.RenderQueueType, filter.LayerMask, settings.cameraSettings)
                {
                    onlyIfDepthPrepass = true
                };

                if (settings.overrideDepthState)
                    m_DepthOnlyPass.SetDetphState(settings.enableWrite, settings.depthCompareFunction);

                if (settings.stencilSettings.overrideStencilState)
                    m_DepthOnlyPass.SetStencilState(settings.stencilSettings.stencilReference,
                        settings.stencilSettings.stencilCompareFunction, settings.stencilSettings.passOperation,
                        settings.stencilSettings.failOperation, settings.stencilSettings.zFailOperation);
            }
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(m_RenderObjectsPass);
            if (m_DepthOnlyPass != null)
            {
                renderer.EnqueuePass(m_DepthOnlyPass);
            }
        }
    }
}

