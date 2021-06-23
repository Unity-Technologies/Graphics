using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Internal
{
    [Serializable]
    internal sealed class SerializableStochasticTexture
    {
        [SerializeField]
        public ProceduralTexture2D proceduralTexture;
    }
}
