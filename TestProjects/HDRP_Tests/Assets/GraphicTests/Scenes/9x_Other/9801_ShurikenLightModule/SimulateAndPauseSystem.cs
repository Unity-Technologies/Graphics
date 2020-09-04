using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulateAndPauseSystem : MonoBehaviour {

    public float forwardTime = 1.5f;
    private ParticleSystem ps;

    private void Start()
    {
    
            ps = GetComponent<ParticleSystem>();

            if (ps)
            {
                ps.Simulate(forwardTime);
                ps.Pause();
            }
            
            
      
    }
}
