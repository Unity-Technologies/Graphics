using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Experimental.VFX.Utility
{
    class PointCacheAsset : ScriptableObject
    {
        public int PointCount;
        public Texture2D[] surfaces;
    }
}
