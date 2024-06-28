using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

[ExecuteInEditMode]
public class ShoreDecalTrigger : MonoBehaviour
{   
    public WaterSurface waterSurface;
    public GameObject decalGameObject = null;
        
    WaterSearchParameters searchParameters = new WaterSearchParameters();
    WaterSearchResult searchResult = new WaterSearchResult();

    void Start()
    {
        decalGameObject.SetActive(false);
    }
    
    // Update is called once per frame
    void Update()
    {
        if (waterSurface != null)
        {
            // Build the search parameters
            searchParameters.startPositionWS = searchResult.candidateLocationWS;
            searchParameters.targetPositionWS = gameObject.transform.position;
            searchParameters.error = 0.01f;
            searchParameters.maxIterations = 8;
            searchParameters.includeDeformation = true;
            //Here, we exclude the simulation to avoid having larger waves (band 0) trigger the shore waves
            searchParameters.excludeSimulation = true;      

            // Do the search
            if (waterSurface.ProjectPointOnWaterSurface(searchParameters, out searchResult))
            {
                // If the trigger is below the water surface, it means a wave hit it, so if there's not already a decal, we instantiate one. 
                if (searchResult.projectedPositionWS.y > this.transform.position.y && !decalGameObject.activeSelf)
                {
                    ActivateDecal();
                }
                
            }
        }
    }

    void ActivateDecal()
    {        
        if(decalGameObject != null)
        {
            decalGameObject.transform.parent = this.transform;
            decalGameObject.transform.localPosition = Vector3.zero;
            decalGameObject.transform.localEulerAngles = new Vector3(90, 0, -90);
            decalGameObject.SetActive(true);
        }
        
    }
}
