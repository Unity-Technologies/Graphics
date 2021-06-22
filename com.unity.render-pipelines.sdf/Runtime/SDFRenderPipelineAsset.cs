// using System.Collections;
// using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
// using UnityEngine.Experimental.Rendering;
// [ExecuteInEditMode]
[CreateAssetMenu(menuName = "SDF/CreateSDFAssetPipeline")]
public class SDFRenderPipelineAsset : RenderPipelineAsset
{
    public Color clearColor = Color.green;

    protected override RenderPipeline CreatePipeline()
    {
        return new SDFRenderPipeline();
    }
}