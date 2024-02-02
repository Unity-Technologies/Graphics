using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

// This script approximate the buoyancy of a spherical object in water. 
// It uses Unity's Rigidbody for force application and angularDrag only. 

[ExecuteInEditMode]
[RequireComponent(typeof(Rigidbody))]
public class Buoyancy : MonoBehaviour
{  
    public WaterSurface targetSurface = null;
    
    public bool includeDeformation = true;
    [Tooltip("Approxative radius of object for buoyancy.")]
    public float sphereRadiusApproximation = 0.25f;
    
    [Tooltip("Specifies the multiplier for the movement induced by other deformation (waves, swell... etc).")]
    public float waveForceMultiplier = 1f;
    [Tooltip("Specifies the multiplier for the movement induced by the current of the water surface.")]
    public float currentSpeedMultiplier = 1f;

    [Tooltip("Specifies the multiplier for the drag forces induced by the viscosity of the mediums.")]
    public float dragMultiplier = 1;
    public float defaultRigidbodyDrag = 0.1f;
    public float underwaterRigidbodyAngularDrag = 1;
    public float overwaterRigidbodyAngularDrag = 0.05f;
    
    //Magic value to avoid having the object bounce too much on the water surface. 
    [Tooltip("Specifies the value for surface tension. A high value will stop the object bouncing faster on water.")]
    public float surfaceTensionDamping = 10f;
    
    [Tooltip("When enabled, the net force is applied with a random offset to create an angular velocity.")]
    public bool applyForceWithRandomOffset = false;
    
    [Tooltip("When enabled, the height of the custom mesh is taken into account when fetching water surface height.")]
    public bool isWaterSurfaceACustomMesh = false;

    [Tooltip("When enabled, a bunch of gizmos will show showing in blue, the position of the sampling for normals, in magenta the computed normal, in green the direction of the deformation force, in red the direction of the current force, and in a yellow sphere, the approximation volume the buoyancy calculations.")]
    public bool drawDebug = false;
    
    private Vector3 currentDirection;
    private Vector3 A, B, C;
    private Vector3 waterPosition;
    private Vector3 normal;
    private Vector3 deformationDirection;
    
    private Rigidbody rigidbodyComponent;
    
    // Height of the sphere that is underwater.
    private float h, hNormalized = 0;
  
