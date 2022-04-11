using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEditor.Rendering.Universal.Path2D
{
    [Serializable]
    internal class IndexedSelection : SerializableSelection<int>
    {
        protected override int GetInvalidElement() { return -1; }
    }
}
