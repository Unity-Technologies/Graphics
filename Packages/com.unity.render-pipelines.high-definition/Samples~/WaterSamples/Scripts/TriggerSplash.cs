using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.VFX;

public class TriggerSplash : MonoBehaviour
{
    public GameObject prefab; 

    private float previousNormalizedHeight = -1f;
	private Rigidbody rigibodyComponent = null;

	void OnEnable()
	{
		rigibodyComponent = this.GetComponent<Rigidbody>();
	}
    
    // Update is called once per frame
    void Update()
    {
        float normalizedHeight = this.GetComponent<Buoyancy>().GetNormalizedHeightOfSphereBelowSurface();
        
        // if the object is at the surface > 0,  h>0 means over the water, h>=1 means fully underwater
        if( (normalizedHeight > 0 && previousNormalizedHeight <= 0) )
        {
            if(PoolManager.Instances[PoolManager.InstanceType.Splash] != null)
            {
                GameObject splashObject = PoolManager.Instances[PoolManager.InstanceType.Splash].getNextAvailable();
                
                if (splashObject != null)
                {
                    splashObject.transform.position = this.GetComponent<Buoyancy>().GetCurrentWaterPosition();

                    VisualEffect splashVFX = splashObject.GetComponent<VisualEffect>();
                    
                    splashVFX.SetFloat("Splash Radius", this.GetComponent<Buoyancy>().sphereRadiusApproximation);
                    splashVFX.SetVector3("Velocity", rigibodyComponent.linearVelocity);
                    
                    splashObject.SetActive(true);   
                    splashVFX.Play();
                }
            }
        }
        
        previousNormalizedHeight = normalizedHeight;
        
    }

}
