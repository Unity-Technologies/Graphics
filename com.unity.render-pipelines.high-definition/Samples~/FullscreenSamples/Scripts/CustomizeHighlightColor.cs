using System.Collections;
using System.Collections.Generic;
using UnityEngine;



[RequireComponent(typeof(Renderer))]

public class CustomizeHighlightColor : MonoBehaviour
{
    public Color selectionColor = Color.white;

    void Start()
    {
        SetColor();
    }

    void OnValidate()
    {
        SetColor();
    }

    void SetColor()
    {
        var rndr = GetComponent<Renderer>();

        var propertyBlock = new MaterialPropertyBlock();
        rndr.GetPropertyBlock(propertyBlock);

        propertyBlock.SetColor("_SelectionColor", selectionColor);

        rndr.SetPropertyBlock(propertyBlock);
    }


}
