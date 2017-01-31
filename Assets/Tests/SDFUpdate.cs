using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SDFUpdate : MonoBehaviour {

    public VFXComponent vfx;

    private float m_AngleY = 0.0f;

	// Use this for initialization
	void Start () {
        //transform.Rotate(new Vector3(0.0f, 0.5f, 0.0f));
	}
	
	// Update is called once per frame
	void Update () {
        //m_AngleY += Time.deltaTime * 0.1f;
        transform.Rotate(new Vector3(0.0f, 25.0f * Time.deltaTime, 0.0f));

        if (vfx != null)
        {
            vfx.SetFloat("offsetSpawnX", 0.2f * Mathf.Sin(Time.time * 1.7f)/*Random.RandomRange(-0.2f, 0.2f)*/);
            vfx.SetFloat("offsetSpawnZ", 0.2f * Mathf.Cos(Time.time * 1.3f)/*Random.RandomRange(-0.2f, 0.2f)*/);
            vfx.SetFloat("spawnRadius", 0.02f + 0.08f * Mathf.Abs(Mathf.Cos(Time.time * 2.3f))/*Random.RandomRange(-0.2f, 0.2f)*/);
        }
	}
}