    void Start()
    {
        rigidbodyComponent = this.GetComponent<Rigidbody>();
        
        // The script doesn't use ridigbody's gravity nor drag. 
        // Only angularDrag is used.  
        rigidbodyComponent.useGravity = false;
        rigidbodyComponent.linearDamping = defaultRigidbodyDrag;
        
        if(targetSurface == null)
            Debug.LogWarning("The variable 'targetSurface' needs a valid Water Surface to be assigned for the script to work properly.");
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (targetSurface != null)
        {
            // Retrieving informations to compute buoyancy;
            FetchWaterSurfaceData(this.transform.position, out waterPosition, out normal, out currentDirection);
            deformationDirection = Vector3.ProjectOnPlane(normal, Vector3.up);
            
            // height of the object that is below the surface. 0 means overwater
            h = Mathf.Clamp(waterPosition.y - (transform.position.y - sphereRadiusApproximation), 0, 2 * sphereRadiusApproximation);
            
            // normalized height of the sphere that is underwater. 0 means overwater, 1 fully below water, in between [0,1]
            hNormalized = h * 1/(2 * sphereRadiusApproximation);

            // Volume of sphere that is below the surface in m3. This only applies if we approximate the volume of the object as a sphere. 
            float volumeOfFluidDisplaced = (Mathf.PI * h * h / 3f) * (3 * sphereRadiusApproximation - h);            

            rigidbodyComponent.angularDamping = Mathf.Lerp(overwaterRigidbodyAngularDrag, underwaterRigidbodyAngularDrag, hNormalized);

            Vector3 weight = rigidbodyComponent.mass * Physics.gravity; // Weight of the object            
            Vector3 gravityForce = Vector3.Lerp(Physics.gravity, weight, hNormalized);

            // Variable for physics calculations.
            float waterDensity = 997f;              // Water is .997 g/m3 at 20°C
            float airDensity = 0.001293f;
            float airViscosity = 0.0000181f;
            float waterViscosity = 0.001f;
            float sphereDragCoefficient = 0.47f;    //Drag coefficient of a smooth sphere. 
            
            // Archimedes force
            Vector3 archimedesForce = -waterDensity * volumeOfFluidDisplaced * Physics.gravity;

            // Drag forces (Stokes' Law)
            Vector3 dragForceAir = 6 * Mathf.PI * sphereRadiusApproximation * airViscosity * -rigidbodyComponent.linearVelocity;
            Vector3 dragForceWater = 6 * Mathf.PI * sphereRadiusApproximation * waterViscosity * -rigidbodyComponent.linearVelocity;
            Vector3 dragForce = Vector3.Lerp(dragForceAir, dragForceWater, hNormalized) * dragMultiplier;
            
            // Terminal velocities
            float terminalVelocityWater = Mathf.Sqrt((2 * rigidbodyComponent.mass * -Physics.gravity.y) / (waterDensity * Mathf.PI * Mathf.Pow(sphereRadiusApproximation, 2) * sphereDragCoefficient));
            float terminalVelocityAir = Mathf.Sqrt((2 * rigidbodyComponent.mass * -Physics.gravity.y) / (airDensity * Mathf.PI * Mathf.Pow(sphereRadiusApproximation, 2) * sphereDragCoefficient));
            float terminalVelocity = Mathf.Lerp(terminalVelocityAir, terminalVelocityWater, hNormalized);
                        
            // Calculate the net force (difference between weight and buoyant force)
            Vector3 netForce = gravityForce + archimedesForce + dragForce;
            
            // Apply the net force to simulate sinking or buoyancy at a random position to simulation rotation
            Vector3 randomOffset = applyForceWithRandomOffset ? new Vector3(Random. Range(-1f, 1f), Random. Range(-1f, 1f), Random. Range(-1f, 1f)) * sphereRadiusApproximation/5f : Vector3.zero;
            rigidbodyComponent.AddForceAtPosition(netForce, this.transform.position + randomOffset, ForceMode.Acceleration);

            // If our object is at the interface of the water surface, this is to avoid the object bouncing too much and simulation surface tension damping
            if (hNormalized > 0 &&  hNormalized < 1)
            {
                Vector3 upwardVelocity = Vector3.Dot(rigidbodyComponent.linearVelocity, Physics.gravity.normalized) * Physics.gravity.normalized;
                Vector3 dampingForce = -upwardVelocity * surfaceTensionDamping;
                rigidbodyComponent.AddForce(dampingForce, ForceMode.Acceleration);
                
                // Adding force for small waves and ripples pushing objects. 
                rigidbodyComponent.AddForce(deformationDirection * waveForceMultiplier);
                
                // Adding force for currents.
                rigidbodyComponent.AddForce(currentDirection * currentSpeedMultiplier);
            } 

            // Preventing our object to reach a velocity higher than its terminal velocity in the current medium (air or water).
            if (rigidbodyComponent.linearVelocity.magnitude > terminalVelocity)
            {
                rigidbodyComponent.linearVelocity = rigidbodyComponent.linearVelocity.normalized * terminalVelocity;
            }

        }
    }
    
    private Vector3 FetchWaterSurfaceData(Vector3 point, out Vector3 positionWS, out Vector3 normalWS, out Vector3 currentDirectionWS)
    {
        WaterSearchParameters searchParameters = new WaterSearchParameters();
        WaterSearchResult searchResult = new WaterSearchResult();
        
        // Build the search parameters
        searchParameters.startPositionWS = searchResult.candidateLocationWS;
        searchParameters.targetPositionWS = point;
        searchParameters.error = 0.01f;
        searchParameters.maxIterations = 8;
        searchParameters.includeDeformation = includeDeformation;
        searchParameters.outputNormal = true;

        // Init the out variable with default values. 
        positionWS = searchResult.candidateLocationWS;
        normalWS = Vector3.up;
        currentDirectionWS = Vector3.right; 
        
        // Do the search
        if (targetSurface.ProjectPointOnWaterSurface(searchParameters, out searchResult))
        {
            positionWS = searchResult.projectedPositionWS;   
            currentDirectionWS = searchResult.currentDirectionWS;
            normalWS = searchResult.normalWS;
        }
        
        // Needed when geometry type is set to custom mesh 
        Vector3 offsetY = isWaterSurfaceACustomMesh ? Vector3.up * targetSurface.transform.position.y : Vector3.zero;
        
        return positionWS + offsetY;
    }
	
