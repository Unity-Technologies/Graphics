using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FatLayers;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;
using UnityEngine.U2D;

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
     * Solution
     * - On seeing a sorting group, find the root. Only add that, ignore the whole branch.
     * - Count up all the children from the branch and add them to the count of the root's layer.
     * - sort the children and apply the separation distance of the root layer
     * -
     *
     * How to account for 2D Characters
     * - it has bones in it, which transforms the vertices to where the bone is and not where the Sprite is
     * - the bones only have Transform (by design)
     * - they always have a SortingGroup
     * - SortingGroup may not be the root GO
     * - if you put depth into the bones the 2d character will be stretched very weirdly
     *
     * Solution - Full Manual
     * - a component to mark an object to have
     * - script to add the component to some GO based on some rules?
     */

    void MoveGO(Transform tr, float dest, bool dontMoveChild)
    {
        var newPos = tr.position;
        var counterMove = dest - newPos.z;
        newPos.z = dest;
        tr.position = newPos;

        if (dontMoveChild)
            return;

        // counter move all children
        for (var i = 0; i < tr.childCount; i++)
        {
            var child = tr.GetChild(i);
            newPos = child.position;
            newPos.z -= counterMove;
            child.position = newPos;
        }
    }

    interface Sortable
    {
        int sortingLayerID { get; }
        int sortingOrder { get; }
        bool activeSelf { get; }
        Transform transform { get; }
    }

    class RendererSortable : Sortable
    {
        private readonly Renderer renderer;
        public RendererSortable(Renderer sr)
        {
            renderer = sr;
        }

        public int sortingLayerID => renderer.sortingLayerID;
        public int sortingOrder => renderer.sortingOrder;
        public bool activeSelf => renderer.gameObject.activeSelf;
        public Transform transform => renderer.transform;
    }

    class SortingGroupSortable : Sortable
    {
        private SortingGroup group;

        public SortingGroupSortable(SortingGroup group)
        {
            this.group = group;
        }

        public int sortingLayerID => group.sortingLayerID;
        public int sortingOrder => group.sortingOrder;
        public bool activeSelf => group.gameObject.activeSelf;
        public Transform transform => group.transform;
    }

    // class SpriteSkinSortable : Sortable
    // {
    //     private Light2D light;
    //
    //     public Light2DSortable(Light2D light)
    //     {
    //         this.light = light;
    //     }
    //
    //     public int sortingLayerID => light.GetTopMostLitLayer()
    //     public int sortingOrder { get; }
    //     public bool activeSelf { get; }
    //     public Transform transform { get; }
    // }

    bool NotInSortingGroup(Renderer sr)
    {
        if (sr.GetComponent<SortingGroup>() != null)
            return false;
        if (sr.GetComponentInParent<SortingGroup>() != null)
            return false;
        return true;
    }

    bool IsRootGroup(SortingGroup sg)
    {
        if(sg.transform.parent != null)
            return sg.transform.parent.GetComponentInParent<SortingGroup>() == null;
        return true;
    }

    void Update()
    {
        if (layerDepthsSettings == null)
            return;

        // sort out all the Z for all sprite renderers
        var renderers = new List<Sortable>();
        renderers.AddRange(FindObjectsOfType<SortingGroup>().Where(x => IsRootGroup(x)).Select( x => new SortingGroupSortable(x)));
        renderers.AddRange(FindObjectsOfType<SpriteRenderer>().Where(x => NotInSortingGroup(x)).Select( x => new RendererSortable(x)));
        renderers.AddRange(FindObjectsOfType<SpriteShapeRenderer>().Where(x => NotInSortingGroup(x)).Select( x => new RendererSortable(x)));
        renderers.AddRange(FindObjectsOfType<TilemapRenderer>().Where(x => NotInSortingGroup(x)).Select( x => new RendererSortable(x)));

        foreach (var layer in SortingLayer.layers)
        {
            var srInLayer = renderers.Where(x => x.activeSelf && x.sortingLayerID == layer.id).OrderByDescending(x => x.sortingOrder).ToArray();
            var (depth, size) = layerDepthsSettings.GetLayerDepths(layer.name);
            var separation = size / srInLayer.Length;
            foreach(var sr in srInLayer)
            {
                MoveGO(sr.transform, depth, sr is SortingGroupSortable);
                depth += separation;
            }
        }

    }
}

