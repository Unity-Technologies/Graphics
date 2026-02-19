using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    [CustomPropertyDrawer(typeof(Shadow2DProviderSources))]
    internal class Shadow2DProviderSources_PropertyDrawer : Provider2DSources_PropertyDrawer<ShadowShape2DProvider, Shadow2DProviderSource>
    {
        public override int GetProviderType() { return (int)ShadowCaster2D.ShadowCastingSources.ShapeProvider; }
    }
}
