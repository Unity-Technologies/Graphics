using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(VFXComponent))]
public class CustomSpawnerTest : MonoBehaviour
{
	// Use this for initialization
	void Start ()
    {
	}

    float time = 0.0f;
	// Update is called once per frame
	void Update ()
    {
        time -= Time.deltaTime;
        if (time < 0.0f)
        {
            var vfx = gameObject.GetComponent<VFXComponent>();
            vfx.SendEvent("Test");
            time = Random.Range(1.0f, 3.0f);
        }
    }
}
