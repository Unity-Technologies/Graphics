using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MobileTestSceneManager : MonoBehaviour {

	static int m_NextSceneIndex = 0;

	static MobileTestSceneManager Instance;

	// Use this for initialization
	void Start () {
		if (Instance != null) {
			GameObject.Destroy (gameObject);
		} else {
			GameObject.DontDestroyOnLoad (gameObject);
			Instance = this;
		}
	}
	
	// Update is called once per frame
	void Update () {
		if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Began) {
			SceneManager.LoadScene(m_NextSceneIndex++);
		}
	}
}
