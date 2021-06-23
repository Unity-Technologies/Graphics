// using System.Collections;
// using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
// using UnityEngine.Experimental.Rendering;
// [ExecuteInEditMode]
namespace UnityEngine.Rendering.SDFRP
{
    [CreateAssetMenu(menuName = "SDF/CreateSDFAssetPipeline")]
    public class SDFRenderPipelineAsset : RenderPipelineAsset
    {
        public Color clearColor = Color.green;
        public bool EnableDepthOfField = true;
        public int lensRes = 9;
        public float lensSiz = 2.0f;
        public float focalDis = 11.0f;
        internal ComputeShader rayMarchingCS;

        protected override RenderPipeline CreatePipeline()
        {
            // TODO - Need to figure out how to do this using the defaultResources
            rayMarchingCS = Resources.Load<ComputeShader>("RayMarch");

            return new SDFRenderPipeline();
        }
    }
}
