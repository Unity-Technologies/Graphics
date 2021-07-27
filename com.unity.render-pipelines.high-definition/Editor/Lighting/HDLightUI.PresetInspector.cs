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
        [HDRPHelpURL("Light-Component")]
        enum Expandable
        {
            General = 1 << 0,
            Emission = 1 << 2,
            Shadows = 1 << 3,
        }

        static readonly ExpandedState<Expandable, Light> k_ExpandedState = new ExpandedState<Expandable, Light>(~(-1), "HDRP-preset");

        static HDLightUI.Styles s_Styles = new HDLightUI.Styles();

        public static readonly CED.IDrawer Inspector = CED.Group(
            CED.FoldoutGroup(s_Styles.shapeHeader, Expandable.General, k_ExpandedState, HDLightUI.DrawGeneralContent),
            CED.FoldoutGroup(s_Styles.emissionHeader, Expandable.Emission, k_ExpandedState, HDLightUI.DrawEmissionContentForPreset),
            CED.FoldoutGroup(s_Styles.shadowHeader, Expandable.Shadows, k_ExpandedState, HDLightUI.DrawEnableShadowMapInternal),
            CED.Group((serialized, owner) =>
                EditorGUILayout.HelpBox(s_Styles.unsupportedFieldsPresetInfoBox, MessageType.Info))
        );
    }
}
