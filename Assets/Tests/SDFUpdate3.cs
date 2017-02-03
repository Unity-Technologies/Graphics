using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SDFUpdate3 : MonoBehaviour {

    public VFXComponent vfx;

    private Vector3 m_Angles = new Vector3();

	// Use this for initialization
	void Start () {

	}
	
	// Update is called once per frame
	void Update () {

        if (vfx != null)
        {
            const float kMul = 0.01f;

            Vector3 angles = new Vector3();
            m_Angles.x += Time.time * 1.3f * kMul;
            m_Angles.y += Time.time * 0.7f * kMul;
            m_Angles.z += Time.time * 0.5f * kMul;

            vfx.SetVector3("vfRotation", m_Angles);
        }
	}
}
