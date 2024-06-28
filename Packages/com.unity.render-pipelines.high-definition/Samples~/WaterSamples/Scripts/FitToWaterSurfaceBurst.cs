using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class FitToWaterSurfaceBurst : MonoBehaviour
{
    // Public parameters
    public int count = 50;
    public WaterSurface waterSurface = null;
    public bool includeDeformation = true;
    public bool excludeSimulation = false;
    public float currentSpeedMultiplier = 1.0f;
    public GameObject prefab;

    // List
    List<GameObject> prefabList;
    BoxCollider boxCollider;

    // Input job parameters
    NativeArray<float3> targetPositionBuffer;

    // Output job parameters
    NativeArray<float> errorBuffer;
    NativeArray<float3> candidatePositionBuffer;
    NativeArray<float3> projectedPositionWSBuffer;
    NativeArray<float3> normalWSBuffer;
    NativeArray<float3> directionBuffer;
    NativeArray<int> stepCountBuffer;

    // Start is called before the first frame update
    void Start()
    {
        boxCollider = this.GetComponent<BoxCollider>();
        Reset();
    }

    void Reset()
    {
        //Dispose buffer if already created
        OnDestroy();

        // Allocate the buffers
        targetPositionBuffer = new NativeArray<float3>(count, Allocator.Persistent);
        errorBuffer = new NativeArray<float>(count, Allocator.Persistent);
        candidatePositionBuffer = new NativeArray<float3>(count, Allocator.Persistent);
        projectedPositionWSBuffer = new NativeArray<float3>(count, Allocator.Persistent);
        normalWSBuffer = new NativeArray<float3>(count, Allocator.Persistent);
        stepCountBuffer = new NativeArray<int>(count, Allocator.Persistent);
        directionBuffer = new NativeArray<float3>(count, Allocator.Persistent);
		
        prefabList = new List<GameObject>();
        prefabList.Clear();

        // Need to do it like this to be able to delete child in edit mode;
        for (int i = this.transform.childCount; i > 0; --i)
            SmartDestroy(this.transform.GetChild(0).gameObject);

        for (int x = 0; x < count; ++x)
        {
            GameObject instance = GameObject.Instantiate(prefab);
            instance.transform.parent = this.transform;
            instance.transform.localPosition = RandomPointInBounds(GetComponent<Collider>().bounds) - this.transform.position;
            instance.transform.localEulerAngles = new Vector3(-180, UnityEngine.Random.Range(0,360), 0);
            prefabList.Add(instance);
        }

    }

    // Update is called once per frame
    void Update()
    {
        if (waterSurface == null)
            return;
        if (!targetPositionBuffer.IsCreated)
            Reset();

        // Try to get the simulation data if available
        WaterSimSearchData simData = new WaterSimSearchData();
        if (!waterSurface.FillWaterSearchData(ref simData))
            return;

        // Fill the input positions
        for (int i = 0; i < prefabList.Count; ++i)
        {
            targetPositionBuffer[i] = prefabList[i].transform.position;
        }

        // Prepare the first band
        WaterSimulationSearchJob searchJob = new WaterSimulationSearchJob();

        // Assign the simulation data
        searchJob.simSearchData = simData;

        // Fill the input data
        searchJob.targetPositionWSBuffer = targetPositionBuffer;
        searchJob.startPositionWSBuffer = targetPositionBuffer;
        searchJob.maxIterations = 8;
        searchJob.error = 0.01f;
        searchJob.includeDeformation = includeDeformation;
        searchJob.excludeSimulation = excludeSimulation;

        searchJob.projectedPositionWSBuffer = projectedPositionWSBuffer;
        searchJob.normalWSBuffer = normalWSBuffer;
        searchJob.errorBuffer = errorBuffer;
        searchJob.candidateLocationWSBuffer = candidatePositionBuffer;
        searchJob.directionBuffer = directionBuffer;
        searchJob.stepCountBuffer = stepCountBuffer;

        // Schedule the job with one Execute per index in the results array and only 1 item per processing batch
        JobHandle handle = searchJob.Schedule(count, 1);
        handle.Complete();

        // Fill the input positions
        for (int i = 0; i < prefabList.Count; ++i)
        {
            float3 projectedPosition = projectedPositionWSBuffer[i];
            prefabList[i].transform.position = projectedPosition + Time.deltaTime * directionBuffer[i] * currentSpeedMultiplier;
        }
    }

    private Vector3 RandomPointInBounds(Bounds bounds) {
        return new Vector3(
            UnityEngine.Random.Range(bounds.min.x, bounds.max.x),
            UnityEngine.Random.Range(bounds.min.y, bounds.max.y),
            UnityEngine.Random.Range(bounds.min.z, bounds.max.z)
        );
    }

    void OnDestroy()
    {
        if(targetPositionBuffer.IsCreated)      	targetPositionBuffer.Dispose();
        if(errorBuffer.IsCreated)               	errorBuffer.Dispose();
        if(candidatePositionBuffer.IsCreated)   	candidatePositionBuffer.Dispose();
        if(projectedPositionWSBuffer.IsCreated)   	projectedPositionWSBuffer.Dispose();
        if(normalWSBuffer.IsCreated)                normalWSBuffer.Dispose();
        if(stepCountBuffer.IsCreated)           	stepCountBuffer.Dispose();
        if(directionBuffer.IsCreated)           	directionBuffer.Dispose();
    }

    void OnDisable()
    {
        OnDestroy();
    }

    public static void SmartDestroy(UnityEngine.Object obj)
    {
        if (obj == null)
        {
        return;
        }

#if UNITY_EDITOR
        if (EditorApplication.isPlaying)
        {
            Destroy(obj);
        }
        else
        {
            DestroyImmediate(obj);
        }
#else
        Destroy(obj);
#endif
    }

}
