using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;


[ExecuteInEditMode]
public class HighlightEffect : MonoBehaviour
{

    public GameObject[] simpleHighlight;
    public GameObject[] ghostHighlight;
    public GameObject[] blinkingHighlight;
  
    //The layer used must be added in the Layer Masks filters inside the DrawRenderer Custom Pass.
    //We use UI for the samples example as it is a built-in layer that is already created in all Unity projects.
    //It is a possible to use a custom layer for this as long as it is added to the custom pass filters. 
    private string selectionLayerName = "UI" ;

    private bool valid = false;
 
    void OnValidate() 
        {
            valid = true;
        }

   
    void Update()
    {
        if (valid)
            {

            foreach (GameObject thing in simpleHighlight)
            {
                if (thing.GetComponent<MeshRenderer>() != null)
                {
                    //set the object to the layer to be taken into the Draw Renderer Custom Pass Filter.
                    thing.layer = LayerMask.NameToLayer(selectionLayerName);
                    SetColorMask (thing, Color.red);
                }
            }

            foreach (GameObject thing in ghostHighlight)
            {
                if (thing.GetComponent<MeshRenderer>() != null)
                {
                    thing.layer = LayerMask.NameToLayer(selectionLayerName);
                    SetColorMask (thing, Color.green);

                }
            }

            foreach (GameObject thing in  blinkingHighlight)
                {
                    if( thing != null && thing.GetComponent<MeshRenderer>() != null )
                        {
                            thing.layer = LayerMask.NameToLayer(selectionLayerName);
                            SetColorMask (thing, Color.blue);
                        }
                }


        }

        valid = false;      
    }

    void SetColorMask(GameObject thing, Color colorMask)
    {
        //We add a property block that will set the variable "_maskColor" of the Draw Renderer Material.
        //The Draw Renderer custom Pass will render it to the custom Color buffer.
        // We will then be able to use the custom Color Buffer inside the Fullscreen Custom Pass as a Mask for the different effects.
        thing.layer = LayerMask.NameToLayer(selectionLayerName);
        var thingRenderer = thing.GetComponent<MeshRenderer>();
        var propertyBlock = new MaterialPropertyBlock();
        thingRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor("_maskColor", colorMask);
        thingRenderer.SetPropertyBlock(propertyBlock);
    }


}




