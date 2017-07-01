using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MobileTestSceneManager : MonoBehaviour {

	static int m_NextSceneIndex = 0;
	static MobileTestSceneManager Instance;

	void Start () {
		if (Instance != null) {
			GameObject.Destroy (gameObject);
		} else {
			GameObject.DontDestroyOnLoad (gameObject);
			Instance = this;
		}
	}

	void Update () {
#if UNITY_EDITOR
		if (Input.GetKeyDown(KeyCode.Space)) {
#else
		if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Began) {
#endif
			SceneManager.LoadScene(m_NextSceneIndex++);
		}
	}		
}
