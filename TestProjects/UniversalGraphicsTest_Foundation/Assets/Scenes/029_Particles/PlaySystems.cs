using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class PlaySystems : MonoBehaviour {

    public ParticleSystem[] particleSystems;
	[Range(0, 1)]
    public float simPoint = 0.5f;
    void OnEnable () {
		foreach(ParticleSystem ps in particleSystems){
            if (ps != null)
            {
                float life = ps.main.duration * simPoint;
                ps.Simulate(life, true, true, false);
            }
        }
	}

	void OnDisable()
    {
        foreach (ParticleSystem ps in particleSystems)
        {
			if(ps != null)
            	ps.Stop();
        }
    }
}
