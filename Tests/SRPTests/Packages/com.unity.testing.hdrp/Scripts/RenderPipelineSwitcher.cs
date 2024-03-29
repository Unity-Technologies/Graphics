using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEngine.Experimental.Rendering.HDPipelineTest
{
    [ExecuteInEditMode]
    public class RenderPipelineSwitcher : MonoBehaviour
    {
        HDRenderPipelineAsset previousPipeline = null;
        public HDRenderPipelineAsset targetPipeline = null;

        void OnEnable()
        {
            if (previousPipeline == null)
            {
                previousPipeline = (GraphicsSettings.defaultRenderPipeline as HDRenderPipelineAsset);
            }
            if (targetPipeline != null && GraphicsSettings.defaultRenderPipeline != targetPipeline)
            {
                GraphicsSettings.defaultRenderPipeline = targetPipeline;
            }
        }

        void Update()
        {
            if (previousPipeline == null)
            {
                previousPipeline = (GraphicsSettings.defaultRenderPipeline as HDRenderPipelineAsset);
            }
            if (targetPipeline != null && GraphicsSettings.defaultRenderPipeline != targetPipeline)
            {
                GraphicsSettings.defaultRenderPipeline = targetPipeline;
            }
        }

        void OnDisable()
        {
            GraphicsSettings.defaultRenderPipeline = previousPipeline;
        }
    }
}
