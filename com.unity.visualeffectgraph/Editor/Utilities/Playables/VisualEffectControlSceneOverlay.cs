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
                return VisualEffectControlErrorHelper.instance.AnyError();
            }
        }

        public override void OnGUI()
        {
            var instance = VisualEffectControlErrorHelper.instance;

            var conflict = instance.GetConflictingControlTrack().Any();
            if (conflict)
            {
                EditorGUILayout.HelpBox(L10n.Tr("Several time tracks are controlling the same effect.\nIt will lead to undefined behavior."), MessageType.Warning);
                foreach (var group in instance.GetConflictingControlTrack())
                {
                    EditorGUI.BeginDisabledGroup(true);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(L10n.Tr("Targeted VFX: "));
                    EditorGUILayout.ObjectField(group.First().GetTarget(), typeof(VisualEffect), true);
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(L10n.Tr("Director: "));
                    foreach (var director in group.Select(o => o.GetDirector()).Distinct())
                    {
                        EditorGUILayout.ObjectField(director, typeof(UnityEngine.Playables.PlayableDirector), true);
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUI.EndDisabledGroup();
                }
            }

            var maxScrubbingTime = instance.GetMaxScrubbingWarnings().Any();
            if (maxScrubbingTime)
            {
                EditorGUILayout.HelpBox(L10n.Tr("Maximum scrubbing time has been reached.\nThe timeline control is providing an approximate result."), MessageType.Warning);
                foreach (var scrubbingWarning in instance.GetMaxScrubbingWarnings())
                {
                    EditorGUILayout.HelpBox(string.Format("Scrubbing Time: {0:N}s (thus, using steps of {1:00}ms)", scrubbingWarning.requestedTime, scrubbingWarning.fixedTimeStep * 1000.0f), MessageType.Info);
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.ObjectField(L10n.Tr("Targeted VFX:"), scrubbingWarning.controller.GetTarget(), typeof(VisualEffect), true);
                    EditorGUILayout.ObjectField(L10n.Tr("Director:"), scrubbingWarning.controller.GetDirector(), typeof(UnityEngine.Playables.PlayableDirector), true);
                    EditorGUI.EndDisabledGroup();
                }
            }
        }
    }
}
#endif
