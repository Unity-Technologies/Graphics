using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ObjectIDScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        var RendererList = Resources.FindObjectsOfTypeAll(typeof(Renderer));

        System.Random rand = new System.Random(3);
        float stratumSize = 1.0f / RendererList.Length;

        int index = 0;
        foreach (Renderer renderer in RendererList)
        {
            MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
            float hue = (float)index / RendererList.Length;
            propertyBlock.SetColor("ObjectColor",  Color.HSVToRGB(hue, 0.7f, 1.0f));
            renderer.SetPropertyBlock(propertyBlock);
            index++;
        }
    }
}
