using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using System.Linq;

namespace UnityEditor.Rendering.Utilities
{
    public class MaterialPropertySupportedRenderPipelines : IConditionalHideInInspector
    {
        public bool IsVisible(MaterialProperty property, string[] parameters)
        {
            if (GraphicsSettings.currentRenderPipeline != null)
                return parameters.Contains(GraphicsSettings.currentRenderPipeline.GetType().Name);
            else
                return false;
        }
    }
}