using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SDFUpdate2 : MonoBehaviour {

    public VFXComponent vfx;

    private float m_AngleY = 0.0f;

	// Use this for initialization
	void Start () {

	}
	
	// Update is called once per frame
	void Update () {

        transform.Rotate(new Vector3(0.0f, 25.0f * Time.deltaTime, 0.0f));

        if (vfx != null)
        {
            Vector3 angles = new Vector3();
            angles.y = 80.0f * Mathf.Sin(Time.time * 0.7f * 2.0f);
            float offsetY = 0.25f * Mathf.Sin(Time.time * 1.3f * 2.0f) - 0.1f;
            float noiseAngle = -Time.time * 33.0f * 2.0f;

            Quaternion quat = Quaternion.Euler(angles);
            vfx.SetVector3("SpawnPosition", quat * Vector3.back + new Vector3(0,offsetY,0));
            vfx.SetFloat("NoiseRot", angles.y);
        }
	}
}
