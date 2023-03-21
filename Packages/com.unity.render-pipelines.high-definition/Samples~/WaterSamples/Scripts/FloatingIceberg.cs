using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

[ExecuteInEditMode]
public class FloatingIceberg : MonoBehaviour
{
    public WaterSurface targetSurface = null;
    public float currentSpeedMultiplier = 1;
    public float selfRotationSpeed = 0;
    public Vector3 initialPosition;
    public float initialScale = 0.25f;
    public bool includeDeformers = true;
    public bool clickToResetPosition = false;

    // Internal search params
    WaterSearchParameters searchParameters = new WaterSearchParameters();
    WaterSearchResult searchResult = new WaterSearchResult();

    void Start()
    {
        Reset();
    }
    
    void Update()
    {
        float boundX = 50;
        float maxBoundX = 68;
        float opacity = 1 - Mathf.Clamp01((Mathf.Abs(this.transform.position.x) - boundX) / (maxBoundX - boundX));
        
        //If iceberg is at the begining or the end of the river. 
        if(this.transform.position.x > 0)
            this.GetComponent<Renderer>().sharedMaterial.SetFloat("_Opacity", opacity);
        else
            this.transform.localScale = Vector3.one * initialScale * opacity;
        
        if (this.transform.position.x < -maxBoundX || clickToResetPosition)
        {
            Reset();
        }
        
        if  (selfRotationSpeed > 0)
        {
            Vector3 r = this.transform.localEulerAngles;
            r.y += selfRotationSpeed;
            this.transform.localEulerAngles = r;
        }
        
        if (targetSurface != null)
        {
            // Build the search parameters
            searchParameters.startPositionWS = searchResult.candidateLocationWS;
            searchParameters.targetPositionWS = this.transform.position;
            searchParameters.error = 0.01f;
            searchParameters.maxIterations = 8;
            searchParameters.includeDeformation = includeDeformers;
            searchParameters.excludeSimulation = false;

            // Do the search
            if (targetSurface.ProjectPointOnWaterSurface(searchParameters, out searchResult))
            {
                // Applying the position to the surface and adding current
                this.transform.position = searchResult.projectedPositionWS + searchResult.currentDirectionWS * currentSpeedMultiplier;

            }
            else
            {
                //Can't Find Height, Script Interaction is probably disabled. 
            }
        }
    }
    
    void Reset()
    {
        this.transform.position = initialPosition;
        this.transform.localScale = Vector3.one * initialScale;
        clickToResetPosition = false;
    }


}
