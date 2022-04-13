using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class FitToWaterSurface : MonoBehaviour
{
    public WaterSurface targetSurface = null;
    public int numIterations = 8;
    public float error = 0.01f;

    // Internal search params
    WaterSearchParameters searchParameters = new WaterSearchParameters();
    WaterSearchResult searchResult = new WaterSearchResult();

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (targetSurface != null)
        {
            // Build the search parameters
            searchParameters.startPosition = searchResult.candidateLocation;
            searchParameters.targetPosition = gameObject.transform.position;
            searchParameters.error = error;
            searchParameters.maxIterations = numIterations;

            // Do the search
            if (targetSurface.FindWaterSurfaceHeight(searchParameters, out searchResult))
            {
                gameObject.transform.position = new Vector3(gameObject.transform.position.x, searchResult.height, gameObject.transform.position.z);
            }
        }
    }
}
