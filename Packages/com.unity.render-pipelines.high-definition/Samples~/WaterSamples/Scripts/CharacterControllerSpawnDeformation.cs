using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class CharacterControllerSpawnDeformation : MonoBehaviour
{
	public static float TimeBtwnEachDeformation = 0.025f;
    public CharacterController controller;
    public PlayerMovement playerMovement;
    private float lastDeformationSpawnedTime = 0f;


    // Update is called once per frame
    void Update()
    {
        if(IsControllerMoving())
        {
            if(Time.realtimeSinceStartup - lastDeformationSpawnedTime >= TimeBtwnEachDeformation)
            {       
                if(PoolManager.Instances[PoolManager.InstanceType.Deformer] != null)
                {
                    GameObject deformer = PoolManager.Instances[PoolManager.InstanceType.Deformer].getNextAvailable();
                    if(deformer != null)
                    {
                        lastDeformationSpawnedTime = Time.realtimeSinceStartup;
                        // We push forward the deformer to appear in front of the gameobject
                        deformer.transform.position = this.transform.position + playerMovement.modelTransform.forward * Vector3.Normalize(controller.velocity).magnitude;
                        deformer.SetActive(true);
                    }
                }
            }
            
        }
    }
	
	private bool IsControllerMoving()
	{
		return controller.velocity.sqrMagnitude > 0;
	}
}
