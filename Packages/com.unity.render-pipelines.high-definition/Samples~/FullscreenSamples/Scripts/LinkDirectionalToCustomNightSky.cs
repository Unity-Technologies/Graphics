using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;


[ExecuteAlways]
public class LinkDirectionalToCustomNightSky : MonoBehaviour
{
    public Material SkyMat;
    Vector3 Dir;
    public bool update = true;

    void Update()
    {
        if (update)
        {
            var hdrp = RenderPipelineManager.currentPipeline as HDRenderPipeline;
            if (hdrp != null && hdrp.GetMainLight() != null)
            {
                Dir = hdrp.GetMainLight().gameObject.transform.forward;
                SkyMat.SetVector("_Moonlight_Forward_Direction", Dir);
            }

        }
    }


}
