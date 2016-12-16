using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Experimental.ScriptableRenderLoop;

public class TestRealtime : MonoBehaviour {

    public GameObject m_SceneSettings = null;
    public float m_RotationSpeed = 50.0f;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        if (m_SceneSettings != null)
        {
            HDRISkyParameters skyParams = m_SceneSettings.GetComponent<HDRISkyParameters>();
            if (skyParams)
                skyParams.rotation = (skyParams.rotation + Time.deltaTime * m_RotationSpeed) % 360.0f;
        }
	}
}
