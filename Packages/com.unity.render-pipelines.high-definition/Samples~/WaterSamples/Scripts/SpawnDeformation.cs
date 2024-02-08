using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class SpawnDeformation : MonoBehaviour
{
    public static float TimeBtwnEachDeformation = 0.5f;
    
	// Below this speed, no deformations are triggered
    public float thresholdSpeed = 0.75f; 
	
	// When the object is above this speed, the excess speed is not taken into account in the amplitude calculation of the ripple
    public float maxSpeed = 5f; 
	
	// Multiplier of the amplitude calculation (if you want bigger or smaller ripples)
    public float deformationAmplitudeMultipler = .1f; 
    
    private float lastDeformationSpawnedTime = 0f;
    private Vector3 lastPosition = Vector3.zero;

    // Update is called once per frame
    void Update()
    {
        Vector3 deltaPos = (this.transform.position - lastPosition) / Time.deltaTime;
        
        // If the object is faster than the threshold and is at the surface. h>0 means over the water, h>=1 means fully underwater
        float normalizedHeight = this.GetComponent<Buoyancy>().GetNormalizedHeightOfSphereBelowSurface();
        if (deltaPos.magnitude > thresholdSpeed && normalizedHeight > 0 && normalizedHeight < 1)
        {
            if ((Time.realtimeSinceStartup - lastDeformationSpawnedTime) >= TimeBtwnEachDeformation)
            {   
                if(PoolManager.Instances[PoolManager.InstanceType.Deformer] != null)
                {
                    GameObject deformer = PoolManager.Instances[PoolManager.InstanceType.Deformer].getNextAvailable();
                    if(deformer != null)
                    {
                        lastDeformationSpawnedTime = Time.realtimeSinceStartup;
                        deformer.transform.position = this.transform.position + this.transform.forward * deltaPos.magnitude * 0.01f;
                        
                        // The normalized speed acts like a multiplier, more speed means bigger ripples.
                        float normalizedSpeed = Mathf.InverseLerp(thresholdSpeed, maxSpeed, deltaPos.magnitude);
                        
                        deformer.GetComponent<DeformationManager>().SetAmplitude(normalizedSpeed * deformationAmplitudeMultipler);
                        deformer.SetActive(true);
                    }
                }
            }
            
        }
        
        lastPosition = this.transform.position;
    }
}
