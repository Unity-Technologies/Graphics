using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

public class MobileTestSceneManager : MonoBehaviour {

	static int m_NextSceneIndex = 0;
	static MobileTestSceneManager Instance;

	private Vector2 startPosition;

	public UnityEngine.Experimental.Rendering.OnTileDeferredRenderPipeline.OnTileDeferredRenderPipeline pipeline;

	void Start () {
		if (Instance != null) {
			GameObject.Destroy (gameObject);
		} else {
			GameObject.DontDestroyOnLoad (gameObject);
			Instance = this;
		}
	}

	int detectSwipe() {
		if (Input.touchCount == 1 && Input.GetTouch (0).phase == TouchPhase.Ended) {
			Vector2 endPosition = Input.GetTouch (0).position;
			//Vector2 delta = endPosition - startPosition;

			if (startPosition.x < endPosition.x)
				return 1;
			else if (startPosition.x > endPosition.x)
				return -1;

		}

		return 0;
	}

	void detectTouchBegan() {
		if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Began) {
			startPosition = Input.GetTouch(0).position;
		}
	}

	void Update () {
#if (UNITY_EDITOR || UNITY_STANDALONE)
		if (Input.GetKeyDown(KeyCode.Space)) {
#else
		if (Input.touchCount == 3 && Input.GetTouch(0).phase == TouchPhase.Began) {
#endif
			SceneManager.LoadScene(m_NextSceneIndex++);
		}

#if (UNITY_EDITOR || UNITY_STANDALONE)
		if (Input.GetKeyDown(KeyCode.Tab)) {
#else
		if (detectSwipe() == 1) {
#endif
			pipeline.TransparencyShadows = !pipeline.TransparencyShadows;
		}

#if (UNITY_EDITOR || UNITY_STANDALONE)
		if (Input.GetKeyDown(KeyCode.LeftShift)) {
#else
		if (detectSwipe() == -1) {
#endif
			pipeline.UseLegacyCookies = !pipeline.UseLegacyCookies;
		}

#if (UNITY_EDITOR || UNITY_STANDALONE)
		if (Input.GetKeyDown(KeyCode.Return)) {
#else
		if (Input.touchCount == 2 && Input.GetTouch(0).phase == TouchPhase.Ended) {
#endif
			if (GraphicsSettings.renderPipelineAsset == null)
				GraphicsSettings.renderPipelineAsset = pipeline;
			else
				GraphicsSettings.renderPipelineAsset = null;
		}

#if !(UNITY_EDITOR || UNITY_STANDALONE)
		detectTouchBegan();
#endif

	}
}
