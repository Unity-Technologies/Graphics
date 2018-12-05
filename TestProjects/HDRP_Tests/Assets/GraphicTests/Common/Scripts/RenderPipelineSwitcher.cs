using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEngine.Experimental.Rendering.HDPipelineTest
{
    [ExecuteInEditMode]
	public class RenderPipelineSwitcher : MonoBehaviour
	{
	    HDRenderPipelineAsset previousPipeline = null;
	    public HDRenderPipelineAsset targetPipeline = null;

		void OnEnable ()
	    {
	    	if(previousPipeline == null)
	    	{
	        	previousPipeline = (GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset);
	    	}
            if (targetPipeline != null && GraphicsSettings.renderPipelineAsset != targetPipeline)
            {
                GraphicsSettings.renderPipelineAsset = targetPipeline;
            }
        }
        void Update()
        {
            if (previousPipeline == null)
            {
                previousPipeline = (GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset);
            }
	        if(targetPipeline != null && GraphicsSettings.renderPipelineAsset != targetPipeline)
	        {
	        	GraphicsSettings.renderPipelineAsset = targetPipeline;
	        }
		}

		void OnDisable()
		{
        	GraphicsSettings.renderPipelineAsset = previousPipeline;
		}
	}
}
