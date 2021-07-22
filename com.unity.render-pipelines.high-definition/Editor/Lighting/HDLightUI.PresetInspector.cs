using System;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    using CED = CoreEditorDrawer<SerializedHDLight>;

    static class HDLightUIPreset
    {
        public static readonly CED.IDrawer Inspector;


        [HDRPHelpURL("Light-Component")]
        enum Expandable
        {
            General = 1 << 0,
            Emission = 1 << 2,
        }
        readonly static ExpandedState<Expandable, Light> k_ExpandedState = new ExpandedState<Expandable, Light>(~(-1), "HDRP");

        static HDLightUI.Styles s_Styles = new HDLightUI.Styles();

        static HDLightUIPreset()
        {
            Inspector = CED.Group(
                CED.FoldoutGroup(s_Styles.shapeHeader, Expandable.General, k_ExpandedState, HDLightUI.DrawGeneralContent),
                CED.FoldoutGroup(s_Styles.emissionHeader, Expandable.Emission, k_ExpandedState, HDLightUI.DrawEmissionContentForPreset)

            );
        }
    }
}
