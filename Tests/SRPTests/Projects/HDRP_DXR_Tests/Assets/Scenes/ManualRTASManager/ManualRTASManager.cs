using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public class ManualRTASManager : MonoBehaviour
{
    RayTracingAccelerationStructure rtas = null;
    public List<GameObject> gameObjects = new List<GameObject>();

    void Update()
    {
        HDRenderPipeline hdrp = RenderPipelineManager.currentPipeline is HDRenderPipeline ? (HDRenderPipeline)RenderPipelineManager.currentPipeline : null;
        if (hdrp != null)
        {
            // Get the HDCamera for the current camera
            var hdCamera = HDCamera.GetOrCreate(GetComponent<Camera>());

            // Evaluate the effect params
            HDEffectsParameters hdEffectParams = HDRenderPipeline.EvaluateEffectsParameters(hdCamera, true, false);

            // Clear the rtas from the previous frame
            if (rtas != null)
                rtas.Dispose();

            // Create the RTAS
            rtas = new RayTracingAccelerationStructure();

            // Add all the objects individually
            int numGameObjects = gameObjects.Count;
            for (int i = 0; i < numGameObjects; ++i)
                HDRenderPipeline.AddInstanceToRAS(rtas, gameObjects[i].GetComponent<Renderer>(), hdEffectParams, ref hdCamera.transformsDirty, ref hdCamera.materialsDirty);

            // Build the RTAS
            rtas.Build(transform.position);

            // Assign it to the camera
            hdCamera.rayTracingAccelerationStructure = rtas;
        }
    }

    void OnDestroy()
    {
        if (rtas != null)
            rtas.Dispose();
    }
}
