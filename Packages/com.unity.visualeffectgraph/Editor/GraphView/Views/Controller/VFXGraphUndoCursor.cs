using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEditor.VFX;

using Object = UnityEngine.Object;

namespace UnityEditor.VFX.UI
{
    class VFXGraphUndoCursor : ScriptableObject
    {
        [SerializeField]
        public int index;
    }
}
