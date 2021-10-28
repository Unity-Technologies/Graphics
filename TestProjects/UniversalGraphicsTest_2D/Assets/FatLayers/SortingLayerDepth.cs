using System;
using System.Collections.Generic;
using UnityEngine;

namespace FatLayers
{
    [CreateAssetMenu(fileName = "sortinglayerdepth.asset", menuName = "2D/Sorting Layer Depth", order = 0)]
    public class SortingLayerDepth : ScriptableObject
    {
        [Serializable]
        public class Depths
        {
            public string name;
            public float min;
            public float size;
        }

        public List<Depths> sortingLayerDepths;

        public (float, float) GetLayerDepths(string name)
        {
            foreach (var depth in sortingLayerDepths)
            {
                if (name == depth.name)
                    return (depth.min, depth.size);
            }

            return (0, 0);
        }
    }
}
