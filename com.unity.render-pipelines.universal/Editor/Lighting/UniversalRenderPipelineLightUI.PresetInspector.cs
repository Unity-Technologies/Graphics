using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    using CED = CoreEditorDrawer<UniversalRenderPipelineSerializedLight>;

    partial class UniversalRenderPipelineLightUI
    {
        static readonly ExpandedState<Expandable, Light> k_ExpandedStatePreset = new(0, "URP-preset");

        public static readonly CED.IDrawer PresetInspector = CED.Group(
            CED.Group((serialized, owner) =>
                EditorGUILayout.HelpBox(LightUI.Styles.unsupportedPresetPropertiesMessage, MessageType.Info)),
            CED.Group((serialized, owner) => EditorGUILayout.Space()),
            CED.FoldoutGroup(LightUI.Styles.generalHeader,
                Expandable.General,
                k_ExpandedStatePreset,
                DrawGeneralContentPreset),
            CED.FoldoutGroup(LightUI.Styles.emissionHeader,
                Expandable.Emission,
                k_ExpandedStatePreset,
                CED.Group(
                    LightUI.DrawColor,
                    DrawEmissionContent)),
            CED.FoldoutGroup(Styles.lightCookieHeader,
                Expandable.LightCookie,
                k_ExpandedState,
                DrawLightCookieContent)
        );
    }
}
