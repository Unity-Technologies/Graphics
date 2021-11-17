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

        public bool visible
        {
            get
            {
                foreach (var activeControl in VisualEffectControlTrackMixerBehaviour.GeActiveControlTracks())
                {
                    if (activeControl.GetCurrentScrubbingWarning().IsValid())
                        return true;
                }
                return false;
            }
        }

        public override void OnGUI()
        {
            var maxScrubbingTime = VisualEffectControlTrackMixerBehaviour.GeActiveControlTracks().Any(o => o.GetCurrentScrubbingWarning().IsValid());
            if (maxScrubbingTime)
            {
                EditorGUILayout.HelpBox(L10n.Tr("Maximum scrubbing time has been reached.\nThe timeline control is providing an approximate result."), MessageType.Warning);
                foreach (var activeControl in VisualEffectControlTrackMixerBehaviour.GeActiveControlTracks())
                {
                    var scrubbingWarning = activeControl.GetCurrentScrubbingWarning();
                    if (scrubbingWarning.IsValid())
                    {
                        EditorGUILayout.HelpBox(string.Format("Scrubbing Time: {0:N}s (thus, using steps of {1:00}ms)", scrubbingWarning.requestedTime, scrubbingWarning.fixedTimeStep * 1000.0f), MessageType.Info);
                        EditorGUI.BeginDisabledGroup(true);
                        EditorGUILayout.ObjectField("Targeted VFX:", scrubbingWarning.target, typeof(VisualEffect), true);
                        EditorGUI.EndDisabledGroup();
                    }
                }
            }
        }
    }
}
#endif
