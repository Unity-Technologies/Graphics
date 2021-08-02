using System;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    using CED = CoreEditorDrawer<SerializedHDLight>;

    static partial class HDLightUI
    {
        static readonly ExpandedState<Expandable, Light> k_ExpandedStatePreset = new(0, "HDRP-preset");

        public static readonly CED.IDrawer PresetInspector;
    }
}
