# Align objects to the water surface using normals

When accessing height, you can query the normal of the water surface at a given point as an additional output.

1. Set the following variable in the search parameter `struct` sent to the system:

	```
	searchParameters.outputNormal = true;
	```

1. Use the result to align an object along the surface of the water, for example:

	```
	gameObject.transform.LookAt(searchResult.projectedPositionWS + searchResult.normalWS, Vector3.up);
	```

1. When using the [Burst version of the API](float-objects-on-a-water-surface.md#burstversion), allocate the array to store normals.

	The following script contains only the relevant lines to add to the [script to float an array of objects](float-objects-on-a-water-surface.md#burstversion) to support querying normals.

	```
	public class FitToWaterSurface_Burst : MonoBehaviour
	{
	    NativeArray<float3> normalWSBuffer;
	
	    void Start()
	    {
	        normalWSBuffer = new NativeArray<float3>(resolution * resolution, Allocator.Persistent);
	    }
	
	    void Update()
	    {
	        searchJob.outputNormal = true;
	        searchJob.normalWSBuffer = normalWSBuffer;
	    }
	
	    private void OnDestroy()
	    {
	        normalWSBuffer.Dispose();
	    }
	}
	```
