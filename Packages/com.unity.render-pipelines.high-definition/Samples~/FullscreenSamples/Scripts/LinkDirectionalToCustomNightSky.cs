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
    Light mainLight;
    float previousIntensity;
    Color previousColor;

    void OnEnable()
    {
        var hdrp = RenderPipelineManager.currentPipeline as HDRenderPipeline;
        if (hdrp != null && hdrp.GetMainLight() != null)
            {              
                 mainLight = hdrp.GetMainLight();

                 //This is to force the mainlight to specific intensity and color for good presentation
                 previousIntensity = mainLight.intensity;
                 previousColor = mainLight.color;
                 mainLight.intensity = 1000f;
                 mainLight.color= new Color(0.5f,0.75f,1f,1f);
            }

    }

    void OnDisable()
    {
        //Reverting the forced values
        if(mainLight != null)
        {
            mainLight.intensity = previousIntensity;
            mainLight.color= previousColor;

        }
    }

    void Update()
    {
        if (update)
        {
            if (mainLight != null)
            {   
                //Sending the forward vector to the material           
                 Dir = mainLight.gameObject.transform.forward;
                 SkyMat.SetVector("_Moonlight_Forward_Direction", Dir);
            }

        }
    }


}
