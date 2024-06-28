using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

[ExecuteInEditMode]
public class FitToWaterSurface : MonoBehaviour
{
    public WaterSurface targetSurface = null;
    public bool includeDeformation = true;
    public bool excludeSimulation = false;

    // Internal search params
    WaterSearchParameters searchParameters = new WaterSearchParameters();
    WaterSearchResult searchResult = new WaterSearchResult();

    // Update is called once per frame
    void Update()
    {
        if (targetSurface != null)
        {
            // Build the search parameters
            searchParameters.startPositionWS = searchResult.candidateLocationWS;
            searchParameters.targetPositionWS = gameObject.transform.position;
            searchParameters.error = 0.01f;
            searchParameters.maxIterations = 8;
            searchParameters.includeDeformation = includeDeformation;
            searchParameters.excludeSimulation = excludeSimulation;

            // Do the search
            if (targetSurface.ProjectPointOnWaterSurface(searchParameters, out searchResult))
            {
                gameObject.transform.position = searchResult.projectedPositionWS;
            }
        }
    }
}
