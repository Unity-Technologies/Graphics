using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
public class EnableTestGraph : MonoBehaviour
{
    public bool enableTestGraph;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (enableTestGraph)
        {
            (RenderPipelineManager.currentPipeline as SRP0802_RenderGraph).enableTestGraph = true;
            enableTestGraph = false;
        }
    }
}
