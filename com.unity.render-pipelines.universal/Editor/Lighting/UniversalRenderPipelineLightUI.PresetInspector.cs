using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    using CED = CoreEditorDrawer<UniversalRenderPipelineSerializedLight>;
    using Styles = UniversalRenderPipelineLightUI.Styles;

    static class UniversalRenderPipelineLightUIPreset
    {
        [URPHelpURL("light-component")]
        enum Expandable
        {
            General = 1 << 0,
            Emission = 1 << 1
        }

        static readonly ExpandedState<Expandable, Light> k_ExpandedState = new ExpandedState<Expandable, Light>(~(-1), "URP-preset");

        public static readonly CED.IDrawer Inspector = CED.Group(
            CED.FoldoutGroup(Styles.generalHeader,
                Expandable.General,
                k_ExpandedState,
                UniversalRenderPipelineLightUI.DrawGeneralContentPreset),
            CED.FoldoutGroup(Styles.emissionHeader,
                Expandable.Emission,
                k_ExpandedState,
                CED.Group(UniversalRenderPipelineLightUI.DrawerColor,
                    UniversalRenderPipelineLightUI.DrawEmissionContent))
        );
    }
}