	public Vector3 GetCurrentWaterPosition()
	{
		return waterPosition;
	}
    
    // Keeping this function for learning purpose but not needed anymore since we can fetch the normal directly from FetchWaterSurfaceData function
    private Vector3 FindNormalAtPoint(Vector3 point, float sampleDistance)
    {
        A = point + new Vector3(sampleDistance * Mathf.Cos(0     * Mathf.PI/180), 0, sampleDistance * Mathf.Sin(0     * Mathf.PI/180));
        B = point + new Vector3(sampleDistance * Mathf.Cos(120   * Mathf.PI/180), 0, sampleDistance * Mathf.Sin(120   * Mathf.PI/180));
        C = point + new Vector3(sampleDistance * Mathf.Cos(240   * Mathf.PI/180), 0, sampleDistance * Mathf.Sin(240   * Mathf.PI/180));

        
        WaterSearchParameters searchParametersNormalA = new WaterSearchParameters();
        WaterSearchResult searchResultNormalA = new WaterSearchResult();
        
        // Build the search parameters for A
        searchParametersNormalA.startPositionWS = searchResultNormalA.candidateLocationWS;
        searchParametersNormalA.targetPositionWS = A;
        searchParametersNormalA.error = 0.01f;
        searchParametersNormalA.maxIterations = 8;
        searchParametersNormalA.includeDeformation = includeDeformation;

        // Do the search
        if (targetSurface.ProjectPointOnWaterSurface(searchParametersNormalA, out searchResultNormalA))
        {
            A = new Vector3(A.x, searchResultNormalA.projectedPositionWS.y, A.z);
        }
        
        WaterSearchParameters searchParametersNormalB = new WaterSearchParameters();
        WaterSearchResult searchResultNormalB = new WaterSearchResult();
        
        // Build the search parameters for B
        searchParametersNormalB.startPositionWS = searchResultNormalB.candidateLocationWS;
        searchParametersNormalB.targetPositionWS = B;
        searchParametersNormalB.error = 0.01f;
        searchParametersNormalB.maxIterations = 8;
        searchParametersNormalB.includeDeformation = includeDeformation;

        // Do the search
        if (targetSurface.ProjectPointOnWaterSurface(searchParametersNormalB, out searchResultNormalB))
        {
            B = new Vector3(B.x, searchResultNormalB.projectedPositionWS.y, B.z);
        }
        
        WaterSearchParameters searchParametersNormalC = new WaterSearchParameters();
        WaterSearchResult searchResultNormalC = new WaterSearchResult();
        
        // Build the search parameters for C
        searchParametersNormalC.startPositionWS = searchResultNormalC.candidateLocationWS;
        searchParametersNormalC.targetPositionWS = C;
        searchParametersNormalC.error = 0.01f;
        searchParametersNormalC.maxIterations = 8;
        searchParametersNormalC.includeDeformation = includeDeformation;

        // Do the search
        if (targetSurface.ProjectPointOnWaterSurface(searchParametersNormalC, out searchResultNormalC))
        {
            C = new Vector3(C.x, searchResultNormalC.projectedPositionWS.y, C.z);
        }
        
        // Compute the normal based on the result of the 3 samples.
        return -Vector3.Normalize(Vector3.Cross(B - A, C - A));
    }
    
    private float GetHeightOfSphereBelowSurface()
    {
        return h;
    }
    
    public float GetNormalizedHeightOfSphereBelowSurface()
    {
        return hNormalized;
    }
    
    void OnDrawGizmosSelected()
    {
        if (drawDebug)
        {
            // Draws sphere where we sample points for 
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(A, sphereRadiusApproximation/10f);
            Gizmos.DrawSphere(B, sphereRadiusApproximation/10f);
            Gizmos.DrawSphere(C, sphereRadiusApproximation/10f);
            
            // Draws a line to show the computed normal
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, transform.position + normal);
            
            // Draws a line to show the direction for deformation force.
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, transform.position + deformationDirection * 10);
            
            // Draws a red line to show the force for the current.
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position + currentDirection);
            
            // Draws in yellow a sphere representing the sphere used for volume approximation
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(transform.position, sphereRadiusApproximation);
        }
    }
}
