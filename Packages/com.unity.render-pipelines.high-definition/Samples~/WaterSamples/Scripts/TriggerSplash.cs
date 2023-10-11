using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.VFX;

public class TriggerSplash : MonoBehaviour
{
    public static float TimeBtwnEachSplash = 0.2f;
    
	// Below this speed, no splash VFX are triggered
    public float thresholdSpeed = 2f; 
	
		// When the object is above this speed, the excess speed is not taken into account in the amplitude calculation of the ripple
    public float maxSpeed = 5f; 
	
   
    private float lastSplashTime = 0f;
    // private Vector3 lastPosition = Vector3.zero;
	private Rigidbody rigibodyComponent = null;

	void Start()
	{
		rigibodyComponent = this.GetComponent<Rigidbody>();
	}
    // Update is called once per frame
    void Update()
    {
        // Vector3 deltaPos = (this.transform.position - lastPosition) / Time.deltaTime;
        
        // If the object is faster than the threshold and is at the surface. h>0 means over the water, h>=1 means fully underwater
        float normalizedHeight = this.GetComponent<Buoyancy>().GetNormalizedHeightOfSphereBelowSurface();
		float magnitude = rigibodyComponent.velocity.magnitude;
        if (magnitude > thresholdSpeed && normalizedHeight > 0 && normalizedHeight < 1)
        {
            if ((Time.realtimeSinceStartup - lastSplashTime) >= TimeBtwnEachSplash)
            {       

				// The normalized speed acts like a multiplier, more speed means bigger ripples.
				float normalizedSpeed = Mathf.InverseLerp(thresholdSpeed, maxSpeed, magnitude);
				
				lastSplashTime = Time.realtimeSinceStartup;
				
				GameObject splashObject = PoolManager.Instances[PoolManager.InstanceType.Splash].getNextAvailable();
				
				if (splashObject != null)
				{
					splashObject.transform.position = this.GetComponent<Buoyancy>().GetCurrentWaterPosition();

					VisualEffect splashVFX = splashObject.GetComponent<VisualEffect>();
					

					splashVFX.SetFloat("radius", this.GetComponent<Buoyancy>().sphereRadiusApproximation);
					splashVFX.SetVector3("velocity", rigibodyComponent.velocity);
					splashVFX.SetFloat("maxSpeed", normalizedSpeed * 10);
					splashVFX.SetFloat("maxSize", normalizedSpeed / 20);
					
					splashObject.SetActive(true);
					
					splashVFX.SendEvent("Play");
				}
			
            }
            
        }
        
    }

}
