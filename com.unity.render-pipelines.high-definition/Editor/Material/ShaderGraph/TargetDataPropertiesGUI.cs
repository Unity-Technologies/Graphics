using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    abstract class TargetDataPropertiesGUI<T> where T : HDTargetData
    {
        protected T targetData;

        public TargetDataPropertiesGUI(T targetData)
        {
            this.targetData = targetData;
        }

    }
}