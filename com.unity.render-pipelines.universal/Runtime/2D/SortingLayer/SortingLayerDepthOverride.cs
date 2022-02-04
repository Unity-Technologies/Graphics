using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;
using UnityEngine.U2D;

namespace UnityEngine.Rendering.Universal
{
    [ExecuteAlways]
    public abstract class SortingLayerDepthOverride : ScriptableObject
    {

        public abstract void Sort();
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


    }
}
