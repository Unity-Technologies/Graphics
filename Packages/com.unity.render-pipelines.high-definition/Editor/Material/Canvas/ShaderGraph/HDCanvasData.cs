using System;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    [Serializable]
    class HDCanvasData : HDTargetData
    {
        [SerializeField]
        public bool supportsMotionVectors;
    }
}
