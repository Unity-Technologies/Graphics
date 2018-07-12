using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.SceneManagement;

public class ChangeVolumetricQuality : MonoBehaviour
{
	HDRenderPipelineAsset	hdrp;

	// This function is called before the graphic test screenshot, hopefully ...
	void Awake ()
	{
		hdrp = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
		SceneManager.sceneUnloaded += ResetRenderPipelineSettings;

		SetRenderPipelineSettings();
	}

	void SetRenderPipelineSettings()
	{
		hdrp.renderPipelineSettings.increaseResolutionOfVolumetrics = true;
	}

	void ResetRenderPipelineSettings(Scene scene)
	{
		hdrp.renderPipelineSettings.increaseResolutionOfVolumetrics = false;
	}
}
