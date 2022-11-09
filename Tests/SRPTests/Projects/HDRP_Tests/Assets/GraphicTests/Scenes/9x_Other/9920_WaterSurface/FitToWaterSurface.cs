using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class FitToWaterSurface : MonoBehaviour
{
    public WaterSurface targetSurface = null;
    public bool includeDeformation = false;

    // Internal search params
    WaterSearchParameters searchParameters = new WaterSearchParameters();
    WaterSearchResult searchResult = new WaterSearchResult();

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (targetSurface != null)
        {
            // Build the search parameters
            searchParameters.startPositionWS = gameObject.transform.position;
            searchParameters.targetPositionWS = gameObject.transform.position;
            searchParameters.error = 0.01f;
            searchParameters.maxIterations = 8;
            searchParameters.includeDeformation = includeDeformation;

            // Do the search
            if (targetSurface.ProjectPointOnWaterSurface(searchParameters, out searchResult))
            {
                gameObject.transform.position = searchResult.projectedPositionWS;
            }
        }
    }
}
