#if VFX_HAS_TIMELINE
using System;
using System.Linq;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    [Overlay(typeof(SceneView), k_OverlayId, k_DisplayName)]
    class VisualEffectControlSceneOverlay : IMGUIOverlay, ITransientOverlay
    {
        const string k_OverlayId = "Scene View/Visual Effect Timeline Control";
        const string k_DisplayName = "Visual Effect Timeline Control";

        public bool visible => VisualEffectControlTrackMixerBehaviour.GetScrubbingWarnings().Any();

        public override void OnGUI()
        {
            EditorGUILayout.HelpBox(L10n.Tr("Maximum scrubbing time has been reached.\nThe timeline control is providing an approximate result."), MessageType.Warning);
            foreach (var warning in VisualEffectControlTrackMixerBehaviour.GetScrubbingWarnings())
            {
                EditorGUILayout.HelpBox(string.Format("Scrubbing Time: {0:N}s (thus, using steps of {1:00}ms)", warning.requestedTime, warning.fixedTimeStep * 1000.0f), MessageType.Info);
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField("Targeted VFX:", warning.target, typeof(VisualEffect), true);
                EditorGUI.EndDisabledGroup();
            }
        }
    }
}
#endif
