using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    [CustomPropertyDrawer(typeof(Light2DProviderSources))]
    internal class Light2DProviderSources_PropertyDrawer : Provider2DSources_PropertyDrawer<Light2DProvider, Light2DProviderSource>
    {
        public override int GetProviderType() { return (int)Light2D.LightType.Provider; }
    }
}
