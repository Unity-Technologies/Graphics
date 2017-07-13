using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeepMeAlive : MonoBehaviour {

	static KeepMeAlive Instance;

	void Start () {
		if (Instance != null) {
			GameObject.Destroy (gameObject);
		} else {
			GameObject.DontDestroyOnLoad (gameObject);
			Instance = this;
		}
	}
}
