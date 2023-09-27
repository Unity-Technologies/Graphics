using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[ExecuteInEditMode]
public class SamplesLinkLightToEmissive : MonoBehaviour
{

    public GameObject emissiveObject;
    public Light lightToLink;
    public string emissionColorProperty = "_Emission_Color";
    public string emissionIntensityProperty ="_Intensity";

    void Update()
    {

        if (lightToLink != null && emissiveObject !=null )
        {
                var renderer = emissiveObject.GetComponent<MeshRenderer>();
                var propertyBlock = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(emissionColorProperty, lightToLink.color * Mathf.CorrelatedColorTemperatureToRGB(lightToLink.colorTemperature));
                propertyBlock.SetFloat(emissionIntensityProperty,lightToLink.intensity);
                renderer.SetPropertyBlock(propertyBlock);
                
        } 
    }
}
