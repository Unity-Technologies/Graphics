using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class DisplayText : MonoBehaviour {

	public UnityEngine.Experimental.Rendering.OnTileDeferredRenderPipeline.OnTileDeferredRenderPipeline pipeline;

	float updateInterval = 0.5f;
	double lastInterval = 0;
	int frames = 0;

	void Start () {
		lastInterval = Time.realtimeSinceStartup;
		frames = 0;
		//Random.state = 13;
	}

	string renderPipeline()
	{
		if (GraphicsSettings.renderPipelineAsset != null)
			return GraphicsSettings.renderPipelineAsset.name + (pipeline.UseLegacyCookies?", legacy cookies":", colored cookies") + ", transparency shadows " + (pipeline.TransparencyShadows?"enabled":"disabled");
		else
			return "None (See Camera Settings)";
	}

	void Update () {
		++frames;
		double timeNow = Time.realtimeSinceStartup;
		if (timeNow > lastInterval + updateInterval) {
			double ms = (timeNow - lastInterval) / frames * 1000.0;
			GetComponent<Text>().text =  SceneManager.GetActiveScene().name + ": " + ms.ToString ("f2") + "ms, RenderPipeline " + renderPipeline();
			frames = 0;
			lastInterval = timeNow;
		}
	}
}
