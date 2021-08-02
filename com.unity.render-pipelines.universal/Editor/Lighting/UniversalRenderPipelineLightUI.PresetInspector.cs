using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    using CED = CoreEditorDrawer<UniversalRenderPipelineSerializedLight>;

    internal partial class UniversalRenderPipelineLightUI
    {
        static readonly ExpandedState<Expandable, Light> k_ExpandedStatePreset = new(0, "URP-preset");

        public static readonly CED.IDrawer PresetInspector = CED.Group(
            CED.Group((serialized, owner) =>
                EditorGUILayout.HelpBox(Styles.unsupportedPresetPropertiesMessage, MessageType.Info)),
            CED.Group((serialized, owner) => EditorGUILayout.Space()),
            CED.FoldoutGroup(Styles.generalHeader,
                Expandable.General,
                k_ExpandedStatePreset,
                UniversalRenderPipelineLightUI.DrawGeneralContentPreset),
            CED.FoldoutGroup(Styles.emissionHeader,
                Expandable.Emission,
                k_ExpandedStatePreset,
                CED.Group(UniversalRenderPipelineLightUI.DrawerColor,
                    UniversalRenderPipelineLightUI.DrawEmissionContent)),
            CED.FoldoutGroup(Styles.lightCookieHeader,
                Expandable.LightCookie,
                k_ExpandedState,
                DrawLightCookieContent)
        );
    }
}
