using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.U2D;

namespace UnityEngine.Rendering.Universal
{
    [CreateAssetMenu(fileName = "sortinglayerdepth.asset", menuName = "2D/Sorting Layer Depth", order = 0)]
    internal class SortingLayerDepth : SortingLayerDepthOverride
    {
        [Serializable]
        internal class Depths
        {
            public string name;
            public float min;
            public float size;
        }

        public List<Depths> sortingLayerDepths;

        private (float, float) GetLayerDepths(string name)
        {
            foreach (var depth in sortingLayerDepths)
            {
                if (name == depth.name)
                    return (depth.min, depth.size);
            }

            return (0, 0);
        }

        private static void MoveGO(Transform tr, float dest, bool dontMoveChild)
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

        private interface Sortable
        {
            int sortingLayerID { get; }
            int sortingOrder { get; }
            bool activeSelf { get; }
            Transform transform { get; }
        }

        private class RendererSortable : Sortable
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

        private class SortingGroupSortable : Sortable
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

        private bool NotInSortingGroup(Renderer sr)
        {
            if (sr.GetComponent<SortingGroup>() != null)
                return false;
            if (sr.GetComponentInParent<SortingGroup>() != null)
                return false;
            return true;
        }

        private static bool IsRootGroup(SortingGroup sg)
        {
            if (sg.transform.parent != null)
                return sg.transform.parent.GetComponentInParent<SortingGroup>() == null;
            return true;
        }

        public override void Sort()
        {
            if (sortingLayerDepths == null)
                return;

            // sort out all the Z for all sprite renderers
            var renderers = new List<Sortable>();
            renderers.AddRange(FindObjectsOfType<SortingGroup>().Where(x => IsRootGroup(x)).Select(x => new SortingGroupSortable(x)));
            renderers.AddRange(FindObjectsOfType<SpriteRenderer>().Where(x => NotInSortingGroup(x)).Select(x => new RendererSortable(x)));
            renderers.AddRange(FindObjectsOfType<SpriteShapeRenderer>().Where(x => NotInSortingGroup(x)).Select(x => new RendererSortable(x)));
            renderers.AddRange(FindObjectsOfType<TilemapRenderer>().Where(x => NotInSortingGroup(x)).Select(x => new RendererSortable(x)));

            foreach (var layer in SortingLayer.layers)
            {
                var srInLayer = renderers.Where(x => x.activeSelf && x.sortingLayerID == layer.id).OrderByDescending(x => x.sortingOrder).ToArray();
                var (depth, size) = GetLayerDepths(layer.name);
                var separation = size / srInLayer.Length;
                foreach (var sr in srInLayer)
                {
                    MoveGO(sr.transform, depth, sr is SortingGroupSortable);
                    depth += separation;
                }
            }
        }
    }
}
