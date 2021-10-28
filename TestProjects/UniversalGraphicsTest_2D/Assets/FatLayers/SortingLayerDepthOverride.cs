using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FatLayers;
using UnityEngine;

[ExecuteAlways]
public class SortingLayerDepthOverride : MonoBehaviour
{
    public SortingLayerDepth layerDepthsSettings;

    // Update is called once per frame
    /**
     * how to account for hierarchy?
     * 1. pre-calculate the layer's seperation
     * 2. go through the objects, move them, and counter move the child.
     *
     * How to account for SortingGroup
     * 1. Ultimately belong to the root group's layer
     * 2. Need to be sorted among the other objects in the layer, using SortingGroup's technique
     */

    void MoveGO(Transform tr, float dest)
    {
        var newPos = tr.position;
        var counterMove = dest - newPos.z;
        newPos.z = dest;
        tr.position = newPos;

        // counter move all children
        for (var i = 0; i < tr.childCount; i++)
        {
            var child = tr.GetChild(i);
            newPos = child.position;
            newPos.z += counterMove;
            child.position = newPos;
        }
    }

    void Update()
    {
        if (layerDepthsSettings == null)
            return;

        // sort out all the Z for all sprite renderers
        var spriteRenderers = FindObjectsOfType<SpriteRenderer>();
        foreach (var layer in SortingLayer.layers)
        {
            var srInLayer = spriteRenderers.Where(x => x.sortingLayerID == layer.id).OrderByDescending(x => x.sortingOrder).ToArray();
            var (depth, size) = layerDepthsSettings.GetLayerDepths(layer.name);
            var separation = size / srInLayer.Length;
            foreach(var sr in srInLayer)
            {
                MoveGO(sr.transform, depth);
                depth += separation;
            }
        }

    }
}

